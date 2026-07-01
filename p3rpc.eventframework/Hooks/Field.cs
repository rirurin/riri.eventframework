using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using p3rpc.commonmodutils;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X64;
using riri.eventframework;
using riri.yamlscans.ReloadedII;
using RyoTune.Persona3Reload.Types;
using RyoTune.Reloaded;
using UE.Toolkit.Core.Types.Unreal.UE5_4_4;
using FName = UE.Toolkit.Core.Types.Unreal.UE5_4_4.FName;
using UWorld = p3rpc.nativetypes.Interfaces.UWorld;

namespace p3rpc.eventframework.Hooks;

// ReSharper disable once ClassNeverInstantiated.Global
internal class Field : ModuleBase<EventContext>
{
    [Function(CallingConventions.Microsoft)]
    private unsafe delegate ULevelStreaming* ULevelStreamingDynamic_LoadLevelInstance(UObject* WorldContextObject, FString* LevelName, FVector* Location, FRotator* Rotation, byte* bOutSuccess, FString* OptionalLevelNameOverride);
    private readonly SHFunction2<ULevelStreamingDynamic_LoadLevelInstance>? _loadLevelInstance;
    
    // UGameplayStatics::GetStreamingLevel
    private unsafe delegate ULevelStreaming* ULevelStreaming_GetStreamingLevel(UObject* WorldContextObject, FName Name);
    private readonly SHFunction2<ULevelStreaming_GetStreamingLevel> _getStreamingLevel;
    
    private static int GetWorld_Offset;
    public unsafe delegate UWorld* UObject_GetWorld(nint UObject); // vtable + 0x160
    
    /*
    private unsafe ULevelStreaming* ULevelStreaming_GetStreamingLevelImpl(UObject* WorldContextObject, FName Name)
    {
        if (Name.Equals(new FName())) { return null; }
        var Result = _getStreamingLevel.Hook!.OriginalFunction(WorldContextObject, Name);
        if (Result == null)
        {
            var NameStr = Name.ToString();
            var AssetPath = $"{NameStr}.{Path.GetFileName(NameStr)}";
            var getWorld = _context._hooks.CreateWrapper<UObject_GetWorld>(
                *(nint*)(*(nint*)WorldContextObject + GetWorld_Offset), out _);
            var World = getWorld((nint)WorldContextObject);
            var WorldObj = _context._toolkitFactory.CreateUObject((nint)World);
            var StreamedLevels = new TArrayList<Ptr<ULevelStreaming>>((TArray<Ptr<ULevelStreaming>>*)&World->StreamingLevels, _context._toolkitMemory);
            var NewLevel = _context._toolkitSpawning.SpawnObject<ULevelStreamingDynamic>(
                $"LevelStreamingDynamic_{StreamedLevels.Count}", WorldObj);
            var pNewLevel = (ULevelStreaming*)NewLevel.Ptr;
            pNewLevel->WorldAsset.SoftObjectPtr.Super.ObjectId.AssetPath.PackageName = new(AssetPath);
            // *(byte*)((nint)pNewLevel + 0xba) |= 0x20; // Lock Level
            StreamedLevels.AddValue(new (pNewLevel));
            LevelStreamingRegistry.Add(AssetPath);
            Log.Debug($"Added level '{AssetPath}' to the level streaming registry: 0x{(nint)Result:X}");
            Result = _getStreamingLevel.Hook!.OriginalFunction(WorldContextObject, Name);
        }
        Log.Debug($"ULevelStreaming::GetStreamingLevel: {Name.ToString()}: 0x{(nint)Result:X}");
        return Result;
    }
    */
    
    private unsafe ULevelStreaming* ULevelStreaming_GetStreamingLevelImpl(UObject* WorldContextObject, FName Name)
    {
        if (Name.Equals(new FName())) { return null; }
        ULevelStreaming* Result = _getStreamingLevel.Hook!.OriginalFunction(WorldContextObject, Name);
        Log.Debug($"ULevelStreaming::GetStreamingLevel: {Name.ToString()}: {(nint)Result:X}");
        if (Result == null)
        {
            if (NewLevels.TryGetValue(Name.ToString(), out var NewLevel))
            {
                Result = (ULevelStreaming*)NewLevel;
            } else
            {
                if (TryCreateNewLevel(WorldContextObject, Name.ToString(), out Result))
                {
                    Log.Debug($"Added level {Name.ToString()} to the level streaming registry: 0x{(nint)Result:X}");
                    NewLevels.TryAdd(Name.ToString(), (nint)Result);                   
                }
                else
                {
                    Log.Warning($"LOADING LEVEL INSTANCE FAILED: {Name.ToString()}");
                }
            }
        }
        return Result;
    }

