using Anivia.Infrastructure;
using Lavalink4NET;
using NetCord.Rest;
using NetCord.Services.Commands;

namespace Anivia.CommandModules;

public sealed class PrefixCommands(
    IAudioService lavalinkAudioService,
    IAuditableOptionsSnapshot<DiscordOptions> discordOptions
) : AniviaCommandModule(lavalinkAudioService)
{
    private readonly IAuditableOptionsSnapshot<DiscordOptions> _discordOptions = discordOptions;

    [AniviaCommand("prefixlist", "Prefix", "Displays availablie command prefixes", "prefix")]
    public async Task ListPrefixesAsync()
    {
        var prefixes = string.Join(", ", _discordOptions.CurrentValue.CommandPrefixes);

        var embed = new EmbedProperties
        {
            Fields =
            [
                new EmbedFieldProperties
                {
                    Name = "Server prefixes",
                    Value = prefixes
                }
            ]
        };

        await ReplyAsync(new ReplyMessageProperties {Embeds = [embed]});
    }

    [AniviaCommand("addprefix", "Prefix", "Adds a new prefix")]
    public async Task SetPrefixAsync(string prefix)
    {
        _discordOptions.Update(options => { options.CommandPrefixes.Add(prefix); });

        await ReplyAsync(new ReplyMessageProperties {Content = $"'{prefix}' added"});
    }
}