namespace riri.eventframework.Interfaces;

public interface IEventFramework
{
    /// <summary>
    /// Add an additional folder, specified in <paramref name="path"/>, to search for Pre Event files
    /// This folder is treated like it was the UnrealEssentials folder inside of a mod
    /// </summary>
    /// <param name="path">Path to the folder that contains Pre Event files</param>
    void AddFolder(string path);
}