using p3rpc.commonmodutils;
using p3rpc.nativetypes.Interfaces;
using Reloaded.Hooks.Definitions;
using riri.eventframework;
using System.Runtime.InteropServices;
using UE.Toolkit.Core.Types;
using UE.Toolkit.Core.Types.Unreal.UE5_4_4;
using HashableInt = UE.Toolkit.Core.Types.Unreal.UE5_4_4.HashableInt;

namespace p3rpc.eventframework.Hooks
{
    internal class Event : ModuleBase<EventContext>
    {
        private string UAtlEvtSubsystem_GetEvtPreData_SIG = "48 89 5C 24 ?? 48 89 74 24 ?? 48 89 7C 24 ?? 55 41 56 41 57 48 8D 6C 24 ?? 48 81 EC 90 00 00 00 45 0F B6 F8";
        private IHook<UAtlEvtSubsystem_GetEvtPreData> _getEvtPreData;
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
                        _preDataService.CustomEvtPreDataAdapted.Add(preHash, preDataAdaptedMaybe);
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

        private PreDataService _preDataService;
        private PreDataAdapterFactory _preDataAdapterFactory;

        public unsafe Event(EventContext context, Dictionary<string, ModuleBase<EventContext>> modules) : base(context, modules)
        {
            _context._utils.SigScan(UAtlEvtSubsystem_GetEvtPreData_SIG, "UAtlEvtSubsystem::GetEvtPreData", _context._utils.GetDirectAddress,
                addr => _getEvtPreData = _context._utils.MakeHooker<UAtlEvtSubsystem_GetEvtPreData>(UAtlEvtSubsystem_GetEvtPreDataImpl, addr));
        }

        public override void Register()
        {
            _preDataService = GetModule<PreDataService>();
            _preDataAdapterFactory = GetModule<PreDataAdapterFactory>();
        }
    }
}
