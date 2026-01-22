using p3rpc.commonmodutils;
using p3rpc.nativetypes.Interfaces;
using riri.eventframework;

namespace p3rpc.eventframework.Hooks;

public class Common : ModuleBase<EventContext>
{
    public ICommonMethods.GetUGlobalWork _getUGlobalWork;
    private bool bCheckedSteam = false;
    private bool bIsAigis = false;
    
    public unsafe IGlobalWork GetUGlobalWorkEx()
    {
        var data = _getUGlobalWork();
        if (!bCheckedSteam)
        {
            _context.bIsSteam = Native.GetModuleHandleA("steam_api64") != nint.Zero;
            bCheckedSteam = true;
        }
        if (!bIsAigis) return new GlobalWork(data);
        if (_context.bIsSteam)
            return new nativetypes.Interfaces.Astrea.GlobalWork((nativetypes.Interfaces.Astrea.UGlobalWork*)data);
        return new nativetypes.Interfaces.Astrea.GlobalWorkUWP((nativetypes.Interfaces.Astrea.UGlobalWorkUWP*)data);
    }
    
    public Common(EventContext context, Dictionary<string, ModuleBase<EventContext>> modules) : base(context, modules)
    {
        _context._sharedScans.CreateListener<ICommonMethods.GetUGlobalWork>(addr => _context._utils.AfterSigScan(addr, _context._utils.GetDirectAddress, addr => _getUGlobalWork = _context._utils.MakeWrapper<ICommonMethods.GetUGlobalWork>(addr)));
        bIsAigis = _context._utils.IsExecutableEpisodeAigisEx();
    }

    public override void Register() {}
}