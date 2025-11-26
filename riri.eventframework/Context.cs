using p3rpc.commonmodutils;
using Reloaded.Hooks.ReloadedII.Interfaces;
using Reloaded.Memory;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;
using SharedScans.Interfaces;
using UE.Toolkit.Interfaces;

namespace riri.eventframework
{
    public class EventContext : UnrealToolkitContext
    {
        public string ModName { get; init; }
        public bool bIsSteam { get; set; }
        public IUnrealState _toolkitState { get; init; }

        public EventContext(long baseAddress, IConfigurable config, ILogger logger, IStartupScanner startupScanner, IReloadedHooks hooks, string modLocation, Utils utils, Memory memory,
            ISharedScans sharedScans, string modName, IUnrealStrings toolkitStrings, IUnrealObjects toolkitObjects, IUnrealMemory toolkitMemory, IUnrealState toolkitState)
            : base(baseAddress, config, logger, startupScanner, hooks, modLocation, utils, memory, sharedScans, toolkitStrings, toolkitObjects, toolkitMemory)
        {
            ModName = modName;
            bIsSteam = Native.GetModuleHandleA("steam_api64") != nint.Zero;
            _toolkitState = toolkitState;
        }

        public override void OnConfigUpdated(IConfigurable newConfig)
        {

        }
    }
}
