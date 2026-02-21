using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace SecurityCheckApp;

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
