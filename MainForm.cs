using System.Text;
using System.Windows.Forms;

namespace SecurityCheckApp;

internal sealed class MainForm : Form
{
    private readonly TextBox _internetTextBox = CreateReadOnlyTextBox();
    private readonly TextBox _firewallInstalledTextBox = CreateReadOnlyTextBox();
    private readonly TextBox _firewallOperationalTextBox = CreateReadOnlyTextBox();

    private readonly TextBox _antivirusInstalledTextBox = CreateReadOnlyTextBox();
    private readonly TextBox _antivirusOperationalTextBox = CreateReadOnlyTextBox();
    private readonly TextBox _antivirusTestTextBox = CreateReadOnlyTextBox();

    private readonly TextBox _summaryTextBox = CreateReadOnlyTextBox(multiline: true);

    public MainForm()
    {
        Text = "Программа проверки информационной безопасности";
        Width = 620;
        Height = 520;
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;

        var root = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(10)
        };

        var firewallGroup = BuildFirewallGroup();
        firewallGroup.Top = 10;
        firewallGroup.Left = 10;

        var antivirusGroup = BuildAntivirusGroup();
        antivirusGroup.Top = firewallGroup.Bottom + 8;
        antivirusGroup.Left = 10;

        var resultsGroup = BuildResultsGroup();
        resultsGroup.Top = antivirusGroup.Bottom + 8;
        resultsGroup.Left = 10;

        root.Controls.Add(firewallGroup);
        root.Controls.Add(antivirusGroup);
        root.Controls.Add(resultsGroup);

