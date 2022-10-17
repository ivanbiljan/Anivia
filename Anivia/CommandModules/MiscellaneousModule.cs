using System;
using System.Linq;
using System.Threading.Tasks;
using Anivia.Extensions;
using Anivia.Options;
using Discord;
using Discord.Commands;

namespace Anivia.CommandModules;

[Name("Meta")]
public sealed class MiscellaneousModule : ModuleBase
{
    private readonly CommandService _commandService;

    public MiscellaneousModule(CommandService commandService)
    {
        _commandService = commandService;
    }

    [Command("ping")]
    [Summary("Performs a health check")]
    public async Task Ping()
    {
        await ReplyAsync("pong");
    }

    [Command("help")]
    [Summary("Lists available commands")]
    public async Task HelpAsync()
    {
        var embedBuilder = new EmbedBuilder();

        foreach (var module in _commandService.Modules)
        {
            var commandSummaries = string.Join(
                Environment.NewLine,
                module.Commands.Select(c => $"**{c.Name}**: {c.Summary.AsItalic()}"));
            
            embedBuilder.AddField(module.Name ?? "Commands", commandSummaries);
        }

        var embed = embedBuilder.Build();
        await ReplyAsync(embed: embed);
    }
    

    [Group("prefix")]
    [Summary("Prefix management")]
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
                options =>
                {
                    options.CommandPrefixes.Add(prefix);
                });

            await ReplyAsync($"'{prefix}' added");
        }
    }
}

public static class Embeds
{
    public static Embed Success(string text)
    {
        return new EmbedBuilder()
            .WithColor(0, 255, 0)
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
}