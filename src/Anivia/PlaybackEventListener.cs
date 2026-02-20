using Anivia.CommandModules;
using Anivia.Infrastructure;
using Discord;
using Discord.WebSocket;
using Lavalink4NET;
using Lavalink4NET.Events.Players;
using Lavalink4NET.Players.Queued;

namespace Anivia;

public sealed class PlaybackEventListener(
    IAudioService lavalinkAudioService,
    DiscordSocketClient discordSocketClient,
    IAuditableOptionsSnapshot<DiscordOptions> discordOptions,
    ILogger<PlaybackEventListener> logger
)
{
    private readonly DiscordOptions _discordOptions = discordOptions.CurrentValue;
    private readonly DiscordSocketClient _discordSocketClient = discordSocketClient;
    private readonly IAudioService _lavalinkAudioService = lavalinkAudioService;
    private readonly ILogger<PlaybackEventListener> _logger = logger;

    public void Subscribe()
    {
        _lavalinkAudioService.TrackStarted += OnTrackStarted;
        _lavalinkAudioService.TrackEnded += OnTrackEnded;
    }

    private async Task OnTrackEnded(object sender, TrackEndedEventArgs eventArgs)
    {
        var textChannel = _discordSocketClient.GetGuild(eventArgs.Player.GuildId)
            .GetTextChannel(discordOptions.CurrentValue.TextChannels.GetValueOrDefault(eventArgs.Player.GuildId));

        if (textChannel is null)
        {
            return;
        }

        if (eventArgs.Player is not QueuedLavalinkPlayer player)
        {
            _logger.LogCritical("Expected a queued Lavalink player, but got {PlayerType}", eventArgs.Player.GetType());

            return;
        }

        if (player.CurrentTrack is null && player.RepeatMode is TrackRepeatMode.None)
        {
            await player.Queue.History!.ClearAsync();
            await textChannel.SendMessageAsync(embed: Embeds.Error("There are no more tracks"));
        }
    }

    private async Task OnTrackStarted(object sender, TrackStartedEventArgs eventArgs)
    {
        var textChannel = _discordSocketClient.GetGuild(eventArgs.Player.GuildId)
            .GetTextChannel(discordOptions.CurrentValue.TextChannels.GetValueOrDefault(eventArgs.Player.GuildId));

        if (textChannel is null)
        {
            return;
        }

        var embed = new EmbedBuilder()
            .WithDescription($"Started playing [{eventArgs.Track.Title}]({eventArgs.Track.Uri})")
            .Build();

        await textChannel.SendMessageAsync(embed: embed);
    }
}