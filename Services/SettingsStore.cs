using System.Text.Json;
using WindowsPrayerTime.Models;

namespace WindowsPrayerTime.Services;

public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public string AppDirectory { get; }
    public string SettingsPath { get; }

    public SettingsStore()
    {
        AppDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WindowsPrayerTime");
        SettingsPath = Path.Combine(AppDirectory, "settings.json");
    }

    public AppSettings Load()
    {
        Directory.CreateDirectory(AppDirectory);

        if (!File.Exists(SettingsPath))
        {
            var created = new AppSettings();
            Save(created);
            return created;
        }

        try
        {
            string json = File.ReadAllText(SettingsPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
            settings.EnsureDefaults();
            Save(settings);
            return settings;
        }
        catch
        {
            var backupPath = SettingsPath + ".broken-" + DateTime.Now.ToString("yyyyMMddHHmmss");
            File.Copy(SettingsPath, backupPath, overwrite: true);

            var settings = new AppSettings();
            Save(settings);
            return settings;
        }
    }

    public void Save(AppSettings settings)
    {
        settings.EnsureDefaults();
        Directory.CreateDirectory(AppDirectory);
        string json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(SettingsPath, json);
    }
}
