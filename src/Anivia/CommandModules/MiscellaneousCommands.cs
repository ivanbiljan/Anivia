using System.Text.Json;
using Anivia.Infrastructure;
using Microsoft.Extensions.Options;
using NetCord.Rest;
using NetCord.Services.Commands;

namespace Anivia.CommandModules;

public sealed class MiscellaneousCommands(
    IAuditableOptionsSnapshot<DiscordOptions> discordOptions,
    IOptionsMonitor<LavalinkOptions> lavalinkOptions,
    HelpPaginator helpPaginator
) : CommandModule<CommandContext>
{
    private readonly IAuditableOptionsSnapshot<DiscordOptions> _discordOptions = discordOptions;
    private readonly HelpPaginator _helpPaginator = helpPaginator;
    private readonly LavalinkOptions _lavalinkOptions = lavalinkOptions.CurrentValue;

    [AniviaCommand("config", "Misc", "Displays the configuration")]
    public async Task DisplayConfigAsync()
    {
        var config = new
        {
            discord = new
            {
                prefixes = _discordOptions.CurrentValue.CommandPrefixes
            },
            textChannels = _discordOptions.CurrentValue.TextChannels,
            lavalink = _lavalinkOptions
        };

        var serialized = JsonSerializer.Serialize(
            config,
            new JsonSerializerOptions
            {
                AllowTrailingCommas = false,
                WriteIndented = true
            }
        );

        await ReplyAsync(new ReplyMessageProperties {Content = serialized});
    }

    [AniviaCommand("help", "Misc", "Displays available commands. help [command]")]
    public async Task HelpAsync([CommandParameter(Remainder = true)] string? commandString = null)
    {
        var modules = _helpPaginator.GetModules();

        if (!string.IsNullOrWhiteSpace(commandString))
        {
            var command = modules
                .SelectMany(m => m.Commands)
                .FirstOrDefault(c =>
                    c.Aliases.Any(a => string.Equals(a, commandString, StringComparison.OrdinalIgnoreCase))
                );

            if (command is null)
            {
                await ReplyAsync(new ReplyMessageProperties {Embeds = [Embeds.Error("Invalid command")]});

                return;
            }

            var embed = new EmbedProperties
            {
                Title = $"Help for command {commandString}",
                Fields =
                [
                    new EmbedFieldProperties {Name = "Aliases", Value = $"```{string.Join(", ", command.Aliases)}```"}
                ]
            };

            await ReplyAsync(new ReplyMessageProperties {Embeds = [embed]});

            return;
        }

        if (modules.Count == 0)
        {
            await ReplyAsync(new ReplyMessageProperties {Embeds = [Embeds.Error("No commands available")]});

            return;
        }

        var page = HelpPaginator.BuildPage(modules[0], 0, modules.Count);
        var buttons = HelpPaginator.BuildButtons(0, modules.Count);

        var message = await SendAsync(
            new MessageProperties
            {
                Embeds = [page],
                Components = buttons
            }
        );

        HelpPaginator.SetPage(message.Id, 0);
    }

    [AniviaCommand("ping", "Misc", "Performs a health check")]
    public async Task PingAsync()
    {
        var client = Context.Client;
        await ReplyAsync(
            new ReplyMessageProperties {Content = $"Pong: {Math.Round(client.Latency.TotalMilliseconds)} ms"}
        );
    }

    [AniviaCommand("settextchannel", "Misc", "Sets the text channel for the player in the current guild", aliases: ["stc"])]
    public async Task SetTextChannelAsync(ulong channelId)
    {
        var guild = Context.Guild;
        if (guild is null)
        {
            return;
        }

        var guildId = guild.Id;
        var channel = guild.Channels.GetValueOrDefault(channelId);
        if (channel is null)
        {
            await ReplyAsync(new ReplyMessageProperties {Embeds = [Embeds.Error("Invalid text channel ID")]});

            return;
        }

        _discordOptions.Update(options =>
            {
                options.TextChannels ??= new Dictionary<ulong, ulong>();
                options.TextChannels[guildId] = channelId;
            }
        );

        await ReplyAsync(new ReplyMessageProperties {Embeds = [Embeds.Success($"Text channel set to <#{channelId}>")]});
    }
}