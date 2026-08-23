using System.Collections.ObjectModel;
using System.Reflection;
using System.Reflection.Emit;
using Capy.Core.Enums;
using Capy.Engine.FakeSync;
using Exiled.API.Features.Doors;
using Exiled.API.Features.Items;
using Interactables.Interobjects.DoorUtils;
using MapGeneration.Distributors;
using Mirror;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.PlayableScps.Scp049.Zombies;
using PlayerRoles.PlayableScps.Scp1507;
using RelativePositioning;
using Respawning;
using UnityEngine;

namespace Capy.Core.Extensions;

public static class NetworkExtensions {
    private static readonly Lazy<ReadOnlyDictionary<string, string>> RpcFullNameCache =
        new(BuildRpcFullNameMap, LazyThreadSafetyMode.ExecutionAndPublication);

    private static readonly Lazy<ReadOnlyDictionary<Type, MethodInfo>> WriterExtensionCache =
        new(BuildWriterExtensionMap, LazyThreadSafetyMode.ExecutionAndPublication);

    public static ReadOnlyDictionary<string, string> RpcFullNames => RpcFullNameCache.Value;

    public static ReadOnlyDictionary<Type, MethodInfo> WriterExtensions => WriterExtensionCache.Value;

    private static int GetComponentIndex(NetworkIdentity identity, Type type) =>
        Array.FindIndex(identity.NetworkBehaviours, x => x.GetType() == type);

    private static ReadOnlyDictionary<string, string> BuildRpcFullNameMap() {
        var map = new Dictionary<string, string>();

        Assembly assembly = typeof(ServerConsole).Assembly;
        IEnumerable<MethodInfo> methods = assembly.GetTypes()
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            .Where(m => m.GetCustomAttributes(typeof(ClientRpcAttribute), false).Length > 0 ||
                        m.GetCustomAttributes(typeof(TargetRpcAttribute), false).Length > 0);

        foreach (MethodInfo method in methods) {
            if (method.GetMethodBody() is not { } body || method.DeclaringType is null)
                continue;

            try {
                byte[] ilCode = body.GetILAsByteArray();
                int index = Array.IndexOf(ilCode, (byte)OpCodes.Ldstr.Value);
                if (index < 0 || index + 5 > ilCode.Length)
                    continue;

                int token = BitConverter.ToInt32(ilCode, index + 1);
                string fullName = $"{method.DeclaringType.Name}.{method.Name}";
                if (!map.ContainsKey(fullName))
                    map.Add(fullName, method.Module.ResolveString(token));
            }
            catch {
                continue;
            }
        }

        return new ReadOnlyDictionary<string, string>(map);
    }

    public static bool HasGeneratorPermission(this Player player, Generator generator, DoorPermissionCheck checkFlags = DoorPermissionCheck.Default)
        => player.HasDoorPermission(generator.Base, checkFlags);
    
    public static bool HasLockerChamberPermission(this Player player, LockerChamber chamber, DoorPermissionCheck checkFlags = DoorPermissionCheck.Default)
        => chamber != null && player.HasDoorPermission(chamber.GetFieldValue<IDoorPermissionRequester>("_targetLocker"), checkFlags);
    
    public static bool HasDoorPermission(this Player player, Door door, DoorPermissionCheck checkFlags = DoorPermissionCheck.Default)
        => player.HasDoorPermission(door.Base, checkFlags);
    
    public static bool HasDoorPermission(this Player player, IDoorPermissionRequester requester, DoorPermissionCheck checkFlags = DoorPermissionCheck.Default) {
        if (checkFlags.HasFlag(DoorPermissionCheck.Bypass) && player.IsBypassModeEnabled)
            return true;

        if (checkFlags.HasFlag(DoorPermissionCheck.Role) && player.Role.Base is IDoorPermissionProvider roleProvider && requester.PermissionsPolicy.CheckPermissions(roleProvider.GetPermissions(requester)))
            return true;

        foreach (Item item in player.Items) {
            bool isCurrent = item == player.CurrentItem;
            if (!checkFlags.HasFlag(DoorPermissionCheck.CurrentItem) && isCurrent)
                continue;

            if (!checkFlags.HasFlag(DoorPermissionCheck.InventoryExcludingCurrent) && !isCurrent)
                continue;

            if (item.Base is IDoorPermissionProvider itemProvider && requester.PermissionsPolicy.CheckPermissions(itemProvider.GetPermissions(requester)))
                return true;
        }

        return false;
    }
    
