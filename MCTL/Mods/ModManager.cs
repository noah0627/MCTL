using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MCTL.Mods;

file class SearchResponse { [JsonPropertyName("hits")] public List<SearchHit> Hits { get; set; } = new(); }
file class SearchHit { [JsonPropertyName("project_id")] public string ProjectId { get; set; } = ""; [JsonPropertyName("title")] public string Title { get; set; } = ""; }
file class VersionEntry { [JsonPropertyName("game_versions")] public List<string> GameVersions { get; set; } = new(); [JsonPropertyName("loaders")] public List<string> Loaders { get; set; } = new(); [JsonPropertyName("files")] public List<VersionFile> Files { get; set; } = new(); }
file class VersionFile { [JsonPropertyName("url")] public string Url { get; set; } = ""; [JsonPropertyName("filename")] public string Filename { get; set; } = ""; [JsonPropertyName("primary")] public bool Primary { get; set; } }

public class ModManager
{
    private static readonly HttpClient Http = new()
    {
        BaseAddress = new Uri("https://api.modrinth.com/v2/"),
        DefaultRequestHeaders = { { "User-Agent", "MCTL/1.0.0" } }
    };

    private readonly string _modsDirectory;

    public ModManager(string gameDir)
    {
        _modsDirectory = Path.Combine(gameDir, "mods");
        Directory.CreateDirectory(_modsDirectory);
    }

    public async Task InstallFirstMatchAsync(string query, string gameVersion, string loader)
    {
        Console.WriteLine($"正在搜索 \"{query}\" ({loader} / {gameVersion})...");
        var facets = JsonSerializer.Serialize(new[] { new[] { $"versions:{gameVersion}" }, new[] { $"categories:{loader}" } });
        var url = $"search?query={Uri.EscapeDataString(query)}&facets={Uri.EscapeDataString(facets)}&limit=1";

        try
        {
            var search = await Http.GetFromJsonAsync<SearchResponse>(url);
            if (search == null || search.Hits.Count == 0) { Console.WriteLine("未找到匹配的模组。"); return; }

            var project = search.Hits[0];
            Console.WriteLine($"找到项目：{project.Title}");

            var versions = await Http.GetFromJsonAsync<List<VersionEntry>>($"project/{project.ProjectId}/version");
            var target = versions?.FirstOrDefault(v => v.GameVersions.Contains(gameVersion) && v.Loaders.Contains(loader, StringComparer.OrdinalIgnoreCase));
            var file = target?.Files.FirstOrDefault(f => f.Primary) ?? target?.Files.FirstOrDefault();

            if (file == null) { Console.WriteLine("未找到适配当前版本的文件。"); return; }

            var destPath = Path.Combine(_modsDirectory, file.Filename);
            Console.Write($"正在下载 {file.Filename} ... ");
            var bytes = await Http.GetByteArrayAsync(file.Url);
            await File.WriteAllBytesAsync(destPath, bytes);
            Console.WriteLine("完成！");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"下载失败：{ex.Message}");
        }
    }
}