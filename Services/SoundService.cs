using System.Media;
using WindowsPrayerTime.Models;

namespace WindowsPrayerTime.Services;

public sealed class SoundService
{
    private static readonly string AdhanCuePath = Path.Combine(AppContext.BaseDirectory, "Assets", "Sounds", "adhan-cue.wav");

    public void PlayAdhanCue(AppSettings settings)
    {
        if (!settings.SoundEnabled)
        {
            return;
        }

        if (File.Exists(AdhanCuePath))
        {
            _ = Task.Run(() =>
            {
                try
                {
                    using var player = new SoundPlayer(AdhanCuePath);
                    player.PlaySync();
                }
                catch
                {
                    SystemSounds.Exclamation.Play();
                }
            });
            return;
        }

        SystemSounds.Exclamation.Play();
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
