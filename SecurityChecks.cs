using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;

namespace SecurityCheckApp;

internal static class SecurityChecks
{
    public static string CheckInternetConnection()
    {
        IPStatus status = IPStatus.Unknown;

        try
        {
            status = new Ping().Send("yandex.ru", 1500)!.Status;
        }
        catch (Exception)
        {
            status = IPStatus.Unknown;
        }

        if (status == IPStatus.Success)
        {
            return "Данный компьютер подключен к интернету";
        }

        return "Данный компьютер не подключен к интернету";
    }

    public static string CheckFirewallInstalled()
    {
        // Простой вариант из методички: проверка наличия файла.
        if (File.Exists(@"C:\Windows\System32\FirewallAPI.dll"))
        {
            return "Фаервол установлен!";
        }

        return "Фаервол не установлен!";
    }

    public static string CheckAntivirusInstalled()
    {
        // Простой вариант из методички: проверка наличия файла/папки.
        if (File.Exists(@"C:\Program Files\Windows Defender\MSASCuiL.exe") ||
            Directory.Exists(@"C:\ProgramData\Microsoft\Windows Defender\Platform"))
        {
            return "Антивирус установлен!";
        }

        return "Антивирус не установлен!";
    }

    public static string CheckFirewallOperational()
    {
        using var client = new WebClient();

        try
        {
            _ = client.DownloadString("http://yandex.ru");
        }
        catch (Exception)
        {
            return "Межсетевой экран функционирует правильно!";
        }

        return "Межсетевой экран функционирует неверно, или не функционирует!";
    }

    public static string CheckAntivirusOperational()
    {
        string taskList = RunCommand("tasklist");

        if (taskList.Contains("MsMpEng", StringComparison.OrdinalIgnoreCase))
        {
            return "Резидентный модуль антивируса работает.";
        }

        if (taskList.Contains("avp", StringComparison.OrdinalIgnoreCase))
        {
            return "Резидентный модуль антивируса работает.";
        }

        if (taskList.Contains("avast", StringComparison.OrdinalIgnoreCase))
        {
            return "Резидентный модуль антивируса работает.";
        }

        return "Резидентный модуль антивируса не запущен.";
    }

    private static string RunCommand(string fileName)
    {
        try
        {
            var startInfo = new ProcessStartInfo();
            startInfo.FileName = fileName;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                return string.Empty;
            }

            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(5000);
            return output;
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }
}
