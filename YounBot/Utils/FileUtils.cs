﻿using System.Text.Json;

namespace YounBot.Utils;

public static class FileUtils
{
    public static void SaveConfig(string configName, object config, bool overwrite = false)
    {
        if (!File.Exists(configName) || overwrite)
        {
            string? directoryPath = Path.GetDirectoryName(configName);
            if (!string.IsNullOrEmpty(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            
            File.WriteAllText(configName, json);
        }
    }
}