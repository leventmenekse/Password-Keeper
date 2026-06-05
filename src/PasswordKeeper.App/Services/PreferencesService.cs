using System.IO;
using System.Text.Json;

namespace PasswordKeeper.App.Services;

public sealed class PreferencesService : IPreferencesService
{
    private static readonly string PrefsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "PasswordKeeper", "preferences.json");

    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public AppPreferences Current { get; private set; } = new();

    public event EventHandler? Changed;

    public void Load()
    {
        try
        {
            if (File.Exists(PrefsPath))
            {
                var json = File.ReadAllText(PrefsPath);
                var prefs = JsonSerializer.Deserialize<AppPreferences>(json, Json);
                if (prefs is not null) Current = prefs;
            }
        }
        catch
        {
            // Corrupt prefs file → fall back to defaults; user can re-save to overwrite.
            Current = new AppPreferences();
        }
    }

    public void Save(AppPreferences updated)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(PrefsPath)!);
        File.WriteAllText(PrefsPath, JsonSerializer.Serialize(updated, Json));
        Current = updated;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
