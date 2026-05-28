using Microsoft.Win32;

namespace WindowsPrayerTime.Services;

public static class StartupService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "WindowsPrayerTime";

    public static bool IsEnabled()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        return !string.IsNullOrWhiteSpace(key?.GetValue(AppName) as string);
    }

    public static void SetEnabled(bool enabled)
    {
        using RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
            ?? Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);

        if (enabled)
        {
            string exePath = Application.ExecutablePath;
            key.SetValue(AppName, Quote(exePath));
            return;
        }

        key.DeleteValue(AppName, throwOnMissingValue: false);
    }

    private static string Quote(string value) => "\"" + value + "\"";
}
