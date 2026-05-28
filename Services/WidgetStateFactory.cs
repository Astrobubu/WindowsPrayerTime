using WindowsPrayerTime.Models;

namespace WindowsPrayerTime.Services;

public static class WidgetStateFactory
{
    public static WidgetState FromSchedule(PrayerSchedule? schedule, AppSettings settings, DateTime now)
    {
        if (schedule is null)
        {
            return new WidgetState("Loading", "--:--", "Fetching prayer times", settings.LocationLabel, false, null);
        }

        string locationLabel = string.IsNullOrWhiteSpace(schedule.Today?.Location)
            ? settings.LocationLabel
            : schedule.Today.Location;

        PrayerOccurrence? iqamah = settings.ShowIqamahCountdownAfterAdhan
            ? schedule.CurrentIqamahCountdown(now)
            : null;

        if (iqamah is not null)
        {
            return new WidgetState(
                "Iqamah " + iqamah.PrayerName,
                FormatCountdown(iqamah.Time - now),
                iqamah.Time.ToString("h:mm tt"),
                "Adhan was " + FormatCountdown(now - iqamah.Time.AddMinutes(-iqamah.IqamahOffsetMinutes)) + " ago",
                true,
                iqamah.Time);
        }

        PrayerOccurrence? next = schedule.NextAdhan(now);
        if (next is null)
        {
            return new WidgetState("Tomorrow", "--:--", "Refreshing schedule", settings.LocationLabel, false, null);
        }

        return new WidgetState(
            "Next " + next.PrayerName,
            FormatCountdown(next.Time - now),
            next.Time.ToString("h:mm tt"),
            locationLabel,
            false,
            next.Time);
    }

    public static string FormatCountdown(TimeSpan timeSpan)
    {
        if (timeSpan < TimeSpan.Zero)
        {
            timeSpan = TimeSpan.Zero;
        }

        if (timeSpan.TotalHours >= 1)
        {
            return $"{(int)timeSpan.TotalHours:0}:{timeSpan.Minutes:00}:{timeSpan.Seconds:00}";
        }

        return $"{timeSpan.Minutes:00}:{timeSpan.Seconds:00}";
    }
}
