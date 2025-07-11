using JailRaSystemLabApi;
using LabApi.Events.Arguments.PlayerEvents;

namespace JailRaSystemLabApi;

public class EventHandlers
{
    public static void Registered()
    {
        LabApi.Events.Handlers.PlayerEvents.Left += OnLeft;
    }
    
    public static void UnRegistered()
    {
        LabApi.Events.Handlers.PlayerEvents.Left -= OnLeft;
    }
    
    private static void OnLeft(PlayerLeftEventArgs ev)
    {
        if(ev.Player is null)
            return;
        JailRoomRegistry.DeletePlayer(ev.Player);
    }
}