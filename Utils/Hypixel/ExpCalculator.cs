using System;

namespace YounBot.Utils.Hypixel;

public static class ExpCalculator
{
    private const double Base = 10000.0;
    private const double Growth = 2500.0;

    /* Constants to generate the total amount of XP to complete a level */
    private static readonly double HalfGrowth = 0.5 * Growth;

    /* Constants to look up the level from the total amount of XP */
    private static readonly double ReversePqPrefix = -(Base - 0.5 * Growth) / Growth;
    private static readonly double ReverseConst = ReversePqPrefix * ReversePqPrefix;
    private static readonly double GrowthDivides2 = 2 / Growth;

    public static double GetExactLevel(long exp)
    {
        return GetLevel(exp) + GetPercentageToNextLevel(exp);
    }

    private static double GetTotalExpToFullLevel(double level)
    {
        return (HalfGrowth * (level - 2) + Base) * (level - 1);
    }

    private static double GetTotalExpToLevel(double level)
    {
        var lv = Math.Floor(level);
        var x0 = GetTotalExpToFullLevel(lv);
        return level == lv ? x0 : (GetTotalExpToFullLevel(lv + 1) - x0) * (level % 1) + x0;
    }

    private static double GetPercentageToNextLevel(long exp)
    {
        var lv = GetLevel(exp);
        var x0 = GetTotalExpToLevel(lv);
        return (exp - x0) / (GetTotalExpToLevel(lv + 1) - x0);
    }

    private static double GetLevel(long exp)
    {
        if (exp < 0) return 1.0;
        return Math.Floor(1 + ReversePqPrefix + Math.Sqrt(ReverseConst + GrowthDivides2 * exp));
    }

    public static double GetBedWarsLevel(int exp)
    {
        double experience = exp;
        var level = 100 * (int)(experience / 487000);
        experience %= 487000;
        if (experience < 500) return level + experience / 500;
        level++;
        if (experience < 1500) return level + (experience - 500) / 1000;
        level++;
        if (experience < 3500) return level + (experience - 1500) / 2000;
        level++;
        if (experience < 7000) return level + (experience - 3500) / 3500;
        level++;
        experience -= 7000.0;
        return level + experience / 5000;
    }

    public static double GetSkyWarsLevel(int xp)
    {
        int[] xps = { 0, 20, 70, 150, 250, 500, 1000, 2000, 3500, 6000, 10000, 15000 };
        double experience = xp;
        if (experience >= 15000)
            return (experience - 15000) / 10000 + 12;
        for (var i = 0; i < xps.Length; i++)
            if (experience < xps[i])
                return i + (experience - xps[i - 1]) / (xps[i] - xps[i - 1]);
        return 1.0;
    }
}