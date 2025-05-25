using System;
using Exiled.API.Features;
using RaCustomMenuExiled.API;

namespace JailRaSystemExiled;

public class Plugin : Plugin<Config>
{
    public override string Name => "JailRaSystemExiled";
    public override string Author => "Bankokwak";
    public override Version Version => new Version(1, 1, 0);
    public override Version RequiredExiledVersion { get; } = new Version(9,6,0);

    public static Plugin Singleton;

    public override void OnEnabled()
    {
        Singleton = this;
        
        EventHandlers.Registered();

        Provider.RegisterAllProviders();
        
        base.OnEnabled();
    }

    public override void OnDisabled()
    {
        Singleton = null;

        EventHandlers.UnRegistered();
        
        base.OnDisabled();
    }
}