using System;
using System.Collections.Generic;
using System.Linq;
using JoksterCube.ProtectSpawnerFarms.Common;
using JoksterCube.ProtectSpawnerFarms.Common.Networking;
using JoksterCube.ProtectSpawnerFarms.Settings;
using UnityEngine;
using static JoksterCube.ProtectSpawnerFarms.Settings.Constants;
using Spawner = JoksterCube.ProtectSpawnerFarms.Domain.SpawnerRegistry.Spawner;

namespace JoksterCube.ProtectSpawnerFarms.Domain.Networking;

// Protection is saved on each spawner (see SpawnerData), so players never send each other spawner data: whoever
// loads a spawner reads it. This only decides who writes, and shares the Compendium list:
// - Server with the mod: players ask it to protect, unprotect and note guarded or blocked hits, and the server
//   writes the spawner. It also sends players with the mod the list of every protected spawner in the world, and
//   each change to it. Only players whose version matched the server's take part.
// - Server without the mod (and single player or hosting): this game writes the spawner itself, taking ownership
//   of it first, the way the game does for anything a player changes.
internal static class ProtectionSync
{
    private const string Feature = "Protection sync";
    private const float ListRequestRetry = 10f;
    // How often the known protected spawners are read again for the Compendium.
    private const float RefreshInterval = 2f;

    private enum ServerMessage
    {
        FullList = 0,
        Update = 1
    }

    private static float _nextListRequest;
    private static float _nextRefresh;

    internal static void Register(ZRoutedRpc rpc)
    {
        rpc.Register<ZPackage>(Rpcs.RequestList, RPC_RequestList);
        rpc.Register<ZPackage>(Rpcs.SetProtected, RPC_SetProtected);
        rpc.Register<ZPackage>(Rpcs.Touch, RPC_Touch);
        rpc.Register<ZPackage>(Rpcs.ProtectedList, RPC_ProtectedList);
        rpc.Register<ZPackage>(Rpcs.Refused, RPC_Refused);
    }

    internal static void ClearSession()
    {
        _nextListRequest = 0f;
        _nextRefresh = 0f;
    }

