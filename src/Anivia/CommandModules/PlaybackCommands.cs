using Anivia.Extensions;
using Anivia.Infrastructure;
using Discord;
using Discord.Commands;
using Fergun.Interactive;
using Fergun.Interactive.Pagination;
using Lavalink4NET;
using Lavalink4NET.Players;
using Lavalink4NET.Players.Queued;
using Lavalink4NET.Rest.Entities.Tracks;
using RunMode = Discord.Commands.RunMode;

namespace Anivia.CommandModules;

[Name("Playback")]
public sealed class PlaybackCommands(
    IAudioService lavalinkAudioService,
    InteractiveService interactiveService
) : AniviaCommandModule(lavalinkAudioService)
{
    private readonly IAudioService _lavalinkAudioService = lavalinkAudioService;
    private readonly InteractiveService _interactiveService = interactiveService;

    [Command("join")]
    [Summary("Makes me join your voice channel")]
    [RequireUserInVoiceChannel]
    public async Task JoinCurrentVoiceChannelAsync()
    {
        var voiceState = (IVoiceState) Context.User;
        await voiceState.VoiceChannel.ConnectAsync(true);
        await ReplyAsync(embed: Embeds.Success("I have been summoned"));
    }

    [Command("leave")]
    [Summary("Makes me leave the voice channel")]
    [RequireUserInVoiceChannel]
    public async Task LeaveAsync()
    {
        var player = await GetPlayerAsync();
        if (player is not null)
        {
            await player.DisconnectAsync();
        }
    }

    [Command("play", RunMode = RunMode.Async)]
    [Alias("p")]
    [Summary("Queue a track or playlist from a search term or url")]
    [RequireUserInVoiceChannel]
    public async Task PlayAsync([Remainder] string song)
    {
        var player = await GetPlayerAsync(connectToVoiceChannel: true).ConfigureAwait(false);
        if (player is null)
        {
            return;
        }

        var trackLoadResult = await _lavalinkAudioService.Tracks.LoadTracksAsync(song, TrackSearchMode.YouTube)
            .ConfigureAwait(false);

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
                await player.PlayAsync(playlistTrack).ConfigureAwait(false);
            }

            var embed = new EmbedBuilder()
                .WithTitle("Added Playlist")
                .WithThumbnailUrl(
                    selectedTrack.ArtworkUri?.ToString() ??
                    $"https://img.youtube.com/vi/{selectedTrack.Identifier}/0.jpg"
                )
                .AddField("Track", $"[{selectedTrack.Title}]({selectedTrack.Uri})")
                // .AddField("Estimated time until played", "n")
                .AddField("Playlist length", playlistDuration.ToString(@"hh\:mm\:ss"), true)
                .AddField("Position in upcoming", positionInUpcoming == 1 ? "Next" : positionInUpcoming)
                .AddField("Number of tracks", trackLoadResult.Tracks.Length, true)
                .WithFooter($"Requested by {Context.User.GlobalName ?? Context.User.Username}", Context.User.GetAvatarUrl())
                .Build();

            await ReplyAsync(embed: embed);
        }
        else
        {
            var track = trackLoadResult.Track ?? trackLoadResult.Tracks.First();

            if (player.State is PlayerState.Playing)
            {
                var embed = new EmbedBuilder()
                    .WithTitle("Added Track")
                    .WithThumbnailUrl(
                        track.ArtworkUri?.ToString() ?? $"https://img.youtube.com/vi/{track.Identifier}/0.jpg"
                    )
                    .AddField("Track", $"[{track.Title}]({track.Uri})")
                    // .AddField("Estimated time until played", "n")
                    .AddField("Track length", track.Duration.ToString(@"hh\:mm\:ss"), true)
                    // .AddField("Position in upcoming", queue.Next == track ? "Next" : queue.Length)
                    .AddField("Position in queue", player.Queue.Count + 1, true)
                    .WithFooter($"Requested by {Context.User.GlobalName ?? Context.User.Username}", Context.User.GetAvatarUrl())
                    .Build();

                await ReplyAsync(embed: embed);
            }
            
            await player.PlayAsync(track).ConfigureAwait(false);
        }
    }

    [Command("queue", RunMode = RunMode.Async)]
    [Alias("q")]
    [Summary("Displays the current queue")]
    [Remarks("queue [page number]")]
    public async Task DisplayQueueAsync()
    {
        var player = await GetPlayerAsync();
        if (player is null)
        {
            return;
        }

        const int tracksPerPage = 10;
        var queue = new List<ITrackQueueItem>();
        if (player.Queue.History is not null)
        {
            queue.AddRange(player.Queue.History);
        }

        if (player.CurrentItem is not null)
        {
            queue.Add(player.CurrentItem);
        }

        queue.AddRange(player.Queue);

        if (queue.Count == 0)
        {
            await ReplyAsync(embed: Embeds.Error("The queue is empty"));

            return;
        }

        var maxPages = (int) Math.Ceiling((double) queue.Count / tracksPerPage);

        var paginator = new LazyPaginatorBuilder()
            .WithUsers(Context.User)
            .WithPageFactory(GeneratePage)
            .WithMaxPageIndex(maxPages - 1)
            .AddOption(new Emoji("⏪"), PaginatorAction.SkipToStart)
            .AddOption(new Emoji("◀"), PaginatorAction.Backward) // Use different emojis and option order.
            .AddOption(new Emoji("▶"), PaginatorAction.Forward)
            .AddOption(new Emoji("⏩"), PaginatorAction.SkipToEnd)
            .AddOption(new Emoji("🔢"), PaginatorAction.Jump) // Use the jump feature
            .WithCacheLoadedPages(
                false
            ) // The lazy paginator caches generated pages by default but it's possible to disable this.
            .WithActionOnCancellation(ActionOnStop.DeleteMessage) // Delete the message after pressing the stop emoji.
            .WithActionOnTimeout(ActionOnStop.DeleteInput) // Disable the input (buttons) after a timeout.
            .WithFooter(
                PaginatorFooter
                    .None
            ) // Do not override the page footer. This allows us to write our own page footer in the page factory.
            .Build();

        await _interactiveService.SendPaginatorAsync(paginator, Context.Channel, TimeSpan.FromMinutes(3));

        return;

        PageBuilder GeneratePage(int index)
        {
            var tracks = queue.Skip(index * tracksPerPage).Take(tracksPerPage);

            return new PageBuilder()
                .WithTitle($"Page {$"{index + 1}/{maxPages}".AsBold()}")
                .WithDescription(
                    string.Join(
                        Environment.NewLine,
                        tracks.Select((queueItem, ix) =>
                            $@"{(queueItem.Identifier == player.CurrentTrack.Identifier ? "🎶 " : string.Empty)}{index * tracksPerPage + ix + 1} - `[{queueItem.Track!.Duration:hh\:mm\:ss}]` [{queueItem.Track.Title.AsBold()}]({queueItem.Track.Uri})"
                        )
                    )
                )
                .WithFooter(
                    player.CurrentTrack is not null
                        ? $"Current track: {player.CurrentTrack!.Title} [{player.Position!.Value.Position.ToShortString()} / {player.CurrentTrack.Duration.ToShortString()}]"
                        : string.Empty
                );
        }
    }

    [Command("loop current")]
    [Alias("loop", "repeat", "repeat current")]
    [RequireUserInVoiceChannel]
    public async Task LoopCurrentSong()
    {
        var player = await GetPlayerAsync();
        if (player is null)
        {
            return;
        }

        player.RepeatMode = player.RepeatMode is not TrackRepeatMode.Track
            ? TrackRepeatMode.Track
            : TrackRepeatMode.None;

        await ReplyAsync(
            embed: Embeds.Success(
                $"I will {(player.RepeatMode is TrackRepeatMode.Track ? "now" : "no longer")} repeat the current track"
            )
        );
    }

    [Command("loop queue")]
    [Alias("repeat queue")]
    [RequireUserInVoiceChannel]
    public async Task LoopQueueAsync()
    {
        var player = await GetPlayerAsync();
        if (player is null)
        {
            return;
        }

        player.RepeatMode = player.RepeatMode is not TrackRepeatMode.Queue
            ? TrackRepeatMode.Queue
            : TrackRepeatMode.None;

        await ReplyAsync(
            embed: Embeds.Success(
                $"I will {(player.RepeatMode is TrackRepeatMode.Queue ? "now" : "no longer")} repeat the queue"
            )
        );
    }

    [Command("remove")]
    [Alias("rm", "delete", "del")]
    [Summary("Removes a track from the queue")]
    [Remarks("remove <track number (1, 2, 3)>")]
    public async Task RemoveTrackAsync(int index)
    {
        var player = await GetPlayerAsync();
        if (player is null)
        {
            return;
        }

        var queueItem = player.Queue.ElementAtOrDefault(index);
        if (queueItem is null)
        {
            return;
        }

        await player.Queue.RemoveAtAsync(index);
        await ReplyAsync(embed: Embeds.Success($"Removed track [{queueItem.Track!.Title}]({queueItem.Track.Uri})"));
    }

    [Command("move")]
    [Alias("mv")]
    public async Task MoveTrackAsync(int from, int to)
    {
        var player = await GetPlayerAsync();
        if (player is null)
        {
            return;
        }

        var queue = player.Queue;
        if (from < 1 || from > queue.Count || to < 1 || to > queue.Count)
        {
            await ReplyAsync(embed: Embeds.Error("Invalid indices"));

            return;
        }

        var fromQueueItem = queue[from];
        await queue.InsertAsync(to, fromQueueItem);
        await queue.RemoveAtAsync(from);

        await ReplyAsync(embed: Embeds.Success("Track moved"));
    }

    [Command("clear")]
    [Summary("Clears the current queue")]
    [RequireUserInVoiceChannel]
    public async Task ClearQueueAsync()
    {
        var player = await GetPlayerAsync();
        if (player is null)
        {
            return;
        }

        await player.Queue.ClearAsync();
        if (player.Queue.History is not null)
        {
            await player.Queue.History.ClearAsync();
        }

        await player.StopAsync();

        await ReplyAsync(embed: Embeds.Success("Cleared the queue"));
    }
}