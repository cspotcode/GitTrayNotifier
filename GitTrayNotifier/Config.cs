using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Win32;

namespace GitTrayNotifier;

record AppConfig(
    string PollingInterval,
    List<RepositoryConfig> Repositories
)
{
    public static readonly AppConfig Default = new("1h", []);

    public TimeSpan ParsedPollingInterval => ParseInterval(PollingInterval);

    static TimeSpan ParseInterval(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length < 2)
            return TimeSpan.FromHours(1);

        var unit = value[^1];
        if (!int.TryParse(value[..^1], out var n) || n <= 0)
            return TimeSpan.FromHours(1);

        return unit switch
        {
            'm' => TimeSpan.FromMinutes(n),
            'h' => TimeSpan.FromHours(n),
            'd' => TimeSpan.FromDays(n),
            _ => TimeSpan.FromHours(1),
        };
    }
}

record RepositoryConfig(
    RepositoryType Type,
    string Path,
    string Branch
);

[JsonConverter(typeof(JsonStringEnumConverter))]
enum RepositoryType { Windows, Wsl2 }

static class ConfigLoader
{
    const string RegistryKeyPath = @"Software\GitTrayNotifier";
    const string RegistryValueName = "ConfigFilePath";

    static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    /// <summary>
    /// Returns the config file path from the registry, creating the registry value with the
    /// default path if it is missing.
    /// </summary>
    public static string GetConfigFilePath()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, writable: false);
        if (key?.GetValue(RegistryValueName) is string path && !string.IsNullOrWhiteSpace(path))
            return path;

        var defaultPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "GitTrayNotifier",
            "config.json");

        // Persist the default so the user can see and edit it in regedit.
        using var writableKey = Registry.CurrentUser.CreateSubKey(RegistryKeyPath);
        writableKey.SetValue(RegistryValueName, defaultPath, RegistryValueKind.String);

        return defaultPath;
    }

    /// <summary>
    /// Writes a new config file path to the registry.
    /// </summary>
    public static void SetConfigFilePath(string path)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RegistryKeyPath);
        key.SetValue(RegistryValueName, path, RegistryValueKind.String);
    }

    /// <summary>
    /// Loads and deserializes the config file. Returns AppConfig.Default if the file does not exist.
    /// Throws on parse errors.
    /// </summary>
    public static AppConfig Load(string path)
    {
        if (!File.Exists(path))
        {
            Console.WriteLine($"Config file not found at {path}; using defaults.");
            return AppConfig.Default;
        }

        using var stream = File.OpenRead(path);
        var dto = JsonSerializer.Deserialize<ConfigDto>(stream, JsonOptions)
                  ?? throw new InvalidDataException("Config file deserialized to null.");

        return new AppConfig(
            PollingInterval: dto.PollingInterval ?? AppConfig.Default.PollingInterval,
            Repositories: dto.Repositories?.Select(r => new RepositoryConfig(
                Type: r.Type ?? RepositoryType.Windows,
                Path: r.Path ?? throw new InvalidDataException("Repository entry missing 'path'."),
                Branch: r.Branch ?? throw new InvalidDataException("Repository entry missing 'branch'.")
            )).ToList() ?? []
        );
    }

    // Private DTOs used only for deserialization (nullable fields, optional properties).
    private class ConfigDto
    {
        public string? PollingInterval { get; set; }
        public List<RepositoryDto>? Repositories { get; set; }
    }

    private class RepositoryDto
    {
        public RepositoryType? Type { get; set; }
        public string? Path { get; set; }
        public string? Branch { get; set; }
    }
}
