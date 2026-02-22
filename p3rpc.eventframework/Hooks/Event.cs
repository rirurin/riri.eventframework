using p3rpc.commonmodutils;
using riri.eventframework;
using Reloaded.Hooks.Definitions.X64;
using RyoTune.Persona3Reload.Types;
using RyoTune.Reloaded;
using UE.Toolkit.Core.Types.Unreal.UE5_4_4;
using UWorld = p3rpc.nativetypes.Interfaces.UWorld;

namespace p3rpc.eventframework.Hooks;

// ReSharper disable once ClassNeverInstantiated.Global
internal class Event : ModuleBase<EventContext>
{
    private unsafe void AddEvtPreData(UAtlEvtPreDataAsset* PreData)
    {
        var ExistEvents = new TArrayList<FAtlEvtPreData>(&PreData->Data, _context._toolkitMemory);
        var HashToIndex = ExistEvents.Select((x, i) => (LocalTypeExtensions.GetEvtPreDataHash(
            (EAtlEvtEventCategoryType)x.Value->EventCategoryTypeID, (uint)x.Value->EventMajorID,
            (uint)x.Value->EventMinorID), i)).ToDictionary();
        // For edited events
        foreach (var EditEvent in _preDataService.NewPreData.Values.Where(x =>
                     HashToIndex.ContainsKey(x.Hash)))
        {
            var Value = ExistEvents[HashToIndex[EditEvent.Hash]].Value;
            SetEventCommon(Value, EditEvent);
        }
        // For new events
        foreach (var NewEvent in _preDataService.NewPreData.Values.Where(x =>
                     !HashToIndex.ContainsKey(x.Hash)))
        {
            ExistEvents.AddValue(new());
            var Current = ExistEvents.Last().Value;
            Current->EventMajorID = NewEvent.EventMajorID;
            Current->EventMinorID = NewEvent.EventMinorID;
            Current->EventCategoryTypeID = NewEvent.EventCategoryTypeID;
            Current->EventRank = new(NewEvent.EventRank);
            Current->EventCategory_2 = new(NewEvent.EventCategory);
            SetEventCommon(Current, NewEvent);
            Current->bDisableAutoLoadFirstLightingScenarioLevel =
                NewEvent.bDisableAutoLoadFirstLightingScenarioLevel ?? false;
            Current->bForceDisableUseCurrentTimeZone = NewEvent.bForceDisableUseCurrentTimeZone ?? false;
            Current->ForcedCldTimeZoneValue = NewEvent.ForcedCldTimeZoneValue ?? 0;
            Current->ForceMonth = NewEvent.ForceMonth ?? 0;
            Current->ForceDay = NewEvent.ForceDay ?? 0;
        }
    }

    private unsafe void SetEventCommon(FAtlEvtPreData* Data, PreDataModel Model)
    {
        if (Model.EventLevel != null) SetEventLevel(Data, Model.EventLevel);
        if (Model.EventSublevels != null) SetEventSublevels(Data, Model.EventSublevels);
        if (Model.LightScenarioSublevels != null) SetEventLightSublevels(Data, Model.LightScenarioSublevels);
        if (Model.DungeonSublevel != null) SetDungeonSublevel(Data, Model.DungeonSublevel);
    }

    private unsafe void SetEventLevel(FAtlEvtPreData* Data, string Value)
    {
        var fEventLevel = _context._toolkitObjects.CreateFString(Value);
        Data->EventLevel = *fEventLevel;
        _context._toolkitMemory.Free((nint)fEventLevel);
    }

