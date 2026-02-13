using System.Text.RegularExpressions;
using Lagrange.Core;
using Lagrange.Core.Message;
using YounBot.Data;
using YounBot.Utils;

namespace YounBot.Listener;

public class QueryPlaceListener
{
    // regex pattern to match "地点数字"，地点不能以数字结尾
    private static readonly string AddPattern = @"^(.+?)(?<!\d)(\d+)$";
    // regex pattern to match "地点-数字"，地点不能以数字结尾
    private static readonly string SubtractPattern = @"^(.+?)(?<!\d)-(\d+)$";
    // regex pattern to match "地点+数字"，地点不能以数字结尾
    private static readonly string AddPattern2 = @"^(.+?)(?<!\d)\+(\d+)$";
    // regex pattern to match "地点++"，地点不能以数字结尾
    private static readonly string AddPattern3 = @"^(.+?)(?<!\d)\+\+$";
    // regex pattern to match "地点--"，地点不能以数字结尾
    private static readonly string SubtractPattern2 = @"^(.+?)(?<!\d)--$";
    // regex pattern to match "地点数字,数字,..."，地点不能以数字结尾
    private static readonly string MultiAddPattern = @"^(.+?)(?<!\d)(\d+(,\d+)*)$";
    // regex pattern to match "地点几/j"，地点不能以数字结尾
    private static readonly string QueryPattern = @"^(.+?)(?<!\d)(几|j)$";

