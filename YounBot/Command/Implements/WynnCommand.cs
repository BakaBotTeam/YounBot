using Lagrange.Core;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Message;
using YounBot.WynnCraftAPI4CSharp;
using YounBot.WynnCraftAPI4CSharp.Model.Player;
using YounBot.WynnCraftAPI4CSharp.Model.Player.Character;
using YounBot.WynnCraftAPI4CSharp.Selection.Player;

namespace YounBot.Command.Implements;

public class WynnCommand
{
    private readonly WynnCraftApi _wynnCraftApi = new();
    [Command("wynncraft", "WynnCraft玩家信息")]
    public async Task WynnCraft(BotContext context, MessageChain chain, string name)
    {
        PlayerSelection? playerSelection = (await _wynnCraftApi.Player.GetPlayer(name))!;
        Player player = playerSelection.player!;
        
        PlayerRank rank = player.GetPlayerRank();
        bool online = player.online;
        DateTime firstJoin = player.firstJoin;
        DateTime lastJoin = player.lastJoin;
        double playtime = player.playtime;
        string server = player.server;
        string rankName = (rank == PlayerRank.UNKNOWN) ? "" : $"[{PlayerRankExtension.FromString(rank.ToString())}]";
        Dictionary<Guid,PlayerCharacter>? characters = playerSelection?.GetPlayer()?.characters!;
        MessageChain[] basicBuilder = new []
        {
            MessageBuilder.Group(chain.GroupUin!.Value).Text(
                "WynnCraft 玩家信息:\n" +
                $"玩家: {rankName} {name}\n" +
            $"首次上线: {firstJoin.ToString("yyyy/MM/dd HH:mm:ss")}\n" +
            $"上次上线: {lastJoin.ToString("yyyy/MM/dd HH:mm:ss")}\n" +
            $"总游玩时长: {playtime} 小时\n" +
            $"在线状态: {(online?"在线":"离线")}\n" +
            $"所在服务器: {server}"
            ).Time(DateTime.Now).Build()
        };

        foreach (KeyValuePair<Guid, PlayerCharacter> pair in characters)
        {
            PlayerCharacter character = pair.Value;
            string dungeonsInfo = "";
            string questsInfo = "";

            if (character.dungeons.list != null)
            {
                dungeonsInfo += $"地下城探索总数: {character.dungeons.total}\n";
                Console.WriteLine(character.dungeons.list);
                foreach (KeyValuePair<string, int> dungeon in character.dungeons.list)
                {
                    if (dungeonsInfo.Length <= 5)
                    {
                        dungeonsInfo += $"  * {dungeon}\n";
                    }
                    else
                    {
                        dungeonsInfo += "And more...\n";
                        break;
                    }
                }
            }

            if (character.quests != null)
            {
                questsInfo += $"已完成的任务 (总数: {character.quests.Length})\n";
                foreach (string quest in character.quests)
                {
                    if (questsInfo.Length <= 5)
                    {
                        questsInfo += $"  * {quest}\n";
                    }
                    else
                    {
                        questsInfo += "And more...\n";
                        break;
                    }
                }
            }

            string? uuid = playerSelection?.GetPlayer()?.characters.ToList()
                .Find(pair => playerSelection?.GetPlayer()?.characters[pair.Key] == character).Key.ToString();

            string text = "档案信息:\n" +
                          $"档案名称: {character.nickName ?? name}\n" +
                          $"档案 UUID: {uuid}\n" +
                          $"职业: {character.type}\n" +
                          $"等级: {character.level}\n" +
                          $"在线时长: {character.playtime} 小时\n" +
                          $"登入次数: {character.logins}\n" +
                          $"死亡次数: {character.deaths}\n" +
                          $"击杀生物数: {character.mobsKilled}\n" +
                          $"移动距离: {character.blocksWalked}b\n" +
                          $"已探索数量: {character.discoveries}\n" +
                          $"找到的箱子: {character.chestsFound}\n" +
                          "技能点信息:\n" +
                          $"  * Strength: {character.skillPoints.strength}\n" +
                          $"  * Dexterity: {character.skillPoints.dexterity}\n" +
                          $"  * Intelligence: {character.skillPoints.intelligence}\n" +
                          $"  * Defense: {character.skillPoints.defence}\n" +
                          $"  * Agility: {character.skillPoints.agility}\n" +
                          $"PVP: 击杀: {character.pvp.kills}, 死亡: {character.pvp.deaths}\n" +
                          $"专业等级:\n" +
                          $"  * Fishing: {character.professions.fishing.level}\n" +
                          $"  * Woodcutting: {character.professions.woodcutting.level}\n" +
                          $"  * Mining: {character.professions.mining.level}\n" +
                          $"  * Farming: {character.professions.farming.level}\n" +
                          $"  * Scribing: {character.professions.scribing.level}\n" +
                          $"  * Jeweling: {character.professions.jeweling.level}\n" +
                          $"  * Alchemism: {character.professions.alchemism.level}\n" +
                          $"  * Cooking: {character.professions.cooking.level}\n" +
                          $"  * Weaponsmithing: {character.professions.weaponsmithing.level}\n" +
                          $"  * Tailoring: {character.professions.tailoring.level}\n" +
                          $"  * Woodworking: {character.professions.woodworking.level}\n" +
                          $"  * Armouring: {character.professions.armouring.level}\n" +
                          dungeonsInfo +
                          questsInfo;
            basicBuilder = basicBuilder.Append(MessageBuilder.Group(chain.GroupUin!.Value).Text(text).Time(DateTime.MaxValue).Build()).ToArray();
        }
            
        await context.SendMessage(MessageBuilder.Group(chain.GroupUin!.Value).MultiMsg(basicBuilder).Build());
        
    }
}