using System.Reflection;
using System.Runtime;
using System.Text;
using Lagrange.Core.Common;
using Lagrange.Core.Common.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace YounBot;

class Program
{
    static void Main(string[] args)
    {
        // thanks to Lagrange.OneBot
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        GCSettings.LatencyMode = GCLatencyMode.Batch;

        if (!File.Exists("appsettings.json"))
        {
            Console.WriteLine("No exist config file, create it now...");

            var assm = Assembly.GetExecutingAssembly();
            using var istr = assm.GetManifestResourceStream("Lagrange.OneBot.Resources.appsettings.json")!;
            using var temp = File.Create("appsettings.json");
            istr.CopyTo(temp);

            istr.Close();
            temp.Close();

            Console.WriteLine("Please Edit the appsettings.json to set configs and press any key to continue");
            Console.ReadLine();
        }

        // 创建IServiceCollection实例
        var services = new ServiceCollection();

        // 创建IConfiguration实例
        var configurationBuilder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables();
        var configuration = configurationBuilder.Build();

        // 创建YounBotAppBuilder实例
        var appBuilder = new YounBotAppBuilder(services, configuration);

        // 配置应用程序
        appBuilder
            .ConfigureBots()
            .ConfigureLogging(logging => logging.AddConsole());

        // 构建应用程序
        var app = appBuilder.Build();
    }
}