namespace WindowsPrayerTime;

static class Program
{
    private static Mutex? _singleInstanceMutex;

    [STAThread]
    static void Main(string[] args)
    {
        if (args.Length > 0 && string.Equals(args[0], "--render-theme-screenshots", StringComparison.OrdinalIgnoreCase))
        {
            UI.ThemeScreenshotRenderer.Render(args.Length > 1 ? args[1] : null);
            return;
        }

        _singleInstanceMutex = new Mutex(true, "WindowsPrayerTime.SingleInstance", out bool createdNew);
        if (!createdNew)
        {
            MessageBox.Show(
                "Windows Prayer Time is already running in the notification area.",
                "Windows Prayer Time",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new PrayerTimeApplicationContext());
        _singleInstanceMutex.ReleaseMutex();
    }
}
