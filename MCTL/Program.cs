using MCTL.Auth;
using MCTL.Config;
using MCTL.Game;
using MCTL.Mods;
using CmlLib.Core;
using System.Text.Json.Nodes;

namespace MCTL;

internal static class Program
{
    private static MinecraftLauncher _launcher = null!;
    private static ModManager _modManager = null!;
    private static string _gameDir = "";

    private static async Task Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        if (args.Length == 0) { PrintHelp(); return; }

        var config = LauncherConfigStore.Load();
        _gameDir = config.GameDirectory;
        if (string.IsNullOrWhiteSpace(_gameDir))
            _gameDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".minecraft");

        for (int i = 0; i < args.Length; i++)
        {
            if ((args[i] == "-d" || args[i] == "--dir") && i + 1 < args.Length)
            {
                _gameDir = Path.GetFullPath(args[i + 1]);
                config.GameDirectory = _gameDir;
                LauncherConfigStore.Save(config);
                break;
            }
        }

        Directory.CreateDirectory(_gameDir);
        _launcher = new MinecraftLauncher(new MinecraftPath(_gameDir));
        _modManager = new ModManager(_gameDir);

        var action = args[0].ToLowerInvariant();
        try
        {
            switch (action)
            {
                case "login": await MicrosoftAuth.LoginAndSaveAsync(); break;
                case "list":
                    var versions = await GameInstaller.GetInstalledVersionsAsync(_launcher);
                    if (versions.Count == 0) Console.WriteLine("（没有已安装的版本）");
                    else versions.ForEach(Console.WriteLine);
                    break;
                case "install": await HandleInstallAsync(args); break;
                case "clone":
                    if (args.Length < 3) { Console.WriteLine("用法: mctl clone <源版本> <新名字>"); return; }
                    await GameInstaller.CloneVersionAsync(_gameDir, args[1], args[2]);
                    break;
                case "mod":
                    if (args.Length < 4) { Console.WriteLine("用法: mctl mod <query> <mc_version> <loader>"); return; }
                    await _modManager.InstallFirstMatchAsync(args[1], args[2], args[3]);
                    break;
                case "launch":
                    if (args.Length < 2) { Console.WriteLine("用法: mctl launch <version>"); return; }
                    var session = MicrosoftAuth.GetCachedSession();
                    if (session == null) { Console.WriteLine("尚未登录，请先运行: mctl login"); return; }
                    await GameLauncher.LaunchAsync(_launcher, args[1], session);
                    break;
                case "config": HandleConfig(args); break;
                case "help": case "--help": case "-h": PrintHelp(); break;
                default: Console.WriteLine($"未知命令: {action}\n"); PrintHelp(); break;
            }
        }
        catch (Exception ex) { Console.WriteLine($"执行失败: {ex.Message}"); }
    }

    private static async Task HandleInstallAsync(string[] args)
    {
        if (args.Length < 2) { Console.WriteLine("用法: mctl install <mc_version> [--modloader=<forge|fabric>] [--loader-version=<ver>] [--name=<name>]"); return; }

        var mcVersion = args[1];
        var modloader = GetArgValue(args, "--modloader")?.ToLowerInvariant();
        var loaderVersion = GetArgValue(args, "--loader-version");
        var customName = GetArgValue(args, "--name");
        string installedId = mcVersion;

        await GameInstaller.InstallVanillaAsync(_launcher, mcVersion);

        if (modloader == "forge") installedId = await GameInstaller.InstallForgeAsync(_launcher, mcVersion, loaderVersion ?? "");
        else if (modloader == "fabric")
        {
            if (string.IsNullOrWhiteSpace(loaderVersion))
            {
                Console.Write("未指定 Loader 版本，正在查询最新 Fabric 版本... ");
                loaderVersion = await FetchLatestFabricLoaderAsync(mcVersion);
                Console.WriteLine(loaderVersion ?? "查询失败");
                if (string.IsNullOrWhiteSpace(loaderVersion)) return;
            }
            var fabricId = await GameInstaller.InstallFabricAsync(_gameDir, mcVersion, loaderVersion);
            if (fabricId != null) installedId = fabricId; else return;
        }

        if (!string.IsNullOrEmpty(customName)) await GameInstaller.CloneVersionAsync(_gameDir, installedId, customName);
    }

    private static void HandleConfig(string[] args)
    {
        var config = LauncherConfigStore.Load();
        if (args.Length == 1)
        {
            Console.WriteLine("=== 当前配置 ===");
            Console.WriteLine($"游戏目录      : {config.GameDirectory}");
            Console.WriteLine($"最大内存 (MB) : {config.MaxRamMb}");
            Console.WriteLine($"JVM 参数      : {(string.IsNullOrWhiteSpace(config.JvmArgs) ? "(无)" : config.JvmArgs)}");
            Console.WriteLine($"版本隔离      : {(config.EnableVersionIsolation ? "开启" : "关闭")}");
            Console.WriteLine($"登录状态      : {(string.IsNullOrEmpty(config.Username) ? "未登录" : config.Username)}");
            return;
        }

        var ram = GetArgValue(args, "--memory") ?? GetArgValue(args, "--ram");
        if (ram != null && int.TryParse(ram, out var r)) config.MaxRamMb = r;

        var jvm = GetArgValue(args, "--jvm-args");
        if (jvm != null) config.JvmArgs = jvm;

        var iso = GetArgValue(args, "--isolation");
        if (iso != null) config.EnableVersionIsolation = iso.Equals("true", StringComparison.OrdinalIgnoreCase) || iso == "1";

        LauncherConfigStore.Save(config);
        Console.WriteLine("配置已更新！");
    }

    private static async Task<string?> FetchLatestFabricLoaderAsync(string mcVersion)
    {
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "MCTL/1.0");
            var json = await client.GetStringAsync($"https://meta.fabricmc.net/v2/versions/loader/{mcVersion}");
            var arr = JsonNode.Parse(json)?.AsArray();
            if (arr != null && arr.Count > 0) return arr[0]?["loader"]?["version"]?.ToString();
        }
        catch { }
        return null;
    }

    private static string? GetArgValue(string[] args, string key)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].StartsWith(key + "=")) return args[i].Substring(key.Length + 1);
            if (args[i] == key && i + 1 < args.Length && !args[i + 1].StartsWith("-")) return args[i + 1];
        }
        return null;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("MCTL - Minecraft Terminal Launcher");
        Console.WriteLine("用法: mctl <command> [options]");
        Console.WriteLine("\n命令:");
        Console.WriteLine("  login                         登录微软账户");
        Console.WriteLine("  list                          列出已安装版本");
        Console.WriteLine("  install <mc_version>          安装游戏");
        Console.WriteLine("  clone <src> <target>          克隆并隔离版本");
        Console.WriteLine("  mod <query> <mc> <loader>     下载 Modrinth 模组");
        Console.WriteLine("  launch <version>              启动游戏");
        Console.WriteLine("  config                        查看/修改全局配置");
        Console.WriteLine("\nInstall 参数:");
        Console.WriteLine("  --modloader=<forge|fabric>    指定加载器");
        Console.WriteLine("  --loader-version=<ver>        指定加载器版本（Fabric不填则自动查最新）");
        Console.WriteLine("  --name=<name>                 安装后自动克隆为独立版本");
        Console.WriteLine("\nConfig 参数:");
        Console.WriteLine("  --memory=<MB>                 分配最大内存");
        Console.WriteLine("  --jvm-args=\"<args>\"           额外 JVM 参数");
        Console.WriteLine("  --isolation=<true|false>      开启或关闭版本隔离");
    }
}