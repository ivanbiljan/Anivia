namespace Anivia.CommandModules;

public sealed class LavaPlayerExtension
{
    public bool IsTrackLooped { get; set; }
        
    public bool IsQueueLooped { get; set; }

    public CustomLavalinkQueue Queue { get; } = new();
}