using System.Text.RegularExpressions;

namespace Anivia.Extensions;

public static class StringExtensions
{
    private static readonly Regex DiscordMarkdownRegex = new("([*,_])");

    public static string AsBold(this string source)
    {
        return $"**{DiscordMarkdownRegex.Replace(source, "\\$1")}**";
    }

    public static string AsItalic(this string source)
    {
        return $"_{DiscordMarkdownRegex.Replace(source, "\\$1")}_";
    }

    public static int ComputeDistanceTo(this string source, string reference)
    {
        if (source.Length == 0)
        {
            return reference.Length;
        }

        if (reference.Length == 0)
        {
            return source.Length;
        }

        var dp = new int[source.Length + 1, reference.Length + 1];

        for (var i = 0; i <= source.Length; ++i)
        {
            dp[i, 0] = i;
        }

        for (var j = 0; j <= reference.Length; ++j)
        {
            dp[0, j] = j;
        }

        for (var i = 1; i <= source.Length; i++)
        {
            for (var j = 1; j <= reference.Length; j++)
            {
                var cost = char.ToLower(reference[j - 1]) == char.ToLower(source[i - 1]) ? 0 : 1;

                dp[i, j] = Math.Min(
                    Math.Min(dp[i - 1, j] + 1, dp[i, j - 1] + 1),
                    dp[i - 1, j - 1] + cost
                );
            }
        }

        return dp[source.Length, reference.Length];
    }

    public static string ToShortString(this TimeSpan timeSpan)
    {
        return timeSpan.Hours > 0 ? $"{timeSpan:hh\\:mm\\:ss}" : $"{timeSpan:mm\\:ss}";
    }

    public static string WithUnderline(this string source)
    {
        return $"__{DiscordMarkdownRegex.Replace(source, "\\$1")}__";
    }
}