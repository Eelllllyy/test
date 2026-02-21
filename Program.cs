using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;

namespace SecurityCheckApp;

internal static class Program
{
    private static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;

        var summary = new StringBuilder();
        summary.AppendLine("Результаты проведенного тестирования антивируса и межсетевого экрана");
        summary.AppendLine(new string('-', 72));

        var internet = CheckInternetConnection("ya.ru");
        var installed = CheckInstalledProtectionSoftware();
        var firewallState = CheckFirewallOperational();
        var antivirusState = CheckAntivirusOperational(installed.AntivirusProducts);

        summary.AppendLine($"1) Подключение к Интернету: {ToStatus(internet.IsSuccess)}");
        summary.AppendLine($"   Детали: {internet.Message}");
        summary.AppendLine();

        summary.AppendLine($"2) Наличие межсетевого экрана: {ToStatus(installed.FirewallDetected)}");
        summary.AppendLine($"   Детали: {installed.FirewallMessage}");
        summary.AppendLine();

        summary.AppendLine($"3) Наличие антивируса: {ToStatus(installed.AntivirusDetected)}");
        summary.AppendLine($"   Детали: {installed.AntivirusMessage}");
        summary.AppendLine();

        summary.AppendLine($"4) Работоспособность межсетевого экрана: {ToStatus(firewallState.IsSuccess)}");
        summary.AppendLine($"   Детали: {firewallState.Message}");
        summary.AppendLine();

        summary.AppendLine($"5) Работоспособность антивируса: {ToStatus(antivirusState.IsSuccess)}");
        summary.AppendLine($"   Детали: {antivirusState.Message}");

        Console.WriteLine(summary);
    }

    private static CheckResult CheckInternetConnection(string host)
    {
        try
        {
            using var ping = new Ping();
            var reply = ping.Send(host, 1500);

            if (reply?.Status == IPStatus.Success)
            {
                return new CheckResult(true, $"Ping до {host} успешен, задержка {reply.RoundtripTime} мс.");
            }

            return new CheckResult(false, $"Ping до {host} завершился статусом: {reply?.Status}.");
        }
        catch (Exception ex)
        {
            return new CheckResult(false, $"Ошибка проверки сети: {ex.Message}");
        }
    }

    private static InstalledSoftwareCheckResult CheckInstalledProtectionSoftware()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return new InstalledSoftwareCheckResult(
                firewallDetected: false,
                firewallMessage: "Проверка наличия штатного МЭ ориентирована на Windows (MpsSvc).",
                antivirusDetected: false,
                antivirusMessage: "Проверка наличия антивируса через SecurityCenter2 доступна на Windows.",
                antivirusProducts: Array.Empty<string>());
        }

        var firewallService = ExecuteCommand("sc", "query MpsSvc");
        var firewallDetected = firewallService.Output.Contains("SERVICE_NAME: MpsSvc", StringComparison.OrdinalIgnoreCase);

        var antivirusProducts = QueryWindowsAntivirusProducts();
        var antivirusDetected = antivirusProducts.Count > 0;

        return new InstalledSoftwareCheckResult(
            firewallDetected,
            firewallDetected
                ? "Обнаружена служба Windows Firewall (MpsSvc)."
                : $"Служба MpsSvc не обнаружена или недоступна. {firewallService.Error}",
            antivirusDetected,
            antivirusDetected
                ? $"Обнаружены антивирусные продукты: {string.Join(", ", antivirusProducts)}"
                : "Антивирусные продукты в SecurityCenter2 не найдены.",
            antivirusProducts);
    }

    private static CheckResult CheckFirewallOperational()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return new CheckResult(false, "Проверка работоспособности МЭ реализована для Windows.");
        }

        var service = ExecuteCommand("sc", "query MpsSvc");
        var serviceRunning = service.Output.Contains("RUNNING", StringComparison.OrdinalIgnoreCase);

        var profiles = ExecuteCommand("netsh", "advfirewall show allprofiles");
        var enabledMentions = profiles.Output.Split('\n')
            .Count(line => line.Contains("State", StringComparison.OrdinalIgnoreCase)
                        && line.Contains("ON", StringComparison.OrdinalIgnoreCase));

        var ok = serviceRunning && enabledMentions > 0;
        var message = ok
            ? $"Служба MpsSvc запущена, включенных профилей firewall: {enabledMentions}."
            : "Firewall может быть отключен/настроен некорректно (служба не RUNNING или профили OFF).";

        return new CheckResult(ok, message);
    }

    private static CheckResult CheckAntivirusOperational(IReadOnlyCollection<string> antivirusProducts)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return new CheckResult(false, "Проверка работоспособности антивируса реализована для Windows.");
        }

        if (antivirusProducts.Count == 0)
        {
            return new CheckResult(false, "Нельзя проверить работоспособность: антивирус не обнаружен.");
        }

        var processList = ExecuteCommand("tasklist", string.Empty).Output;

        var knownProcessHints = new[]
        {
            "MsMpEng", "avp", "avg", "avast", "ekrn", "mcshield", "savservice", "bdagent"
        };

        var hasResidentModule = knownProcessHints.Any(h =>
            processList.Contains(h, StringComparison.OrdinalIgnoreCase));

        return hasResidentModule
            ? new CheckResult(true, "Обнаружены признаки работы резидентного модуля антивируса.")
            : new CheckResult(false, "Антивирус найден, но резидентный модуль не обнаружен в списке процессов.");
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

    private static string ToStatus(bool value) => value ? "УСПЕШНО" : "НЕУСПЕШНО";

    private sealed record CheckResult(bool IsSuccess, string Message);

    private sealed record InstalledSoftwareCheckResult(
        bool FirewallDetected,
        string FirewallMessage,
        bool AntivirusDetected,
        string AntivirusMessage,
        IReadOnlyList<string> AntivirusProducts);
}
