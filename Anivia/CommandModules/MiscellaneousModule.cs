using System.Runtime.InteropServices;
using System.Text.Json;
using Anivia.Extensions;
using Anivia.Options;
using Discord;
using Discord.Commands;
using Fergun.Interactive;
using Fergun.Interactive.Pagination;
using GScraper.Google;
using Microsoft.Extensions.Options;
using Victoria.Node;

namespace Anivia.CommandModules;

public sealed class MiscellaneousModule : ModuleBase
{
    private readonly CommandService _commandService;
    private readonly DiscordOptions _discordOptions;
    private readonly LavalinkOptions _lavalinkOptions;

    public MiscellaneousModule(
        CommandService commandService,
        IOptionsMonitor<DiscordOptions> discordOptions,
        IOptionsMonitor<LavalinkOptions> lavalinkOptions)
    {
        _commandService = commandService;
        _discordOptions = discordOptions.CurrentValue;
        _lavalinkOptions = lavalinkOptions.CurrentValue;
    }

    [Command("config")]
    [Summary("Displays the configuration")]
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
    
    private static readonly GoogleScraper Scraper = new();
    
    // Sends a lazy paginator that displays images and uses more options.
    [Command("img", RunMode = RunMode.Async)]
    public async Task ImgAsync(string query = "discord")
    {
        // Get images from Google Images.
        var images = (await Scraper.GetImagesAsync(query)).ToList();

        var paginator = new LazyPaginatorBuilder()
            .AddUser(Context.User)
            .WithPageFactory(GeneratePage)
            .WithMaxPageIndex(images.Count - 1) // You must specify the max. index the page factory can go.
            .AddOption(new Emoji("◀"), PaginatorAction.Backward) // Use different emojis and option order.
            .AddOption(new Emoji("▶"), PaginatorAction.Forward)
            .AddOption(new Emoji("🔢"), PaginatorAction.Jump) // Use the jump feature
            .AddOption(new Emoji("🛑"), PaginatorAction.Exit)
            .WithCacheLoadedPages(false) // The lazy paginator caches generated pages by default but it's possible to disable this.
            .WithActionOnCancellation(ActionOnStop.DeleteMessage) // Delete the message after pressing the stop emoji.
            .WithActionOnTimeout(ActionOnStop.DisableInput) // Disable the input (buttons) after a timeout.
            .WithFooter(PaginatorFooter.None) // Do not override the page footer. This allows us to write our own page footer in the page factory.
            .Build();

        await Interactive.SendPaginatorAsync(paginator, Context.Channel, TimeSpan.FromMinutes(10));

        PageBuilder GeneratePage(int index)
        {
            return new PageBuilder()
                .WithAuthor(Context.User)
                .WithTitle(images[index].Title)
                .WithUrl(images[index].SourceUrl)
                .WithDescription("Image paginator example")
                .WithImageUrl(images[index].Url)
                .WithFooter($"Page {index + 1}/{images.Count}");
        }
    }
    
    public InteractiveService Interactive { get; set; }
    
    [Command("reaction", RunMode = RunMode.Async)]
    public async Task NextReactionAsync()
    {
        var msg = await ReplyAsync("Add a reaction to this message.");

        // Wait for a reaction in the message.
        var result = await Interactive.NextReactionAsync(x => x.MessageId == msg.Id, timeout: TimeSpan.FromSeconds(30));

        await msg.ModifyAsync(x =>
        {
            x.Content = result.IsSuccess ? $"{MentionUtils.MentionUser(result.Value!.UserId)} reacted: {result.Value.Emote}" : $"Failed to get reaction. Status: {result.Status}";
            x.AllowedMentions = AllowedMentions.None;
            x.Embeds = Array.Empty<Embed>(); // workaround for d.net bug
        });
    }
    
    [Command("help")]
    [Summary("Help command")]
    [Remarks("help [command]")]
    public async Task HelpAsync([Remainder] [Optional] string? commandString)
    {
        var embedBuilder = new EmbedBuilder();
        if (!string.IsNullOrWhiteSpace(commandString))
        {
            var command = _commandService.Commands.FirstOrDefault(
                c => string.Equals(c.Name, commandString, StringComparison.InvariantCultureIgnoreCase) ||
                     c.Aliases.Any(a => string.Equals(a, commandString, StringComparison.InvariantCultureIgnoreCase)));

            if (command is null)
            {
                await ReplyAsync(embed: Embeds.Error("Invalid command"));

                return;
            }

            embedBuilder
                .WithTitle($"Help for command {commandString}")
                .WithDescription(command.Summary ?? "Summary unavailable")
                .AddField("Usage", $"```{command.Remarks ?? "N/A"}```")
                .AddField("Aliases", $"```{string.Join(Environment.NewLine, command.Aliases)}```")
                .Build();

            await ReplyAsync(embed: embedBuilder.Build());

            return;
        }

        foreach (var module in _commandService.Modules)
        {
            var commandSummaries = string.Join(
                Environment.NewLine,
                module.Commands.Select(c => $"**{c.Name}**: {c.Summary?.AsItalic() ?? " mater ti jebem"}"));

            embedBuilder.AddField(module.Name ?? "Commands", commandSummaries);
        }

        var embed = embedBuilder.Build();
        await ReplyAsync(embed: embed);
    }

    [Command("ping")]
    [Summary("Performs a health check")]
    public async Task PingAsync()
    {
        await ReplyAsync("pong");
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
                options => { options.CommandPrefixes.Add(prefix); });

            await ReplyAsync($"'{prefix}' added");
        }
    }
}