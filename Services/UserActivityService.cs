using System.Runtime.InteropServices;

namespace WindowsPrayerTime.Services;

public static class UserActivityService
{
    public static TimeSpan GetIdleTime()
    {
        var info = new LastInputInfo
        {
            CbSize = (uint)Marshal.SizeOf<LastInputInfo>()
        };

        if (!GetLastInputInfo(ref info))
        {
            return TimeSpan.Zero;
        }

        uint tickCount = unchecked((uint)Environment.TickCount);
        uint idleMilliseconds = tickCount - info.DwTime;
        return TimeSpan.FromMilliseconds(idleMilliseconds);
    }

    public static bool IsUserActive(TimeSpan threshold) => GetIdleTime() <= threshold;

    [DllImport("user32.dll")]
    private static extern bool GetLastInputInfo(ref LastInputInfo plii);

    [StructLayout(LayoutKind.Sequential)]
    private struct LastInputInfo
    {
        public uint CbSize;
        public uint DwTime;
    }
}
