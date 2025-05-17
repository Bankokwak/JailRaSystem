using System.Collections.Generic;
using System.IO;
using System.Linq;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.Loader.Features.Configs;
using Exiled.Permissions.Extensions;
using MEC;
using NetworkManagerUtils.Dummies;
using PlayerRoles;
using RaCustomMenuExiled.API;
using UnityEngine;
using YamlDotNet.Serialization;

namespace JailRaSystemExiled;

public class JailMenu: Provider
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
                            if(existingRoom.players.ContainsKey(pl))return;
                            existingRoom.AddPlayer(pl);
                            Provider.AddActionDynamic($"Jail/Room: {room.id}", new List<DummyAction>
                            {
                                new DummyAction($"{pl.Nickname}", () =>
                                {
                                    if (existingRoom.players.ContainsKey(pl))
                                    {
                                        existingRoom.RemovePlayer(pl);
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
                                existingRoom.RemovePlayer(player);
                            }
                        }
                        JailRoomRegistry.Rooms.Remove(room.id);
                        Provider.UnregisterDynamicProvider($"Jail/Room: {room.id}");
                    })
                });
            }),
            new DummyAction("Clear All Room", () =>
            {
                foreach (var room in JailRoomRegistry.Rooms)
                {
                    if (JailRoomRegistry.Rooms.TryGetValue(room.Key, out var existingRoom))
                    {
                        foreach (var player in existingRoom.players.Keys.ToList())
                        {
                            existingRoom.RemovePlayer(player);
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
    public Dictionary<Player, Data> players { get; set; }

    private static int RoomIdCounter = 1;
    
    private Vector3 RoomPostion { get; set; } = Vector3.zero;

    public RoomManager()
    {
        this.id = RoomIdCounter++;
        this.players = new Dictionary<Player, Data>();

        var positions = Plugin.Singleton.Config.room_postion;
        if (positions != null && positions.Count > 0)
        {
            int randomIndex = Random.Range(0, positions.Count);
            this.RoomPostion = positions[randomIndex].ToVector3();
        }
    }

    public void AddPlayer(Player player)
    {
        if(players.ContainsKey(player))return;
        players.Add(player, new Data(player));
        player.Role.Set(RoleTypeId.Tutorial);
        if (RoomPostion != Vector3.zero)
        {
            Timing.CallDelayed(0.1f, () =>
            {
                player.Position = RoomPostion;
            });
        }
    }

    public void RemovePlayer(Player player)
    {
        if(!players.ContainsKey(player))return;
        players[player].Recover(player);
        players.Remove(player);
    }
}

public class Data
{
    public RoleTypeId Role { get; set; }
    public Vector3 Position { get; set; }
    public float Health { get; set; }
    public List<ItemType> ItemTypes { get; set; }
    public Dictionary<ItemType, ushort> AmmoTypes { get; set; }

    public Data(Player player)
    {
        Role = player.Role.Type;
        Position = player.Position;
        Health = player.Health;
        ItemTypes = player.Items.Select(i => i.Type).ToList();
        AmmoTypes = new Dictionary<ItemType, ushort>(player.Ammo);
    }

    public void Recover(Player player)
    {
        player.Role.Set(Role, SpawnReason.Respawn, RoleSpawnFlags.None);

        Timing.CallDelayed(1f, () =>
        {
            player.Health = Health;

            foreach (var itemType in ItemTypes)
                player.AddItem(itemType);

            foreach (var ammo in AmmoTypes)
                player.AddAmmo((AmmoType)ammo.Key, ammo.Value);

            player.Position = Position;
        });
    }
}

public static class JailRoomRegistry
{
    public static readonly Dictionary<int, RoomManager> Rooms = new();
    
    public static void DeletePlayer(Player player)
    {
        foreach (var room in Rooms)
        {
            if(JailRoomRegistry.Rooms.TryGetValue(room.Key, out var existingRoom))
            {
                if (existingRoom.players.ContainsKey(player))
                {
                    existingRoom.players.Remove(player);
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

        Log.Info("Room added with succes");
    }
}