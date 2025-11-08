namespace riri.eventframework
{
    public static class Constants
    {
        public static readonly string[] YAML_EXTENSION = { "yaml", "yml" };
        public static string MakeAssetPath(string path) => $"{path}.{path.Split("/")[^1]}";
    }
}
