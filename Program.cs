using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace SecurityCheckApp;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}

internal sealed class MainForm : Form
{
    private readonly TextBox _internetTextBox = CreateReadOnlyTextBox();
    private readonly TextBox _installedTextBox = CreateReadOnlyTextBox();
    private readonly TextBox _firewallTextBox = CreateReadOnlyTextBox();
    private readonly TextBox _antivirusTextBox = CreateReadOnlyTextBox();
    private readonly TextBox _summaryTextBox = CreateReadOnlyTextBox(multiline: true);

    public MainForm()
    {
        Text = "Проверка антивируса и межсетевого экрана";
        Width = 940;
        Height = 700;
        StartPosition = FormStartPosition.CenterScreen;

        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 7,
            Padding = new Padding(10),
            AutoSize = true
        };

        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 320));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        AddRow(panel, 0, "1) Проверка подключения к интернету", OnCheckInternet, _internetTextBox);
        AddRow(panel, 1, "2) Проверка наличия МЭ и АВ", OnCheckInstalled, _installedTextBox);
        AddRow(panel, 2, "3) Проверка работоспособности МЭ", OnCheckFirewall, _firewallTextBox);
        AddRow(panel, 3, "4) Проверка работоспособности АВ", OnCheckAntivirus, _antivirusTextBox);

        var printButton = new Button { Text = "Вывод результатов", Dock = DockStyle.Fill, Height = 34 };
        printButton.Click += OnPrintSummary;
        panel.Controls.Add(printButton, 0, 4);

        panel.SetColumnSpan(_summaryTextBox, 2);
        _summaryTextBox.Height = 220;
        _summaryTextBox.ScrollBars = ScrollBars.Vertical;
        panel.Controls.Add(_summaryTextBox, 1, 4);

        var clearButton = new Button { Text = "Очистить", Dock = DockStyle.Fill, Height = 34 };
        clearButton.Click += (_, _) => ClearAllFields();
        panel.Controls.Add(clearButton, 0, 5);

        var closeButton = new Button { Text = "Выход", Dock = DockStyle.Fill, Height = 34 };
        closeButton.Click += (_, _) => Close();
        panel.Controls.Add(closeButton, 1, 5);

        Controls.Add(panel);
    }

    private static TextBox CreateReadOnlyTextBox(bool multiline = false) => new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        Multiline = multiline,
        Height = multiline ? 180 : 34
    };

    private static void AddRow(TableLayoutPanel panel, int row, string labelText, EventHandler onClick, TextBox output)
    {
        var label = new Label
        {
            Text = labelText,
            AutoSize = true,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };
        panel.Controls.Add(label, 0, row);

        var button = new Button { Text = "Проверить", Dock = DockStyle.Fill, Height = 34 };
        button.Click += onClick;
        panel.Controls.Add(button, 1, row);

        panel.Controls.Add(output, 2, row);
    }

    private void OnCheckInternet(object? sender, EventArgs e)
    {
        var result = SecurityChecks.CheckInternetConnection("ya.ru");
        _internetTextBox.Text = result.Message;
    }

    private void OnCheckInstalled(object? sender, EventArgs e)
    {
        var result = SecurityChecks.CheckInstalledProtectionSoftware();
        _installedTextBox.Text = $"МЭ: {AsYesNo(result.FirewallDetected)}; АВ: {AsYesNo(result.AntivirusDetected)}";
    }

    private void OnCheckFirewall(object? sender, EventArgs e)
    {
        var result = SecurityChecks.CheckFirewallOperational();
        _firewallTextBox.Text = result.Message;
    }

    private void OnCheckAntivirus(object? sender, EventArgs e)
    {
        var installed = SecurityChecks.CheckInstalledProtectionSoftware();
        var result = SecurityChecks.CheckAntivirusOperational(installed.AntivirusProducts);
        _antivirusTextBox.Text = result.Message;
    }

    private void OnPrintSummary(object? sender, EventArgs e)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Результаты проведенного тестирования антивируса и межсетевого экрана");
        sb.AppendLine();
        sb.AppendLine($"1. Интернет: {EnsureText(_internetTextBox.Text, "Проверка не выполнялась")}");
        sb.AppendLine($"2. Наличие МЭ/АВ: {EnsureText(_installedTextBox.Text, "Проверка не выполнялась")}");
        sb.AppendLine($"3. Работоспособность МЭ: {EnsureText(_firewallTextBox.Text, "Проверка не выполнялась")}");
        sb.AppendLine($"4. Работоспособность АВ: {EnsureText(_antivirusTextBox.Text, "Проверка не выполнялась")}");
        _summaryTextBox.Text = sb.ToString();
    }

    private void ClearAllFields()
    {
        _internetTextBox.Clear();
        _installedTextBox.Clear();
        _firewallTextBox.Clear();
        _antivirusTextBox.Clear();
        _summaryTextBox.Clear();
    }

    private static string EnsureText(string text, string fallback) => string.IsNullOrWhiteSpace(text) ? fallback : text;

    private static string AsYesNo(bool value) => value ? "Да" : "Нет";
}

