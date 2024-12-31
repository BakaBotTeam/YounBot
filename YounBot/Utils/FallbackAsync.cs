namespace YounBot.Utils;

public class FallbackAsync
{
    private readonly List<Func<CancellationToken, Task<bool>>> _executors = [];

    internal static FallbackAsync Create()
    {
        return new();
    }

    public FallbackAsync Add(Func<CancellationToken, Task<bool>> executor)
    {
        _executors.Add(executor);
        return this;
    }

    public async Task<bool> ExecuteAsync(CancellationToken token = default)
    {
        foreach (Func<CancellationToken, Task<bool>> executor in _executors)
        {
            if (await executor(token)) return true;
        }
        return false;
    }
}

public class FallbackAsync<T>
{
    private readonly List<Func<CancellationToken, Task<T?>>> _executors = [];

    internal static FallbackAsync<T> Create()
    {
        return new();
    }

    public FallbackAsync<T> Add(Func<CancellationToken, Task<T?>> executor)
    {
        _executors.Add(executor);
        return this;
    }

    public async Task<T> ExecuteAsync(Func<CancellationToken, Task<T>> @default, CancellationToken token = default)
    {
        foreach (Func<CancellationToken, Task<T?>> executor in _executors)
        {
            T? result = await executor(token);
            if (result != null) return result;
        }
        return await @default(token);
    }
}
