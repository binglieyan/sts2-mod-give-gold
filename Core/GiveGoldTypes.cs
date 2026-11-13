#nullable enable

namespace GiveGold.Core;

/// <summary>公开 API 的返回值类型，因此保持 public（GiveGoldService 的方法签名暴露了它们）。</summary>
public static class GiveGoldTypes
{
    public readonly record struct GiveTarget(ulong NetId, string DisplayName);

    public readonly record struct GiveResult(bool Success, string Message);
}