namespace Anivia.Options;

public sealed class DiscordOptions
{
    public const string SectionName = "Discord";

    public string BotToken { get; set; }

    public string ClientId { get; set; }

    public string ClientSecret { get; set; }

    public ulong TextChannelId { get; set; }

    public List<string> CommandPrefixes { get; set; } = new();
}