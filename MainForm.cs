using System.Text;
using System.Windows.Forms;

namespace SecurityCheckApp;

internal sealed class MainForm : Form
{
    // Простые размеры, чтобы интерфейс не "ломался" и текст не обрезался.
    private const int SectionHeight = 340;
    private const int CheckRowHeight = 100;

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
        ClientSize = new Size(1320, 1100);
        MinimumSize = new Size(1260, 980);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            ColumnCount = 1,
            RowCount = 3
        };

        // Увеличили первые 2 секции, чтобы третьи кнопки были видны полностью.
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, SectionHeight));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, SectionHeight));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        root.Controls.Add(BuildFirewallGroup(), 0, 0);
        root.Controls.Add(BuildAntivirusGroup(), 0, 1);
        root.Controls.Add(BuildResultsGroup(), 0, 2);

        Controls.Add(root);
    }

    private GroupBox BuildFirewallGroup()
    {
        var group = CreateGroup("Проверка межсетевого экрана");
        var grid = CreateChecksGrid();

        AddCheckRow(grid, 0, "Проверка подключения\nк Интернету", OnCheckInternet, _internetTextBox);
        AddCheckRow(grid, 1, "Проверка наличия установленного\nмежсетевого экрана", OnCheckFirewallInstalled, _firewallInstalledTextBox);
        AddCheckRow(grid, 2, "Проверка работоспособности\nмежсетевого экрана", OnCheckFirewallOperational, _firewallOperationalTextBox);

        group.Controls.Add(grid);
        return group;
    }

    private GroupBox BuildAntivirusGroup()
    {
        var group = CreateGroup("Проверка антивирусного программного обеспечения");
        var grid = CreateChecksGrid();

        AddCheckRow(grid, 0, "Проверка наличия установленного\nантивируса", OnCheckAntivirusInstalled, _antivirusInstalledTextBox);
        AddCheckRow(grid, 1, "Проверка работоспособности\nантивирусного ПО", OnCheckAntivirusOperational, _antivirusOperationalTextBox);
        AddCheckRow(grid, 2, "Тестирование\nантивирусного ПО", OnTestAntivirus, _antivirusTestTextBox);

        group.Controls.Add(grid);
        return group;
    }

    private GroupBox BuildResultsGroup()
    {
        var group = CreateGroup("Результаты проверок и рекомендации");

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(10),
            ColumnCount = 2,
            RowCount = 1
        };

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 84));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16));

        _summaryTextBox.Multiline = true;
        _summaryTextBox.ScrollBars = ScrollBars.Vertical;
        _summaryTextBox.Dock = DockStyle.Fill;

        var rightButtons = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(10, 20, 10, 10)
        };

        rightButtons.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));
        rightButtons.RowStyles.Add(new RowStyle(SizeType.Absolute, 124));
        rightButtons.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));
        rightButtons.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var btnPrint = CreateActionButton("Вывести\nрезультаты", OnPrintSummary);
        var btnSave = CreateActionButton("Сохранить\nрезультаты в\nфайл", OnSaveSummaryToFile);
        var btnExit = CreateActionButton("Выход", (_, _) => Close());

        rightButtons.Controls.Add(btnPrint, 0, 0);
        rightButtons.Controls.Add(btnSave, 0, 1);
        rightButtons.Controls.Add(btnExit, 0, 2);

        layout.Controls.Add(_summaryTextBox, 0, 0);
        layout.Controls.Add(rightButtons, 1, 0);

        group.Controls.Add(layout);
        return group;
    }

    private static GroupBox CreateGroup(string title) => new()
    {
        Text = title,
        Dock = DockStyle.Fill,
        Padding = new Padding(10)
    };

    private static TableLayoutPanel CreateChecksGrid()
    {
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            Padding = new Padding(8)
        };

        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));

        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, CheckRowHeight));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, CheckRowHeight));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, CheckRowHeight));

        return grid;
    }

    private static void AddCheckRow(TableLayoutPanel grid, int row, string title, EventHandler handler, TextBox output)
    {
        var button = new Button
        {
            Text = title,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 12, 8),
            Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point),
            TextAlign = ContentAlignment.MiddleCenter,
            AutoEllipsis = false
        };
        button.Click += handler;

        output.Dock = DockStyle.Fill;
        output.Margin = new Padding(0, 0, 0, 8);
        output.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point);

        grid.Controls.Add(button, 0, row);
        grid.Controls.Add(output, 1, row);
    }

    private static Button CreateActionButton(string text, EventHandler onClick)
    {
        var button = new Button
        {
            Text = text,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 12),
            Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point),
            TextAlign = ContentAlignment.MiddleCenter,
            AutoEllipsis = false
        };
        button.Click += onClick;
        return button;
    }

    private static TextBox CreateReadOnlyTextBox(bool multiline = false) => new()
    {
        ReadOnly = true,
        Multiline = multiline,
        BorderStyle = BorderStyle.FixedSingle
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
