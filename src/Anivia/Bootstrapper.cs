using System.Collections.Concurrent;
using Anivia.CommandModules;
using Anivia.Extensions;
using Anivia.Infrastructure;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Options;
using Victoria;
using Victoria.WebSocket.EventArgs;

namespace Anivia;

internal sealed class Bootstrapper(
    DiscordSocketClient discordSocketClient,
    CommandService commandService,
    LavaNode<LavaPlayer<LavaTrack>, LavaTrack> lavaNode,
    IOptions<DiscordOptions> discordOptions,
    IServiceProvider serviceProvider,
    ILogger<Bootstrapper> logger
)
{
    private readonly DiscordOptions _discordOptions = discordOptions.Value;
    private readonly DiscordSocketClient _discordSocketClient = discordSocketClient;
    private readonly CommandService _commandService = commandService;
    private readonly InteractionService _interactionService = new(discordSocketClient.Rest);
    private readonly LavaNode<LavaPlayer<LavaTrack>, LavaTrack> _lavaNode = lavaNode;
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly ILogger<Bootstrapper> _logger = logger;

    public ConcurrentDictionary<ulong, ulong> GuildToTextChannelMap { get; } = new();

    public async Task BootstrapAsync()
    {
        _lavaNode.OnTrackStart += OnTrackStartAsync;
        _lavaNode.OnTrackEnd += OnTrackEndAsync;

        _discordSocketClient.Ready += OnBotReadyAsync;
        _discordSocketClient.InteractionCreated += OnInteractionCreatedAsync;
        _discordSocketClient.MessageReceived += OnMessageReceivedAsync;

        _logger.LogInformation("Starting socket client");
        await _discordSocketClient.LoginAsync(TokenType.Bot, _discordOptions.BotToken);
        await _discordSocketClient.StartAsync();
    }

    private async Task OnMessageReceivedAsync(SocketMessage message)
    {
        if (message is not SocketUserMessage socketUserMessage)
        {
            return;
        }

        // Create a number to track where the prefix ends and the command begins
        var argPos = 0;

        // Determine if the message is a command based on the prefix and make sure no bots trigger commands
        if (!_discordOptions.CommandPrefixes.Any(p => socketUserMessage.HasStringPrefix(p, ref argPos)) &&
            !socketUserMessage.HasMentionPrefix(_discordSocketClient.CurrentUser, ref argPos) ||
            socketUserMessage.Author.IsBot)
        {
            return;
        }

        // Create a WebSocket-based command context based on the message
        var context = new SocketCommandContext(_discordSocketClient, socketUserMessage);

        // Execute the command with the command context we just
        // created, along with the service provider for precondition checks.
        await _commandService.ExecuteAsync(
            context,
            argPos,
            _serviceProvider
        );
    }

    private async Task OnBotReadyAsync()
    {
        _logger.LogInformation("Discord socket client ready");
        await _serviceProvider.UseLavaNodeAsync();
        if (!_lavaNode.IsConnected)
        {
            try
            {
                _logger.LogInformation("Acquiring Lavalink connection");
                await _lavaNode.ConnectAsync();
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Couldn't connect to Lavalink");
            }
        }
        
        _logger.LogInformation("Initializing command modules");
        await _commandService.AddModulesAsync(typeof(Program).Assembly, _serviceProvider);
        
        _logger.LogInformation("Initializing interaction service");
        await _interactionService.AddModulesAsync(typeof(Program).Assembly, _serviceProvider);
        await _interactionService.RegisterCommandsGloballyAsync();
    }

    private async Task OnInteractionCreatedAsync(SocketInteraction interaction)
    {
        var ctx = new SocketInteractionContext(_discordSocketClient, interaction);
        await _interactionService.ExecuteCommandAsync(ctx, _serviceProvider);
    }

    private async Task OnTrackEndAsync(TrackEndEventArg args)
    {
        var player = await _lavaNode.GetPlayerAsync(args.GuildId);
        var queue = player.GetCustomQueue();
        if (queue.IsCurrentTrackLooped)
        {
            await player.PlayAsync(_lavaNode, args.Track);

            return;
        }

        var textChannel = _discordSocketClient
            .GetGuild(args.GuildId)
            .GetTextChannel(GuildToTextChannelMap[args.GuildId]);

        if (queue.Next is null && !queue.IsLooped)
        {
            queue.Clear();
            await textChannel.SendMessageAsync(embed: Embeds.Error("There are no more tracks"));
            _logger.LogInformation("Queue cleared; no more tracks");

            return;
        }

        var track = queue.ConsumeNext()!;
        await player.PlayAsync(_lavaNode, track);
    }

    private async Task OnTrackStartAsync(TrackStartEventArg args)
    {
        _logger.LogInformation("Started playing {TrackTitle} ({TrackUrl})", args.Track.Title, args.Track.Url);

        var embed = new EmbedBuilder()
            .WithDescription($"Started playing [{args.Track.Title}]({args.Track.Url})")
            .Build();

        var textChannel = _discordSocketClient.GetGuild(args.GuildId)
            .GetTextChannel(GuildToTextChannelMap[args.GuildId]);

        await textChannel.SendMessageAsync(embed: embed);
    }
}