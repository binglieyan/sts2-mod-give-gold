#nullable enable

using System.Collections.Concurrent;

namespace GiveGold.Core;

internal static class GiveGoldRequestDedup
{
    private static readonly ConcurrentDictionary<string, byte> _processedIds = new();
    private static readonly ConcurrentQueue<string> _processedIdQueue = new();
    private const int MaxProcessedIds = 1000;

    public static bool TryAdd(string requestId)
    {
        if (!_processedIds.TryAdd(requestId, 0))
            return false;

        _processedIdQueue.Enqueue(requestId);

        while (_processedIdQueue.Count > MaxProcessedIds)
        {
            if (_processedIdQueue.TryDequeue(out string? oldId))
                _processedIds.TryRemove(oldId, out _);
        }

        return true;
    }

    public static void Clear()
    {
        _processedIds.Clear();
        while (_processedIdQueue.TryDequeue(out _)) { }
    }
}