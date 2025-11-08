using riri.eventframework.Yaml;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace riri.eventframework
{
    public class PreDataSerializer : YamlMappingParser<PreDataModel>
    {
        public static readonly YamlMappingParser<PreDataModel> Instance = new PreDataSerializer();
        public PreDataSerializer() : base()
        {
            ValueParsers.Add("EventLevel", ReadEventLevel);
            ValueParsers.Add("EventSublevels", ReadEventSublevels);
            ValueParsers.Add("LightScenarioSublevels", ReadLightScenarioSublevels);
            ValueParsers.Add("DungeonSublevel", ReadDungeonSublevel);
            ValueParsers.Add("bDisableAutoLoadFirstLightingScenarioLevel", ReadDisableAutoLoadFirstLightingScenarioLevel);
            ValueParsers.Add("bForceDisableUseCurrentTimeZone", ReadForceDisableUseCurrentTimeZone);
            ValueParsers.Add("ForcedCldTimeZoneValue", ReadForcedCldTimeZoneValue);
            ValueParsers.Add("ForceMonth", ReadForceMonth);
            ValueParsers.Add("ForceDay", ReadForceDay);
        }

        private void ReadEventLevel(IParser parser, PreDataModel data) => data.EventLevel = parser.Consume<Scalar>().Value;
        private void ReadEventSublevels(IParser parser, PreDataModel data)
        {
            data.EventSublevels = new();
            parser.Consume<SequenceStart>();
            while (parser.Accept<MappingStart>(out _))
                data.EventSublevels.Add(PreDataSublevelConverter.Instance.ReadCurrentMapping(parser, new PreDataSublevels()));
            parser.Consume<SequenceEnd>();
        }
        private void ReadLightScenarioSublevels(IParser parser, PreDataModel data) => data.LightScenarioSublevels = ReadSequence(parser);
        private void ReadDungeonSublevel(IParser parser, PreDataModel data) => data.DungeonSublevel = PreDataDungeonSublevelConverter.Instance.ReadCurrentMapping(parser, new PreDataDungeonSublevel());
        private void ReadDisableAutoLoadFirstLightingScenarioLevel(IParser parser, PreDataModel data) => data.bDisableAutoLoadFirstLightingScenarioLevel = parser.Consume<Scalar>().Value == "true" ? true : false;
        private void ReadForceDisableUseCurrentTimeZone(IParser parser, PreDataModel data) => data.bForceDisableUseCurrentTimeZone = parser.Consume<Scalar>().Value == "true" ? true : false;
        private void ReadForcedCldTimeZoneValue(IParser parser, PreDataModel data) => data.ForcedCldTimeZoneValue = byte.Parse(parser.Consume<Scalar>().Value);
        private void ReadForceMonth(IParser parser, PreDataModel data) => data.ForceMonth = int.Parse(parser.Consume<Scalar>().Value);
        private void ReadForceDay(IParser parser, PreDataModel data) => data.ForceDay = int.Parse(parser.Consume<Scalar>().Value);
    }

    public class PreDataSublevelConverter : YamlMappingParser<PreDataSublevels>
    {
        public static readonly YamlMappingParser<PreDataSublevels> Instance = new PreDataSublevelConverter();
        public PreDataSublevelConverter() : base()
        {
            ValueParsers.Add("EventBGLevels", ReadEventBGLevels);
            ValueParsers.Add("BGFieldSeasonSubLevel", ReadBGFieldSeasonSubLevel);
            ValueParsers.Add("BGFieldSoundSubLevel", ReadBGFieldSoundSublevel);
        }

        private void ReadEventBGLevels(IParser parser, PreDataSublevels data) => data.EventBGLevels = ReadSequence(parser);
        private void ReadBGFieldSeasonSubLevel(IParser parser, PreDataSublevels data) => data.BGFieldSeasonSubLevel = NullIfEmpty(parser.Consume<Scalar>().Value);
        private void ReadBGFieldSoundSublevel(IParser parser, PreDataSublevels data) => data.BGFieldSoundSubLevel = NullIfEmpty(parser.Consume<Scalar>().Value);
    }

    public class PreDataDungeonSublevelConverter : YamlMappingParser<PreDataDungeonSublevel>
    {
        public static readonly YamlMappingParser<PreDataDungeonSublevel> Instance = new PreDataDungeonSublevelConverter();
        public PreDataDungeonSublevelConverter() : base()
        {
            ValueParsers.Add("EventBGFloorLevel", ReadEventBGFloorLevel);
            ValueParsers.Add("BGEnvironmentSubLevel", ReadBGEnvironmentSubLevel);
        }
        private void ReadEventBGFloorLevel(IParser parser, PreDataDungeonSublevel data) => data.EventBGFloorLevel = NullIfEmpty(parser.Consume<Scalar>().Value);
        private void ReadBGEnvironmentSubLevel(IParser parser, PreDataDungeonSublevel data) => data.BGEnvironmentSubLevel = NullIfEmpty(parser.Consume<Scalar>().Value);
    }
    public class PreDataYamlConverter : IYamlTypeConverter
    {
        public static readonly IYamlTypeConverter Instance = new PreDataYamlConverter();
        public bool Accepts(Type type) => type == typeof(PreDataModel);
        public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
            => PreDataSerializer.Instance.ReadCurrentMapping(parser, new PreDataModel());
        public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
            => throw new NotImplementedException();
    }
}
