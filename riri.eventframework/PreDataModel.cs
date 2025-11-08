using p3rpc.nativetypes.Interfaces;

namespace riri.eventframework
{
    public class PreDataSublevels
    {
        public List<string> EventBGLevels { get; set; } = new();
        public string? BGFieldSeasonSubLevel { get; set; }
        public string? BGFieldSoundSubLevel { get; set; }
        public int BGFieldMajorID { get; set; }
        public int BGFieldMinorID { get; set; }
    }
    public class PreDataDungeonSublevel
    {
        public string? EventBGFloorLevel { get; set; }
        public string? BGEnvironmentSubLevel { get; set; }
    }
    public class PreDataModel
    {
        public uint Hash { get; private set; } = 0;
        public void RecalculateHash() => Hash = UAtlEvtSubsystem.GetEvtPreDataHash((EAtlEvtEventCategoryType)EventCategoryTypeID, (uint)EventMajorID, (uint)EventMinorID);
        public int EventMajorID { get; set; }
        public int EventMinorID { get; set; }
        public int EventCategoryTypeID { get; set; }
        public string EventRank { get; set; } = "";
        public string EventCategory { get; set; } = "";
        public string? EventLevel { get; set; } = null;
        public List<PreDataSublevels>? EventSublevels { get; set; } = null;
        public List<string>? LightScenarioSublevels { get; set; } = null;
        public PreDataDungeonSublevel? DungeonSublevel { get; set; } = null;
        public bool? bDisableAutoLoadFirstLightingScenarioLevel { get; set; } = null;
        public bool? bForceDisableUseCurrentTimeZone { get; set; } = null;
        public byte? ForcedCldTimeZoneValue { get; set; } = null;
        public int? ForceMonth { get; set; } = null;
        public int? ForceDay { get; set; } = null;
    }
}
