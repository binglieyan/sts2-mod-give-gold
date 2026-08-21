#nullable enable

using MegaCrit.Sts2.Core.Runs;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace GiveGold.Core;

/// <summary>
/// 跨版本 API 兼容层（旧 API 兜底）。
/// beta 把 RunLobby 从 MegaCrit.Sts2.Core.Runs 移到了
/// MegaCrit.Sts2.Core.Multiplayer.Game.Lobby，并把 ConnectedPlayerIds 改名为 PlayerIds。
/// 本层用反射读取，同一份 DLL 在 stable（旧字段）与 beta（新字段）上都能运行。
///
/// 【正式版合入新 API 后的迁移】
/// 1. 删除本文件；
/// 2. GiveGoldValidator.GetConnectedPlayerIds() 方法体改为直接调用新 API：
///        return RunManager.Instance?.RunLobby?.PlayerIds;
/// 3. 构建确认通过即可（所有调用点都走该方法，无需改动）。
///
/// 注意：调用点不能直接写 RunLobby?.PlayerIds —— 编译后的 IL 会引用 beta 独有的
/// RunLobby 类型标记，旧版游戏 JIT 到该表达式时会直接 TypeLoadException，兜底来不及生效。
/// 因此必须收敛到上面这一个方法。
/// </summary>
internal static class Sts2ApiCompat
{
    private const string RunLobbyPropertyName = "RunLobby";
    private const string PlayerIdsPropertyName = "PlayerIds"; // beta
    private const string ConnectedPlayerIdsPropertyName = "ConnectedPlayerIds"; // stable

    private const BindingFlags InstanceFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    private static PropertyInfo? _runLobbyProperty;
    private static PropertyInfo? _connectedIdsProperty;
    private static Type? _resolvedLobbyType;

    /// <summary>Returns the NetIds of the players in the current run lobby, or null if unresolvable.</summary>
    public static IEnumerable<ulong>? GetConnectedPlayerIds(RunManager? runManager)
    {
        if (runManager == null)
            return null;

        object? lobby = GetRunLobby(runManager);
        if (lobby == null)
            return null;

        PropertyInfo? connectedIdsProperty = ResolveConnectedIdsProperty(lobby.GetType());
        if (connectedIdsProperty == null)
        {
            GiveGoldInitializer.Logger.Warn(
                "GiveGold could not resolve the lobby player-ids API (PlayerIds / ConnectedPlayerIds).");
            return null;
        }

        try
        {
            return connectedIdsProperty.GetValue(lobby) as IEnumerable<ulong>;
        }
        catch (Exception ex)
        {
            GiveGoldInitializer.Logger.Error($"GiveGold failed to read lobby player ids: {ex}");
            return null;
        }
    }

    private static object? GetRunLobby(RunManager runManager)
    {
        // Accessed via reflection so the compiled assembly never references the RunLobby
        // type directly — its namespace moved between game versions.
        // NonPublic included: the publicizer only affects compile time; the real game
        // assembly may keep some members internal.
        PropertyInfo? property = _runLobbyProperty ??=
            typeof(RunManager).GetProperty(RunLobbyPropertyName, InstanceFlags);
        if (property == null)
            return null;

        try
        {
            return property.GetValue(runManager);
        }
        catch (Exception ex)
        {
            GiveGoldInitializer.Logger.Error($"GiveGold failed to read RunManager.RunLobby: {ex}");
            return null;
        }
    }

    private static PropertyInfo? ResolveConnectedIdsProperty(Type lobbyType)
    {
        if (_connectedIdsProperty != null && ReferenceEquals(_resolvedLobbyType, lobbyType))
            return _connectedIdsProperty;

        _resolvedLobbyType = lobbyType;
        _connectedIdsProperty = lobbyType.GetProperty(PlayerIdsPropertyName, InstanceFlags)
            ?? lobbyType.GetProperty(ConnectedPlayerIdsPropertyName, InstanceFlags);
        return _connectedIdsProperty;
    }
}
