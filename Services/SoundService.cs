using System.Media;
using WindowsPrayerTime.Models;

namespace WindowsPrayerTime.Services;

public sealed class SoundService
{
    public void PlayAdhanCue(AppSettings settings)
    {
        if (!settings.SoundEnabled)
        {
            return;
        }

        SystemSounds.Asterisk.Play();
    }

    public void PlayIqamahCue(AppSettings settings, bool userActive)
    {
        if (!settings.SoundEnabled)
        {
            return;
        }

        if (userActive)
        {
            SystemSounds.Exclamation.Play();
            return;
        }

        SystemSounds.Beep.Play();
    }
}
