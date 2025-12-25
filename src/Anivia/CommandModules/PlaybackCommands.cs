using Discord;
using Discord.Commands;
using Discord.Interactions;
using Lavalink4NET;
using Lavalink4NET.Players.Queued;
using Lavalink4NET.Rest.Entities.Tracks;
using Summary = Discord.Commands.SummaryAttribute;

namespace Anivia.CommandModules;

[Name("Playback")]
public sealed class PlaybackCommands(IAudioService lavalinkAudioService, InteractionService interactionService) : AniviaCommandModule(lavalinkAudioService)
{
    private readonly IAudioService _lavalinkAudioService = lavalinkAudioService;
    private readonly InteractionService _interactionService = interactionService;

    [Command("play")]
    [Alias("p")]
    [Summary("Queue a track or playlist from a search term or url")]
    public async Task PlayAsync([Remainder] string song)
    {
        var player = await GetPlayerAsync();
        if (player is null)
        {
            return;
        }

        var trackLoadResult = await _lavalinkAudioService.Tracks.LoadTracksAsync(song, TrackSearchMode.YouTube);
        if (trackLoadResult.IsFailed)
        {
            await ReplyAsync(
                embed: Embeds.Error($"YouTube search failed: {trackLoadResult.Exception?.Message ?? "unknown error"}")
            );
        }
        
        if (!trackLoadResult.HasMatches)
        {
            await ReplyAsync(embed: Embeds.Error("No tracks that match your search"));

            return;
        }
        
        if (trackLoadResult.IsPlaylist)
        {
            var selectedTrack = trackLoadResult.Playlist.SelectedTrack ?? trackLoadResult.Tracks.First();
            var playlistDuration = new TimeSpan(trackLoadResult.Tracks.Sum(t => t.Duration.Ticks));
            var positionInUpcoming = player.Queue.Count - (player.Queue.History?.Count ?? 0) + 1;
            
            foreach (var playlistTrack in trackLoadResult.Tracks)
            {
                await player.Queue.AddAsync(new TrackQueueItem(playlistTrack));
            }
            
            var embed = new EmbedBuilder()
                .WithTitle("Added Playlist")
                .WithThumbnailUrl(selectedTrack.ArtworkUri?.ToString() ?? $"https://img.youtube.com/vi/{selectedTrack.Identifier}/0.jpg")
                .AddField("Track", $"[{selectedTrack.Title}]({selectedTrack.Uri})")
                // .AddField("Estimated time until played", "n")
                .AddField("Playlist length", playlistDuration.ToString(@"hh\:mm\:ss"), true)
                .AddField("Position in upcoming", positionInUpcoming == 1 ? "Next" : positionInUpcoming)
                .AddField("Number of tracks", trackLoadResult.Tracks.Length, true)
                .WithFooter($"Requested by {Context.User.Username}", Context.User.GetAvatarUrl())
                .Build();

            await ReplyAsync(embed: embed);

            return;
        }

        var track = trackLoadResult.Track ?? trackLoadResult.Tracks.First();
        await player.Queue.AddAsync(new TrackQueueItem(track));

        if (player.Queue.Count > 1)
        {
            var embed = new EmbedBuilder()
                .WithTitle("Added Track")
                .WithThumbnailUrl(track.ArtworkUri?.ToString() ?? $"https://img.youtube.com/vi/{track.Identifier}/0.jpg")
                .AddField("Track", $"[{track.Title}]({track.Uri})")
                // .AddField("Estimated time until played", "n")
                .AddField("Track length", track.Duration.ToString(@"hh\:mm\:ss"), true)
                // .AddField("Position in upcoming", queue.Next == track ? "Next" : queue.Length)
                .AddField("Position in queue", player.Queue.Count, true)
                .WithFooter($"Requested by {Context.User.Username}", Context.User.GetAvatarUrl())
                .Build();

            await ReplyAsync(embed: embed);
        }
    }
}