using System.Reflection;
using System.Runtime;
using System.Text;
using Microsoft.Extensions.Configuration;
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
            var assem = Assembly.GetExecutingAssembly();
            await using var resourceStream = assem.GetManifestResourceStream("YounBot.Resources.appsettings.json")!;
            await using var temp = File.Create("appsettings.json");
            await resourceStream.CopyToAsync(temp);

            resourceStream.Close();
            temp.Close();

            Console.WriteLine("Please Edit the appsettings.json to set configs and press any key to continue");
            Console.ReadLine();
        }
        
        // 输出GitVersionInformation
        var assembly = Assembly.GetExecutingAssembly();
        var assemblyName = assembly.GetName().Name;
        var gitVersionInformationType = assembly.GetType("GitVersionInformation");
        var fields = gitVersionInformationType.GetFields();

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
        await app.Init(appBuilder.GetConfig(), appBuilder.GetDeviceInfo(), appBuilder.GetKeystore(), assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "Unknown");
        await QrCodeLogin.Login(app);
        await app.Run();
    }
}