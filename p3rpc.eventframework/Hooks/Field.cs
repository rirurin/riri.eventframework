using System.Runtime.InteropServices;
using System.Text;
using p3rpc.commonmodutils;
using p3rpc.nativetypes.Interfaces;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X64;
using riri.eventframework;

using FName = UE.Toolkit.Core.Types.Unreal.UE5_4_4.FName;

namespace p3rpc.eventframework.Hooks;

public class Field : ModuleBase<EventContext>
{
    private string ULevelStreamingDynamic_LoadLevelInstance_SIG = "E8 ?? ?? ?? ?? 48 8B 4D ?? 49 89 06 48 85 C9 74 ?? E8 ?? ?? ?? ?? 48 8B 4D ?? 48 85 C9 74 ?? E8 ?? ?? ?? ?? 48 8B 9C 24 ?? ?? ?? ??";
    [Function(CallingConventions.Microsoft)]
    public unsafe delegate ULevelStreaming* ULevelStreamingDynamic_LoadLevelInstance(UObject* WorldContextObject, FString* LevelName, FVector* Location, FRotator* Rotation, byte* bOutSuccess, FString* OptionalLevelNameOverride);
    public ULevelStreamingDynamic_LoadLevelInstance? _loadLevelInstance;
    
    // UGameplayStatics::GetStreamingLevel
    private string ULevelStreaming_GetStreamingLevel_SIG = "48 89 54 24 ?? 55 53 56 57 41 55 41 56 41 57 48 8B EC 48 83 EC 40";
    public unsafe delegate ULevelStreaming* ULevelStreaming_GetStreamingLevel(UObject* WorldContextObject, FName Name);
    private IHook<ULevelStreaming_GetStreamingLevel> _getStreamingLevel;
    
    public unsafe ULevelStreaming* ULevelStreaming_GetStreamingLevelImpl(UObject* WorldContextObject, FName Name)
    {
        if (Name.Equals(new FName())) { return null; }
        ULevelStreaming* Result = _getStreamingLevel.OriginalFunction(WorldContextObject, Name);
        _context._utils.Log($"ULevelStreaming::GetStreamingLevel: {Name.ToString()}: {(nint)Result:X}");
        if (Result == null)
        {
            if (NewLevels.TryGetValue(Name.ToString(), out var NewLevel))
            {
                Result = (ULevelStreaming*)NewLevel;
            } else
            {
                // Try to create a new level...
                FVector OriginLocation = new FVector(0, 0, 0);
                FRotator OriginRotator = new FRotator(0, 0, 0);
                byte bSucceeded = 0;
                FString* StreamPathCopy = (FString*)_context._toolkitObjects.CreateFString(Name.ToString());
                FString* LevelNameOverride = (FString*)_context._toolkitObjects.CreateFString("");
                _context._logger.WriteLine($"{StreamPathCopy->ToString()} / {LevelNameOverride->ToString()}");
                Result = _loadLevelInstance!.Invoke(WorldContextObject, StreamPathCopy, &OriginLocation, &OriginRotator, &bSucceeded, LevelNameOverride);
                _context._toolkitMemory.Free((nint)StreamPathCopy);
                _context._toolkitMemory.Free((nint)LevelNameOverride);
                if (bSucceeded == 1 && Result != null)
                {
                    _context._logger.WriteLine($"Added level {Name.ToString()} to the level streaming registry: 0x{(nint)Result:X}");
                    NewLevels.Add(Name.ToString(), (nint)Result);
                } else
                {
                    _context._logger.WriteLine($"LOADING LEVEL INSTANCE FAILED: {Name.ToString()}");
                }
            }
        }
        return Result;
    }
    
    private unsafe delegate void ULevelStreaming_SetShouldBeLoaded(ULevelStreaming* Self, byte bVisible); // vtable + 0x270
    private static int SetShouldBeLoaded_Offset = 0x270;
    private unsafe delegate bool ULevelStreaming_ShouldBeLoaded(ULevelStreaming* Self); // vtable + 0x278
    private static int ShouldBeLoaded_Offset = 0x278;

