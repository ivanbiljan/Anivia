using Anivia.Extensions;
using Anivia.Infrastructure;
using Lavalink4NET;
using Lavalink4NET.Filters;
using Lavalink4NET.Players;
using Lavalink4NET.Players.Queued;
using Lavalink4NET.Rest.Entities.Tracks;
using NetCord.Rest;
using NetCord.Services.Commands;

namespace Anivia.CommandModules;

public sealed class PlaybackCommands(
    IAudioService lavalinkAudioService
) : AniviaCommandModule(lavalinkAudioService)
{
    private readonly IAudioService _lavalinkAudioService = lavalinkAudioService;

    [AniviaCommand("bassboost", "Playback", "Sets the bass boost level: low|normal|medium|high", "bb")]
    [RequireUserInVoiceChannel]
    public async Task BassBoostAsync(string level)
    {
        var player = await GetPlayerAsync();
        if (player is null)
        {
            return;
        }

        var equalizer = level.ToLowerInvariant() switch
        {
            "low" => new Equalizer
            {
                Band0 = 0.15f,
                Band1 = 0.10f,
                Band2 = 0.05f
            },
            "medium" => new Equalizer
            {
                Band0 = 0.35f,
                Band1 = 0.25f,
                Band2 = 0.15f,
                Band3 = 0.05f
            },
            "high" => new Equalizer
            {
                Band0 = 0.60f,
                Band1 = 0.45f,
                Band2 = 0.25f,
                Band3 = 0.10f
            },
            "normal" => new Equalizer(),
            _        => null
        };

        if (equalizer is null)
        {
            await ReplyAsync(
                new ReplyMessageProperties {Embeds = [Embeds.Error("Invalid level. Use `low`, `normal`, or `high`")]}
            );

            return;
        }

        var filters = player.Filters;
        filters.Equalizer = new EqualizerFilterOptions(equalizer);
        await filters.CommitAsync();

        var message = level.ToLowerInvariant() == "normal"
            ? "Bass boost disabled"
            : $"Bass boost set to **{level.ToLowerInvariant()}**";

        await ReplyAsync(new ReplyMessageProperties {Embeds = [Embeds.Success(message)]});
    }

    [AniviaCommand("clear", "Playback", "Clears the current queue")]
    [RequireUserInVoiceChannel]
    public async Task ClearQueueAsync()
    {
        var player = await GetPlayerAsync();
        if (player is null)
        {
            return;
        }

        player.Queue.Clear();
        await player.StopAsync();

        await ReplyAsync(new ReplyMessageProperties {Embeds = [Embeds.Success("Cleared the queue")]});
    }

    [AniviaCommand("queue", "Playback", "Displays the current queue", "q")]
    public async Task DisplayQueueAsync()
    {
        var player = await GetPlayerAsync();
        if (player is null)
        {
            return;
        }

        var queue = player.Queue.Tracks;

        if (queue.Count == 0)
        {
            await ReplyAsync(new ReplyMessageProperties {Embeds = [Embeds.Error("The queue is empty")]});

            return;
        }

        const int tracksPerPage = 10;
        var tracks = queue.Take(tracksPerPage);

        var description = string.Join(
            Environment.NewLine,
            tracks.Select((queueItem, ix) =>
                $@"{(queueItem.Identifier == player.CurrentTrack?.Identifier ? "🎶 " : string.Empty)}{ix + 1} - `[{queueItem.Duration:hh\:mm\:ss}]` [{queueItem.Title.AsBold()}]({queueItem.Uri})"
            )
        );

        var footer = player.CurrentTrack is not null
            ? $"Current track: {player.CurrentTrack.Title} [{player.Position!.Value.Position.ToShortString()} / {player.CurrentTrack.Duration.ToShortString()}] | {queue.Count} tracks total"
            : $"{queue.Count} tracks total";

        var embed = new EmbedProperties
        {
            Title = "Queue",
            Description = description,
            Footer = new EmbedFooterProperties {Text = footer}
        };

        await ReplyAsync(new ReplyMessageProperties {Embeds = [embed]});
    }

    [AniviaCommand("join", "Playback", "Makes me join your voice channel")]
    [RequireUserInVoiceChannel]
    public async Task JoinCurrentVoiceChannelAsync()
    {
        await GetPlayerAsync();
        await ReplyAsync(new ReplyMessageProperties {Embeds = [Embeds.Success("I have been summoned")]});
    }

    [AniviaCommand("leave", "Playback", "Makes me leave the voice channel")]
    [RequireUserInVoiceChannel]
    public async Task LeaveAsync()
    {
        var player = await GetPlayerAsync();
        if (player is not null)
        {
            await player.DisconnectAsync();
        }
    }

    [AniviaCommand("loop", "Playback", "Loops the current track or queue", "repeat")]
    [RequireUserInVoiceChannel]
    public Task LoopAsync([CommandParameter(Remainder = true)] string sub = "current")
    {
        return sub switch
        {
            "queue" => ToggleQueueLoopAsync(),
            _       => ToggleCurrentTrackLoopAsync()
        };
    }

    [AniviaCommand("move", "Playback", "Moves a track from position x to y", "mv")]
    public async Task MoveTrackAsync(int from, int to)
    {
        var player = await GetPlayerAsync();
        if (player is null)
        {
            return;
        }

        if (player.MoveTrack(from - 1, to - 1))
        {
            await ReplyAsync(new ReplyMessageProperties {Embeds = [Embeds.Success("Track moved")]});
        }
        else
        {
            await ReplyAsync(new ReplyMessageProperties {Embeds = [Embeds.Error("Swap failed")]});
        }
    }

    [AniviaCommand("play", "Playback", "Queue a track or playlist from a search term or url", "p")]
    [RequireUserInVoiceChannel]
    public async Task PlayAsync([CommandParameter(Remainder = true)] string song)
    {
        var player = await GetPlayerAsync().ConfigureAwait(false);
        if (player is null)
        {
            return;
        }

        TrackLoadResult trackLoadResult;
        if (Uri.TryCreate(song, UriKind.Absolute, out _))
        {
            trackLoadResult = await _lavalinkAudioService.Tracks.LoadTracksAsync(song, TrackSearchMode.None)
                .ConfigureAwait(false);
        }
        else
        {
            trackLoadResult = await _lavalinkAudioService.Tracks.LoadTracksAsync(song, TrackSearchMode.YouTube)
                .ConfigureAwait(false);
        }

        if (!trackLoadResult.HasMatches)
        {
            await ReplyAsync(new ReplyMessageProperties {Embeds = [Embeds.Error("No tracks that match your search")]});

            return;
        }

        var user = Context.User;
        var username = user.GlobalName ?? user.Username;

        if (trackLoadResult.IsPlaylist)
        {
            var selectedTrack = trackLoadResult.Playlist.SelectedTrack ?? trackLoadResult.Tracks.First();
            var playlistDuration = new TimeSpan(trackLoadResult.Tracks.Sum(t => t.Duration.Ticks));
            var positionInUpcoming = player.Queue.Count - (player.Queue.History?.Count ?? 0) + 1;

            foreach (var playlistTrack in trackLoadResult.Tracks)
            {
                await player.PlayAsync(playlistTrack).ConfigureAwait(false);
            }

            var embed = new EmbedProperties
            {
                Title = "Added Playlist",
                Thumbnail = new EmbedThumbnailProperties(
                    selectedTrack.ArtworkUri?.ToString() ??
                    $"https://img.youtube.com/vi/{selectedTrack.Identifier}/0.jpg"
                ),
                Fields =
                [
                    new EmbedFieldProperties {Name = "Track", Value = $"[{selectedTrack.Title}]({selectedTrack.Uri})"},
                    new EmbedFieldProperties
                        {Name = "Playlist length", Value = playlistDuration.ToString(@"hh\:mm\:ss"), Inline = true},
                    new EmbedFieldProperties
                    {
                        Name = "Position in upcoming",
                        Value = positionInUpcoming == 1 ? "Next" : positionInUpcoming.ToString()
                    },
                    new EmbedFieldProperties
                        {Name = "Number of tracks", Value = trackLoadResult.Tracks.Length.ToString(), Inline = true}
                ],
                Footer = new EmbedFooterProperties
                {
                    Text = $"Requested by {username}",
                    IconUrl = user.HasAvatar ? user.GetAvatarUrl().ToString() : null
                }
            };

            await ReplyAsync(new ReplyMessageProperties {Embeds = [embed]});
        }
        else
        {
            var track = trackLoadResult.Track ?? trackLoadResult.Tracks.First();

            if (player.State is PlayerState.Playing)
            {
                var embed = new EmbedProperties
                {
                    Title = "Added Track",
                    Thumbnail = new EmbedThumbnailProperties(
                        track.ArtworkUri?.ToString() ?? $"https://img.youtube.com/vi/{track.Identifier}/0.jpg"
                    ),
                    Fields =
                    [
                        new EmbedFieldProperties {Name = "Track", Value = $"[{track.Title}]({track.Uri})"},
                        new EmbedFieldProperties
                            {Name = "Track length", Value = track.Duration.ToString(@"hh\:mm\:ss"), Inline = true},
                        new EmbedFieldProperties
                            {Name = "Position in queue", Value = (player.Queue.Count + 1).ToString(), Inline = true}
                    ],
                    Footer = new EmbedFooterProperties
                    {
                        Text = $"Requested by {username}",
                        IconUrl = user.HasAvatar ? user.GetAvatarUrl().ToString() : null
                    }
                };

                await ReplyAsync(new ReplyMessageProperties {Embeds = [embed]});
            }

            await player.PlayAsync(track).ConfigureAwait(false);
        }
    }

    [AniviaCommand("remove", "Playback", "Removes a track from the queue. rm <track number>", "rm", "delete", "del")]
    public async Task RemoveTrackAsync(int position)
    {
        var player = await GetPlayerAsync();
        if (player is null)
        {
            return;
        }

        var index = position - 1;
        if (index < 0 || index >= player.Queue.Count)
        {
            await ReplyAsync(new ReplyMessageProperties {Embeds = [Embeds.Error("Invalid track position")]});

            return;
        }

        var track = player.Queue[index]!;
        player.Queue.RemoveAt(index);

        await ReplyAsync(
            new ReplyMessageProperties {Embeds = [Embeds.Success($"Removed track [{track.Title}]({track.Uri})")]}
        );
    }

    private async Task ToggleCurrentTrackLoopAsync()
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
            new ReplyMessageProperties
            {
                Embeds =
                [
                    Embeds.Success(
                        $"I will {(player.RepeatMode is TrackRepeatMode.Track ? "now" : "no longer")} repeat the current track"
                    )
                ]
            }
        );
    }

    private async Task ToggleQueueLoopAsync()
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
            new ReplyMessageProperties
            {
                Embeds =
                [
                    Embeds.Success(
                        $"I will {(player.RepeatMode is TrackRepeatMode.Queue ? "now" : "no longer")} repeat the queue"
                    )
                ]
            }
        );
    }
}