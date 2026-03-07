using NetCord;
using NetCord.Rest;

namespace Anivia.CommandModules;

public static class Embeds
{
    public static EmbedProperties Default(string text)
    {
        return new EmbedProperties
        {
            Description = text
        };
    }

    public static EmbedProperties Error(string text)
    {
        return new EmbedProperties
        {
            Color = new Color(0xFF0000),
            Description = text
        };
    }

    public static EmbedProperties Information(string text)
    {
        return new EmbedProperties
        {
            Color = new Color(0x0000FF),
            Description = text
        };
    }

    public static EmbedProperties Success(string text)
    {
        return new EmbedProperties
        {
            Color = new Color(0x00FF00),
            Description = text
        };
    }
}