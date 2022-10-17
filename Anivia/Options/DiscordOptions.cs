using System.Collections.Generic;

namespace Anivia.Options;

public sealed class DiscordOptions
{
    public const string SectionName = "Discord";
    
    public string BotToken { get; set; }

    public string ClientId { get; set; }
    
    public string ClientSecret { get; set; }

    public List<string> CommandPrefixes { get; set; } = new();
}

public sealed class LavalinkOptions
{
    public const string SectionName = "Lavalink";
    
    public string Host { get; set; }
    
    public ushort Port { get; set; }
    
    public string Password { get; set; }
    
    public bool IsSsl { get; set; }
}