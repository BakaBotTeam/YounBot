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
            Assembly assem = Assembly.GetExecutingAssembly();
            await using Stream resourceStream = assem.GetManifestResourceStream("YounBot.Resources.appsettings.json")!;
            await using FileStream temp = File.Create("appsettings.json");
            await resourceStream.CopyToAsync(temp);

            resourceStream.Close();
            temp.Close();

            Console.WriteLine("Please Edit the appsettings.json to set configs and press any key to continue");
            Console.ReadLine();
        }
        
        // 输出GitVersionInformation
        Assembly assembly = Assembly.GetExecutingAssembly();
        string? assemblyName = assembly.GetName().Name;
        Type? gitVersionInformationType = assembly.GetType("GitVersionInformation");
        FieldInfo[] fields = gitVersionInformationType.GetFields();

        // 创建IConfiguration实例
        IConfigurationBuilder configurationBuilder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", false, true)
            .AddEnvironmentVariables();
        IConfigurationRoot configuration = configurationBuilder.Build();
        // 创建YounBotAppBuilder实例
        YounBotAppBuilder appBuilder = new YounBotAppBuilder(configuration);
        YounBotApp app = appBuilder.Build();
        // 配置应用程序
        appBuilder.ConfigureBots();

        //登录
        await app.Init(appBuilder.GetConfig(), appBuilder.GetDeviceInfo(), appBuilder.GetKeystore(), assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "Unknown");
        await QrCodeLogin.Login(app);
        await app.Run();
    }
}