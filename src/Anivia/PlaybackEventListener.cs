using Anivia.CommandModules;
using Anivia.Infrastructure;
using Lavalink4NET;
using Lavalink4NET.Events.Players;
using Lavalink4NET.Players.Queued;
using NetCord;
using NetCord.Gateway;
using NetCord.Rest;

namespace Anivia;

public sealed class PlaybackEventListener(
    IAudioService lavalinkAudioService,
    GatewayClient gatewayClient,
    IAuditableOptionsSnapshot<DiscordOptions> discordOptions,
    ILogger<PlaybackEventListener> logger
)
{
    private readonly GatewayClient _gatewayClient = gatewayClient;
    private readonly IAudioService _lavalinkAudioService = lavalinkAudioService;
    private readonly ILogger<PlaybackEventListener> _logger = logger;

    public void Subscribe()
    {
        _lavalinkAudioService.TrackStarted += OnTrackStarted;
        _lavalinkAudioService.TrackEnded += OnTrackEnded;
    }

    private TextGuildChannel? GetTextChannel(ulong guildId)
    {
        var channelId = discordOptions.CurrentValue.TextChannels.GetValueOrDefault(guildId);
        if (channelId == 0)
        {
            return null;
        }

        var guild = _gatewayClient.Cache.Guilds.GetValueOrDefault(guildId);

        return guild?.Channels.GetValueOrDefault(channelId) as TextGuildChannel;
    }

    private async Task OnTrackEnded(object sender, TrackEndedEventArgs eventArgs)
    {
        var textChannel = GetTextChannel(eventArgs.Player.GuildId);
        if (textChannel is null)
        {
            return;
        }

        if (eventArgs.Player is not AniviaPlayer player)
        {
            _logger.LogCritical("Expected a queued Lavalink player, but got {PlayerType}", eventArgs.Player.GetType());

            return;
        }

        if (player.CurrentTrack is null && player.RepeatMode is TrackRepeatMode.None)
        {
            await textChannel.SendMessageAsync(
                new MessageProperties {Embeds = [Embeds.Error("There are no more tracks")]}
            );
        }
    }

    private async Task OnTrackStarted(object sender, TrackStartedEventArgs eventArgs)
    {
        var textChannel = GetTextChannel(eventArgs.Player.GuildId);
        if (textChannel is null)
        {
            return;
        }

        var embed = new EmbedProperties
        {
            Description = $"Started playing [{eventArgs.Track.Title}]({eventArgs.Track.Uri})"
        };

        await textChannel.SendMessageAsync(new MessageProperties {Embeds = [embed]});
    }
}