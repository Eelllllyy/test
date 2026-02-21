using System.Text;
using System.Windows.Forms;

namespace SecurityCheckApp;

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
