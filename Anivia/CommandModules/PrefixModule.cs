using Anivia.Options;
using Discord;
using Discord.Commands;

namespace Anivia.CommandModules;

[Name("Prefix")]
[Group("prefix")]
[Summary("Prefix related commands")]
public sealed class PrefixModule : ModuleBase
{
    private readonly IAuditableOptionsSnapshot<DiscordOptions> _discordOptions;

    public PrefixModule(IAuditableOptionsSnapshot<DiscordOptions> discordOptions)
    {
        _discordOptions = discordOptions;
    }

    [Command("list")]
    [Alias("l", "")]
    public async Task ListPrefixesAsync()
    {
        var prefixes = string.Join(", ", _discordOptions.CurrentValue.CommandPrefixes);

        var embed = new EmbedBuilder()
            .WithFields(
                new EmbedFieldBuilder()
                    .WithName("Server prefixes")
                    .WithValue(prefixes))
            .Build();

        await ReplyAsync(embed: embed);
    }

    [Command("add")]
    public async Task SetPrefixAsync(string prefix)
    {
        _discordOptions.Update(
            options => { options.CommandPrefixes.Add(prefix); });
    
        await ReplyAsync($"'{prefix}' added");
    }
}