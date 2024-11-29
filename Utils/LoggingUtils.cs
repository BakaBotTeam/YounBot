using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace YounBot.Utils
{
    public class LoggingUtils
    {
        // logger cache dictonary with tag name
        private static readonly ConcurrentDictionary<string, ILogger> Loggers = new();
        public static ILogger Logger = CreateLogger();

        public static ILogger CreateLogger()
        {
            return CreateLogger("YounBot");
        }
        
        // create a simple colored logger with tag name, accept min log level, cache it for later use
        public static ILogger CreateLogger(string tag)
        {
            // read log level from Configuration
            var minLogLevel = LogLevel.Information;
            switch (YounBotApp.Configuration[$"Logging:LogLevel:{tag}"] ??
                    YounBotApp.Configuration["Logging:LogLevel:Default"])
            {
                case "Trace":
                    minLogLevel = LogLevel.Trace;
                    break;
                case "Debug":
                    minLogLevel = LogLevel.Debug;
                    break;
                case "Information":
                    minLogLevel = LogLevel.Information;
                    break;
                case "Warning":
                    minLogLevel = LogLevel.Warning;
                    break;
                case "Error":
                    minLogLevel = LogLevel.Error;
                    break;
                case "Critical":
                    minLogLevel = LogLevel.Critical;
                    break;
            }
            return CreateLogger(tag, minLogLevel);
        }
        
        // create a simple colored logger with tag name, accept min log level, cache it for later use
        public static ILogger CreateLogger(string tag, LogLevel minLogLevel)
        {
            if (Loggers.ContainsKey(tag)) return Loggers[tag];
            var logger = LoggerFactory.Create(builder =>
            {
                builder.AddSimpleConsole(options =>
                {
                    options.IncludeScopes = true;
                    options.SingleLine = false;
                    options.TimestampFormat = "[HH:mm:ss] ";
                    options.UseUtcTimestamp = false;
                    options.ColorBehavior = LoggerColorBehavior.Enabled;
                }).SetMinimumLevel(minLogLevel);
            }).CreateLogger(tag);
            Loggers[tag] = logger;
            return logger;
        }
    }
}