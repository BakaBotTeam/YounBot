using YounBot.WynnCraftAPI4CSharp.Model.Character;
using YounBot.WynnCraftAPI4CSharp.Model.Player.Global;

namespace YounBot.WynnCraftAPI4CSharp.Model.Player.Character;

public class PlayerCharacter
    {
        public CharacterType type { get; set; }
        public string nickName { get; set; }
        public int level { get; set; }
        public long xp { get; set; }
        public int xpPercent { get; set; }
        public int totalLevel { get; set; }
        public int wars { get; set; }
        public double playtime { get; set; }
        public int mobsKilled { get; set; }
        public int chestsFound { get; set; }
        public int blocksWalked { get; set; }
        public int? itemsIdentified { get; set; }
        public int logins { get; set; }
        public int? deaths { get; set; }
        public int discoveries { get; set; }
        public PlayerPvP pvp { get; set; }
        public string[] gamemode { get; set; }
        public PlayerSkillPoints skillPoints { get; set; } = new PlayerSkillPoints();
        public PlayerProfessions professions { get; set; }
        public PlayerDungeons dungeons { get; set; }
        public PlayerRaids raids { get; set; }
        public string[]? quests { get; set; }
    }