using System.Runtime.InteropServices;
using System.Text.Json;
using Anivia.Extensions;
using Anivia.Infrastructure;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Fergun.Interactive;
using Fergun.Interactive.Pagination;
using GScraper.Google;
using Microsoft.Extensions.Options;

namespace Anivia.CommandModules;

[Name("Misc")]
public sealed class MiscellaneousModule(
    CommandService commandService,
    IOptionsMonitor<DiscordOptions> discordOptions,
    IOptionsMonitor<LavalinkOptions> lavalinkOptions,
    InteractiveService interactiveService
)
    : ModuleBase
{
    private static readonly GoogleScraper Scraper = new();
    private readonly DiscordOptions _discordOptions = discordOptions.CurrentValue;
    private readonly LavalinkOptions _lavalinkOptions = lavalinkOptions.CurrentValue;
    private readonly CommandService _commandService = commandService;
    private readonly InteractiveService _interactiveService = interactiveService;

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
            }
        );

        await ReplyAsync(serialized);
    }

    [Command("help", RunMode = RunMode.Async)]
    [Summary("Help command")]
    [Remarks("help [command]")]
    public async Task HelpAsync([Remainder] [Optional] string? commandString)
    {
        if (!string.IsNullOrWhiteSpace(commandString))
        {
            var embedBuilder = new EmbedBuilder();
            var command = _commandService.Commands.FirstOrDefault(
                c => string.Equals(c.Name, commandString, StringComparison.InvariantCultureIgnoreCase) ||
                     c.Aliases.Any(a => string.Equals(a, commandString, StringComparison.InvariantCultureIgnoreCase))
            );

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

        var modules = _commandService.Modules.ToList();

        var paginator = new LazyPaginatorBuilder()
            .AddUser(Context.User)
            .WithPageFactory(GeneratePage)
            .WithMaxPageIndex(modules.Count - 1) // You must specify the max. index the page factory can go.
            .AddOption(new Emoji("◀"), PaginatorAction.Backward) // Use different emojis and option order.
            .AddOption(new Emoji("▶"), PaginatorAction.Forward)
            .AddOption(new Emoji("🛑"), PaginatorAction.Exit)
            .WithCacheLoadedPages(
                false
            ) // The lazy paginator caches generated pages by default but it's possible to disable this.
            .WithActionOnCancellation(ActionOnStop.DeleteMessage) // Delete the message after pressing the stop emoji.
            .WithActionOnTimeout(ActionOnStop.DeleteMessage) // Disable the input (buttons) after a timeout.
            .WithFooter(
                PaginatorFooter
                    .None
            ) // Do not override the page footer. This allows us to write our own page footer in the page factory.
            .Build();

        await _interactiveService.SendPaginatorAsync(paginator, Context.Channel, TimeSpan.FromMinutes(10));

        PageBuilder GeneratePage(int index)
        {
            var commandSummaries = string.Join(
                Environment.NewLine,
                modules[index]
                    .Commands.Select(
                        c => $"**{c.Name ?? "balls"}**: {c.Summary?.AsItalic() ?? " mater ti jebem"}"
                    )
            );

            return new PageBuilder()
                .WithAuthor(Context.User)
                .WithTitle(modules[index].Name)
                .WithDescription(modules[index].Summary ?? "Missing summary")
                .AddField("Commands", commandSummaries);
        }
    }

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
            .WithCacheLoadedPages(
                false
            ) // The lazy paginator caches generated pages by default but it's possible to disable this.
            .WithActionOnCancellation(ActionOnStop.DeleteMessage) // Delete the message after pressing the stop emoji.
            .WithActionOnTimeout(ActionOnStop.DisableInput) // Disable the input (buttons) after a timeout.
            .WithFooter(
                PaginatorFooter
                    .None
            ) // Do not override the page footer. This allows us to write our own page footer in the page factory.
            .Build();

        await _interactiveService.SendPaginatorAsync(paginator, Context.Channel, TimeSpan.FromMinutes(10));

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

    [Command("reaction", RunMode = RunMode.Async)]
    public async Task NextReactionAsync()
    {
        var msg = await ReplyAsync("Add a reaction to this message.");

        // Wait for a reaction in the message.
        var result = await _interactiveService.NextReactionAsync(
            x => x.MessageId == msg.Id,
            timeout: TimeSpan.FromSeconds(30)
        );

        await msg.ModifyAsync(
            x =>
            {
                x.Content = result.IsSuccess
                    ? $"{MentionUtils.MentionUser(result.Value!.UserId)} reacted: {result.Value.Emote}"
                    : $"Failed to get reaction. Status: {result.Status}";

                x.AllowedMentions = AllowedMentions.None;
                x.Embeds = Array.Empty<Embed>(); // workaround for d.net bug
            }
        );
    }

    [Command("ping")]
    [Summary("Performs a health check")]
    public async Task PingAsync()
    {
        var socketClient = (DiscordSocketClient) Context.Client;
        await ReplyAsync($"Pong: {socketClient.Latency} ms");
    }
}