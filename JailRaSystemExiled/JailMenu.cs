using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using Exiled.CustomItems.API.Features;
using Exiled.Loader.Features.Configs;
using Exiled.Permissions.Extensions;
using MEC;
using NetworkManagerUtils.Dummies;
using PlayerRoles;
using RaCustomMenuExiled.API;
using RelativePositioning;
using UnityEngine;
using YamlDotNet.Serialization;
using Random = UnityEngine.Random;

namespace JailRaSystemExiled;

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
                            Log.Warn($"Room ID {room.id} not found!");
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
                }, null);
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
                if (pl.CheckPermission("jail.add"))
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
                RelativePosition = player.RelativePosition,
                Items = player.Items.ToList(),
                Effects = player.ActiveEffects.Select(x => new Effect(x)).ToList(),
                Name = player.Nickname,
                Role = player.Role.Type,
                Ammo = player.Ammo.ToDictionary(x => x.Key.GetAmmoType(), x => x.Value),
            });
        }

        if (player.IsOverwatchEnabled)
            player.IsOverwatchEnabled = false;
        player.Ammo.Clear();
        player.Inventory.SendAmmoNextFrame = true;

        player.ClearInventory(false);
        player.Position = this.RoomPostion;
        if (!Plugin.Singleton.Config.EnableKeepRole)
            player.Role.Set(RoleTypeId.Tutorial, SpawnReason.ForceClass, RoleSpawnFlags.None);
    }

    public void DoUnJail(Player player)
    {
        if (!players.TryGetValue(player.UserId, out Jailed jail))
            return;
        player.Role.Set(jail.Role, RoleSpawnFlags.None);
        try
        {
            Timing.CallDelayed(1f, () =>
            {
                player.ClearInventory();
                foreach (Item item in jail.Items)
                {
                    if (CustomItem.TryGet(item, out CustomItem ci))
                    {
                        player.AddItem(item);
                    }
                    else
                    {
                        player.AddItem(item.Base);
                    }
                }

                player.Position = jail.RelativePosition.Position;
                player.Health = jail.Health;
                foreach (KeyValuePair<AmmoType, ushort> kvp in jail.Ammo)
                    player.Ammo[kvp.Key.GetItemType()] = kvp.Value;
                player.SyncEffects(jail.Effects);

                player.Inventory.SendItemsNextFrame = true;
                player.Inventory.SendAmmoNextFrame = true;
            });
        }
        catch (Exception e)
        {
            Log.Error($"{nameof(DoUnJail)}: {e}");
        }
        players.Remove(player.UserId);
    }
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

public class Jailed
{
    public string Name;
    public List<Item> Items;
    public List<Effect> Effects;
    public RoleTypeId Role;
    public RelativePosition RelativePosition;
    public float Health;
    public Dictionary<AmmoType, ushort> Ammo;
}

class YamlWrite
{
    public static void AddRoomPosition(Vector3 position)
    {
        string filePath = Plugin.Singleton.ConfigPath;

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

        Log.Debug("Room added with succes");
    }
}