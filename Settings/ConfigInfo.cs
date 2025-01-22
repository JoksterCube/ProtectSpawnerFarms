namespace JoksterCube.ProtectSpawnerFarms.Settings;

internal class ConfigInfo
{
    public string Name { get; }
    public string Description { get; }

    public ConfigInfo(string name, string description)
    {
        Name = name;
        Description = description;
    }
}