    public static void SendFakeCassieMessage(
        this Player target,
        string message,
        bool isHeld = false,
        bool isNoisy = true,
        bool isSubtitles = true,
        string customSubtitles = "") {
        foreach (ReferenceHub hub in ReferenceHub.AllHubs) {
            if (hub.TryGetComponent(out RespawnEffectsController controller)) {
                target.SendFakeRPC(controller, "RpcCassieAnnouncement", message, isHeld, isNoisy, isSubtitles, customSubtitles);
            }
        }
    }
    
    public static ReadOnlyDictionary<Type, MethodInfo> BuildWriterExtensionMap() {
        var map = new Dictionary<Type, MethodInfo>();

        IEnumerable<MethodInfo> writerMethods = typeof(NetworkWriterExtensions).GetMethods()
            .Where(m => !m.IsGenericMethod &&
                        m.GetCustomAttribute(typeof(ObsoleteAttribute)) == null &&
                        m.GetParameters().Length == 2);

        foreach (MethodInfo method in writerMethods) {
            ParameterInfo param = method.GetParameters().First(p => p.ParameterType != typeof(NetworkWriter));
            if (!map.ContainsKey(param.ParameterType))
                map.Add(param.ParameterType, method);
        }

        Type? generatedType = Assembly.GetAssembly(typeof(RoleTypeId))
            ?.GetType("Mirror.GeneratedNetworkCode");

        if (generatedType != null) {
            IEnumerable<MethodInfo> genMethods = generatedType.GetMethods()
                .Where(m => !m.IsGenericMethod &&
                            m.GetParameters().Length == 2 &&
                            m.ReturnType == typeof(void));

            foreach (MethodInfo method in genMethods) {
                ParameterInfo param = method.GetParameters().First(p => p.ParameterType != typeof(NetworkWriter));
                if (!map.ContainsKey(param.ParameterType))
                    map.Add(param.ParameterType, method);
            }
        }

        IEnumerable<Type> serializerTypes = typeof(ServerConsole).Assembly.GetTypes()
            .Where(t => t.Name.EndsWith("Serializer"));

        foreach (Type serializer in serializerTypes) {
            IEnumerable<MethodInfo> writeMethods = serializer.GetMethods()
                .Where(m => m.ReturnType == typeof(void) && m.Name.StartsWith("Write"));

            foreach (MethodInfo method in writeMethods) {
                try {
                    ParameterInfo param = method.GetParameters().First(p => p.ParameterType != typeof(NetworkWriter));
                    if (!map.ContainsKey(param.ParameterType))
                        map.Add(param.ParameterType, method);
                }
                catch (InvalidOperationException) {
                    continue;
                }
            }
        }

        return new ReadOnlyDictionary<Type, MethodInfo>(map);
    }

    public static void PlayBeepSound(this Player player) =>
        SendFakeTargetRpc(player, ReferenceHub.HostHub.networkIdentity, typeof(AmbientSoundPlayer),
            "RpcPlaySound", 7);

    public static void ChangeAppearance(this Player player, RoleTypeId type, bool skipJump = false, byte unitId = 0) =>
        ChangeAppearance(player, type, Player.List.Where(x => x != player), skipJump, unitId);

