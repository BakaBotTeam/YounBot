using YounBot.WynnCraftAPI4CSharp.Model.Player.Character;
using YounBot.WynnCraftAPI4CSharp.Model.Player.Global;

namespace YounBot.WynnCraftAPI4CSharp.Model.Player;

public class Player
    {
        public string uuid { get; set; }
        public string username { get; set; }
        public bool online { get; set; }
        public string server { get; set; }
        public double playtime { get; set; }
        public DateTime firstJoin { get; set; }
        public DateTime lastJoin { get; set; }
        public string rank { get; set; }
        public string shortenedRank { get; set; }
        public string supportRank { get; set; }
        public string rankBadge { get; set; }
        public PlayerLegacyRankColour legacyRankColour { get; set; }
        public PlayerGuild guild { get; set; }
        public PlayerGlobalData globalData { get; set; }
        public Dictionary<string, int> ranking { get; set; }
        public bool? veteran { get; set; }
        public bool? publicProfile { get; set; }
        public int? forumLink { get; set; }
        public string activeCharacter { get; set; }
        public Dictionary<Guid, PlayerCharacter> characters { get; set; }

        public string GetFormattedranking(string type)
        {
            if (ranking == null || !ranking.ContainsKey(type))
                return "N/A";

            return $"#{(ranking[type] + 1):N0}";
        }

        public PlayerRank GetPlayerRank()
        {
            return PlayerRankExtension.FromString(rank == "Player" && supportRank != null ? supportRank : rank);
        }
    }