internal static class SecurityChecks
{
    internal static CheckResult CheckInternetConnection(string host)
    {
        try
        {
            using var ping = new Ping();
            var reply = ping.Send(host, 1500);
            return reply?.Status == IPStatus.Success
                ? new CheckResult(true, $"Подключение есть (Ping {host}: {reply.RoundtripTime} мс).")
                : new CheckResult(false, $"Подключение отсутствует или нестабильно (статус: {reply?.Status}).");
        }
        catch (Exception ex)
        {
            return new CheckResult(false, $"Ошибка проверки сети: {ex.Message}");
        }
    }

    internal static InstalledSoftwareCheckResult CheckInstalledProtectionSoftware()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return new InstalledSoftwareCheckResult(false, "Только Windows", false, "Только Windows", Array.Empty<string>());
        }

        var firewallService = ExecuteCommand("sc", "query MpsSvc");
        var firewallDetected = firewallService.Output.Contains("SERVICE_NAME: MpsSvc", StringComparison.OrdinalIgnoreCase);

        var antivirusProducts = QueryWindowsAntivirusProducts();
        var antivirusDetected = antivirusProducts.Count > 0;

        return new InstalledSoftwareCheckResult(
            firewallDetected,
            firewallDetected ? "Служба Windows Firewall обнаружена." : "Служба Windows Firewall не обнаружена.",
            antivirusDetected,
            antivirusDetected ? $"Найдено: {string.Join(", ", antivirusProducts)}" : "Антивирус не найден.",
            antivirusProducts);
    }

    internal static CheckResult CheckFirewallOperational()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return new CheckResult(false, "Проверка доступна только в Windows.");
        }

        var service = ExecuteCommand("sc", "query MpsSvc");
        var serviceRunning = service.Output.Contains("RUNNING", StringComparison.OrdinalIgnoreCase);
        var profiles = ExecuteCommand("netsh", "advfirewall show allprofiles");
        var enabledMentions = profiles.Output.Split('\n')
            .Count(line => line.Contains("State", StringComparison.OrdinalIgnoreCase)
                        && line.Contains("ON", StringComparison.OrdinalIgnoreCase));

        return serviceRunning && enabledMentions > 0
            ? new CheckResult(true, $"МЭ работает (служба активна, профилей ON: {enabledMentions}).")
            : new CheckResult(false, "МЭ отключен или настроен неверно.");
    }

    internal static CheckResult CheckAntivirusOperational(IReadOnlyCollection<string> antivirusProducts)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return new CheckResult(false, "Проверка доступна только в Windows.");
        }

        if (antivirusProducts.Count == 0)
        {
            return new CheckResult(false, "Антивирус не найден — проверка невозможна.");
        }

        var processList = ExecuteCommand("tasklist", string.Empty).Output;
        var knownProcessHints = new[] { "MsMpEng", "avp", "avg", "avast", "ekrn", "mcshield", "savservice", "bdagent" };
        var hasResidentModule = knownProcessHints.Any(h => processList.Contains(h, StringComparison.OrdinalIgnoreCase));

        return hasResidentModule
            ? new CheckResult(true, "Обнаружены признаки работы резидентного модуля антивируса.")
            : new CheckResult(false, "Резидентный модуль антивируса не обнаружен в процессах.");
    }

    private static List<string> QueryWindowsAntivirusProducts()
    {
        var command = "Get-CimInstance -Namespace root/SecurityCenter2 -ClassName AntivirusProduct | Select-Object -ExpandProperty displayName";
        var result = ExecuteCommand("powershell", $"-NoProfile -Command \"{command}\"");

        return result.Output
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(v => v.Trim())
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static (string Output, string Error) ExecuteCommand(string fileName, string args)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return (string.Empty, "Не удалось запустить процесс.");
            }

            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit(5000);

            return (output, error);
        }
        catch (Exception ex)
        {
            return (string.Empty, ex.Message);
        }
    }
}

internal sealed record CheckResult(bool IsSuccess, string Message);

internal sealed record InstalledSoftwareCheckResult(
    bool FirewallDetected,
    string FirewallMessage,
    bool AntivirusDetected,
    string AntivirusMessage,
    IReadOnlyList<string> AntivirusProducts);
