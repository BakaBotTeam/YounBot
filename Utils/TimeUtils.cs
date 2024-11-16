using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace YounBot.Utils;
public static class TimeUtils
{
    public static string ConvertDate(long dateInMilliseconds)
    {
        try
        {
            var dateTime = DateTimeOffset.FromUnixTimeMilliseconds(dateInMilliseconds).ToOffset(TimeSpan.FromHours(8)).DateTime;

            return dateTime.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
        }
        catch (Exception exception)
        {
            return $"无法解析日期: {exception.Message}";
        }
    }
    
    public static TimeSpan ParseDuration(string input)
    {
        var regex = new Regex(@"((?<weeks>\d+)w)?((?<days>\d+)d)?((?<hours>\d+)h)?((?<minutes>\d+)m)?((?<seconds>\d+)s)?");
        var match = regex.Match(input);

        if (!match.Success)
        {
            throw new ArgumentException("Invalid duration format");
        }

        int weeks = match.Groups["weeks"].Success ? int.Parse(match.Groups["weeks"].Value) : 0;
        int days = match.Groups["days"].Success ? int.Parse(match.Groups["days"].Value) : 0;
        int hours = match.Groups["hours"].Success ? int.Parse(match.Groups["hours"].Value) : 0;
        int minutes = match.Groups["minutes"].Success ? int.Parse(match.Groups["minutes"].Value) : 0;
        int seconds = match.Groups["seconds"].Success ? int.Parse(match.Groups["seconds"].Value) : 0;

        return new TimeSpan(weeks * 7 + days, hours, minutes, seconds);
    }
}