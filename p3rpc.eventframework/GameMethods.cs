using riri.eventframework.Game;

namespace p3rpc.eventframework
{
    internal class GameMethods : IGameMethods
    {
        public string GetCinemaBasePathForUnrealEssentials()
            => Path.Join("P3R", "Content", "Xrd777", "Events", "Cinema");

        public string GetCinemaBasePathForUnrealEngine()
            => $"Game/Xrd777/Events/Cinema";

        public int GetEventCategoryTypeInt(string typeName)
            => typeName switch
            {
                "Main" => 0,
                "Cmmu" => 1,
                "Qest" => 0,
                "Extr" => 0,
                "Fild" => 4,
                _ => throw new Exception($"Unimplemented event category type {typeName}")
            };
    }
}
