using System;
using Exiled.API.Features;
using RaCustomMenuExiled.API;

namespace JailRaSystem;

public class Plugin : Plugin<Config>
{
    public override string Name => "JailRaSystem";
    public override string Author => "Bankokwak";
    public override Version Version => new Version(1, 0, 0);
    public override Version RequiredExiledVersion { get; } = new Version(9,8,0);

    public static Plugin Singleton;

    private static EventHandlers Handlers;

    public override void OnEnabled()
    {
        Singleton = this;
        Handlers = new EventHandlers();

        Provider.RegisterAllProviders();
        
        base.OnEnabled();
    }

    public override void OnDisabled()
    {
        Singleton = null;
        Handlers = null;
        base.OnDisabled();
    }
}