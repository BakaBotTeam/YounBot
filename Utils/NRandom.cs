using System;

namespace YounBot.Utils;

public class NRandom(long seed)
{
    private long _seed = seed;

    public double NextDouble()
    {
        return (((long)Next(26) << 27) + Next(27)) * Math.Pow(2, -53);
    }

    public double NextDouble(double min, double max)
    {
        return NextDouble() * (max - min) + min;
    }

    public double NextGaussian()
    {
        var u = NextDouble();
        var v = NextDouble();
        return Math.Sqrt(-2 * Math.Log(u)) * Math.Cos(2 * Math.PI * v);
    }

    public double NextGaussian(double mean, double std)
    {
        return NextGaussian() * std + mean;
    }

    private int Next(int bits)
    {
        _seed = (_seed * 0x5DEECE66DL + 0xBL) & ((1L << 48) - 1);
        return (int)(_seed >>> (48 - bits));
    }
}