using System.Globalization;

namespace YounBot.Utils;
public static class TimeUtils
{
    public static string ConvertDate(long dateInMilliseconds)
    {
        try
        {
            DateTime dateTime = DateTimeOffset.FromUnixTimeMilliseconds(dateInMilliseconds).ToOffset(TimeSpan.FromHours(8)).DateTime;

            return dateTime.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
        }
        catch (Exception exception)
        {
            return $"无法解析日期: {exception.Message}";
        }
    }
}