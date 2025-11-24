using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using p3rpc.commonmodutils;
using p3rpc.nativetypes.Interfaces;
using riri.eventframework.Game;
using riri.eventframework.Yaml;
using UE.Toolkit.Core.Types.Unreal.UE5_4_4;
using HashableInt8 = UE.Toolkit.Core.Types.Unreal.UE5_4_4.HashableInt8;

using FName = p3rpc.nativetypes.Interfaces.FName;
using FString = p3rpc.nativetypes.Interfaces.FString;

namespace riri.eventframework
{
    
    [StructLayout(LayoutKind.Explicit, Pack = 8, Size = 0x50)]
    public unsafe struct FBustupFace
    {
        [FieldOffset(0x0)] public UE.Toolkit.Core.Types.Unreal.UE5_4_4.TMap<int, FBustupCloth> Faces; // Size: 0x50
    }
    
    [StructLayout(LayoutKind.Explicit, Pack = 8, Size = 0x50)]
    public unsafe struct FBustupCloth
    {
        [FieldOffset(0x0)] public UE.Toolkit.Core.Types.Unreal.UE5_4_4.TMap<int, FBustupParts> Clothes; // Size: 0x50
    }
    
    [StructLayout(LayoutKind.Explicit, Pack = 8, Size = 0x40)]
    public unsafe struct FBustupParts
    {
        [FieldOffset(0x0)] public FString Pose; // Size: 0x10
        [FieldOffset(0x10)] public ushort EyePartsID; // Size: 0x2
        [FieldOffset(0x12)] public ushort MouthPartsID; // Size: 0x2
        [FieldOffset(0x14)] public bool bEyeAnim; // Size: 0x1
        [FieldOffset(0x14)] public bool bMouthAnim; // Size: 0x1
        [FieldOffset(0x15)] public byte InBetween; // Size: 0x1
        [FieldOffset(0x18)] public float EyeX; // Size: 0x4
        [FieldOffset(0x1C)] public float EyeY; // Size: 0x4
        [FieldOffset(0x20)] public float MouthX; // Size: 0x4
        [FieldOffset(0x24)] public float MouthY; // Size: 0x4
        [FieldOffset(0x28)] public float BlushX; // Size: 0x4
        [FieldOffset(0x2C)] public float BlushY; // Size: 0x4
        [FieldOffset(0x30)] public float SweatX; // Size: 0x4
        [FieldOffset(0x34)] public float SweatY; // Size: 0x4
        [FieldOffset(0x38)] public float OffsetX; // Size: 0x4
        [FieldOffset(0x3C)] public float OffsetY; // Size: 0x4
    }
    
    public class PreDataService : ModuleBase<EventContext>
    {

        public IGameMethods? _game { get; set; }
        public ConcurrentDictionary<uint, PreDataModel> CustomEvtPreDataManaged = new();
        public ConcurrentDictionary<uint, PreDataAdapter> CustomEvtPreDataAdapted = new();

        public unsafe PreDataService(EventContext context, Dictionary<string, ModuleBase<EventContext>> modules) : base(context, modules)
        {
            _context._toolkitObjects.OnObjectLoadedByName<UObject>("BustupExistDataAsset", x =>
            {
                var CurrentDict = new TMapDynamicDictionary<HashableInt8>(
                    (UE.Toolkit.Core.Types.Unreal.UE5_4_4.TMap<HashableInt8, byte>*)((nint)x.Self + 0x30),
                    typeof(FBustupFace),
                    _context._toolkitMemory);
                _context._utils.Log($"BustupExistDataAsset: 0x{(nint)x.Self:x}");
                _context._utils.Log($"Entries: {CurrentDict.Count}");
                if (CurrentDict.TryGetValue(new HashableInt8(1), out var chara))
                {
                    _context._utils.Log($"Got entry 1: 0x{chara:x}");
                }
            });
        }

        public override void Register()
        {

        }

        private unsafe void CopyDungeonSublevelPreData(FAtlEvtPreDungeonSublevelData* copy, FAtlEvtPreDungeonSublevelData* src)
        {
            copy->EventBGFloorLevel = src->EventBGFloorLevel;
            copy->BGEnvironmentSubLevel = src->BGEnvironmentSubLevel;
        }