        Controls.Add(root);
    }

    private GroupBox BuildFirewallGroup()
    {
        var group = new GroupBox
        {
            Text = "Проверка межсетевого экрана",
            Width = 580,
            Height = 135
        };

        AddRow(group, 20, "Проверка подключения к Интернету", OnCheckInternet, _internetTextBox);
        AddRow(group, 62, "Проверка наличия установленного\nмежсетевого экрана", OnCheckFirewallInstalled, _firewallInstalledTextBox);
        AddRow(group, 104, "Проверка работоспособности\nмежсетевого экрана", OnCheckFirewallOperational, _firewallOperationalTextBox);

        return group;
    }

    private GroupBox BuildAntivirusGroup()
    {
        var group = new GroupBox
        {
            Text = "Проверка антивирусного программного обеспечения",
            Width = 580,
            Height = 165
        };

        AddRow(group, 20, "Проверка наличия установленного\nантивируса", OnCheckAntivirusInstalled, _antivirusInstalledTextBox);
        AddRow(group, 62, "Проверка работоспособности\nантивирусного ПО", OnCheckAntivirusOperational, _antivirusOperationalTextBox);
        AddRow(group, 104, "Тестирование антивирусного ПО", OnTestAntivirus, _antivirusTestTextBox);

        return group;
    }

    private GroupBox BuildResultsGroup()
    {
        var group = new GroupBox
        {
            Text = "Результаты проверок и рекомендации",
            Width = 580,
            Height = 160
        };

        _summaryTextBox.Multiline = true;
        _summaryTextBox.ScrollBars = ScrollBars.Vertical;
        _summaryTextBox.Left = 10;
        _summaryTextBox.Top = 22;
        _summaryTextBox.Width = 425;
        _summaryTextBox.Height = 125;

        var btnPrint = new Button
        {
            Text = "Вывести\nрезультаты",
            Left = 445,
            Top = 22,
            Width = 120,
            Height = 38
        };
        btnPrint.Click += OnPrintSummary;

        var btnSave = new Button
        {
            Text = "Сохранить\nрезультаты в\nфайл",
            Left = 445,
            Top = 68,
            Width = 120,
            Height = 48
        };
        btnSave.Click += OnSaveSummaryToFile;

        var btnExit = new Button
        {
            Text = "Выход",
            Left = 445,
            Top = 121,
            Width = 120,
            Height = 26
        };
        btnExit.Click += (_, _) => Close();

        group.Controls.Add(_summaryTextBox);
        group.Controls.Add(btnPrint);
        group.Controls.Add(btnSave);
        group.Controls.Add(btnExit);

        return group;
    }

    private static void AddRow(Control parent, int top, string title, EventHandler handler, TextBox output)
    {
        var button = new Button
        {
            Text = title,
            Left = 10,
            Top = top,
            Width = 205,
            Height = 36
        };
        button.Click += handler;

        output.Left = 223;
        output.Top = top;
        output.Width = 345;
        output.Height = 36;

        parent.Controls.Add(button);
        parent.Controls.Add(output);
    }

    private static TextBox CreateReadOnlyTextBox(bool multiline = false) => new()
    {
        ReadOnly = true,
        Multiline = multiline
    };

    private void OnCheckInternet(object? sender, EventArgs e)
    {
        var result = SecurityChecks.CheckInternetConnection("ya.ru");
        _internetTextBox.Text = result.Message;
    }

    private void OnCheckFirewallInstalled(object? sender, EventArgs e)
    {
        var result = SecurityChecks.CheckInstalledProtectionSoftware();
        _firewallInstalledTextBox.Text = result.FirewallMessage;
    }

    private void OnCheckFirewallOperational(object? sender, EventArgs e)
    {
        var result = SecurityChecks.CheckFirewallOperational();
        _firewallOperationalTextBox.Text = result.Message;
    }

    private void OnCheckAntivirusInstalled(object? sender, EventArgs e)
    {
        var result = SecurityChecks.CheckInstalledProtectionSoftware();
        _antivirusInstalledTextBox.Text = result.AntivirusMessage;
    }

    private void OnCheckAntivirusOperational(object? sender, EventArgs e)
    {
        var installed = SecurityChecks.CheckInstalledProtectionSoftware();
        var result = SecurityChecks.CheckAntivirusOperational(installed.AntivirusProducts);
        _antivirusOperationalTextBox.Text = result.Message;
    }

    private void OnTestAntivirus(object? sender, EventArgs e)
    {
        var installed = SecurityChecks.CheckInstalledProtectionSoftware();
        var operational = SecurityChecks.CheckAntivirusOperational(installed.AntivirusProducts);

        _antivirusTestTextBox.Text = operational.IsSuccess
            ? "Тест пройден: антивирус активен, признаков сбоя не обнаружено."
            : "Тест не пройден: требуется проверка настроек/состояния антивируса.";
    }

    private void OnPrintSummary(object? sender, EventArgs e)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Результаты проведенного тестирования:");
        sb.AppendLine();
        sb.AppendLine($"1. Интернет: {EnsureText(_internetTextBox.Text)}");
        sb.AppendLine($"2. Наличие межсетевого экрана: {EnsureText(_firewallInstalledTextBox.Text)}");
        sb.AppendLine($"3. Работоспособность межсетевого экрана: {EnsureText(_firewallOperationalTextBox.Text)}");
        sb.AppendLine($"4. Наличие антивируса: {EnsureText(_antivirusInstalledTextBox.Text)}");
        sb.AppendLine($"5. Работоспособность антивируса: {EnsureText(_antivirusOperationalTextBox.Text)}");
        sb.AppendLine($"6. Тестирование антивируса: {EnsureText(_antivirusTestTextBox.Text)}");
        sb.AppendLine();
        sb.AppendLine("Рекомендация: при любых отрицательных результатах обновите сигнатуры АВ и проверьте правила МЭ.");
        _summaryTextBox.Text = sb.ToString();
    }

    private void OnSaveSummaryToFile(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_summaryTextBox.Text))
        {
            OnPrintSummary(sender, e);
        }

        using var saveDialog = new SaveFileDialog
        {
            Title = "Сохранение результатов",
            Filter = "Текстовый файл (*.txt)|*.txt",
            FileName = $"security-check-results-{DateTime.Now:yyyyMMdd-HHmmss}.txt"
        };

        if (saveDialog.ShowDialog(this) == DialogResult.OK)
        {
            File.WriteAllText(saveDialog.FileName, _summaryTextBox.Text, Encoding.UTF8);
        }
    }

    private static string EnsureText(string text) =>
        string.IsNullOrWhiteSpace(text) ? "Проверка не выполнялась" : text;
}
