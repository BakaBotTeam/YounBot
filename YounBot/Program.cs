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
        
        Assembly assembly = Assembly.GetExecutingAssembly();

        IConfigurationBuilder configurationBuilder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", false, true)
            .AddEnvironmentVariables();
        IConfigurationRoot configuration = configurationBuilder.Build();
        YounBotAppBuilder appBuilder = new YounBotAppBuilder(configuration);
        YounBotApp app = appBuilder.Build();
        appBuilder.ConfigureBots();

        await app.Init(appBuilder.GetConfig(), appBuilder.GetDeviceInfo(), appBuilder.GetKeystore(), assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "Unknown");
        await QrCodeLogin.Login(app);
        await app.Run();
    }
}