using System.Collections.Generic;
using JoksterCube.ProtectSpawnerFarms.Common;
using JoksterCube.ProtectSpawnerFarms.Common.Networking;
using JoksterCube.ProtectSpawnerFarms.Settings;
using static JoksterCube.ProtectSpawnerFarms.Settings.Constants;

namespace JoksterCube.ProtectSpawnerFarms.Domain.Networking;

// Shares protection records between everyone with the mod.
// - Server with the mod: it keeps the list. Players send it their changes; it sends everyone the list and each
//   change. Only players whose version matched the server's take part.
// - Server without the mod: players with the mod send each other their lists through the server, which only
//   passes the messages on. Lists are merged so the newest change of each spawner wins. Messages carry the mod
//   version, and players with another version ignore them. Players without the mod never react to them.
internal static class ProtectionSync
{
    private const float ListRequestRetry = 10f;

    private enum ServerMessage
    {
        FullList = 0,
        Update = 1
    }

    private enum PlayerMessage
    {
        // Sent on joining: here is my list, please send me yours.
        Hello = 0,
        // The answer to Hello.
        Reply = 1,
        Update = 2
    }

    private static float _nextListRequest;
    private static bool _helloSent;

    internal static void Register(ZRoutedRpc rpc)
    {
        rpc.Register<ZPackage>(Rpcs.RequestList, RPC_RequestList);
        rpc.Register<ZPackage>(Rpcs.SetProtected, RPC_SetProtected);
        rpc.Register<ZPackage>(Rpcs.Touch, RPC_Touch);
        rpc.Register<ZPackage>(Rpcs.ProtectedList, RPC_ProtectedList);
        rpc.Register<ZPackage>(Rpcs.Refused, RPC_Refused);
        rpc.Register<ZPackage>(Rpcs.PlayerRecords, RPC_PlayerRecords);
    }

    internal static void ClearSession()
    {
        _nextListRequest = 0f;
        _helloSent = false;
    }

    internal static void Update(float now)
    {
        if (ZRoutedRpc.instance == null || !Player.m_localPlayer) return;
        switch (ProtectionStore.Mode)
        {
            case ShareMode.Server when !ProtectionStore.SharedListReceived && now >= _nextListRequest:
                _nextListRequest = now + ListRequestRetry;
                ZRoutedRpc.instance.InvokeRoutedRPC(Rpcs.RequestList, new ZPackage());
                break;
            case ShareMode.Players when !_helloSent:
                _helloSent = true;
                SendToPlayers(ZRoutedRpc.Everybody, PlayerMessage.Hello, ProtectionStore.LocalLines());
                break;
        }
    }

    // Protects or unprotects a spawner, on whichever list this game uses.
    // Returns false when this game already knows the change isn't allowed (and says why). On a server with the mod
    // the server decides, and tells the player if it refuses.
    internal static bool RequestSet(string key, bool isProtected)
    {
        if (ProtectionStore.Mode == ShareMode.Server)
        {
            ZPackage package = new();
            package.Write(key);
            package.Write(isProtected);
            ZRoutedRpc.instance.InvokeRoutedRPC(Rpcs.SetProtected, package);
            return true;
        }

        string me = ProtectionStore.LocalPlayerName;
        string myId = ProtectionStore.LocalPlayerId;
        bool isAdmin = ZNet.instance && ZNet.instance.LocalPlayerIsAdminOrHost();
        ProtectionPermissions.Verdict verdict = Check(key, isProtected, myId, isAdmin, out string protector);
        if (verdict != ProtectionPermissions.Verdict.Allowed)
        {
            ShowRefusal(verdict, protector);
            return false;
        }
        Shared(ProtectionStore.SetLocal(key, isProtected, me, myId));
        return true;
    }

    private static ProtectionPermissions.Verdict Check(string key, bool protect, string requesterId, bool isAdmin, out string protector)
    {
        protector = string.Empty;
        string protectorId = string.Empty;
        if (ProtectionStore.Current.TryGet(key, out SpawnerRecord record) && record.IsProtected)
        {
            protector = record.ProtectedBy;
            protectorId = record.ProtectedById;
        }
        return ProtectionPermissions.Check(PluginConfig.WhoCanChange.Value, protect, protectorId, requesterId, isAdmin);
    }

    private static void ShowRefusal(ProtectionPermissions.Verdict verdict, string protector) =>
        PlayerMessages.Warning(verdict == ProtectionPermissions.Verdict.ProtectorOnly
            ? string.Format(Messages.RefusedProtectorOnly, string.IsNullOrEmpty(protector) ? Messages.CompendiumUnknownPlayer : protector)
            : Messages.RefusedAdminsOnly);

    // Notes that this game guarded a protected spawner or blocked a hit on it.
    internal static void Touch(string key, TouchKind kind)
    {
        switch (ProtectionStore.Mode)
        {
            case ShareMode.Server:
                ZPackage package = new();
                package.Write(key);
                package.Write((int)kind);
                ZRoutedRpc.instance.InvokeRoutedRPC(Rpcs.Touch, package);
                break;
            default:
                Shared(ProtectionStore.TouchLocal(key, kind, ProtectionStore.LocalPlayerName));
                break;
        }
    }

    // Passes on a change made to this game's own list.
    private static void Shared(SpawnerRecord? changed)
    {
        if (changed == null || ZRoutedRpc.instance == null) return;
        List<string> lines = [SpawnerRecords.Format(changed)];
        if (ProtectionStore.Mode == ShareMode.Host) SendToValidatedPeers(ServerMessage.Update, lines);
        else if (ProtectionStore.Mode == ShareMode.Players) SendToPlayers(ZRoutedRpc.Everybody, PlayerMessage.Update, lines);
    }

