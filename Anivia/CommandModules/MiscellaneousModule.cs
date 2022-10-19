using System.Text.Json;
using Anivia.Extensions;
using Anivia.Options;
using Discord;
using Discord.Commands;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Options;

namespace Anivia.CommandModules;

public sealed class MiscellaneousModule : ModuleBase
{
    private readonly CommandService _commandService;
    private readonly DiscordOptions _discordOptions;
    private readonly LavalinkOptions _lavalinkOptions;

    public MiscellaneousModule(CommandService commandService, IOptionsMonitor<DiscordOptions> discordOptions, IOptionsMonitor<LavalinkOptions> lavalinkOptions)
    {
        _commandService = commandService;
        _discordOptions = discordOptions.CurrentValue;
        _lavalinkOptions = lavalinkOptions.CurrentValue;
    }

    [Command("help")]
    [Discord.Commands.Summary("Lists available commands")]
    public async Task HelpAsync()
    {
        var embedBuilder = new EmbedBuilder();

        foreach (var module in _commandService.Modules)
        {
            var commandSummaries = string.Join(
                Environment.NewLine,
                module.Commands.Select(c => $"**{c.Name}**: {c.Summary?.AsItalic() ??" mater ti jebem"}"));

            embedBuilder.AddField(module.Name ?? "Commands", commandSummaries);
        }

        var embed = embedBuilder.Build();
        await ReplyAsync(embed: embed);
    }

    [Command("ping")]
    [Discord.Commands.Summary("Performs a health check")]
    public async Task PingAsync()
    {
        await ReplyAsync("pong");
    }

    [Command("config")]
    [Discord.Commands.Summary("Displays the configuration")]
    public async Task DisplayConfigAsync()
    {
        var config = new
        {
            discord = new
            {
                prefixes = _discordOptions.CommandPrefixes
            },
            lavalink = _lavalinkOptions
        };

        var serialized = JsonSerializer.Serialize(
            config,
            new JsonSerializerOptions
            {
                AllowTrailingCommas = false,
                WriteIndented = true
            });

        await ReplyAsync(serialized);
    }

    [Discord.Commands.Group("prefix")]
    [Discord.Commands.Summary("Prefix management")]
    public sealed class PrefixCommands : ModuleBase
    {
        private readonly IAuditableOptionsSnapshot<DiscordOptions> _discordOptions;

        public PrefixCommands(IAuditableOptionsSnapshot<DiscordOptions> discordOptions)
        {
            _discordOptions = discordOptions;
        }

        [Command]
        [Alias("list", "l")]
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
}

public static class Embeds
{
    public static Embed Error(string text) =>
        new EmbedBuilder()
            .WithColor(255, 0, 0)
            .WithDescription(text)
            .Build();

    public static Embed Success(string text) =>
        new EmbedBuilder()
            .WithColor(0, 255, 0)
            .WithDescription(text)
            .Build();
}