    // UGameplayStatics::UnloadStreamLevel
    private string UGameplayStatics_UnloadStreamLevel_SIG = "48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 54 24 ?? 56 48 83 EC 50";
    public unsafe delegate void UGameplayStatics_UnloadStreamLevel(UObject* WorldContextObject, FName Name, nint LatentInfo, bool bShouldBlockOnUnload);
    private IHook<UGameplayStatics_UnloadStreamLevel> _unloadStreamingLevel;
    
    public unsafe void UGameplayStatics_UnloadStreamLevelImpl(UObject* WorldContextObject, FName Name, nint LatentInfo, bool bShouldBlockOnUnload)
    {
        
        string ExecutionFunction = ((FName*)(LatentInfo + 0x8))->ToString();
        string LevelPath = Name.ToString();
        _context._utils.Log($"UGameplayStatics::UnloadStreamingLevel: {LevelPath} (EXEC: {ExecutionFunction})");
        _unloadStreamingLevel.OriginalFunction(WorldContextObject, Name, LatentInfo, bShouldBlockOnUnload);
        if (NewLevels.TryGetValue(LevelPath, out var NewLevel))
        {
            _context._utils.Log($"Force hide this! {LevelPath}");
            var level = (ULevelStreaming*)NewLevel;
            var setShouldLoad = _context._hooks.CreateWrapper<ULevelStreaming_SetShouldBeLoaded>(*(nint*)(*(nint*)level + SetShouldBeLoaded_Offset), out _);
            setShouldLoad(level, 0);
        }
    }
   
    private string AFldCmmActor_CheckExistSpawnActor_SIG = "48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 41 54 41 56 48 83 EC 20 4D 63 E1";
    private IHook<AFldCmmActor_CheckExistSpawnActor> _checkExistSpawnActor;
    public unsafe delegate int AFldCmmActor_CheckExistSpawnActor(TArray<nint>* cmmExist, short uniqId, byte mType, int daysPassed);
    public unsafe int AFldCmmActor_CheckExistSpawnActorImpl(TArray<nint>* cmmExist, short uniqId, byte mType, int daysPassed)
    {
        // TODO: Write proper logic for this
        return 1;
        //_checkExistSpawnActor.OriginalFunction(cmmExist, uniqId, mType, daysPassed);
    
    }
    
    public Dictionary<string, nint> NewLevels = new();
    
    public unsafe Field(EventContext context, Dictionary<string, ModuleBase<EventContext>> modules) : base(context,
        modules)
    {
        _context._utils.SigScan(ULevelStreamingDynamic_LoadLevelInstance_SIG, "ULevelStreamingDynamic::LoadLevelInstance", _context._utils.GetIndirectAddressShort,
            addr => _loadLevelInstance = _context._utils.MakeWrapper<ULevelStreamingDynamic_LoadLevelInstance>(addr));   
        _context._utils.SigScan(ULevelStreaming_GetStreamingLevel_SIG, "ULevelStreaming::GetStreamingLevel", _context._utils.GetDirectAddress,
            addr => _getStreamingLevel = _context._utils.MakeHooker<ULevelStreaming_GetStreamingLevel>(ULevelStreaming_GetStreamingLevelImpl, addr));
        _context._utils.SigScan(UGameplayStatics_UnloadStreamLevel_SIG, "UGameplayStatics::UnloadStreamLevel", _context._utils.GetDirectAddress,
            addr => _unloadStreamingLevel = _context._utils.MakeHooker<UGameplayStatics_UnloadStreamLevel>(UGameplayStatics_UnloadStreamLevelImpl, addr));
        _context._utils.SigScan(AFldCmmActor_CheckExistSpawnActor_SIG, "AFldCmmActor::CheckExistSpawnActor", _context._utils.GetDirectAddress,
            addr => _checkExistSpawnActor = _context._utils.MakeHooker<AFldCmmActor_CheckExistSpawnActor>(AFldCmmActor_CheckExistSpawnActorImpl, addr));
    }
    public override void Register()
    {
    }   
}