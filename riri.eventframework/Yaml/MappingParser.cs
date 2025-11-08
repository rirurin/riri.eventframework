using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace riri.eventframework.Yaml
{
    public abstract class YamlMappingParser<TDeserializeModel> where TDeserializeModel : class
    {
        public Dictionary<string, EvtPreDataModelGetField> ValueParsers { get; init; } = new();
        public delegate void EvtPreDataModelGetField(IParser parser, TDeserializeModel data);
        public TDeserializeModel ReadCurrentMapping(IParser parser, TDeserializeModel data)
        {
            parser.Consume<MappingStart>();
            while (parser.Accept<Scalar>(out _))
            {
                var currKey = parser.Consume<Scalar>().Value;
                if (!ValueParsers.TryGetValue(currKey, out EvtPreDataModelGetField? getFieldCb)) break;
                getFieldCb(parser, data);
            }
            parser.Consume<MappingEnd>();
            return data;
        }
        public static string? NullIfEmpty(string str) => (str.Length > 0) ? str : null;
        public static List<string> ReadSequence(IParser parser)
        {
            var sequenceIn = new List<string>();
            parser.Consume<SequenceStart>();
            while (parser.Accept<Scalar>(out _))
                sequenceIn.Add(parser.Consume<Scalar>().Value);
            parser.Consume<SequenceEnd>();
            return sequenceIn;
        }
    }
}
