using p3rpc.commonmodutils;
using p3rpc.nativetypes.Interfaces;
using Reloaded.Hooks.Definitions;
using riri.eventframework;
using System.Runtime.InteropServices;
using Reloaded.Hooks.Definitions.X64;
using UE.Toolkit.Core.Types;
using UE.Toolkit.Core.Types.Unreal.UE5_4_4;
using HashableInt = UE.Toolkit.Core.Types.Unreal.UE5_4_4.HashableInt;
using UWorld = p3rpc.nativetypes.Interfaces.UWorld;

namespace p3rpc.eventframework.Hooks
{
    internal class Event : ModuleBase<EventContext>
    {
        private string UAtlEvtSubsystem_GetEvtPreData_SIG = "48 89 5C 24 ?? 48 89 74 24 ?? 48 89 7C 24 ?? 55 41 56 41 57 48 8D 6C 24 ?? 48 81 EC 90 00 00 00 45 0F B6 F8";
        private IHook<UAtlEvtSubsystem_GetEvtPreData> _getEvtPreData;
        [Function(CallingConventions.Microsoft)]
        public unsafe delegate FAtlEvtPreData* UAtlEvtSubsystem_GetEvtPreData(UAtlEvtSubsystem* self, FAtlEvtPreData* dataOut, EAtlEvtEventCategoryType category, uint MajorId, uint MinorId);

        private unsafe FAtlEvtPreData* UAtlEvtSubsystem_GetEvtPreDataImpl(UAtlEvtSubsystem* self, FAtlEvtPreData* dataOut, EAtlEvtEventCategoryType category, uint MajorId, uint MinorId)
        {
            var preHash = UAtlEvtSubsystem.GetEvtPreDataHash(category, MajorId, MinorId);
            var pPreDataMap = (UE.Toolkit.Core.Types.Unreal.UE5_4_4.TMap<HashableInt, FAtlEvtPreData>*)(&self->EvtPreDataMap);
            var PreDataMap = new TMapDictionary<HashableInt, FAtlEvtPreData>(pPreDataMap, _context._toolkitMemory);
            Ptr<FAtlEvtPreData> foundPreData = PreDataMap[new((int)preHash)];
            NativeMemory.Fill(dataOut, (nuint)sizeof(FAtlEvtPreData), 0);
            if (!_preDataService.CustomEvtPreDataAdapted.TryGetValue(preHash, out PreDataAdapter preDataAdapted))
            {
                if (_preDataService.CustomEvtPreDataManaged.TryGetValue(preHash, out PreDataModel preDataManaged))
                {
                    PreDataAdapter? preDataAdaptedMaybe = (foundPreData != null)
                        ? _preDataAdapterFactory.HookFromYamlModel(preDataManaged)
                        : _preDataAdapterFactory.NewFromYamlModel(preDataManaged);
                    if (preDataAdaptedMaybe != null)
                    {
                        _preDataService.CustomEvtPreDataAdapted.TryAdd(preHash, preDataAdaptedMaybe);
                        _preDataService.ToNative(dataOut, foundPreData.Value, preDataAdaptedMaybe);
                    }
                    else _preDataService.ToNative(dataOut, foundPreData.Value, null);
                }
                else
                {
                    if (foundPreData.Value != null) _preDataService.ToNative(dataOut, foundPreData.Value, null);
                    else dataOut->MakeInvalidEvent();
                }
            }
            else
            {
                _preDataService.ToNative(dataOut, foundPreData.Value, preDataAdapted);
            }
            return dataOut;
        }
        
        private string UAtlEvtSubsystem_DoesLevelStreamingLevelExist_SIG = "48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 40 4C 89 C7";
        [Function(CallingConventions.Microsoft)]
        public unsafe delegate byte UAtlEvtSubsystem_DoesLevelStreamingLevelExist(UAtlEvtSubsystem* self, UWorld* worldOut, nativetypes.Interfaces.FString* pathOut);
        private IHook<UAtlEvtSubsystem_DoesLevelStreamingLevelExist> _doesLevelStreamingExist;


        public unsafe byte UAtlEvtSubsystem_DoesLevelStreamingLevelExistImpl(UAtlEvtSubsystem* self, UWorld* BaseWorld, nativetypes.Interfaces.FString* StreamPath)
        {

            string StreamPathStr = StreamPath->ToString();
            _context._utils.Log($"UAtlEvtSubsystem::DoesLevelStreamingLevelExist: {StreamPathStr}");
            byte bInExistingLevelList = _doesLevelStreamingExist.OriginalFunction(self, BaseWorld, StreamPath);
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
        private PreDataAdapterFactory _preDataAdapterFactory;

        public unsafe Event(EventContext context, Dictionary<string, ModuleBase<EventContext>> modules) : base(context, modules)
        {
            _context._utils.SigScan(UAtlEvtSubsystem_GetEvtPreData_SIG, "UAtlEvtSubsystem::GetEvtPreData", _context._utils.GetDirectAddress,
                addr => _getEvtPreData = _context._utils.MakeHooker<UAtlEvtSubsystem_GetEvtPreData>(UAtlEvtSubsystem_GetEvtPreDataImpl, addr));
            _context._utils.SigScan(UAtlEvtSubsystem_DoesLevelStreamingLevelExist_SIG, "UAtlEvtSubsystem::DoesLevelStreamingLevelExist", _context._utils.GetDirectAddress,
                addr => _doesLevelStreamingExist = _context._utils.MakeHooker<UAtlEvtSubsystem_DoesLevelStreamingLevelExist>(UAtlEvtSubsystem_DoesLevelStreamingLevelExistImpl, addr));
        }

        public override void Register()
        {
            _field = GetModule<Field>();
            _preDataService = GetModule<PreDataService>();
            _preDataAdapterFactory = GetModule<PreDataAdapterFactory>();
        }
    }
}