    public static async Task OnGroupMessage(BotContext context, MessageChain chain)
    {
        if (chain.GroupUin == null || chain.GroupMemberInfo == null) return;
        string message = MessageUtils.GetPlainText(chain);
        Match match;
        if ((match = Regex.Match(message, SubtractPattern)).Success)
        {
            if (match.Value.Length != message.Length) return;
            string place = match.Groups[1].Value.Trim();
            short number = short.Parse(match.Groups[2].Value);
            QueryPlace? queryPlace = YounBotApp.QueryPlaceManager?.GetPlace(place, chain.GroupUin.Value);
            if (queryPlace == null) return;
            if (queryPlace.Count.Count == 0) queryPlace.Count.Add(0);
            short oldCount = queryPlace.Count[0];
            queryPlace.Count[0] = (short)Math.Max(0, queryPlace.Count[0] - number);
            YounBotApp.QueryPlaceManager?.UpdateCount(place, chain.GroupUin.Value, queryPlace.Count);
            string response = $"更新成功！";
            response += (queryPlace.Count[0] - oldCount) switch
            {
                > 0 => $"新增了 {queryPlace.Count[0] - oldCount} 卡",
                < 0 => $"减少了 {oldCount - queryPlace.Count[0]} 卡",
                _ => $"卡数未变化"
            };
            response += $"\n{queryPlace.Name}现在 {string.Join(", ", queryPlace.Count)} 卡";
            await MessageUtils.SendMessage(context, chain, response);
        }
        if ((match = Regex.Match(message, AddPattern2)).Success)
        {
            if (match.Value.Length != message.Length) return;
            string place = match.Groups[1].Value.Trim();
            short number = short.Parse(match.Groups[2].Value);
            QueryPlace? queryPlace = YounBotApp.QueryPlaceManager?.GetPlace(place, chain.GroupUin.Value);
            if (queryPlace == null) return;
            if (queryPlace.Count.Count == 0) queryPlace.Count.Add(0);
            short oldCount = queryPlace.Count[0];
            queryPlace.Count[0] += number;
            YounBotApp.QueryPlaceManager?.UpdateCount(place, chain.GroupUin.Value, queryPlace.Count);
            string response = $"更新成功！";
            response += (queryPlace.Count[0] - oldCount) switch
            {
                > 0 => $"新增了 {queryPlace.Count[0] - oldCount} 卡",
                < 0 => $"减少了 {oldCount - queryPlace.Count[0]} 卡",
                _ => $"卡数未变化"
            };
            response += $"\n{queryPlace.Name}现在 {string.Join(", ", queryPlace.Count)} 卡";
            await MessageUtils.SendMessage(context, chain, response);
        }
        if ((match = Regex.Match(message, AddPattern3)).Success)
        {
            if (match.Value.Length != message.Length) return;
            string place = match.Groups[1].Value.Trim();
            QueryPlace? queryPlace = YounBotApp.QueryPlaceManager?.GetPlace(place, chain.GroupUin.Value);
            if (queryPlace == null) return;
            if (queryPlace.Count.Count == 0) queryPlace.Count.Add(0);
            short oldCount = queryPlace.Count[0];
            queryPlace.Count[0] += 1;
            YounBotApp.QueryPlaceManager?.UpdateCount(place, chain.GroupUin.Value, queryPlace.Count);
            string response = $"更新成功！";
            response += (queryPlace.Count[0] - oldCount) switch
            {
                > 0 => $"新增了 {queryPlace.Count[0] - oldCount} 卡",
                < 0 => $"减少了 {oldCount - queryPlace.Count[0]} 卡",
                _ => $"卡数未变化"
            };
            response += $"\n{queryPlace.Name}现在 {string.Join(", ", queryPlace.Count)} 卡";
            await MessageUtils.SendMessage(context, chain, response);
        }
        if ((match = Regex.Match(message, SubtractPattern2)).Success)
        {
            if (match.Value.Length != message.Length) return;
            string place = match.Groups[1].Value.Trim();
            QueryPlace? queryPlace = YounBotApp.QueryPlaceManager?.GetPlace(place, chain.GroupUin.Value);
            if (queryPlace == null) return;
            if (queryPlace.Count.Count == 0) queryPlace.Count.Add(0);
            short oldCount = queryPlace.Count[0];
            queryPlace.Count[0] = (short)Math.Max(0, oldCount - 1);
            YounBotApp.QueryPlaceManager?.UpdateCount(place, chain.GroupUin.Value, queryPlace.Count);
            string response = $"更新成功！";
            response += (queryPlace.Count[0] - oldCount) switch
            {
                > 0 => $"新增了 {queryPlace.Count[0] - oldCount} 卡",
                < 0 => $"减少了 {oldCount - queryPlace.Count[0]} 卡",
                _ => $"卡数未变化"
            };
            response += $"\n{queryPlace.Name}现在 {string.Join(", ", queryPlace.Count)} 卡";
            await MessageUtils.SendMessage(context, chain, response);
        }
        if ((match = Regex.Match(message, MultiAddPattern)).Success)
        {
            if (match.Value.Length != message.Length) return;
            string place = match.Groups[1].Value.Trim();
            List<short> numbers = match.Groups[2].Value.Split(',').Select(short.Parse).ToList();
            QueryPlace? queryPlace = YounBotApp.QueryPlaceManager?.GetPlace(place, chain.GroupUin.Value);
            if (queryPlace == null) return;
            List<short> oldCount = queryPlace.Count.Count == 0 ? new List<short> { 0 } : new List<short>(queryPlace.Count);
            queryPlace.Count.Clear();
            queryPlace.Count.AddRange(numbers);
            YounBotApp.QueryPlaceManager?.UpdateCount(place, chain.GroupUin.Value, queryPlace.Count);
            string response = $"更新成功！";
            short oldTotal = (short)oldCount.Sum(c => c);
            short newTotal = (short)queryPlace.Count.Sum(c => c);
            response += (newTotal - oldTotal) switch
            {
                > 0 => $"新增了 {newTotal - oldTotal} 卡",
                < 0 => $"减少了 {oldTotal - newTotal} 卡",
                _ => $"卡数未变化"
            };
            response += $"\n{queryPlace.Name}现在 {string.Join(", ", queryPlace.Count)} 卡";
            await MessageUtils.SendMessage(context, chain, response);
        }
        if ((match = Regex.Match(message, QueryPattern)).Success)
        {
            if (match.Value.Length != message.Length) return;
            string place = match.Groups[1].Value.Trim();
            QueryPlace? queryPlace = YounBotApp.QueryPlaceManager?.GetPlace(place, chain.GroupUin.Value);
            if (queryPlace == null) return;
            string response = $"{queryPlace.Name}现在 {string.Join(", ", queryPlace.Count.Count == 0 ? new List<short> { 0 } : queryPlace.Count)} 卡";
            await MessageUtils.SendMessage(context, chain, response);
        } /*
        if ((match = Regex.Match(message, AddPattern)).Success)
        {
            if (match.Value.Length != message.Length) return;
            string place = match.Groups[1].Value.Trim();
            short number = short.Parse(match.Groups[2].Value);
            QueryPlace? queryPlace = YounBotApp.QueryPlaceManager?.GetPlace(place, chain.GroupUin.Value);
            if (queryPlace == null) return;
            if (queryPlace.Count.Count == 0) queryPlace.Count.Add(0);
            short oldCount = queryPlace.Count[0];
            queryPlace.Count[0] = number;
            YounBotApp.QueryPlaceManager?.UpdateCount(place, chain.GroupUin.Value, queryPlace.Count);
            string response = $"更新成功！";
            response += (queryPlace.Count[0] - oldCount) switch
            {
                > 0 => $"新增了 {queryPlace.Count[0] - oldCount} 卡",
                < 0 => $"减少了 {oldCount - queryPlace.Count[0]} 卡",
                _ => $"卡数未变化"
            };
            response += $"\n{queryPlace.Name}现在 {string.Join(", ", queryPlace.Count)} 卡";
            await MessageUtils.SendMessage(context, chain, response);
        } */
        if (message is "j" or "几")
        {
            List<QueryPlace> places = YounBotApp.QueryPlaceManager?.GetAllPlaces(chain.GroupUin.Value) ?? [];
            if (places.Count == 0) return;
            string response = "";
            short total = 0;
            foreach (QueryPlace place in places)
            {
                if (place.Count.Count == 0)
                {
                    response += $"{place.Name}: 今日未更新\n";
                }
                else
                {
                    response += $"{place.Name}: {string.Join(",", place.Count)} 卡({place.LastUpdated.Hour}:{place.LastUpdated.Minute}:{place.LastUpdated.Second})\n";
                    total += place.Count[0];
                }
            }
            response += $"出勤总人数: {total}\n";
            response += $"发送\"机厅名++\"加卡,\"机厅名--\"减卡,\"机厅名数字\"设置卡数";
            await MessageUtils.SendMessage(context, chain, response);
        }
    }
}