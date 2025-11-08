using p3rpc.commonmodutils;
using p3rpc.nativetypes.Interfaces;
using UE.Toolkit.Core.Types;
using UE.Toolkit.Core.Types.Unreal.UE5_4_4;
using UE.Toolkit.Interfaces;
using FName = UE.Toolkit.Core.Types.Unreal.UE5_4_4.FName;
using FString = UE.Toolkit.Core.Types.Unreal.UE5_4_4.FString;

namespace riri.eventframework
{

    public class PreDataAdapterFactory : ModuleBase<EventContext>
    {
        public PreDataAdapterFactory(EventContext context, Dictionary<string, ModuleBase<EventContext>> modules) : base(context, modules)
        {
        }

        public override void Register()
        {

        }
        
        public PreDataAdapter? HookFromYamlModel(PreDataModel model)
        {
            var newAdapter = new PreDataAdapter();
            newAdapter.FromYamlModelCommon(model);
            unsafe
            {
                newAdapter.EventLevel = (model.EventLevel != null) ? _context._toolkitObjects.CreateFString(model.EventLevel) : null;
                newAdapter.EventSublevels = newAdapter.SublevelHookFromYamlModel(_context._toolkitMemory, _context._toolkitObjects, model);
                newAdapter.LightScenarioSublevels = newAdapter.LightScenarioFromYamlModel(_context._toolkitMemory, model);
                newAdapter.DungeonSublevel = (model.DungeonSublevel != null) ? newAdapter.DungeonSublevelHookFromYamlModel(_context._toolkitMemory, model.DungeonSublevel) : null;
            }
            newAdapter.bDisableAutoLoadFirstLightingScenarioLevel = model.bDisableAutoLoadFirstLightingScenarioLevel;
            newAdapter.bForceDisableUseCurrentTimeZone = model.bForceDisableUseCurrentTimeZone;
            newAdapter.ForcedCldTimeZoneValue = model.ForcedCldTimeZoneValue;
            newAdapter.ForceMonth = model.ForceMonth;
            newAdapter.ForceDay = model.ForceDay;
            return newAdapter;
        }

        public unsafe PreDataAdapter? NewFromYamlModel(PreDataModel model)
        {
            var newAdapter = new PreDataAdapter();
            var errorReporter = new PreDataNativeAdapterErrors(newAdapter);
            newAdapter.FromYamlModelCommon(model);
            if (model.EventLevel != null) newAdapter.EventLevel = _context._toolkitObjects.CreateFString(model.EventLevel); else errorReporter.MissingParameters.Add("EventLevel");
            if (model.EventSublevels != null) newAdapter.EventSublevels = newAdapter.SublevelHookFromYamlModel(_context._toolkitMemory, _context._toolkitObjects, model); else errorReporter.MissingParameters.Add("EventSublevels");
            if (model.LightScenarioSublevels != null) newAdapter.LightScenarioSublevels = newAdapter.LightScenarioFromYamlModel(_context._toolkitMemory, model); else errorReporter.MissingParameters.Add("LightScenarioSublevels");
            errorReporter.ReportErrors(s => _context._utils.Log(s, LogLevel.Error));
            return (errorReporter.MissingParameters.Count == 0) ? newAdapter : null;
        }
    }
    
    public class PreDataAdapter
    {
        public int EventMajorID { get; set; }
        public int EventMinorID { get; set; }
        public int EventCategoryTypeID { get; set; }

        private FName _EventRank;
        public FName EventRank
        {
            get => _EventRank;
            set => _EventRank = value;
        }
        private FName _EventCategory;
        public FName EventCategory
        {
            get => _EventCategory;
            set => _EventCategory = value;
        }
        public unsafe FString* EventLevel { get; set; } = null;
        public unsafe TArrayList<FAtlEvtPreSublevelData>? EventSublevels { get; set; } = null;
        public unsafe TArrayList<FName>? LightScenarioSublevels { get; set; } = null;
        public unsafe FAtlEvtPreDungeonSublevelData* DungeonSublevel { get; set; } = null;
        public bool? bDisableAutoLoadFirstLightingScenarioLevel { get; set; } = null;
        public bool? bForceDisableUseCurrentTimeZone { get; set; } = null;
        public byte? ForcedCldTimeZoneValue { get; set; } = null;
        public int? ForceMonth { get; set; } = null;
        public int? ForceDay { get; set; } = null;

