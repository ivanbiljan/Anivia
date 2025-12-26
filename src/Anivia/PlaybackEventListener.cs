using Anivia.Infrastructure;
using Discord;
using Discord.WebSocket;
using Lavalink4NET;
using Lavalink4NET.Events.Players;

namespace Anivia;

public sealed class PlaybackEventListener(
    IAudioService lavalinkAudiService,
    DiscordSocketClient discordSocketClient,
    IAuditableOptionsSnapshot<DiscordOptions> discordOptons
)
{
    private readonly IAudioService _lavalinkAudiService = lavalinkAudiService;
    private readonly DiscordSocketClient _discordSocketClient = discordSocketClient;
    private readonly DiscordOptions _discordOptons = discordOptons.CurrentValue;

    public void Subscribe()
    {
        _lavalinkAudiService.TrackStarted += OnTrackStarted;
    }

    private async Task OnTrackStarted(object sender, TrackStartedEventArgs eventArgs)
    {
        var textChannel = _discordSocketClient.GetGuild(eventArgs.Player.GuildId)
            .GetTextChannel(discordOptons.CurrentValue.TextChannelId);

        var embed = new EmbedBuilder()
            .WithDescription($"Started playing [{eventArgs.Track.Title}]({eventArgs.Track.Uri})")
            .Build();

        await textChannel.SendMessageAsync(embed: embed);
    }
}