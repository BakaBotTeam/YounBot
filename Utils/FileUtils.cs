using System.Text.Json;

namespace YounBot.Utils;

public static class FileUtils
{
    public static void SaveConfig(string configName, object config, bool overwrite = false)
    {
        if (!File.Exists(configName) || overwrite)
        {
            var directoryPath = Path.GetDirectoryName(configName);
            if (!string.IsNullOrEmpty(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            var json = JsonSerializer.Serialize(config);
            
            File.WriteAllText(configName, json);
        }
    }
}