        private unsafe TArrayList<FString> CopySublevelsBGLevels(TArrayList<FString> src)
        {
            TArrayList<FString> dst = new(_context._toolkitMemory);
            dst.ResizeTo(src.ArrayMax);
            foreach (var Level in src)
                dst.AddValue(*(FString*)_context._toolkitObjects.CreateFString(Level.Value->ToString()));
            dst.Leak();
            return dst;
        }

        private unsafe void CopySublevelsPreData(TArrayList<FAtlEvtPreSublevelData> dst, TArrayList<FAtlEvtPreSublevelData> src)
        {
            dst.ResizeTo(src.ArrayMax);
            foreach (var Level in src)
            {
                var BgLevelList = new TArrayList<FString>((UE.Toolkit.Core.Types.Unreal.UE5_4_4.TArray<FString>*)(&Level.Value->EventBGLevels), _context._toolkitMemory);
                var BgLevelListOut = CopySublevelsBGLevels(BgLevelList);
                
                dst.AddValue(new()
                {
                    EventBGLevels = *(p3rpc.nativetypes.Interfaces.TArray<FString>*)BgLevelListOut.Base(),
                    BGFieldMajorID = Level.Value->BGFieldMajorID,
                    BGFieldMinorID = Level.Value->BGFieldMinorID,
                    BGFieldSeasonSubLevel = *(FString*)_context._toolkitObjects.CreateFString(Level.Value->BGFieldSeasonSubLevel.ToString()),
                    BGFieldSoundSubLevel = *(FString*)_context._toolkitObjects.CreateFString(Level.Value->BGFieldSoundSubLevel.ToString())
                });
                _context._toolkitMemory.Free((nint)BgLevelListOut.Base());
            }
        }

        public unsafe void ToNative(FAtlEvtPreData* copy, FAtlEvtPreData* original, PreDataAdapter? hook)
        {
            // These values would be the same anyway...
            copy->EventMajorID = (original != null) ? original->EventMajorID : hook!.EventMajorID;
            copy->EventMinorID = (original != null) ? original->EventMinorID : hook!.EventMinorID;
            copy->EventCategoryTypeID = (original != null) ? original->EventCategoryTypeID : hook!.EventCategoryTypeID;
            copy->EventRank = (original != null) ? original->EventRank : hook!.GetEventRankNT();
            copy->EventCategory = (original != null) ? original->EventCategory : hook!.GetEventCategoryNT();

            // Set Event Level file path
            var EventLevelSource = (hook is { EventLevel: not null } ) switch
            {
                true => (FString*)hook.EventLevel, false => &original->EventLevel
            };
            copy->EventLevel = *(FString*)_context._toolkitObjects.CreateFString(EventLevelSource->ToString());
            // Set Event Sublevels
            var EventSublevelSrc = (hook is { EventSublevels: not null }) switch
            {
                true => hook.EventSublevels,
                false => new TArrayList<FAtlEvtPreSublevelData>((UE.Toolkit.Core.Types.Unreal.UE5_4_4.TArray<FAtlEvtPreSublevelData>*)(&original->EventSublevels), _context._toolkitMemory)
            };
            var EventSublevelDest = new TArrayList<FAtlEvtPreSublevelData>((UE.Toolkit.Core.Types.Unreal.UE5_4_4.TArray<FAtlEvtPreSublevelData>*)(&copy->EventSublevels), _context._toolkitMemory);
            CopySublevelsPreData(EventSublevelDest, EventSublevelSrc);
            // Set Event Light Scenario Sublevels
            var LightScenarioDest = new TArrayList<FName>((UE.Toolkit.Core.Types.Unreal.UE5_4_4.TArray<FName>*)(&copy->LightScenarioSublevels), _context._toolkitMemory);
            switch (hook is { LightScenarioSublevels: not null })
            {
                case true:
                    LightScenarioDest.ResizeTo(hook.LightScenarioSublevels.ArrayMax);
                    foreach (var Sublevel in hook.LightScenarioSublevels)
                        LightScenarioDest.AddValue(Sublevel.Value->ToNT());
                    break;
                case false:
                    var LightScenarioArr = new TArrayList<FName>(
                        (UE.Toolkit.Core.Types.Unreal.UE5_4_4.TArray<FName>*)(&original->LightScenarioSublevels),
                        _context._toolkitMemory);
                    LightScenarioDest.ResizeTo(LightScenarioArr.ArrayMax);
                    foreach (var Sublevel in LightScenarioArr)
                        LightScenarioDest.AddValue(*Sublevel.Value);
                    break;
            }
            // Set Dungeon Sublevels (optional)
            var DungeonSublevelSrc = (hook is { DungeonSublevel: not null }) switch
            {
                true => hook.DungeonSublevel, false => original != null ? &original->DungeonSublevel : null
            };
            if (DungeonSublevelSrc != null)
            {
                CopyDungeonSublevelPreData(&copy->DungeonSublevel, DungeonSublevelSrc);
            }
        }

