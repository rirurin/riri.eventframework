using System.Collections.Concurrent;
using p3rpc.commonmodutils;
using p3rpc.nativetypes.Interfaces;
using riri.eventframework.Game;
using riri.eventframework.Yaml;

namespace riri.eventframework
{
    public class PreDataService : ModuleBase<EventContext>
    {

        public IGameMethods? _game { get; set; }
        public ConcurrentDictionary<uint, PreDataModel> NewPreData = new();

        public PreDataService(EventContext context, Dictionary<string, ModuleBase<EventContext>> modules) 
            : base(context, modules) {}

        public override void Register() {}

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
                x => Constants.YAML_EXTENSION.Contains(Path.GetExtension(x)[1..]) && TryRegisterPreEventYaml(x)
            );
            foreach (var preEvtFile in preEvtFiles)
            {
                // Get params stored in yml file, then generate anything that can be implied from file name
                // Params can be null if they're an event hook. New events must have all parameters defined to be accepted on GetEvtPreData
                _context._utils.Log($"Reading file {preEvtFile}");
                var preDataManaged = Serializer.deserializer.Deserialize<PreDataModel>(new StreamReader(preEvtFile));
                if (preDataManaged == null) { continue; }
                var preFileNameParts = Path.GetFileNameWithoutExtension(preEvtFile).Split("_"); // PRE_Event_Cmmu_120_100_C
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
                        var eventBgLevelParts = Path.GetFileNameWithoutExtension(eventBgLevel).Split("_"); // LV_F101_141_001_BG
                                                                                                                // [0] [1] [2] [3] [4]
                        eventSublevel.BGFieldMajorID = int.Parse(eventBgLevelParts[1][1..]);
                        eventSublevel.BGFieldMinorID = int.Parse(eventBgLevelParts[2]);
                    }
                }
                var preDataHash = UAtlEvtSubsystem.GetEvtPreDataHash((EAtlEvtEventCategoryType)preDataManaged.EventCategoryTypeID, (uint)preDataManaged.EventMajorID, (uint)preDataManaged.EventMinorID);
                NewPreData.TryAdd(preDataHash, preDataManaged);
                _context._utils.Log($"Registered pre event yaml {preDataManaged.EventCategory}_{preDataManaged.EventMajorID:D3}_{preDataManaged.EventMinorID:D3}_{preDataManaged.EventRank} (hash: 0x{preDataHash:X})");

            }
        }
    }
}
