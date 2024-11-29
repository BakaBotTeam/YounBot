using System.Text.Json.Nodes;
using Lagrange.Core;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Message;
using Microsoft.Extensions.Logging;
using YounBot.Utils;
using YounBot.Utils.Hypixel;

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
                await context.SendMessage(MessageBuilder.Group(chain.GroupUin!.Value).Text($"你可以在 {_cooldown.GetLeftTime(user!)/1000} 秒后再试").Build());
            return;
        }

        var uuid = (await MojangApiUtils.GetUuidByName(name)).Replace("\"", "");
        _cooldown.Flag(user);

        var playerInfo = (await HypixelApiUtils.RequestAsync($"/player?uuid={uuid}"))["player"]!.AsObject();
        var rank = GetRank(playerInfo);

        var level = GetLevel(playerInfo);
        var firstLogin = playerInfo.ConvertDate("firstLogin");
        var lastLogin = playerInfo.ConvertDate("lastLogin");
        var lastLogout = playerInfo.ConvertDate("lastLogout");

        var statusInfo = (await HypixelApiUtils.RequestAsync($"/status?uuid={uuid}"))["session"]!.AsObject();
        var stringOnlineStatus = statusInfo["online"]!.GetValue<bool>() 
            ? "在线 -> " + HypixelApiUtils.ResolveGameType(statusInfo.GetString("gameType", "Lobby?")) 
            : "离线";

        var playerName = (rank != null ? $"[{rank}]" : "") + $"{name}\n";
        var basicBuilder = new []
        {
            MessageBuilder.Group(chain.GroupUin!.Value).Text(
                "Hypixel 玩家数据:\n" +
                $"玩家名: {playerName}\n" +
                $"等级: {level}\n" +
                $"Karma: {playerInfo.GetIntOrNull("karma")}\n" +
                $"玩家使用语言: {playerInfo.GetString("userLanguage", "查询失败")}\n" +
                $"首次登入: {firstLogin}\n" +
                $"上次登入: {lastLogin}\n" +
                $"上次登出: {lastLogout}\n" +
                $"最近常玩: {HypixelApiUtils.ResolveGameType(playerInfo.GetStringOrNull("mostRecentGameType"))}\n" +
                $"当前状态: {stringOnlineStatus}"
            ).Time(DateTime.MaxValue).Build()
        };

        basicBuilder = AppendStats(basicBuilder, chain.GroupUin!.Value, playerInfo);

        await context.SendMessage(MessageBuilder.Group(chain.GroupUin!.Value).MultiMsg(basicBuilder).Build());
    }

    private static string? GetRank(JsonObject playerInfo)
    {
        if (playerInfo.ContainsKey("rank") && !playerInfo["rank"]!.GetValue<string>().Equals("NORMAL"))
            return HypixelApiUtils.ResolveRank(playerInfo["rank"]!.GetValue<string>());
        if (playerInfo.ContainsKey("monthlyPackageRank") && !playerInfo["monthlyPackageRank"]!.GetValue<string>().Equals("NONE"))
            return HypixelApiUtils.ResolveRank(playerInfo["monthlyPackageRank"]!.GetValue<string>());
        if (playerInfo.ContainsKey("newPackageRank") && !playerInfo["newPackageRank"]!.GetValue<string>().Equals("NONE"))
            return HypixelApiUtils.ResolveRank(playerInfo["newPackageRank"]!.GetValue<string>());
        return null;
    }

    private static double GetLevel(JsonObject playerInfo)
    {
        try
        {
            return Math.Round(ExpCalculator.GetExactLevel((long)playerInfo["networkExp"]!.GetValue<double>()) * 100) / 100.0;
        }
        catch (Exception)
        {
            return 1.0;
        }
    }

    private static MessageChain[] AppendStats(MessageChain[] basicBuilder, uint groupUin, JsonObject playerInfo)
    {
        if (playerInfo.ContainsKey("stats"))
        {
            var playerStats = playerInfo.GetObject("stats");
            var gameModes = new Dictionary<string, Func<MessageChain[], uint, JsonObject, MessageChain[]>>
            {
                { "Bedwars", AppendBedwarsStats },
                { "SkyWars", AppendSkywarsStats },
                { "Duels", AppendDuelsStats },
                { "Walls3", AppendMegaWallsStats },
                { "UHC", AppendUhcStats },
                { "Arcade", AppendArcadeStats },
                { "Pit", AppendPitStats }
            };

            foreach (var gameMode in gameModes)
            {
                try
                {
                    if (playerStats.ContainsKey(gameMode.Key))
                        basicBuilder = gameMode.Value(basicBuilder, groupUin, playerStats.GetObject(gameMode.Key));
                }
                catch (Exception e)
                {
                    LoggingUtils.Logger.LogWarning(e.ToString());
                }
            }
        }
        return basicBuilder;
    }

    private static MessageChain[] AppendBedwarsStats(MessageChain[] basicBuilder, uint groupUin, JsonObject bwStats)
    {
        return basicBuilder.Append(MessageBuilder.Group(groupUin).Text("Bedwars 信息:\n" +
            $"等级: {Math.Round(ExpCalculator.GetBedWarsLevel(bwStats.GetIntOrNull("Experience")) * 100) / 100.0}\n" +
            $"硬币: {bwStats.GetIntOrNull("coins")}\n" +
            $"毁床数: {bwStats.GetIntOrNull("beds_broken_bedwars")}\n" +
            $"总游戏数: {bwStats.GetIntOrNull("games_played_bedwars")}\n" +
            $"胜利/失败: {bwStats.GetIntOrNull("wins_bedwars")}/{bwStats.GetIntOrNull("losses_bedwars")} " +
            $"WLR: {CalculatorR(bwStats.GetIntOrNull("wins_bedwars"), bwStats.GetIntOrNull("losses_bedwars"))}\n" +
            $"击杀/死亡: {bwStats.GetIntOrNull("kills_bedwars") + bwStats.GetIntOrNull("final_kills_bedwars")}/{
                bwStats.GetIntOrNull("deaths_bedwars")
            } " +
            $"KDR: {
                CalculatorR(
                    bwStats.GetIntOrNull("kills_bedwars") + bwStats.GetIntOrNull("final_kills_bedwars"),
                    bwStats.GetIntOrNull("deaths_bedwars")
                )
            }\n" +
            $"最终击杀/死亡数: {bwStats.GetIntOrNull("final_kills_bedwars")}/{bwStats.GetIntOrNull("final_deaths_bedwars")} FKDR: {CalculatorR(bwStats.GetIntOrNull("final_kills_bedwars"), bwStats.GetIntOrNull("final_deaths_bedwars"))}").Time(DateTime.MaxValue).Build()).ToArray();
    }

    private static MessageChain[] AppendSkywarsStats(MessageChain[] basicBuilder, uint groupUin, JsonObject swStats)
    {
        return basicBuilder.Append(MessageBuilder.Group(groupUin).Text("Skywars 信息:\n" +
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
        ).Time(DateTime.MaxValue).Build()).ToArray();
    }

    private static MessageChain[] AppendDuelsStats(MessageChain[] basicBuilder, uint groupUin, JsonObject duelStats)
    {
        return basicBuilder.Append(MessageBuilder.Group(groupUin).Text("Duel 信息:\n" +
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
        ).Time(DateTime.MaxValue).Build()).ToArray();
    }

    private static MessageChain[] AppendMegaWallsStats(MessageChain[] basicBuilder, uint groupUin, JsonObject mwStats)
    {
        return basicBuilder.Append(MessageBuilder.Group(groupUin).Text("Mega Walls 信息:\n" +
            $"硬币: {mwStats.GetIntOrNull("coins")}\n" +
            $"胜利/失败: {mwStats.GetIntOrNull("wins")}/{mwStats.GetIntOrNull("losses")} " +
            $"WLR: {
                CalculatorR(
                    mwStats.GetIntOrNull("wins"),
                    mwStats.GetIntOrNull("losses")
                )
            }\n" +
            $"击杀/助攻/死亡: {mwStats.GetIntOrNull("kills") + mwStats.GetIntOrNull("final_kills")}/{mwStats.GetIntOrNull("assists")}/{
                mwStats.GetIntOrNull(
                    "deaths"
                ) + mwStats.GetIntOrNull("final_deaths")
            } " +
            $"KDR: {
                CalculatorR(
                    mwStats.GetIntOrNull("kills") + mwStats.GetIntOrNull("final_kills"),
                    mwStats.GetIntOrNull("deaths") + mwStats.GetIntOrNull("final_deaths")
                )
            }\n" +
            $"Final Kill/Death: {mwStats.GetIntOrNull("final_kills")}/{mwStats.GetIntOrNull("final_deaths")} FKDR: {CalculatorR(mwStats.GetIntOrNull("final_kills"), mwStats.GetIntOrNull("final_deaths"))}\n" +
            "\n共计:\n" +
            $"造成了 {mwStats.GetIntOrNull("damage_dealt")} 伤害, 共有 {
                mwStats["packages"]?.AsArray().Count
            } 个 Packages"
        ).Time(DateTime.MaxValue).Build()).ToArray();
    }

    private static MessageChain[] AppendUhcStats(MessageChain[] basicBuilder, uint groupUin, JsonObject uhcStats)
    {
        return basicBuilder.Append(MessageBuilder.Group(groupUin).Text("UHC 信息:\n" +
            $"硬币: {uhcStats.GetIntOrNull("coins")}\n" +
            $"已选择的职业 {uhcStats.GetStringOrNull("equippedKit")}\n" +
            $"胜利/失败: {uhcStats.GetIntOrNull("wins")}/{uhcStats.GetIntOrNull("deaths")} " +
            $"WLR: {CalculatorR(uhcStats.GetIntOrNull("wins"), uhcStats.GetIntOrNull("deaths"))}\n" +
            $"击杀/死亡: {uhcStats.GetIntOrNull("kills")}/{uhcStats.GetIntOrNull("deaths")} " +
            $"KDR: {CalculatorR(uhcStats.GetIntOrNull("kills"), uhcStats.GetIntOrNull("deaths"))}\n" +
            "\n共计:\n" +
            $"共有 {((JsonArray?)uhcStats["packages"])?.Count} 个合成配方").Time(DateTime.MaxValue).Build()).ToArray();
    }

    private static MessageChain[] AppendArcadeStats(MessageChain[] basicBuilder, uint groupUin, JsonObject arcadeStats)
    {
        basicBuilder = basicBuilder.Append(MessageBuilder.Group(groupUin).Text(
            "街机游戏 信息:\n" +
            $"硬币: {arcadeStats.GetIntOrNull("coins")}\n\n" +
            "以下为街机游戏:"
        ).Build()).ToArray();
        basicBuilder = basicBuilder.Append(MessageBuilder.Group(groupUin).Text(
            "Mini Walls:\n" +
            $"胜利: {arcadeStats.GetIntOrNull("wins_mini_walls")}\n" +
            $"击杀/死亡: {arcadeStats.GetIntOrNull("kills_mini_walls") + arcadeStats.GetIntOrNull("final_kills_mini_walls")}/{arcadeStats.GetIntOrNull("deaths_mini_walls")} " +
            $"KDR: {CalculatorR(arcadeStats.GetIntOrNull("kills_mini_walls") + arcadeStats.GetIntOrNull("final_kills_mini_walls"), arcadeStats.GetIntOrNull("deaths_mini_walls"))}\n" +
            $"最终击杀: {arcadeStats.GetIntOrNull("final_kills_mini_walls")}\n" +
            $"凋零击杀数: {arcadeStats.GetIntOrNull("wither_kills_mini_walls")}\n" +
            $"弓箭命中率: {CalculatorR(arcadeStats.GetIntOrNull("arrows_hit_mini_walls"), arcadeStats.GetIntOrNull("arrows_shot_mini_walls"))}"
        ).Build()).ToArray();
        basicBuilder = basicBuilder.Append(MessageBuilder.Group(groupUin).Text(
            $"派对游戏 胜场: {arcadeStats.GetIntOrNull("wins_party")}"
        ).Build()).ToArray();
        basicBuilder = basicBuilder.Append(MessageBuilder.Group(groupUin).Text(
            "僵尸末日: \n" +
            $"最高坚持了 {arcadeStats.GetIntOrNull("best_round_zombies")} 轮\n" +
            $"死亡次数: {arcadeStats.GetIntOrNull("deaths_zombies")}\n" +
            $"命中头部率: {CalculatorR(arcadeStats.GetIntOrNull("headshots_zombies"), arcadeStats.GetIntOrNull("bullets_shot_zombies"))}\n" +
            $"打开门数量: {arcadeStats.GetIntOrNull("doors_opened_zombies")}\n" +
            $"救起其他玩家次数: {arcadeStats.GetIntOrNull("players_revived_zombies")}"
        ).Build()).ToArray();
        return basicBuilder.Append(MessageBuilder.Group(groupUin).Text("以上为街机游戏").Build()).ToArray();
    }

    private static MessageChain[] AppendPitStats(MessageChain[] basicBuilder, uint groupUin, JsonObject pitStats)
    {
        foreach (var key in ((IDictionary<string, JsonNode?>)pitStats).Keys)
        {
            var pitProfileStats = pitStats[key]!.AsObject();
            if (key != "profile")
            {
                basicBuilder = basicBuilder.Append(MessageBuilder.Group(groupUin).Text(
                    $"天坑 ({key}): \n" +
                    $"进入次数: {pitProfileStats.GetIntOrNull("joins")} 跳入天坑次数: {pitProfileStats.GetIntOrNull("jumped_into_pit")}\n" +
                    $"击杀/助攻/死亡: {pitProfileStats.GetIntOrNull("kills")}/{pitProfileStats.GetIntOrNull("assists")}/{pitProfileStats.GetIntOrNull("deaths")} " +
                    $"KDR: {CalculatorR(pitProfileStats.GetIntOrNull("kills"), pitProfileStats.GetIntOrNull("deaths"))}\n" +
                    $"近战命中率: {CalculatorR(pitProfileStats.GetIntOrNull("sword_hits"), pitProfileStats.GetIntOrNull("left_clicks"))}\n" +
                    $"最高连杀: {pitProfileStats.GetIntOrNull("max_streak")}\n" +
                    $"总游玩时长: {pitProfileStats.GetIntOrNull("playtime_minutes")} 分钟").Build()).ToArray();
            }
        }
        return basicBuilder;
    }

    public static double CalculatorR(int num1, int num2)
    {
        try
        {
            return Math.Round(num1 / (double)num2, 2);
        }
        catch (Exception)
        {
            return num1;
        }
    }
}