    public unsafe bool TryCreateNewLevel(UObject* WorldContextObject, string Name, [MaybeNullWhen(false)] out ULevelStreaming* Result)
    {
        // Try to create a new level...
        var OriginLocation = new FVector { X = 0, Y = 0, Z = 0 };
        var OriginRotator = new FRotator { Pitch = 0, Yaw = 0, Roll = 0 };
        byte bSucceeded = 0;
        var StreamPathCopy = _context._toolkitObjects.CreateFString(Name);
        var LevelNameOverride = _context._toolkitObjects.CreateFString("");
        // ULevelStreaming: CurrentState -> TargetState
        // ECurrentState::Loading -> ETargetState::LoadedVisible
        // ECurrentState::LoadedVisible -> ETargetState::LoadedVisible
        Result = _loadLevelInstance!.Wrapper.Invoke(WorldContextObject, StreamPathCopy, &OriginLocation, &OriginRotator, &bSucceeded, LevelNameOverride);
        _context._toolkitMemory.Free((nint)StreamPathCopy);
        _context._toolkitMemory.Free((nint)LevelNameOverride);
        return bSucceeded == 1 && Result != null;
    }
    
    private unsafe delegate void ULevelStreaming_SetShouldBeLoaded(ULevelStreaming* Self, byte bVisible); // vtable + 0x270
    private static int SetShouldBeLoaded_Offset;
    private unsafe delegate bool ULevelStreaming_ShouldBeLoaded(ULevelStreaming* Self); // vtable + 0x278
    private static int ShouldBeLoaded_Offset;

    // UGameplayStatics::UnloadStreamLevel
    private unsafe delegate void UGameplayStatics_UnloadStreamLevel(UObject* WorldContextObject, FName Name, nint LatentInfo, bool bShouldBlockOnUnload);
    private readonly SHFunction2<UGameplayStatics_UnloadStreamLevel> _unloadStreamingLevel;

    private unsafe void UGameplayStatics_UnloadStreamLevelImpl(UObject* WorldContextObject, FName Name, nint LatentInfo, bool bShouldBlockOnUnload)
    {
        var ExecutionFunction = ((FName*)(LatentInfo + 0x8))->ToString();
        var LevelPath = Name.ToString();
        Log.Debug($"UGameplayStatics::UnloadStreamingLevel: {LevelPath} (EXEC: {ExecutionFunction})");
        _unloadStreamingLevel.Hook!.OriginalFunction(WorldContextObject, Name, LatentInfo, bShouldBlockOnUnload);
        if (NewLevels.TryGetValue(LevelPath, out var NewLevel))
        {
            Log.Debug($"Unloading custom level {LevelPath}");
            var level = (ULevelStreaming*)NewLevel;
            var setShouldLoad = _context._hooks.CreateWrapper<ULevelStreaming_SetShouldBeLoaded>(*(nint*)(*(nint*)level + SetShouldBeLoaded_Offset), out _);
            // ULevelStreaming: CurrentState -> TargetState
            // ECurrentState::Unloaded -> ETargetState::Unloaded
            setShouldLoad(level, 0);
            NewLevels.Remove(LevelPath, out _);
        }
    }
    
