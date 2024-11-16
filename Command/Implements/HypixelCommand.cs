using System;
using System.Linq;
using System.Threading.Tasks;
using Lagrange.Core;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Message;
using YounBot.Utils;
using YounBot.Utils.Hypixel;
using static YounBot.Utils.MessageUtils;

namespace YounBot.Command.Implements;

public class HypixelCommand
{
    private readonly CooldownUtils _cooldown = new(1000);

    [Command("hypixel", "Hypixel玩家信息")]
    public async Task Hypixel(BotContext context, MessageChain chain, string name)
    {
        var user = chain.FriendUin;
        if (!_cooldown.IsTimePassed(user!))
        {
            if (_cooldown.ShouldSendCooldownNotice(user))
                await SendMessage(context, chain, $"你可以在 {_cooldown.GetLeftTime(user) / 1000} 秒后继续使用该指令");
            return;
        }

        var uuid = await MojangApiUtils.GetUuidByName(name);
        uuid = uuid.Replace("\"", "");

        _cooldown.Flag(user);


        var result = await HypixelApiUtils.RequestAsync($"/player?uuid={uuid}");
        var playerInfo = result["player"]!.AsObject();
        string? rank;
        if (playerInfo.ContainsKey("rank") && !playerInfo["rank"]!.GetValue<string>().Equals("NORMAL"))
            rank = HypixelApiUtils.ResolveRank(playerInfo["rank"]!.GetValue<string>());
        else if (playerInfo.ContainsKey("monthlyPackageRank") &&
                 !playerInfo["monthlyPackageRank"]!.GetValue<string>().Equals("NONE"))
            rank = HypixelApiUtils.ResolveRank(playerInfo["monthlyPackageRank"]!.GetValue<string>());
        else if (playerInfo.ContainsKey("newPackageRank") &&
                 !playerInfo["newPackageRank"]!.GetValue<string>().Equals("NONE"))
            rank = HypixelApiUtils.ResolveRank(playerInfo["newPackageRank"]!.GetValue<string>());
        else
            rank = null;

        double level;

        try
        {
            level = Math.Round(
                ExpCalculator.GetExactLevel((long)playerInfo["networkExp"]!.GetValue<double>()) * 100) / 100.0;
        }
        catch (Exception)
        {
            level = 1.0;
        }

        var firstLogin = playerInfo.ConvertDate("firstLogin");
        var lastLogin = playerInfo.ConvertDate("lastLogin");
        var lastLogout = playerInfo.ConvertDate("lastLogout");

        var statusInfo = HypixelApiUtils.RequestAsync($"/status?uuid={uuid}").Result["session"]!.AsObject();
        string stringOnlineStatus;
        if (!statusInfo["online"]!.GetValue<bool>())
            stringOnlineStatus = "离线";
        else
            stringOnlineStatus =
                "在线 -> " + HypixelApiUtils.ResolveGameType(statusInfo.GetString("gameType", "Lobby?"));

        var playerName = (rank != null ? $"[{rank}]" : "") + $"{name}\n";

        var basicBuilder = new MessageChain[]{MessageBuilder.Group(chain.GroupUin!.Value).Text(
            "Hypixel 玩家数据:\n" +
            $"玩家名: {playerName}\n" +
            $"等级: {level}\n" +
            $"Karma: {playerInfo.GetIntOrNull("karma")}\n" +
            $"玩家使用语言: {playerInfo.GetString("userLanguage", "查询失败")}\n" +
            $"首次登入: {firstLogin}\n" +
            $"上次登入: {lastLogin}\n" +
            $"上次登出: {lastLogout}\n" +
            $"最近常玩: {HypixelApiUtils.ResolveGameType(playerInfo.GetStringOrNull("mostRecentGameType"))}\n" +
            $"当前状态: {stringOnlineStatus}").Time(DateTime.Now).Build()};
        
        if (playerInfo.ContainsKey("stats"))
        {
            var playerStats = playerInfo.GetObject("stats");
            if (playerStats.ContainsKey("Bedwars"))
            {
                var bwStats = playerStats.GetObject("Bedwars");
                basicBuilder = basicBuilder.Append(MessageBuilder.Group(chain.GroupUin!.Value).Text("Bedwars 信息:\n" +
                    $"等级: {Math.Round(ExpCalculator.GetBedWarsLevel(bwStats.GetIntOrNull("Experience")) * 100) / 100.0}\n" +
                    $"硬币: {bwStats.GetIntOrNull("coins")}\n" +
                    $"毁床数: {bwStats.GetIntOrNull("beds_broken_bedwars")}\n" +
                    $"总游戏数: {bwStats.GetIntOrNull("games_played_bedwars")}\n" +
                    $"胜利/失败: {bwStats.GetIntOrNull("wins_bedwars")}/{bwStats.GetIntOrNull("losses_bedwars")} " +
                    $"WLR: {CalculatorR(bwStats.GetIntOrNull("wins_bedwars"), bwStats.GetIntOrNull("losses_bedwars"))}\n" +
                    $"击杀/死亡: {bwStats.GetIntOrNull("kills_bedwars") + bwStats.GetIntOrNull("final_kills_bedwars")}/{
                        bwStats.GetIntOrNull(
                            "deaths_bedwars"
                        )
                    } " +
                    $"KDR: {
                        CalculatorR(
                            bwStats.GetIntOrNull("kills_bedwars") + bwStats.GetIntOrNull("final_kills_bedwars"),
                            bwStats.GetIntOrNull("deaths_bedwars")
                        )
                    }\n" +
                    $"最终击杀数: {bwStats.GetIntOrNull("final_kills_bedwars")}").Time(DateTime.Now).Build()).ToArray();
            }

