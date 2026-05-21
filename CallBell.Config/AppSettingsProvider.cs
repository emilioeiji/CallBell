using System.Configuration;
using System.Text.Json;

namespace CallBell.Config;

public static class AppSettingsProvider
{
    private const string CompanyName = "CallBell";
    private const string ProductName = "CallBell";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static CallBellSettings Load()
    {
        var portableMode = ReadBool("PortableMode", true);
        var defaultDataRoot = portableMode
            ? EnsureDirectory(Path.Combine(AppContext.BaseDirectory, "data"))
            : EnsureDirectory(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                CompanyName,
                ProductName));

        var databasePath = ResolvePath("DatabasePath", Path.Combine(defaultDataRoot, "callbell.db"));
        var databaseDirectory = EnsureDirectory(Path.GetDirectoryName(databasePath) ?? defaultDataRoot);
        var installationRoot = Directory.GetParent(databaseDirectory)?.FullName ?? databaseDirectory;
        var triggerDirectory = ResolvePath("TriggerDirectory", Path.Combine(installationRoot, "trigger"));
        var profileDirectory = ResolvePath("ProfileDirectory", Path.Combine(installationRoot, "Profiles"));
        var monitorBoardTitle = ReadString("MonitorBoardTitle", "CallBell Monitor");
        var monitorRefreshSeconds = Math.Max(1, ReadInt("MonitorRefreshSeconds", 3));

        Directory.CreateDirectory(databaseDirectory);
        Directory.CreateDirectory(triggerDirectory);
        Directory.CreateDirectory(profileDirectory);

        return new CallBellSettings
        {
            DatabasePath = databasePath,
            TriggerDirectory = triggerDirectory,
            ProfileDirectory = profileDirectory,
            MonitorBoardTitle = monitorBoardTitle,
            MonitorRefreshSeconds = monitorRefreshSeconds,
            PortableMode = portableMode
        };
    }

    public static string GetProfilePath(CallBellSettings settings, string fileName)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        Directory.CreateDirectory(settings.ProfileDirectory);
        return Path.Combine(settings.ProfileDirectory, fileName);
    }

    public static T? LoadProfile<T>(CallBellSettings settings, string fileName)
    {
        var path = GetProfilePath(settings, fileName);
        if (!File.Exists(path))
        {
            return default;
        }

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    public static void SaveProfile<T>(CallBellSettings settings, string fileName, T value)
    {
        var path = GetProfilePath(settings, fileName);
        var json = JsonSerializer.Serialize(value, JsonOptions);
        File.WriteAllText(path, json);
    }

    private static string ResolvePath(string key, string fallback)
    {
        var rawValue = ConfigurationManager.AppSettings[key];
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return fallback;
        }

        return Path.IsPathRooted(rawValue)
            ? rawValue
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, rawValue));
    }

    private static string ReadString(string key, string fallback)
    {
        return string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings[key])
            ? fallback
            : ConfigurationManager.AppSettings[key]!;
    }

    private static int ReadInt(string key, int fallback)
    {
        return int.TryParse(ConfigurationManager.AppSettings[key], out var parsed)
            ? parsed
            : fallback;
    }

    private static bool ReadBool(string key, bool fallback)
    {
        return bool.TryParse(ConfigurationManager.AppSettings[key], out var parsed)
            ? parsed
            : fallback;
    }

    private static string EnsureDirectory(string path)
    {
        Directory.CreateDirectory(path);
        return path;
    }
}
