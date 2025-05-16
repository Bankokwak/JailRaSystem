using System.Collections.Generic;
using Exiled.API.Features;
using NetworkManagerUtils.Dummies;
using PlayerRoles;
using RaCustomMenuExiled.API;

namespace JailRaSystem;

public class JailMenu: RaCustomMenuExiled.API.Provider
{
    public override List<DummyAction> AddAction(ReferenceHub hub)
    {
        return new List<DummyAction>()
        {
            new DummyAction("Create Room", () =>
            {
                var room = new RoomManager();
                JailRoomRegistry.Rooms.Add(room.id, room);
                Provider.RegisterDynamicProvider($"Jail/Room: {room.id}", true, hub => new List<DummyAction>
                {
                    new DummyAction("Add jail", () =>
                    {
                        if (JailRoomRegistry.Rooms.TryGetValue(room.id, out var existingRoom))
                        {
                            Player pl = Player.Get(hub);
                            if(existingRoom.players.Contains(pl))return;
                            existingRoom.AddPlayer(pl);
                            pl.Role.Set(RoleTypeId.Tutorial);
                            Provider.AddActionDynamic($"Jail/Room: {room.id}", new List<DummyAction>
                            {
                                new DummyAction($"{pl.Nickname}", () =>
                                {
                                    existingRoom.RemovePlayer(pl);
                                })
                            });
                        }
                        else
                        {
                            Log.Warn($"Room ID {room.id} not found!");
                        }
                    }),
                });
            }),
        };
    }

    public override string CategoryName { get; } = "Jail";
    public override bool IsDirty { get; } = true;
}

public class RoomManager
{
    public int id { get; set; } = 0;
    public List<Player> players { get; set; }

    private static int RoomIdCounter = 1;

    public RoomManager()
    {
        id = RoomIdCounter++;
        players = new List<Player>();
    }

    public void AddPlayer(Player player)
    {
        if(players.Contains(player))return;
        players.Add(player);
    }

    public void RemovePlayer(Player player)
    {
        if(!players.Contains(player))return;
        players.Remove(player);
    }
}
public static class JailRoomRegistry
{
    public static readonly Dictionary<int, RoomManager> Rooms = new();
}