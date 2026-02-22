using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;

namespace SecurityCheckApp;

internal static class SecurityChecks
{
    // Простые пути для учебного примера (Windows).
    private static readonly string[] FirewallExePaths =
    {
        @"C:\Windows\System32\FirewallAPI.dll"
    };

    private static readonly string[] AntivirusExePaths =
    {
        @"C:\Program Files\Windows Defender\MSASCuiL.exe",
        @"C:\ProgramData\Microsoft\Windows Defender\Platform"
    };

    internal static CheckResult CheckInternetConnection(string host)
    {
        IPStatus status = IPStatus.Unknown;

        try
        {
            status = new Ping().Send(host, 1500)!.Status;
        }
        catch
        {
            // Для простоты в учебном коде оставляем пустой catch,
            // как в методичке.
        }

        if (status == IPStatus.Success)
        {
            return new CheckResult(true, "Данный компьютер подключен к интернету");
        }

        return new CheckResult(false, "Данный компьютер не подключен к интернету");
    }

    internal static InstalledSoftwareCheckResult CheckInstalledProtectionSoftware()
    {
        bool firewallInstalled = FirewallExePaths.Any(File.Exists);
        bool antivirusInstalled = AntivirusExePaths.Any(path => File.Exists(path) || Directory.Exists(path));

        string firewallText = firewallInstalled
            ? "Фаервол установлен!"
            : "Фаервол не установлен!";

        string antivirusText = antivirusInstalled
            ? "Антивирус установлен!"
            : "Антивирус не установлен!";

        var antivirusProducts = antivirusInstalled
            ? new List<string> { "Антивирус обнаружен" }
            : new List<string>();

        return new InstalledSoftwareCheckResult(
            firewallInstalled,
            firewallText,
            antivirusInstalled,
            antivirusText,
            antivirusProducts);
    }

    internal static CheckResult CheckFirewallOperational()
    {
        using var client = new WebClient();

        try
        {
            // Учебная идея из методички: пробуем внешний ресурс.
            _ = client.DownloadString("http://example.com");
        }
        catch
        {
            return new CheckResult(true, "Межсетевой экран функционирует правильно!");
        }

        return new CheckResult(false, "Межсетевой экран функционирует неверно, или не функционирует!");
    }

    internal static CheckResult CheckAntivirusOperational(IReadOnlyCollection<string> antivirusProducts)
    {
        if (antivirusProducts.Count == 0)
        {
            return new CheckResult(false, "Антивирус не найден — проверка невозможна.");
        }

        string taskList = RunCommand("tasklist");

        bool residentModuleRunning = taskList.Contains("MsMpEng", StringComparison.OrdinalIgnoreCase)
                                     || taskList.Contains("avp", StringComparison.OrdinalIgnoreCase)
                                     || taskList.Contains("avast", StringComparison.OrdinalIgnoreCase)
                                     || taskList.Contains("avg", StringComparison.OrdinalIgnoreCase);

        if (residentModuleRunning)
        {
            return new CheckResult(true, "Резидентный модуль антивируса работает.");
        }

        return new CheckResult(false, "Резидентный модуль антивируса не запущен.");
    }

    private static string RunCommand(string fileName)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                return string.Empty;
            }

            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(5000);
            return output;
        }
        catch
        {
            return string.Empty;
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