            if (playerStats.ContainsKey("SkyWars"))
            {
                var swStats = playerStats.GetObject("SkyWars");
                basicBuilder = basicBuilder.Append(MessageBuilder.Group(chain.GroupUin!.Value).Text("Skywars 信息:\n" +
                    $"硬币: {swStats.GetIntOrNull("coins")}\n" +
                    $"灵魂数量: {swStats.GetIntOrNull("souls")}\n" +
                    $"总游戏数: {swStats.GetIntOrNull("games_played_skywars")}\n" +
                    $"胜利/失败: {swStats.GetIntOrNull("wins")}/{swStats.GetIntOrNull("losses")} " +
                    $"WLR: {CalculatorR(swStats.GetIntOrNull("wins"), swStats.GetIntOrNull("losses"))}\n" +
                    $"击杀/助攻/死亡: {swStats.GetIntOrNull("kills")}/{swStats.GetIntOrNull("assists")}/{swStats.GetIntOrNull("deaths")} " +
                    $"KDR: {CalculatorR(swStats.GetIntOrNull("kills"), swStats.GetIntOrNull("deaths"))
                    }\n" +
                    "\n共计:\n" +
                    $"共有 {swStats.GetIntOrNull("heads")} 个 Heads, 放置了 {swStats.GetIntOrNull("blocks_placed")} 个方块, 打开了 {
                        swStats.GetIntOrNull(
                            "chests_opened"
                        )
                    } 个箱子"
                ).Time(DateTime.Now).Build()).ToArray();
            }
            
            if (playerStats.ContainsKey("Duels"))
            {
                var duelStats = playerStats.GetObject("Duels");
                basicBuilder = basicBuilder.Append(MessageBuilder.Group(chain.GroupUin!.Value).Text("Duel 信息:\n" +
                    $"硬币: {duelStats.GetIntOrNull("coins")}\n" +
                    $"总游戏数: {duelStats.GetIntOrNull("rounds_played")}\n" +
                    $"胜利/失败: {duelStats.GetIntOrNull("wins")}/{duelStats.GetIntOrNull("losses")} " +
                    $"WLR: {
                        CalculatorR(
                            duelStats.GetIntOrNull("wins"),
                            duelStats.GetIntOrNull("losses")
                        )
                    }\n" +
                    $"击杀/死亡: {duelStats.GetIntOrNull("kills")}/{duelStats.GetIntOrNull("deaths")} " +
                    $"KDR: {
                        CalculatorR(
                            duelStats.GetIntOrNull("kills"),
                            duelStats.GetIntOrNull("deaths")
                        )
                    }\n" +
                    $"近战命中: {
                        CalculatorR(
                            duelStats.GetIntOrNull("melee_hits"),
                            duelStats.GetIntOrNull("melee_swings")
                        )
                    }\n" +
                    $"弓箭命中: {
                        CalculatorR(
                            duelStats.GetIntOrNull("bow_hits"),
                            duelStats.GetIntOrNull("bow_shots")
                        )
                    }\n" +
                    "\n共计:\n" +
                    $"造成了 {duelStats.GetIntOrNull("damage_dealt")} 伤害, 恢复了 {
                        duelStats.GetIntOrNull(
                            "health_regenerated"
                        )
                    } 血量"
                ).Time(DateTime.Now).Build()).ToArray();
            }
        }


        await context.SendMessage(MessageBuilder.Group(chain.GroupUin!.Value).MultiMsg(chain.GroupUin!.Value, basicBuilder).Build());
    }

    public static double CalculatorR(int num1, int num2)
    {
        double result;
        try
        {
            result = Math.Round(num1 / (double)num2 * 100.0, 2);
        }
        catch (Exception)
        {
            result = num1;
        }

        return result;
    }
}