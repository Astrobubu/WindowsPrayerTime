namespace WindowsPrayerTime;

static class Program
{
    private static Mutex? _singleInstanceMutex;

    [STAThread]
    static void Main()
    {
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
