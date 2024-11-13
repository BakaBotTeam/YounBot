using System.Reflection;
using System.Runtime;
using System.Text;
using Lagrange.Core.Common;
using Lagrange.Core.Common.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using YounBot.Login;

namespace YounBot;

class Program
{
    public static async Task Main() {
        // thanks to Lagrange.OneBot
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        GCSettings.LatencyMode = GCLatencyMode.Batch;

        if (!File.Exists("appsettings.json"))
        {

            var assm = Assembly.GetExecutingAssembly();
            using var istr = assm.GetManifestResourceStream("YounBot.Resources.appsettings.json")!;
            using var temp = File.Create("appsettings.json");
            istr.CopyTo(temp);

            istr.Close();
            temp.Close();

            Console.WriteLine("Please Edit the appsettings.json to set configs and press any key to continue");
            Console.ReadLine();
        }
        

        // 创建IConfiguration实例
        var configurationBuilder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", false, true)
            .AddEnvironmentVariables();
        var configuration = configurationBuilder.Build();
        // 创建YounBotAppBuilder实例
        var appBuilder = new YounBotAppBuilder(configuration);
        var app = appBuilder.Build();
        // 配置应用程序
        appBuilder.ConfigureBots();

        //登录
        await QrCodeLogin.Login(app, appBuilder.GetConfig(), appBuilder.GetDeviceInfo(), appBuilder.GetKeystore());

    }
}