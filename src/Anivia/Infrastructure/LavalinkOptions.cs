namespace Anivia.Infrastructure;

public sealed class LavalinkOptions
{
    public const string SectionName = "Lavalink";

    public string Host { get; set; }

    public bool IsSsl { get; set; }

    public string Password { get; set; }

    public ushort Port { get; set; }
}