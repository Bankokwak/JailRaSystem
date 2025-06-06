using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CustomPlayerEffects;
using LabApi.Features.Permissions;
using LabApi.Features.Wrappers;
using MEC;
using NetworkManagerUtils.Dummies;
using PlayerRoles;
using RaCustomMenuLabApi.API;
using UnityEngine;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Logger = LabApi.Features.Console.Logger;
using Random = UnityEngine.Random;

namespace JailRaSystemLabApi;

public class JailMenu : Provider
{
    public override List<DummyAction> AddAction(ReferenceHub hub)
    {
        return new List<DummyAction>()
        {
            new DummyAction("U-Create Room", () =>
            {
                var room = new RoomManager();
                JailRoomRegistry.Rooms.Add(room.id, room);
                Provider.RegisterDynamicProvider($"Jail/Room: {room.id}", true, referenceHub => new List<DummyAction>
                {
                    new DummyAction("Add jail", () =>
                    {
                        if (JailRoomRegistry.Rooms.TryGetValue(room.id, out var existingRoom))
                        {
                            Player pl = Player.Get(referenceHub);
                            if (existingRoom.players.ContainsKey(pl.UserId)) return;
                            existingRoom.DoJail(pl);
                            Provider.AddActionDynamic($"Jail/Room: {room.id}", new List<DummyAction>
                            {
                                new DummyAction($"{pl.Nickname}", () =>
                                {
                                    if (existingRoom.players.ContainsKey(pl.UserId))
                                    {
                                        existingRoom.DoUnJail(pl);
                                    }

                                    Provider.RemoveActionDynamic($"Jail/Room: {room.id}", pl.Nickname);
                                })
                            });
                        }
                        else
                        {
                            Logger.Warn($"Room ID {room.id} not found!");
                        }
                    }),
                    new DummyAction("Delete Room", () =>
                    {
                        if (JailRoomRegistry.Rooms.TryGetValue(room.id, out var existingRoom))
                        {
                            foreach (var player in existingRoom.players.Keys.ToList())
                            {
                                Player ply = Player.Get(player);
                                existingRoom.DoUnJail(ply);
                            }
                        }

                        JailRoomRegistry.Rooms.Remove(room.id);
                        Provider.UnregisterDynamicProvider($"Jail/Room: {room.id}");
                    })
                },null);
            }),
            new DummyAction("Clear All Room", () =>
            {
                foreach (var room in JailRoomRegistry.Rooms)
                {
                    if (JailRoomRegistry.Rooms.TryGetValue(room.Key, out var existingRoom))
                    {
                        foreach (var player in existingRoom.players.Keys.ToList())
                        {
                            Player ply = Player.Get(player);
                            existingRoom.DoUnJail(ply);
                        }
                    }

                    Provider.UnregisterDynamicProvider($"Jail/Room: {room.Key}");
                }

                JailRoomRegistry.Rooms.Clear();
            }),
            new DummyAction("Add Room Position", () =>
            {
                Player pl = Player.Get(hub);
                if (pl.HasPermissions("jail.add"))
                {
                    YamlWrite.AddRoomPosition(pl.Position);
                }
            })
        };
    }

    public override string CategoryName { get; } = "Jail";
    public override bool IsDirty { get; } = true;
}

public class RoomManager
{
    public int id { get; set; }
    public Dictionary<string, Jailed> players { get; set; }

    private static int RoomIdCounter = 1;

    private Vector3 RoomPostion { get; set; } = Vector3.zero;

    public RoomManager()
    {
        this.id = RoomIdCounter++;
        this.players = new Dictionary<string, Jailed>();

        var positions = Plugin.Singleton.Config.room_postion;
        if (positions != null && positions.Count > 0)
        {
            int randomIndex = Random.Range(0, positions.Count);
            this.RoomPostion = positions[randomIndex].ToVector3();
        }
    }


    public void DoJail(Player player, bool skipadd = false)
    {
        if (players.ContainsKey(player.UserId))
            return;
        if (!skipadd)
        {
            players.Add(player.UserId, new Jailed
            {
                Health = player.Health,
                Position = player.Position,
                Items = player.Items.ToList(),
                Effects = player.ActiveEffects.ToList(),
                Name = player.Nickname,
                Role = player.Role,
                Ammo = player.Ammo,
            });
        }

        if (player.IsOverwatchEnabled)
            player.IsOverwatchEnabled = false;
        player.Ammo.Clear();
        player.Inventory.SendAmmoNextFrame = true;

        player.ClearInventory(false);
        player.Position = this.RoomPostion;
        if (!Plugin.Singleton.Config.EnableKeepRole)
            player.SetRole(RoleTypeId.Tutorial, RoleChangeReason.RemoteAdmin, RoleSpawnFlags.None);
    }

    public void DoUnJail(Player player)
    {
        if (!players.TryGetValue(player.UserId, out Jailed jail))
            return;
        player.SetRole(jail.Role, RoleChangeReason.RemoteAdmin, RoleSpawnFlags.None);
        try
        {
            Timing.CallDelayed(1f, () =>
            {
                player.ClearInventory();
                foreach (Item item in jail.Items)
                {
                    player.AddItem(item.Type);
                }

                player.Position = jail.Position;
                player.Health = jail.Health;
                foreach (KeyValuePair<ItemType, ushort> kvp in jail.Ammo)
                    player.AddAmmo(kvp.Key, kvp.Value);
                foreach (var effect in jail.Effects)
                {
                    player.EnableEffect(effect);
                }

                player.Inventory.SendItemsNextFrame = true;
                player.Inventory.SendAmmoNextFrame = true;
            });
        }
        catch (Exception e)
        {
            Logger.Info($"{nameof(DoUnJail)}: {e}");
        }
        players.Remove(player.UserId);
    }
}

public class Jailed
{
    public string Name;
    public List<Item> Items;
    public List<StatusEffectBase> Effects;
    public RoleTypeId Role;
    public Vector3 Position;
    public float Health;
    public Dictionary<ItemType, ushort> Ammo;
}

public static class JailRoomRegistry
{
    public static readonly Dictionary<int, RoomManager> Rooms = new();

    public static void DeletePlayer(Player player)
    {
        foreach (var room in Rooms)
        {
            if (JailRoomRegistry.Rooms.TryGetValue(room.Key, out var existingRoom))
            {
                if (existingRoom.players.ContainsKey(player.UserId))
                {
                    existingRoom.players.Remove(player.UserId);
                    Provider.RemoveActionDynamic($"Jail/Room: {room.Key}", player.Nickname);
                    return;
                }
            }
        }
    }
}

class YamlWrite
{
    public static void AddRoomPosition(Vector3 position)
    {
        string filePath = Path.Combine(Directory.GetCurrentDirectory() +
                                       $"LabAPI/configs/{Server.Port}/JailRaSystemLabApi/config.yml");

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

        var serializer = new SerializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

        Config config;
        using (var reader = new StreamReader(filePath))
        {
            config = deserializer.Deserialize<Config>(reader);
        }

        config.room_postion.Add(new RoomPosition(position));

        using (var writer = new StreamWriter(filePath))
        {
            serializer.Serialize(writer, config);
        }

        Logger.Debug("Room added with succes", config.Debug);
    }
}