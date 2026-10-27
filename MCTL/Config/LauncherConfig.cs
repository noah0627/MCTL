using System.Text.Json;
using System.Text.Json.Serialization;

namespace MCTL.Config;

public sealed class LauncherConfig
{
    [JsonPropertyName("gameDirectory")]
    public string GameDirectory { get; set; } = "";

    [JsonPropertyName("clientId")]
    public string ClientId { get; set; } = "870e4645-b36a-4890-a8f3-6c408f2754bc";

    [JsonPropertyName("accessToken")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("username")]
    public string? Username { get; set; }

    [JsonPropertyName("uuid")]
    public string? Uuid { get; set; }

    [JsonPropertyName("maxRamMb")]
    public int MaxRamMb { get; set; } = 4096;

    [JsonPropertyName("jvmArgs")]
    public string JvmArgs { get; set; } = "";

    [JsonPropertyName("enableVersionIsolation")]
    public bool EnableVersionIsolation { get; set; } = false;

    [JsonPropertyName("isOfflineMode")]
    public bool IsOfflineMode { get; set; } = false;
}

public static class LauncherConfigStore
{
    private static readonly string ConfigDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MCTL");

    private static readonly string ConfigPath = Path.Combine(ConfigDir, "config.json");

    public static LauncherConfig Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var json = File.ReadAllText(ConfigPath);
                return JsonSerializer.Deserialize<LauncherConfig>(json) ?? new LauncherConfig();
            }
        }
        catch { /* 忽略损坏的配置 */ }
        return new LauncherConfig();
    }

    public static void Save(LauncherConfig config)
    {
        Directory.CreateDirectory(ConfigDir);
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ConfigPath, json);
    }
}