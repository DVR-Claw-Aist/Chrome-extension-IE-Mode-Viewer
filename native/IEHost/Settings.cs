using System.IO;
using System.Text.Json;

namespace IEHost;

public class AppSettings
{
    public string DefaultUrl { get; set; } = "192.168.0.1";
    public string LastUrl { get; set; } = "";
    public bool FirstRun { get; set; } = true;
    public string Hotkey { get; set; } = "Ctrl+Alt+E";
    public int DebugPort { get; set; } = 9222;
    public string ChromePath { get; set; } = "";

    static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static string SettingsDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "IEHost");

    public static string SettingsPath => Path.Combine(SettingsDir, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                var s = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                if (s != null) return s;
            }
        }
        catch { }
        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(SettingsDir);
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(this, JsonOptions));
        }
        catch { }
    }
}
