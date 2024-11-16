using System;
using System.Text.Json.Nodes;

namespace YounBot.Utils;

public static class JsonUtils
{
    public static int GetIntOrNull(this JsonObject jsonObject, string key)
    {
        try
        {
            return jsonObject[key]!.GetValue<int>();
        }
        catch (Exception)
        {
            return 0;
        }
    }
    
    public static string GetString(this JsonObject jsonObject, string key, string defaultValue)
    {
        try
        {
            return jsonObject[key]!.GetValue<string>();
        }
        catch (Exception)
        {
            return defaultValue;
        }
    }
    
    public static JsonObject GetObject(this JsonObject jsonObject, string key) => jsonObject[key]!.AsObject();
    
    public static int GetInt(this JsonObject jsonObject, string key, int defaultValue)
    {
        try
        {
            return jsonObject[key]!.GetValue<int>();
        }
        catch (Exception)
        {
            return defaultValue;
        }
    }
    
    public static string GetStringOrNull(this JsonObject jsonObject, string key)
    {
        try
        {
            return jsonObject[key]!.GetValue<string>();
        }
        catch (Exception)
        {
            return "Null";
        }
    }
    
    public static string ConvertDate(this JsonObject jsonObject, string key)
    {
        try
        {
            return TimeUtils.ConvertDate(jsonObject[key]!.GetValue<long>());
        }
        catch (Exception)
        {
            return "无法获取";
        }
    }
}