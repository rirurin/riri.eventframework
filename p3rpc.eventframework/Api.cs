using p3rpc.commonmodutils;
using riri.eventframework.Interfaces;
using riri.eventframework;

namespace p3rpc.eventframework;

// ReSharper disable once ClassNeverInstantiated.Global
public class Api(EventContext context, Dictionary<string, ModuleBase<EventContext>> modules)
    : ModuleBase<EventContext>(context, modules), IEventFramework
{
    public override void Register() {}

    public void AddFolder(string path) => GetModule<PreDataService>().RegisterFolder(path);
}