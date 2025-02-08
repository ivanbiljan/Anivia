using Discord;

namespace Anivia.CommandModules;

public static class Embeds
{
    public static Embed Default(string text)
    {
        return new EmbedBuilder()
            .WithDescription(text)
            .Build();
    }

    public static Embed Error(string text)
    {
        return new EmbedBuilder()
            .WithColor(255, 0, 0)
            .WithDescription(text)
            .Build();
    }

    public static Embed Information(string text)
    {
        return new EmbedBuilder()
            .WithColor(0, 0, 255)
            .WithDescription(text)
            .Build();
    }

    public static Embed Success(string text)
    {
        return new EmbedBuilder()
            .WithColor(0, 255, 0)
            .WithDescription(text)
            .Build();
    }
}