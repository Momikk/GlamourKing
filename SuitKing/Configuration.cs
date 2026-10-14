using Dalamud.Configuration;

namespace SuitKing;

[Serializable]
public sealed class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;
    public bool HideGearsetItems { get; set; }
}
