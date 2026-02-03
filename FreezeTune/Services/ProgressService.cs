using System.Collections.Concurrent;

namespace FreezeTune.Services;

public class ProgressService
{
    private readonly ConcurrentDictionary<string, ProgressInfo> _progress = new();

    public void Update(string sessionId, int percent, string stage)
    {
        _progress[sessionId] = new ProgressInfo(percent, stage);
    }

    public ProgressInfo? Get(string sessionId)
    {
        return _progress.TryGetValue(sessionId, out var info) ? info : null;
    }

    public void Remove(string sessionId)
    {
        _progress.TryRemove(sessionId, out _);
    }
}

public record ProgressInfo(int Percent, string Stage);
