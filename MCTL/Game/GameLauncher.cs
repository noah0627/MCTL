using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.ProcessBuilder;
using MCTL.Config;

namespace MCTL.Game;

public static class GameLauncher
{
    public static async Task LaunchAsync(MinecraftLauncher launcher, string version, MSession session)
    {
        Console.WriteLine($"正在启动 Minecraft {version} ...");
        var config = LauncherConfigStore.Load();

        var options = new MLaunchOption
        {
            Session = session,
            MaximumRamMb = config.MaxRamMb,
        };

        if (!string.IsNullOrWhiteSpace(config.JvmArgs))
        {
            options.ExtraJvmArguments = new[] { MArgument.FromCommandLine(config.JvmArgs) };
        }

        if (config.EnableVersionIsolation)
        {
            var isolatedRoot = Path.Combine(launcher.MinecraftPath.BasePath, "versions", version);
            var isolatedPath = new MinecraftPath(isolatedRoot);
            var isolatedLauncher = new MinecraftLauncher(isolatedPath);
            var process = await isolatedLauncher.BuildProcessAsync(version, options);
            process.Start();
            Console.WriteLine($"已启用版本隔离，游戏目录：{isolatedRoot}");
            return;
        }

        // 非隔离模式
        var normalProcess = await launcher.BuildProcessAsync(version, options);
        normalProcess.Start();
        Console.WriteLine("游戏已启动。");
    }
}