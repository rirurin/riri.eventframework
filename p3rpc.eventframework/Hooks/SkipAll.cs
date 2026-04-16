using System.Runtime.InteropServices;
using p3rpc.commonmodutils;
using p3rpc.eventframework.Configuration;
using Reloaded.Hooks.Definitions;
using Reloaded.Memory.Extensions;
using riri.eventframework;
using RyoTune.Persona3Reload.Types;
using RyoTune.Reloaded;
using UBustupDraw = p3rpc.nativetypes.Interfaces.UBustupDraw;
using UClass = UE.Toolkit.Core.Types.Unreal.UE4_27_2.UClass;

namespace p3rpc.eventframework.Hooks;

[StructLayout(LayoutKind.Explicit, Size = 0x18)]
internal struct FBustupDrawParam
{
    [FieldOffset(0x0)] public int CharaId;
    [FieldOffset(0x4)] public int ExprId;
    [FieldOffset(0x8)] public int CostumeId;
    [FieldOffset(0x10)] public int HasBlush;
    [FieldOffset(0x14)] public int HasSweat;
}

// ReSharper disable once ClassNeverInstantiated.Global
internal class SkipAll : ModuleBase<EventContext>
{
    
    private string[] UBustupDraw_SetObjectPointers_SIG =
    [
        "48 89 74 24 ?? 57 48 83 EC 20 4C 8B 41 ?? 48 89 CF",
        "48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 40 49 8B F8 48 85 D2",
    ];
    
    private unsafe delegate void UBustupDraw_SetObjectPointers(UBustupDraw* This);
    private UBustupDraw_SetObjectPointers? _UBustupDraw_SetObjectPointers;
    private MultiSignature _UBustupDraw_SetObjectPointers_MS;
    
    public unsafe delegate void AAtlEvtEventManager_Tick(AAtlEvtEventManager* This, float Delta);
    private IHook<AAtlEvtEventManager_Tick>? _evtEventManagerTick;
    private static int Tick_Offset;

    private unsafe void AAtlEvtEventManager_TickImpl(AAtlEvtEventManager* This, float Delta)
    {
        _evtEventManagerTick!.OriginalFunction(This, Delta);
        // Hide bustup if it's still on screen after PauseControllerActor is null
        if (This->PauseControllerActor == null)
        {
            // Hide bustup
            var GWork = _common!.GetUGlobalWorkEx();
            var BustupDraw = GWork.GetBustupController()->pModel->pBustupDraw;
            var pState = (int*)((nint)BustupDraw + 0x30);
            if (*pState != 4)
            {
                var Param = (FBustupDrawParam*)((nint)BustupDraw + 0x58);
                Param->CharaId = 0;
                Param->ExprId = 0;
                Param->CostumeId = 0;
                _UBustupDraw_SetObjectPointers!(BustupDraw);
                *pState = 4;   
            }
        }
        if (This->EventRank == EEventManagerEventRank.EventRankA) return;
        // Make event skippable
        *(byte*)((nint)This + 0x2a8) = ((Config)_context._config).CanSkipAnyEvent.ToByte();
    }
    
    public unsafe SkipAll(EventContext context, Dictionary<string, ModuleBase<EventContext>> modules) : base(context, modules)
    {
        _UBustupDraw_SetObjectPointers_MS = new();
        _context._utils.MultiSigScan(UBustupDraw_SetObjectPointers_SIG,
            "UAtlEvtSubsystem::DoesLevelStreamingLevelExist", _context._utils.GetDirectAddress, 
            x => _UBustupDraw_SetObjectPointers = _context._utils.MakeWrapper<UBustupDraw_SetObjectPointers>(x), 
            _UBustupDraw_SetObjectPointers_MS);
        Project.Inis.UsingSetting<int>(Constants.UnrealIniId, "Tick", nameof(AActor),
            x => Tick_Offset = x);
        // UBlueprintGeneratedClass
        _context._toolkitObjects.OnObjectLoadedByName<UClass>("BP_AtlEvtEventManager_C", x =>
        {
            var DefaultObject = x.Self->class_default_obj;
            if (DefaultObject != null)
            {
                _evtEventManagerTick ??= _context._hooks.CreateHook<AAtlEvtEventManager_Tick>(
                    AAtlEvtEventManager_TickImpl, *(nint*)(*(nint*)DefaultObject + Tick_Offset)).Activate();
            }
        });
    }

    private Common? _common;

    public override void Register()
    {
        _common = GetModule<Common>();
    }
}