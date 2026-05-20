#nullable enable

namespace GiveGold.Core;

public static class GiveGoldTypes
{
    public readonly record struct GiveTarget(ulong NetId, string DisplayName);

    public readonly record struct GiveResult(bool Success, string Message);
}