using Anivia.CommandModules;
using Anivia.Infrastructure;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Lavalink4NET;
using Lavalink4NET.DiscordNet;

namespace Anivia;

public sealed class Bootstrapper(
    DiscordSocketClient discordSocketClient,
    IAuditableOptionsSnapshot<DiscordOptions> discordOptions,
    CommandService commandService,
    IAudioService lavalinkAudioService,
    PlaybackEventListener playbackEventListener,
    IServiceProvider serviceProvider,
    ILogger<Bootstrapper> logger
)
{
    private readonly DiscordSocketClient _discordSocketClient = discordSocketClient;
    private readonly CommandService _commandService = commandService;
    private readonly IAudioService _lavalinkAudioService = lavalinkAudioService;
    private readonly PlaybackEventListener _playbackEventListener = playbackEventListener;
    private readonly InteractionService _interactionService = new(discordSocketClient.Rest);
    private readonly DiscordOptions _discordOptions = discordOptions.CurrentValue;
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly ILogger<Bootstrapper> _logger = logger;

    public async Task InitializeAsync()
    {
        _discordSocketClient.Ready += OnBotReadyAsync;
        _discordSocketClient.InteractionCreated += OnInteractionCreatedAsync;
        _discordSocketClient.MessageReceived += OnMessageReceivedAsync;
        _discordSocketClient.UserVoiceStateUpdated += OnUserVoiceStateUpdatedAsync;

        _logger.LogInformation("Starting Discord socket client");
        await _discordSocketClient.LoginAsync(TokenType.Bot, _discordOptions.BotToken);
        await _discordSocketClient.StartAsync();
    }
    
    private async Task OnBotReadyAsync()
    {
        _logger.LogInformation("Discord socket client ready");
        
        _logger.LogInformation("Initializing command modules");
        await _commandService.AddModulesAsync(typeof(Program).Assembly, _serviceProvider);

        _logger.LogInformation("Initializing interaction service");
        await _interactionService.AddModulesAsync(typeof(Program).Assembly, _serviceProvider);
        await _interactionService.RegisterCommandsGloballyAsync();
        
        _logger.LogInformation("Registering playback event handlers");
        _playbackEventListener.Subscribe();

        _logger.LogInformation("Initialization complete");
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
        if ((!_discordOptions.CommandPrefixes.Any(p => socketUserMessage.HasStringPrefix(p, ref argPos)) &&
             !socketUserMessage.HasMentionPrefix(_discordSocketClient.CurrentUser, ref argPos)) ||
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
    
    private async Task OnInteractionCreatedAsync(SocketInteraction interaction)
    {
        var ctx = new SocketInteractionContext(_discordSocketClient, interaction);
        await _interactionService.ExecuteCommandAsync(ctx, _serviceProvider);
    }

    private async Task OnUserVoiceStateUpdatedAsync(SocketUser user, SocketVoiceState state, SocketVoiceState _)
    {
        if (state.VoiceChannel.ConnectedUsers.Count >= 2)
        {
            return;
        }

        var player = await _lavalinkAudioService.Players.GetPlayerAsync(state.VoiceChannel.Guild);
        if (player is not null)
        {
            await player.DisconnectAsync();
            
            var textChannel = _discordSocketClient.GetGuild(state.VoiceChannel.Guild.Id)
                .GetTextChannel(_discordOptions.TextChannelId);
        
            await textChannel.SendMessageAsync(embed: Embeds.Error("Stopping because everyone left"));
        }
    }
}