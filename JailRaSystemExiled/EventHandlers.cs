using Exiled.Events.EventArgs.Player;

namespace JailRaSystemExiled;

public class EventHandlers
{
    public static void Registered()
    {
        Exiled.Events.Handlers.Player.Left += OnLeft;
    }
    
    public static void UnRegistered()
    {
        Exiled.Events.Handlers.Player.Left -= OnLeft;
    }
    
    private static void OnLeft(LeftEventArgs ev)
    {
        if(ev.Player is null)
            return;
        JailRoomRegistry.DeletePlayer(ev.Player);
    }
}