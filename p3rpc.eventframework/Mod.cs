using p3rpc.commonmodutils;
using p3rpc.eventframework.Configuration;
using p3rpc.eventframework.Template;
using Reloaded.Hooks.ReloadedII.Interfaces;
using Reloaded.Memory;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;
using Reloaded.Mod.Interfaces.Internal;
using riri.eventframework;
using SharedScans.Interfaces;
using System.Diagnostics;
using RyoTune.Reloaded;
using UE.Toolkit.Core.Types.Unreal.Factories;
using UE.Toolkit.Interfaces;

namespace p3rpc.eventframework
{
    public class Mod : ModBase
    {
        private readonly IModLoader _modLoader;
        private readonly IReloadedHooks? _hooks;
        private readonly ILogger _logger;
        private readonly IMod _owner;
        private Config _configuration;
        private readonly IModConfig _modConfig;

        private EventContext _context;
        private readonly ModuleRuntime<EventContext> _runtime;

        public Mod(ModContext context)
        {
            _modLoader = context.ModLoader;
            _hooks = context.Hooks;
            _logger = context.Logger;
            _owner = context.Owner;
            _configuration = context.Configuration;
            _modConfig = context.ModConfig;
#if DEBUG
            //Debugger.Launch();
#endif
            var process = Process.GetCurrentProcess();
            if (process?.MainModule == null) throw new Exception($"[{_modConfig.ModName}] Process is null");
            var baseAddress = process.MainModule.BaseAddress;
            if (_hooks == null) throw new Exception($"[{_modConfig.ModName}] Could not get controller for Reloaded hooks");
            var logColor = System.Drawing.Color.PaleTurquoise;
            Project.Initialize(_modConfig, _modLoader, _logger, logColor, true);
            Log.LogLevel = _configuration.LogLevel;
            var startupScanner = Utils.GetDependency<IStartupScanner>(_modLoader, _modConfig.ModName, "Reloaded Startup Scanner");
            var utils = Utils.Create(_modLoader, startupScanner, _logger, _hooks, baseAddress, _modConfig.ModName, logColor);

            var sharedScans = utils.GetDependencyEx<ISharedScans>("Shared Scans");
            var toolkitObjects = utils.GetDependencyEx<IUnrealObjects>("Object Interface (UE Toolkit)");
            var toolkitMemory = utils.GetDependencyEx<IUnrealMemory>("Memory Interface (UE Toolkit)");
            var toolkitStrings = utils.GetDependencyEx<IUnrealStrings>("String Interface (UE Toolkit)");
            var toolkitState = utils.GetDependencyEx<IUnrealState>("Unreal State (UE Toolkit)");
            var toolkitFactory = utils.GetDependencyEx<IUnrealFactory>("Factory (UE Toolkit)");

            _context = new(
                baseAddress, _configuration, _logger, startupScanner, _hooks,
                _modLoader.GetDirectoryForModId(_modConfig.ModId), utils,
                new Memory(), sharedScans, _modConfig.ModId,
                toolkitStrings, toolkitObjects, toolkitMemory, toolkitState, toolkitFactory);
            _runtime = new(_context);
            _runtime.AddModule<Hooks.Event>();
            _runtime.AddModule<Hooks.Field>();
            _runtime.AddModule<PreDataService>();
            _runtime.RegisterModules();
            _runtime.GetModule<PreDataService>()._game = new GameMethods();

            _modLoader.OnModLoaderInitialized += OnLoaderInit;
            _modLoader.ModLoading += OnModLoading;
        }

        private void OnLoaderInit()
        {
            _modLoader.OnModLoaderInitialized -= OnLoaderInit;
            _modLoader.ModLoading -= OnModLoading;
        }

        private void OnModLoading(IModV1 mod, IModConfigV1 conf)
        {
            if (!conf.ModDependencies.Contains(_modConfig.ModId)) return;
            _runtime.GetModule<PreDataService>().OnModLoaded(_modLoader.GetDirectoryForModId(conf.ModId));
        }

        #region Standard Overrides
        public override void ConfigurationUpdated(Config configuration)
        {
            _configuration = configuration;
            _logger.WriteLine($"[{_modConfig.ModId}] Config Updated: Applying");
            _runtime.UpdateConfiguration(configuration);
        }
        #endregion

        #region For Exports, Serialization etc.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public Mod() { }
#pragma warning restore CS8618
        #endregion
    }
}