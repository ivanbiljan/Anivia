namespace Anivia.Infrastructure;

public sealed class DiscordOptions
{
    public const string SectionName = "Discord";

    public string BotToken { get; set; }

    public string ClientId { get; set; }

    public string ClientSecret { get; set; }

    public List<string> CommandPrefixes { get; set; } = new();

    public Dictionary<ulong, ulong> TextChannels { get; set; }
}