using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace riri.eventframework.Yaml
{
    internal static class Serializer
    {
        public static readonly IDeserializer deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .WithTypeConverter(PreDataYamlConverter.Instance)
            .Build();
    }
}
