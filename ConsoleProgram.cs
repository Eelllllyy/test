using System.Text;

namespace SecurityCheckApp;

internal static class ConsoleProgram
{
    private static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine("SecurityCheckApp (Console) — кроссплатформенный режим");
        Console.WriteLine();

        var internet = SecurityChecks.CheckInternetConnection("ya.ru");
        var installed = SecurityChecks.CheckInstalledProtectionSoftware();
        var firewall = SecurityChecks.CheckFirewallOperational();
        var antivirus = SecurityChecks.CheckAntivirusOperational(installed.AntivirusProducts);

        Console.WriteLine($"1) Интернет: {internet.Message}");
        Console.WriteLine($"2) Наличие МЭ: {installed.FirewallMessage}");
        Console.WriteLine($"3) Наличие АВ: {installed.AntivirusMessage}");
        Console.WriteLine($"4) Работоспособность МЭ: {firewall.Message}");
        Console.WriteLine($"5) Работоспособность АВ: {antivirus.Message}");
        Console.WriteLine();
        Console.WriteLine("Примечание: полноценные проверки МЭ/АВ выполняются только в Windows.");
    }
}
