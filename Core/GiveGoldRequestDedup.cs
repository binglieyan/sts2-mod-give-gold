#nullable enable

using System.Collections.Concurrent;
using System.Threading;

namespace GiveGold.Core;

internal static class GiveGoldRequestDedup
{
    private static readonly ConcurrentDictionary<string, byte> _processedIds = new();
    private static readonly ConcurrentQueue<string> _processedIdQueue = new();
    private static readonly Lock _lock = new();
    private const int MaxProcessedIds = 1024;

    public static bool TryAdd(string requestId)
    {
        lock (_lock)
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
    }

    public static void Clear()
    {
        lock (_lock)
        {
            _processedIds.Clear();
            while (_processedIdQueue.TryDequeue(out _)) { }
        }
    }
}