    // Every frame, on servers and players.
    internal static void Update()
    {
        if (PatchGuard.IsDisabled(Feature) || ZRoutedRpc.instance == null) return;
        try
        {
            float now = Time.time;
            switch (ProtectedSpawners.Mode)
            {
                case ShareMode.Server when !ProtectedSpawners.ListReceived && Player.m_localPlayer && now >= _nextListRequest:
                    _nextListRequest = now + ListRequestRetry;
                    ZRoutedRpc.instance.InvokeRoutedRPC(Rpcs.RequestList, new ZPackage());
                    break;
                case ShareMode.Host or ShareMode.Players when now >= _nextRefresh:
                    _nextRefresh = now + RefreshInterval;
                    Shared(ProtectedSpawners.Refresh());
                    break;
            }
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    // Protects or unprotects a spawner. Returns false when this game already knows the change isn't allowed (and
    // says why). On a server with the mod the server decides, and tells the player if it refuses.
    internal static bool RequestSet(Spawner spawner, bool protect)
    {
        ZDO zdo = spawner.View.GetZDO();
        if (ProtectedSpawners.Mode == ShareMode.Server)
        {
            // The server's setting is synced here, so an obvious refusal is shown at once instead of after a
            // "now protected" message. The server still decides.
            if (PluginConfig.WhoCanChange.Value == ProtectionPermission.AdminsOnly && !ZNet.instance.LocalPlayerIsAdminOrHost())
            {
                ShowRefusal(ProtectionPermissions.Verdict.AdminsOnly, string.Empty);
                return false;
            }
            ZPackage package = new();
            package.Write(zdo.m_uid);
            package.Write(protect);
            ZRoutedRpc.instance.InvokeRoutedRPC(Rpcs.SetProtected, package);
            return true;
        }

        string myId = ProtectedSpawners.LocalPlayerId;
        bool isAdmin = ZNet.instance && ZNet.instance.LocalPlayerIsAdminOrHost();
        ProtectionPermissions.Verdict verdict = Check(zdo, protect, myId, isAdmin, out string protector);
        if (verdict != ProtectionPermissions.Verdict.Allowed)
        {
            ShowRefusal(verdict, protector);
            return false;
        }

        if (!spawner.View.IsOwner())
        {
            spawner.View.ClaimOwnership();
            spawner.ClaimedAt = Time.time;
        }
        SpawnerData.SetProtected(zdo, protect, ProtectedSpawners.LocalPlayerName, myId, ProtectedSpawners.Now);
        Shared(ProtectedSpawners.Note(zdo, spawner.Key));
        return true;
    }

    // Notes that this game guarded a protected spawner or blocked a hit on it. Without the mod on the server only
    // the spawner's owner writes it; a protecting player owns the spawners it guards.
    internal static void Touch(Spawner spawner, TouchKind kind)
    {
        ZDO zdo = spawner.View.GetZDO();
        if (ProtectedSpawners.Mode == ShareMode.Server)
        {
            ZPackage package = new();
            package.Write(zdo.m_uid);
            package.Write((int)kind);
            ZRoutedRpc.instance.InvokeRoutedRPC(Rpcs.Touch, package);
            return;
        }
        if (SpawnerData.CanWrite(zdo) && SpawnerData.Touch(zdo, kind, ProtectedSpawners.LocalPlayerName, ProtectedSpawners.Now))
            Shared(ProtectedSpawners.Note(zdo, spawner.Key));
    }

    private static ProtectionPermissions.Verdict Check(ZDO zdo, bool protect, string requesterId, bool isAdmin, out string protector)
    {
        bool isProtected = SpawnerData.IsProtected(zdo);
        protector = isProtected ? SpawnerData.ProtectedBy(zdo) : string.Empty;
        string protectorId = isProtected ? SpawnerData.ProtectedById(zdo) : string.Empty;
        return ProtectionPermissions.Check(PluginConfig.WhoCanChange.Value, protect, protectorId, requesterId, isAdmin);
    }

    private static void ShowRefusal(ProtectionPermissions.Verdict verdict, string protector) =>
        PlayerMessages.Warning(verdict == ProtectionPermissions.Verdict.ProtectorOnly
            ? string.Format(Messages.RefusedProtectorOnly, string.IsNullOrEmpty(protector) ? Messages.CompendiumUnknownPlayer : protector)
            : Messages.RefusedAdminsOnly);

    // Sends changes to the list to players with the mod, when this game is the server.
    private static void Shared(SpawnerRecord? change)
    {
        if (change != null) Shared([change]);
    }

    private static void Shared(List<SpawnerRecord> changes)
    {
        if (changes.Count == 0 || ProtectedSpawners.Mode != ShareMode.Host || ZRoutedRpc.instance == null) return;
        ZPackage package = ServerPackage(ServerMessage.Update, changes);
        foreach (ZNetPeer peer in ZNet.instance.GetPeers())
            if (peer.m_uid != 0 && RpcHandlers.ValidatedPeers.Contains(peer.m_rpc))
                ZRoutedRpc.instance.InvokeRoutedRPC(peer.m_uid, Rpcs.ProtectedList, package);
    }

    // ---- Server with the mod ----

    private static void RPC_RequestList(long sender, ZPackage _)
    {
        if (!IsServer() || !IsValidatedPeer(sender, out ZNetPeer? _)) return;
        ZRoutedRpc.instance.InvokeRoutedRPC(sender, Rpcs.ProtectedList, ServerPackage(ServerMessage.FullList, ProtectedSpawners.All.ToList()));
    }

    private static void RPC_SetProtected(long sender, ZPackage package)
    {
        if (!IsServer() || !IsValidatedPeer(sender, out ZNetPeer? peer)) return;
        ZDO? zdo = ZDOMan.instance.GetZDO(package.ReadZDOID());
        bool protect = package.ReadBool();
        if (zdo == null || SpawnerRegistry.KeyOf(zdo) is not { } key || SpawnerData.IsProtected(zdo) == protect) return;

        string peerId = peer!.m_rpc.GetPeerHostName();
        ProtectionPermissions.Verdict verdict = Check(zdo, protect, RequesterId(peer, zdo), IsAdmin(peer), out string protector);
        if (verdict != ProtectionPermissions.Verdict.Allowed)
        {
            Plugin.ModLogger.LogInfo($"Refused a protection change from {peer.m_playerName} ({verdict}).");
            ZPackage refusal = new();
            refusal.Write((int)verdict);
            refusal.Write(protector);
            ZRoutedRpc.instance.InvokeRoutedRPC(sender, Rpcs.Refused, refusal);
            return;
        }

        SpawnerData.SetProtected(zdo, protect, peer.m_playerName, peerId, ProtectedSpawners.Now);
        // The server sets the health too: its copy wins over the player's (see SpawnerData).
        if (protect) SpawnerProtection.RaiseHealth(zdo);
        else SpawnerProtection.RestoreHealth(zdo);
        Plugin.ModLogger.LogInfo($"{peer.m_playerName} {(protect ? "protected" : "unprotected")} {key}.");
        Shared(ProtectedSpawners.Note(zdo, key));
    }

    private static void RPC_Touch(long sender, ZPackage package)
    {
        if (!IsServer() || !IsValidatedPeer(sender, out ZNetPeer? peer)) return;
        ZDO? zdo = ZDOMan.instance.GetZDO(package.ReadZDOID());
        TouchKind kind = (TouchKind)package.ReadInt();
        if (zdo == null || SpawnerRegistry.KeyOf(zdo) is not { } key) return;
        if (SpawnerData.Touch(zdo, kind, peer!.m_playerName, ProtectedSpawners.Now))
            Shared(ProtectedSpawners.Note(zdo, key));
    }

    // Players keep their platform id here, but a spawner protected on a server without the mod has the character's
    // id; either one counts as the protector.
    private static string RequesterId(ZNetPeer peer, ZDO spawner)
    {
        string protectorId = SpawnerData.ProtectedById(spawner);
        ZDO? character = ZDOMan.instance.GetZDO(peer.m_characterID);
        long characterId = character != null ? character.GetLong(ZDOVars.s_playerID) : 0L;
        return characterId != 0L && protectorId == characterId.ToString(System.Globalization.CultureInfo.InvariantCulture)
            ? protectorId
            : peer.m_rpc.GetPeerHostName();
    }

    // ---- Players on a server with the mod ----

    private static void RPC_ProtectedList(long sender, ZPackage package)
    {
        if (!ZNet.instance || ZNet.instance.IsServer() || sender != ZRoutedRpc.instance.GetServerPeerID()) return;
        ServerMessage kind = (ServerMessage)package.ReadInt();
        ProtectedSpawners.Receive(ReadRecords(package), kind == ServerMessage.FullList);
    }

    private static void RPC_Refused(long sender, ZPackage package)
    {
        if (!ZNet.instance || ZNet.instance.IsServer() || sender != ZRoutedRpc.instance.GetServerPeerID()) return;
        ShowRefusal((ProtectionPermissions.Verdict)package.ReadInt(), package.ReadString());
    }

    // ---- Shared helpers ----

    private static ZPackage ServerPackage(ServerMessage kind, List<SpawnerRecord> records)
    {
        ZPackage package = new();
        package.Write((int)kind);
        package.Write(records.Count);
        foreach (SpawnerRecord record in records) package.Write(SpawnerRecords.Format(record));
        return package;
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