        private bool TryRegisterPreEventYaml(string path)
        {
            var leafDirectory = Path.GetDirectoryName(path)!.Split(Path.DirectorySeparatorChar)[^1];
            var fileNameComponents = Path.GetFileNameWithoutExtension(path).Split("_", 2);
            return fileNameComponents[0] == "PRE" && fileNameComponents[1] == leafDirectory;
        }

        public void OnModLoaded(string modPath)
        {
            string EventsPath = Path.Join(modPath, _game!.GetCinemaBasePath());
            if (!Path.Exists(EventsPath)) return;
            var preEvtFiles = Directory.EnumerateFiles(EventsPath, "*.*", SearchOption.AllDirectories).Where(
                x => Constants.YAML_EXTENSION.Contains(Path.GetExtension(x).Substring(1)) && TryRegisterPreEventYaml(x)
            );
            foreach (var preEvtFile in preEvtFiles)
            {
                // Get params stored in yml file, then generate anything that can be implied from file name
                // Params can be null if they're an event hook. New events must have all parameters defined to be accepted on GetEvtPreData
                _context._utils.Log($"Reading file {preEvtFile}");
                PreDataModel preDataManaged = Serializer.deserializer.Deserialize<PreDataModel>(new StreamReader(preEvtFile));
                if (preDataManaged == null) { continue; }
                string[] preFileNameParts = Path.GetFileNameWithoutExtension(preEvtFile).Split("_"); // PRE_Event_Cmmu_120_100_C
                                                                                                     // [0]  [1]  [2]  [3] [4] [5]
                preDataManaged.EventMajorID = int.Parse(preFileNameParts[3]);
                preDataManaged.EventMinorID = int.Parse(preFileNameParts[4]);
                preDataManaged.EventCategoryTypeID = _game!.GetEventCategoryTypeInt(preFileNameParts[2]);
                preDataManaged.RecalculateHash();
                preDataManaged.EventRank = preFileNameParts[5];
                preDataManaged.EventCategory = preFileNameParts[2];
                // Validate each sublevel passed into EventSublevels.EventBGLevels
                foreach (var eventSublevel in preDataManaged.EventSublevels!)
                {
                    foreach (var eventBgLevel in eventSublevel.EventBGLevels)
                    {
                        string[] eventBgLevelParts = Path.GetFileNameWithoutExtension(eventBgLevel).Split("_"); // LV_F101_141_001_BG
                                                                                                                // [0] [1] [2] [3] [4]
                        eventSublevel.BGFieldMajorID = int.Parse(eventBgLevelParts[1].Substring(1));
                        eventSublevel.BGFieldMinorID = int.Parse(eventBgLevelParts[2]);
                    }
                }
                var preDataHash = UAtlEvtSubsystem.GetEvtPreDataHash((EAtlEvtEventCategoryType)preDataManaged.EventCategoryTypeID, (uint)preDataManaged.EventMajorID, (uint)preDataManaged.EventMinorID);
                CustomEvtPreDataManaged.TryAdd(preDataHash, preDataManaged);
                _context._utils.Log($"Registered pre event yaml {preDataManaged.EventCategory}_{preDataManaged.EventMajorID:D3}_{preDataManaged.EventMinorID:D3}_{preDataManaged.EventRank} (hash: 0x{preDataHash:X})");

            }
        }
    }
}
