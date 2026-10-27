using System.Diagnostics;
using CmlLib.Core;
using CmlLib.Core.Installer.Forge;
using System.Text.Json.Nodes;

namespace MCTL.Game;

public static class GameInstaller
{
    public static async Task InstallVanillaAsync(MinecraftLauncher launcher, string version)
    {
        Console.WriteLine($"正在检查并安装 Minecraft {version} ...");
        launcher.FileProgressChanged += (_, args) =>
        {
            if (args.TotalTasks > 0)
                Console.Write($"\r下载：{args.ProgressedTasks}/{args.TotalTasks}  {args.Name}");
        };
        await launcher.InstallAsync(version);
        Console.WriteLine($"\nMinecraft {version} 安装完成。");
    }

    public static Task<List<string>> GetInstalledVersionsAsync(MinecraftLauncher launcher)
    {
        var versionsDir = launcher.MinecraftPath.Versions;
        if (!Directory.Exists(versionsDir))
            return Task.FromResult(new List<string>());

        var installedVersions = Directory.GetDirectories(versionsDir)
            .Where(dir =>
            {
                var name = Path.GetFileName(dir);
                return !string.IsNullOrEmpty(name) &&
                       File.Exists(Path.Combine(dir, $"{name}.json"));
            })
            .Select(dir => Path.GetFileName(dir)!)
            .ToList();

        return Task.FromResult(installedVersions);
    }

    public static async Task<string> InstallForgeAsync(MinecraftLauncher launcher, string mcVersion, string forgeVersion)
    {
        Console.WriteLine($"正在安装 Forge {forgeVersion} (MC {mcVersion}) ...");
        var installer = new ForgeInstaller(launcher);

        string installedId = string.IsNullOrWhiteSpace(forgeVersion)
            ? await installer.Install(mcVersion, new ForgeInstallOptions())
            : await installer.Install(mcVersion, forgeVersion, new ForgeInstallOptions());

        Console.WriteLine($"Forge 安装完成，版本 ID：{installedId}");
        Console.WriteLine("正在补全游戏资源...");
        await launcher.InstallAsync(installedId);
        return installedId;
    }
    public static async Task<string?> InstallFabricAsync(string mcDir, string mcVersion, string loaderVersion)
    {
        var installerPath = Path.Combine(AppContext.BaseDirectory, "fabric-installer.jar");
        if (!File.Exists(installerPath))
        {
            Console.WriteLine($"缺少 fabric-installer.jar，请将其放在：{AppContext.BaseDirectory}");
            return null;
        }

        var psi = new ProcessStartInfo
        {
            FileName = "java",
            Arguments = $"-jar \"{installerPath}\" client -dir \"{mcDir}\" -mcversion {mcVersion} -loader {loaderVersion} -noprofile",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        Console.WriteLine("正在调用 Fabric Installer...");
        using var process = Process.Start(psi)!;
        await process.WaitForExitAsync();

        if (process.ExitCode == 0)
        {
            return $"fabric-loader-{loaderVersion}-{mcVersion}";
        }

        Console.WriteLine("Fabric 安装失败，请检查 Java 环境。");
        return null;
    }

    public static Task CloneVersionAsync(string mcDir, string sourceVersion, string targetVersion)
    {
        if (string.Equals(sourceVersion, targetVersion, StringComparison.OrdinalIgnoreCase)) return Task.CompletedTask;

        var sourceDir = Path.Combine(mcDir, "versions", sourceVersion);
        var targetDir = Path.Combine(mcDir, "versions", targetVersion);

        if (!Directory.Exists(sourceDir) || Directory.Exists(targetDir))
        {
            Console.WriteLine($"克隆失败：源版本不存在或目标版本已存在。");
            return Task.CompletedTask;
        }

        Console.WriteLine($"正在克隆 {sourceVersion} 到 {targetVersion} ...");
        CopyDirectory(sourceDir, targetDir);

        var sourceJsonPath = Path.Combine(targetDir, $"{sourceVersion}.json");
        var targetJsonPath = Path.Combine(targetDir, $"{targetVersion}.json");

        if (File.Exists(sourceJsonPath))
        {
            var json = JsonNode.Parse(File.ReadAllText(sourceJsonPath));
            if (json != null)
            {
                json["id"] = targetVersion;
                if (json["jar"] != null) json["jar"] = sourceVersion;
                File.WriteAllText(targetJsonPath, json.ToString());
            }
            File.Delete(sourceJsonPath);
        }

        var sourceJarPath = Path.Combine(targetDir, $"{sourceVersion}.jar");
        var targetJarPath = Path.Combine(targetDir, $"{targetVersion}.jar");
        if (File.Exists(sourceJarPath)) File.Move(sourceJarPath, targetJarPath);

        Directory.CreateDirectory(Path.Combine(targetDir, "mods"));
        Console.WriteLine($"克隆成功！专属 mods 文件夹已创建。启动命令：mctl launch {targetVersion}");
        return Task.CompletedTask;
    }

    private static void CopyDirectory(string sourceDir, string destinationDir)
    {
        Directory.CreateDirectory(destinationDir);
        foreach (var file in Directory.GetFiles(sourceDir))
            File.Copy(file, Path.Combine(destinationDir, Path.GetFileName(file)), true);
        foreach (var dir in Directory.GetDirectories(sourceDir))
            CopyDirectory(dir, Path.Combine(destinationDir, Path.GetFileName(dir)));
    }
}