    /*
    // Disabled, not neccessary for now
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct FCmmExistAvailablity
    {
        internal fixed uint OkFlags[3];
        internal fixed uint PlayFlags[3];
        internal fixed byte IsAvailableOnDay[365];       
    }

    // In AF_FldBinaryData.arc
    [StructLayout(LayoutKind.Sequential, Size = 0x314)]
    public struct FCmmExistEntry
    {
        internal ushort Arcana;
        internal FCmmExistAvailablity Cmm;
        internal FCmmExistAvailablity Normal;
    }

    public enum EFldCmmNpcType : byte
    {
        Cmm = 0,
        Normal = 1,
    }
   
    private SHFunction<AFldCmmActor_CheckExistSpawnActor> _checkExistSpawnActor;
    public unsafe delegate int AFldCmmActor_CheckExistSpawnActor(TArray<FCmmExistEntry>* cmmExist, short uniqId, EFldCmmNpcType mType, int daysPassed);

    // Values != 1 will destroy the actor
    private unsafe int AFldCmmActor_CheckExistSpawnActorImpl(TArray<FCmmExistEntry>* cmmExist, short uniqId, EFldCmmNpcType mType, int daysPassed)
    {
        var GWork = _common.GetUGlobalWorkEx();
        var Entries = new TArrayList<FCmmExistEntry>(cmmExist, _context._toolkitMemory);
        foreach (var Entry in Entries)
        {
            if (Entry.Value->Arcana != uniqId) continue;
            var Available = mType switch
            {
                EFldCmmNpcType.Cmm => &Entry.Value->Cmm,
                EFldCmmNpcType.Normal => &Entry.Value->Normal
            };
            // For PlayFlags, check that any flag that exists and is false
            // For OkFlags, check that all flags that exists is false
            for (var i = 0; i < 3; i++)
            {
                var PlayFlag = Available->PlayFlags[i];
                if (PlayFlag == uint.MaxValue || GWork.GetBitflag(PlayFlag)) continue;
                for (var j = 0; j < 3; j++)
                {
                    var OkFlag = Available->OkFlags[j];
                    // Log.Debug($"[AFldCmmActor::CheckExistSpawnActor]: OK_FLAG(0x{OkFlag:x})");
                    if (OkFlag == uint.MaxValue)
                    {
                        Log.Debug($"[AFldCmmActor::CheckExistSpawnActor] Availability for {mType}:{uniqId} on day {daysPassed} = {Available->IsAvailableOnDay[daysPassed]}");
                        return Available->IsAvailableOnDay[daysPassed];
                    }

                    if (GWork.GetBitflag(OkFlag)) return -1;
                }
            }
            return -1;
        }
        return 1;
    }
    */
    
    public readonly ConcurrentDictionary<string, nint> NewLevels = new();
    private Common? _common;
    private PreDataService? _preDataService;
    private HashSet<string> LevelStreamingRegistry = [];
    
    public unsafe Field(EventContext context, Dictionary<string, ModuleBase<EventContext>> modules) : base(context,
        modules)
    {
        _loadLevelInstance = new();
        _getStreamingLevel = new(ULevelStreaming_GetStreamingLevelImpl);
        _unloadStreamingLevel = new(UGameplayStatics_UnloadStreamLevelImpl);
        // _checkExistSpawnActor = new(AFldCmmActor_CheckExistSpawnActorImpl);

        Project.Inis.UsingSetting<int>(Constants.UnrealIniId, "SetShouldBeLoaded", nameof(ULevelStreaming),
            x => SetShouldBeLoaded_Offset = x);
        Project.Inis.UsingSetting<int>(Constants.UnrealIniId, "ShouldBeLoaded", nameof(ULevelStreaming),
            x => ShouldBeLoaded_Offset = x);
        Project.Inis.UsingSetting<int>(Constants.UnrealIniId, "GetWorld", nameof(UObject),
            x => GetWorld_Offset = x);
        
        _context._toolkitObjects.OnObjectLoadedByName<UWorld>("LV_Xrd777_P", x =>
        {
            var World = x.Self;
            var WorldObj = _context._toolkitFactory.CreateUObject((nint)World);
            var StreamedLevels = new TArrayList<UE.Toolkit.Core.Types.Ptr<ULevelStreaming>>((TArray<UE.Toolkit.Core.Types.Ptr<ULevelStreaming>>*)&World->StreamingLevels, _context._toolkitMemory);

            foreach (var Level in StreamedLevels)
            {
                var StreamingLevelPtr = Level.Value->Value;
                var PackageName = StreamingLevelPtr->WorldAsset.SoftObjectPtr.Super.ObjectId.AssetPath.PackageName.ToString();
                LevelStreamingRegistry.Add(PackageName);
            }
            var OldLength = StreamedLevels.Count;
            foreach (var EditedLevelPackage in _preDataService!.CachedLevelPackages)
            {
                // This is only required for new events that don't exist in the registry, not events that have modified pre data
                if (LevelStreamingRegistry.Contains(EditedLevelPackage)) continue;
                Log.Debug($"Caching event level with package ID '{EditedLevelPackage}'");
                var NewLevel = _context._toolkitSpawning.SpawnObject<ULevelStreamingDynamic>(
                    $"LevelStreamingDynamic_{StreamedLevels.Count}", WorldObj);
                var pNewLevel = (ULevelStreaming*)NewLevel.Ptr;
                pNewLevel->WorldAsset.SoftObjectPtr.Super.ObjectId.AssetPath.PackageName = new(EditedLevelPackage);
                *(byte*)((nint)pNewLevel + 0xba) |= 0x20; // Lock Level
                StreamedLevels.AddValue(new (pNewLevel));
            }
            Log.Debug($"Added {StreamedLevels.Count - OldLength} levels into the global registry");
        });
    }
    public override void Register()
    {
        _common = GetModule<Common>();
        _preDataService = GetModule<PreDataService>();
    }
}