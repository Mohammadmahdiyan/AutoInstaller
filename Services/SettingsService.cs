using System.Text.Json;
using GtaSaModManager.Models;

namespace GtaSaModManager.Services;

public class SettingsService
{
    private readonly string _settingsDirectory;
    private readonly string _settingsPath;

    public SettingsService()
    {
        _settingsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GtaSaModManager");
        _settingsPath = Path.Combine(_settingsDirectory, "settings.json");
    }

    public AppSettings Load()
    {
        if (!File.Exists(_settingsPath))
        {
            return new AppSettings();
        }

        try
        {
            var json = File.ReadAllText(_settingsPath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return new AppSettings();
            }

            var settings = JsonSerializer.Deserialize<AppSettings>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return settings ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(_settingsDirectory);
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(_settingsPath, json);
    }
}