    public static void ChangeAppearance(this Player player, RoleTypeId type, IEnumerable<Player> playersToAffect, bool skipJump = false, byte unitId = 0) {
        if (!PlayerRoleLoader.TryGetRoleTemplate(type, out PlayerRoleBase roleBase))
            return;

        bool isRisky = type.GetTeam() is Team.Dead || !player.IsAlive;
        NetworkWriterPooled writer = NetworkWriterPool.Get();

        writer.WriteUShort(38952);
        writer.WriteUInt(player.NetId);
        writer.WriteRoleType(type);

        switch (roleBase) {
            case HumanRole: {
                if (player.Role.Base is not HumanRole)
                    isRisky = true;
                writer.WriteByte(unitId);
                break;
            }
            case ZombieRole: {
                if (player.Role.Base is not ZombieRole)
                    isRisky = true;
                writer.WriteUShort((ushort)Mathf.Clamp(Mathf.CeilToInt(player.MaxHealth), ushort.MinValue, ushort.MaxValue));
                writer.WriteBool(true);
                break;
            }
        }

        if (roleBase is Scp1507Role) {
            if (player.Role.Base is not Scp1507Role)
                isRisky = true;
            byte spawnReason = player.Role.Base is Scp1507Role scp1507 ? Convert.ToByte(scp1507.GetFieldValue<object>("_spawnReason")) : (byte)0;
            writer.WriteByte(spawnReason);
        }

        if (roleBase is FpcStandardRoleBase fpc) {
            if (player.Role.Base is not FpcStandardRoleBase playerFpc)
                isRisky = true;
            else
                fpc = playerFpc;

            fpc.FpcModule.MouseLook.GetSyncValues(0, out ushort value, out ushort _);
            writer.WriteRelativePosition(new RelativePosition(player.Position));
            writer.WriteUShort(value);
        }

        foreach (Player target in playersToAffect) {
            if (target != player || !isRisky)
                target.Connection.Send(writer.ToArraySegment());
            else
                Log.Error($"Prevent Self-Desync of {player.Nickname} with {type}");
        }

        NetworkWriterPool.Return(writer);
        if (!skipJump)
            player.Position += Vector3.up * 0.25f;
    }

    public static void PlayCassieAnnouncement(this Player player, string words, bool makeHold = false, bool makeNoise = true, bool isSubtitles = false) {
        foreach (ReferenceHub hub in ReferenceHub.AllHubs) {
            if (hub.TryGetComponent(out RespawnEffectsController controller)) {
                SendFakeTargetRpc(player, controller.netIdentity, typeof(RespawnEffectsController),
                    "RpcCassieAnnouncement", words, makeHold, makeNoise, isSubtitles);
            }
        }
    }

    public static void SendFakeTargetRpc(Player target, NetworkIdentity behaviorOwner, Type targetType, string rpcName, params object[] values) {
        NetworkWriterPooled writer = NetworkWriterPool.Get();

        try {
            foreach (object value in values) {
                if (value == null) {
                    Log.Warn($"[NetworkExtensions] SendFakeTargetRpc {targetType.Name}.{rpcName}: получен null-аргумент, вызов отменён.");
                    return;
                }

                Type valueType = value.GetType();
                if (!WriterExtensions.TryGetValue(valueType, out MethodInfo? method)) {
                    Log.Warn($"[NetworkExtensions] SendFakeTargetRpc {targetType.Name}.{rpcName}: нет writer для типа {valueType.Name}, вызов отменён.");
                    return;
                }

                method.Invoke(null, [writer, value]);
            }

            int componentIndex = GetComponentIndex(behaviorOwner, targetType);
            if (componentIndex < 0) {
                Log.Warn($"[NetworkExtensions] SendFakeTargetRpc: компонент {targetType.Name} не найден на объекте {behaviorOwner.netId}, вызов отменён.");
                return;
            }

            if (!RpcFullNames.TryGetValue($"{targetType.Name}.{rpcName}", out string? fullName)) {
                Log.Warn($"[NetworkExtensions] SendFakeTargetRpc: RPC '{targetType.Name}.{rpcName}' не найден, вызов отменён.");
                return;
            }

            RpcMessage msg = new() {
                netId = behaviorOwner.netId,
                componentIndex = (byte)componentIndex,
                functionHash = (ushort)fullName.GetStableHashCode(),
                payload = writer.ToArraySegment(),
            };

            target.Connection.Send(msg);
        }
        finally {
            NetworkWriterPool.Return(writer);
        }
    }
}