    // ---- Server with the mod ----

    private static void RPC_RequestList(long sender, ZPackage _)
    {
        if (!IsServer() || !IsValidatedPeer(sender, out ZNetPeer? _)) return;
        ZRoutedRpc.instance.InvokeRoutedRPC(sender, Rpcs.ProtectedList, ServerPackage(ServerMessage.FullList, ProtectionStore.LocalLines()));
    }

    private static void RPC_SetProtected(long sender, ZPackage package)
    {
        if (!IsServer() || !IsValidatedPeer(sender, out ZNetPeer? peer)) return;
        string key = package.ReadString();
        bool isProtected = package.ReadBool();

        string peerId = peer!.m_rpc.GetPeerHostName();
        ProtectionPermissions.Verdict verdict = Check(key, isProtected, peerId, IsAdmin(peer), out string protector);
        if (verdict != ProtectionPermissions.Verdict.Allowed)
        {
            Plugin.ModLogger.LogInfo($"Refused a protection change from {peer.m_playerName} ({verdict}).");
            ZPackage refusal = new();
            refusal.Write((int)verdict);
            refusal.Write(protector);
            ZRoutedRpc.instance.InvokeRoutedRPC(sender, Rpcs.Refused, refusal);
            return;
        }

        SpawnerRecord? changed = ProtectionStore.SetLocal(key, isProtected, peer.m_playerName, peerId);
        if (changed == null) return;
        Plugin.ModLogger.LogInfo($"{peer.m_playerName} {(isProtected ? "protected" : "unprotected")} {key}.");
        Shared(changed);
    }

    private static void RPC_Touch(long sender, ZPackage package)
    {
        if (!IsServer() || !IsValidatedPeer(sender, out ZNetPeer? peer)) return;
        string key = package.ReadString();
        TouchKind kind = (TouchKind)package.ReadInt();
        Shared(ProtectionStore.TouchLocal(key, kind, peer!.m_playerName));
    }

    private static void RPC_ProtectedList(long sender, ZPackage package)
    {
        if (!ZNet.instance || ZNet.instance.IsServer() || sender != ZRoutedRpc.instance.GetServerPeerID()) return;
        ServerMessage kind = (ServerMessage)package.ReadInt();
        ProtectionStore.ReceiveShared(ReadRecords(package), kind == ServerMessage.FullList);
    }

    private static void RPC_Refused(long sender, ZPackage package)
    {
        if (!ZNet.instance || ZNet.instance.IsServer() || sender != ZRoutedRpc.instance.GetServerPeerID()) return;
        ShowRefusal((ProtectionPermissions.Verdict)package.ReadInt(), package.ReadString());
    }

    private static void SendToValidatedPeers(ServerMessage kind, List<string> lines)
    {
        ZPackage package = ServerPackage(kind, lines);
        foreach (ZNetPeer peer in ZNet.instance.GetPeers())
            if (peer.m_uid != 0 && RpcHandlers.ValidatedPeers.Contains(peer.m_rpc))
                ZRoutedRpc.instance.InvokeRoutedRPC(peer.m_uid, Rpcs.ProtectedList, package);
    }

    private static ZPackage ServerPackage(ServerMessage kind, List<string> lines)
    {
        ZPackage package = new();
        package.Write((int)kind);
        WriteLines(package, lines);
        return package;
    }

    // ---- Server without the mod: players share with each other ----

    private static void RPC_PlayerRecords(long sender, ZPackage package)
    {
        if (ProtectionStore.Mode != ShareMode.Players || sender == ZRoutedRpc.instance.m_id) return;
        // Another version may store or mean things differently.
        if (package.ReadString() != Constants.Plugin.ModVersion) return;
        PlayerMessage kind = (PlayerMessage)package.ReadInt();

        List<SpawnerRecord> changed = ProtectionStore.MergeLocal(ReadRecords(package));
        if (changed.Count > 0)
            Plugin.ModLogger.LogInfo($"Merged {changed.Count} spawner protection changes from another player.");

        if (kind == PlayerMessage.Hello)
            SendToPlayers(sender, PlayerMessage.Reply, ProtectionStore.LocalLines());
    }

    private static void SendToPlayers(long target, PlayerMessage kind, List<string> lines)
    {
        if (kind == PlayerMessage.Reply && lines.Count == 0) return;
        ZPackage package = new();
        package.Write(Constants.Plugin.ModVersion);
        package.Write((int)kind);
        WriteLines(package, lines);
        ZRoutedRpc.instance.InvokeRoutedRPC(target, Rpcs.PlayerRecords, package);
    }

    // ---- Shared helpers ----

    private static void WriteLines(ZPackage package, List<string> lines)
    {
        package.Write(lines.Count);
        foreach (string line in lines) package.Write(line);
    }

    private static List<SpawnerRecord> ReadRecords(ZPackage package)
    {
        int count = package.ReadInt();
        List<SpawnerRecord> records = new(count);
        for (int index = 0; index < count; index++)
        {
            SpawnerRecord? record = SpawnerRecords.Parse(package.ReadString());
            if (record != null) records.Add(record);
        }
        return records;
    }

    private static bool IsServer() => ZNet.instance && ZNet.instance.IsServer();

    private static bool IsValidatedPeer(long sender, out ZNetPeer? peer)
    {
        peer = ZNet.instance.GetPeer(sender);
        return peer != null && RpcHandlers.ValidatedPeers.Contains(peer.m_rpc);
    }

    private static bool IsAdmin(ZNetPeer peer) =>
        ZNet.instance.ListContainsId(ZNet.instance.m_adminList, peer.m_rpc.GetPeerHostName());
}