        internal void FromYamlModelCommon(PreDataModel model)
        {
            EventMajorID = model.EventMajorID;
            EventMinorID = model.EventMinorID;
            EventCategoryTypeID = model.EventCategoryTypeID;
            EventRank = new FName(model.EventRank);
            EventCategory = new FName(model.EventCategory);
        }
        
        internal unsafe TArrayList<FAtlEvtPreSublevelData> SublevelHookFromYamlModel(IUnrealMemory _memory, 
            IUnrealObjects _objects, PreDataModel model)
        {
            var Sublevels = new TArrayList<FAtlEvtPreSublevelData>(_memory);
            Sublevels.ResizeTo(model.EventSublevels!.Count);
            foreach (var Sublevel in model.EventSublevels!)
            {
                TArrayList<FString> BgLevels = new(_memory);
                BgLevels.ResizeTo(Sublevel.EventBGLevels.Count);
                foreach (var Level in Sublevel.EventBGLevels)
                    BgLevels.AddValue(*_objects.CreateFString(Level));
                BgLevels.Leak();
                Sublevels.AddValue(new FAtlEvtPreSublevelData()
                {
                    EventBGLevels = *(p3rpc.nativetypes.Interfaces.TArray<p3rpc.nativetypes.Interfaces.FString>*)BgLevels.Base(),
                    BGFieldMajorID = Sublevel.BGFieldMajorID,
                    BGFieldMinorID = Sublevel.BGFieldMinorID,
                    BGFieldSeasonSubLevel = *(p3rpc.nativetypes.Interfaces.FString*)_objects.CreateFString(Sublevel.BGFieldSeasonSubLevel),
                    BGFieldSoundSubLevel = *(p3rpc.nativetypes.Interfaces.FString*)_objects.CreateFString(Sublevel.BGFieldSoundSubLevel),
                });
                _memory.Free((nint)BgLevels.Base());
            }
            return Sublevels;
        }

        internal TArrayList<FName> LightScenarioFromYamlModel(IUnrealMemory _memory, PreDataModel model)
        {
            TArrayList<FName> LightScenarios = new(_memory);
            LightScenarios.ResizeTo(model.LightScenarioSublevels.Count);
            foreach (var Sublevel in model.LightScenarioSublevels)
            {
                LightScenarios.AddValue(new FName(Sublevel));
            }
            return LightScenarios;
        }

        internal unsafe FAtlEvtPreDungeonSublevelData* DungeonSublevelHookFromYamlModel(IUnrealMemory _memory, PreDataDungeonSublevel model)
        {
            var dungeonSublevel = (FAtlEvtPreDungeonSublevelData*)_memory.Malloc(sizeof(FAtlEvtPreDungeonSublevelData));
            dungeonSublevel->EventBGFloorLevel = new FName(model.EventBGFloorLevel).ToNT();
            dungeonSublevel->BGEnvironmentSubLevel = new FName(model.EventBGFloorLevel).ToNT();
            return dungeonSublevel;
        }

        public p3rpc.nativetypes.Interfaces.FName GetEventRankNT()
        {
            unsafe { fixed (FName* pEventRank = &_EventRank) { return *(p3rpc.nativetypes.Interfaces.FName*)pEventRank; } }
        }

        public p3rpc.nativetypes.Interfaces.FName GetEventCategoryNT()
        {
            unsafe { fixed (FName* pEventCategory = &_EventCategory) { return *(p3rpc.nativetypes.Interfaces.FName*)pEventCategory; } }
        }
    }

    public class PreDataNativeAdapterErrors
    {
        public PreDataAdapter Owner;
        public List<string> MissingParameters { get; private set; } = new();
        public PreDataNativeAdapterErrors(PreDataAdapter owner) { Owner = owner; }
        public void ReportErrors(Action<string> _log)
        {
            foreach (var missingParam in MissingParameters)
                _log(
                    $"[PRE_{Owner.EventCategory.ToString()}_{Owner.EventMajorID:D3}_{Owner.EventMinorID:D3}]: " +
                    $"Missing parameter {missingParam}. Event won't be loaded."
                );
        }
    }
}
