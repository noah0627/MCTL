using CmlLib.Core.Auth;
using CmlLib.Core.Auth.Microsoft;
using MCTL.Config;
using XboxAuthNet.Game.Msal;
using XboxAuthNet.Game.Msal.OAuth;
using Microsoft.Identity.Client;

namespace MCTL.Auth;

public static class MicrosoftAuth
{
    public static async Task<MSession?> LoginAndSaveAsync()
    {
        var config = LauncherConfigStore.Load();
        if (string.IsNullOrWhiteSpace(config.ClientId) || config.ClientId == "你的Azure应用ClientID")
        {
            Console.WriteLine("错误：请先在配置文件中设置正确的 Azure Client ID。");
            return null;
        }

        var app = await MsalClientHelper.BuildApplicationWithCache(config.ClientId);
        var loginHandler = JELoginHandlerBuilder.BuildDefault();
        var authenticator = loginHandler.CreateAuthenticatorWithNewAccount();

        authenticator.AddMsalOAuth(app, msal => msal.DeviceCode(
            deviceCodeResult =>
            {
                Console.WriteLine("=========================================");
                Console.WriteLine(deviceCodeResult.Message);
                Console.WriteLine("=========================================");
                return Task.CompletedTask;
            }
        ));
        authenticator.AddXboxAuthForJE(xbox => xbox.Basic());
        authenticator.AddJEAuthenticator();

        try
        {
            Console.WriteLine("正在请求设备代码...");
            var session = await authenticator.ExecuteForLauncherAsync();

            config.AccessToken = session.AccessToken;
            config.Username = session.Username;
            config.Uuid = session.UUID;
            LauncherConfigStore.Save(config);

            Console.WriteLine($"登录成功！欢迎回来，{session.Username}");
            return session;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"登录失败：{ex.Message}");
            return null;
        }
    }

    public static MSession? GetCachedSession()
    {
        var config = LauncherConfigStore.Load();
        if (string.IsNullOrWhiteSpace(config.AccessToken)) return null;

        return new MSession
        {
            AccessToken = config.AccessToken,
            Username = config.Username ?? "Player",
            UUID = config.Uuid ?? ""
        };
    }
}