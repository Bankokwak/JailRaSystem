using System;
using System.IO;
using LabApi.Features;
using LabApi.Features.Console;
using LabApi.Loader.Features.Plugins;
using RaCustomMenuLabApi.API;

namespace JailRaSystemLabApi;

public class Plugin : Plugin<Config>
{
    public override string Name { get; } = "JailRaSystemLabApi";
    public override string Description { get; } = "";
    public override string Author { get; } = "Bankokwak";
    public override Version Version { get; } = new Version(1, 1, 1);
    public override Version RequiredApiVersion { get; } = new(LabApiProperties.CompiledVersion);

    public static Plugin Singleton;

    public override void Enable()
    {
        Singleton = this;
        
        EventHandlers.Registered();
        
        Provider.RegisterAllProviders();
    }

    public override void Disable()
    {
        EventHandlers.UnRegistered();

        Singleton = null;
    }
}