    private unsafe void SetEventSublevels(FAtlEvtPreData* Data, List<PreDataSublevels> Value)
    {
        var Sublevels = new TArrayList<FAtlEvtPreSublevelData>(&Data->EventSublevels, _context._toolkitMemory);
        Sublevels.Clear();
        Sublevels.ResizeTo(Value.Count);
        foreach (var Sublevel in Value)
        {
            var fBGFieldSeasonSubLevel = _context._toolkitObjects.CreateFString(Sublevel.BGFieldSeasonSubLevel);
            var fBGFieldSoundSubLevel = _context._toolkitObjects.CreateFString(Sublevel.BGFieldSoundSubLevel);
            TArrayList<FString> BgLevels = new(_context._toolkitMemory);
            foreach (var Level in Sublevel.EventBGLevels)
            {
                var fBgLevel = _context._toolkitObjects.CreateFString(Level);
                BgLevels.AddValue(*fBgLevel);
                _context._toolkitMemory.Free((nint)fBgLevel);
            }
            BgLevels.Leak();
            Sublevels.AddValue(new()
            {
                EventBGLevels = *BgLevels.Base(),
                BGFieldMajorID = Sublevel.BGFieldMajorID,
                BGFieldMinorID = Sublevel.BGFieldMinorID,
                BGFieldSeasonSubLevel = *fBGFieldSeasonSubLevel,
                BGFieldSoundSubLevel = *fBGFieldSoundSubLevel
            });
            _context._toolkitMemory.Free((nint)fBGFieldSeasonSubLevel);
            _context._toolkitMemory.Free((nint)fBGFieldSoundSubLevel);
            _context._toolkitMemory.Free((nint)BgLevels.Base());
        }
    }

    private unsafe void SetEventLightSublevels(FAtlEvtPreData* Data, List<string> Value)
    {
        var LightLevels = new TArrayList<FName>(&Data->LightScenarioSublevels, _context._toolkitMemory);
        LightLevels.Clear();
        LightLevels.ResizeTo(Value.Count);
        foreach (var Level in Value)
            LightLevels.AddValue(new FName(Level));
    }

    private unsafe void SetDungeonSublevel(FAtlEvtPreData* Data, PreDataDungeonSublevel Value)
    {
        Data->DungeonSublevel.EventBGFloorLevel = new (Value.EventBGFloorLevel);
        Data->DungeonSublevel.BGEnvironmentSubLevel = new (Value.BGEnvironmentSubLevel);
    }
    
    
    [Function(CallingConventions.Microsoft)]
    private unsafe delegate byte UAtlEvtSubsystem_DoesLevelStreamingLevelExist(UAtlEvtSubsystem* self, UWorld* worldOut, nativetypes.Interfaces.FString* pathOut);
    private readonly SHFunction<UAtlEvtSubsystem_DoesLevelStreamingLevelExist> _doesLevelStreamingExist;

    private unsafe byte UAtlEvtSubsystem_DoesLevelStreamingLevelExistImpl(UAtlEvtSubsystem* self, UWorld* BaseWorld, nativetypes.Interfaces.FString* StreamPath)
    {

        var StreamPathStr = StreamPath->ToString();
        _context._utils.Log($"UAtlEvtSubsystem::DoesLevelStreamingLevelExist: {StreamPathStr}");
        var bInExistingLevelList = _doesLevelStreamingExist.Hook!.OriginalFunction(self, BaseWorld, StreamPath);
        if (bInExistingLevelList == 0 && _field.NewLevels.TryGetValue(StreamPathStr, out _))
            bInExistingLevelList = 1;
        if (bInExistingLevelList == 0)
        {
            bInExistingLevelList = _field.TryCreateNewLevel((UObject*)BaseWorld, StreamPathStr, 
                out var StreamedLevel) ? (byte)1 : (byte)0;
            switch (bInExistingLevelList)
            {
                case 1:
                    _context._logger.WriteLine($"Added level {StreamPathStr} to the level streaming registry: 0x{(nint)StreamedLevel:X}");
                    _field.NewLevels.TryAdd(StreamPathStr, (nint)StreamedLevel);
                    break;
                default:
                    _context._logger.WriteLine($"LOADING LEVEL INSTANCE FAILED: {StreamPathStr}");
                    break;
            }
        }
        return bInExistingLevelList;
    }

    private Field _field;
    private PreDataService _preDataService;

    public unsafe Event(EventContext context, Dictionary<string, ModuleBase<EventContext>> modules) : base(context, modules)
    {
        _doesLevelStreamingExist = new(UAtlEvtSubsystem_DoesLevelStreamingLevelExistImpl);
        _context._toolkitObjects.OnObjectLoadedByName<UAtlEvtPreDataAsset>("EvtPreDataAsset", x =>
        {
            var AsObject = _context._toolkitFactory.CreateUObject((nint)x.Self);
            _context._utils.Log($"EvtPreDataAsset::PostInit: Loaded object {AsObject.NamePrivate}");
            AddEvtPreData(x.Self);
        });
    }

    public override void Register()
    {
        _field = GetModule<Field>();
        _preDataService = GetModule<PreDataService>();
    }
}