namespace YounBot.Utils;

public class CooldownUtils(long cooldown)
{
    private readonly Dictionary<object, long> _cooldownMap = new();
    private readonly Dictionary<object, long> _cooldownNoticeMap = new();

    public void Flag(object target)
    {
        _cooldownMap[target] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    public bool IsTimePassed(object target)
    {
        return IsTimePassed(target, cooldown);
    }

    public bool IsTimePassed(object target, long time)
    {
        if (!_cooldownMap.TryGetValue(target, out var t)) return true;
        if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - t >= time) return true;
        return false;
    }

    public bool ShouldSendCooldownNotice(object target)
    {
        if (!_cooldownNoticeMap.ContainsKey(target)) return true;
        if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - _cooldownNoticeMap[target] >= 3000)
        {
            _cooldownNoticeMap[target] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            return true;
        }
        return false;
    }

    public long GetLeftTime(object target)
    {
        return GetLeftTime(target, cooldown);
    }

    private long GetLeftTime(object target, long time)
    {
        if (!_cooldownMap.TryGetValue(target, out var t)) return -1;
        return time - (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - t);
    }

    public void AddLeftTime(object target, long time)
    {
        if (!_cooldownMap.ContainsKey(target)) return;
        _cooldownMap[target] += time;
    }
}