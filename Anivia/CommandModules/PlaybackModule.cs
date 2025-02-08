using System.Text.RegularExpressions;
using Anivia.Extensions;
using Discord;
using Discord.Commands;
using Fergun.Interactive;
using Fergun.Interactive.Pagination;
using Victoria;
using Victoria.Rest;
using Victoria.Rest.Filters;
using Victoria.Rest.Search;
using YouTubeSearch;

namespace Anivia.CommandModules;

[Name("Playback")]
public sealed class PlaybackModule : ModuleBase
{
    public enum BassBoost
    {
        Mute,
        None,
        Low,
        Medium,
        High
    }

    private static readonly Regex LinkRegex = new(
        "(http[s]?:\\/\\/(www\\.)?|ftp:\\/\\/(www\\.)?|www\\.){1}([0-9A-Za-z-\\.@:%_\\+~#=]+)+((\\.[a-zA-Z]{2,3})+)(/(.)*)?(\\?(.)*)?"
    );

    private readonly InteractiveService _interactiveService;

    private readonly LavaNode _lavaNode;

    public PlaybackModule(LavaNode lavaNode, InteractiveService interactiveService)
    {
        _lavaNode = lavaNode;
        _interactiveService = interactiveService;
    }

    [Command("bassboost")]
    [Summary("Sets the bass boost")]
    [Remarks("bassboost <mute|none|low|medium|high>")]
    public async Task BassBoostAsync(BassBoost boost)
    {
        // Victoria TODO: this only works if boosting from 0 gain. I.e., High -> Mute does not work
        var voiceState = (IVoiceState) Context.User;
        if (voiceState.VoiceChannel is null)
        {
            await ReplyAsync(embed: Embeds.Error("You are not in a voice channel"));

            return;
        }

        var player = await _lavaNode.JoinAsync(voiceState.VoiceChannel);

        var gain = MapBoost();

        await player.EqualizeAsync(_lavaNode, Enumerable.Range(0, 3).Select(x => new EqualizerBand(x, 0)).ToArray());
        await player.EqualizeAsync(_lavaNode, Enumerable.Range(0, 3).Select(x => new EqualizerBand(x, gain)).ToArray());
        await ReplyAsync(embed: Embeds.Success($"Bass boost set to {boost.ToString()}"));

        return;

        double MapBoost()
        {
            return boost switch
            {
                BassBoost.Mute => -0.25,
                BassBoost.None => 0.0,
                BassBoost.Low => 0.20,
                BassBoost.Medium => 0.30,
                BassBoost.High => 0.35,
                _ => throw new ArgumentOutOfRangeException(nameof(boost), boost, null)
            };
        }
    }

    [Command("clear")]
    [Summary("Clears the current queue")]
    public async Task ClearAsync()
    {
        var voiceState = (IVoiceState) Context.User;
        if (voiceState.VoiceChannel is null)
        {
            await ReplyAsync(embed: Embeds.Error("You are not in a voice channel"));

            return;
        }

        var player = await _lavaNode.GetPlayerAsync(Context.Guild.Id);
        if (player is null)
        {
            await ReplyAsync(embed: Embeds.Error("Nothing is playing"));

            return;
        }
        
        player.GetCustomQueue().Clear();
        await player.StopAsync(_lavaNode, player.Track);

        await ReplyAsync(embed: Embeds.Success("Cleared the queue"));
    }

    [Command("queue", RunMode = RunMode.Async)]
    [Alias("q")]
    [Summary("Displays the current queue")]
    [Remarks("queue [page number]")]
    public async Task DisplayQueueAsync()
    {
        const int tracksPerPage = 10;

        var player = await _lavaNode.GetPlayerAsync(Context.Guild.Id);
        if (player is null)
        {
            await ReplyAsync(embed: Embeds.Error("Nothing is playing"));

            return;
        }

        var queue = player.GetCustomQueue();
        if (queue.Length == 0)
        {
            await ReplyAsync(embed: Embeds.Error("The queue is empty"));

            return;
        }

        var maxPages = (int) Math.Ceiling((double) queue.Length / tracksPerPage);

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
                        tracks.Select(
                            (track, ix) =>
                                $"{(track == queue.Current ? "🎶 " : string.Empty)}{index * tracksPerPage + ix + 1} - `[{track.Duration}]` [{track.Title.AsBold()}]({track.Url})"
                        )
                    )
                )
                .WithFooter(
                    $"Current track: {player.Track.Title} [{player.Track.Position.ToShortString()} / {player.Track.Duration.ToShortString()}]"
                );
        }
    }

    [Command("insert")]
    [Summary("Insert a track right after the one that is currently playing")]
    public async Task InsertAsync()
    {
        // TODO
        await ReplyAsync("TODO");
    }

    [Command("join")]
    [Summary("Makes me join your voice channel")]
    public async Task JoinCurrentVoiceChannelAsync()
    {
        var voiceState = (IVoiceState) Context.User;
        if (voiceState.VoiceChannel is null)
        {
            await ReplyAsync(embed: Embeds.Error("You are not in a voice channel"));

            return;
        }

        await voiceState.VoiceChannel.ConnectAsync(true);
        await ReplyAsync(embed: Embeds.Success("I have been summoned"));
    }

    [Command("leave")]
    [Summary("Makes me leave the voice channel")]
    public async Task LeaveAsync()
    {
        var voiceState = (IVoiceState) Context.User;
        if (voiceState.VoiceChannel is null)
        {
            await ReplyAsync(embed: Embeds.Error("You are not in a voice channel"));

            return;
        }

        await _lavaNode.LeaveAsync(voiceState.VoiceChannel);
    }

    [Command("loop current")]
    [Alias("loop", "repeat", "repeat current")]
    public async Task LoopCurrentSong()
    {
        var voiceState = (IVoiceState) Context.User;
        if (voiceState.VoiceChannel is null)
        {
            await ReplyAsync(embed: Embeds.Error("You are not in a voice channel"));

            return;
        }

        var player = await _lavaNode.GetPlayerAsync(Context.Guild.Id);
        if (player is null)
        {
            await ReplyAsync(embed: Embeds.Error("Nothing is playing"));

            return;
        }

        var queue = player.GetCustomQueue();
        queue.IsCurrentTrackLooped = !queue.IsCurrentTrackLooped;

        await ReplyAsync(
            embed: Embeds.Success(
                $"I will {(queue.IsCurrentTrackLooped ? "now" : "no longer")} repeat the current track"
            )
        );
    }

    [Command("loop queue")]
    [Alias("repeat queue")]
    public async Task LoopQueueAsync()
    {
        var voiceState = (IVoiceState) Context.User;
        if (voiceState.VoiceChannel is null)
        {
            await ReplyAsync(embed: Embeds.Error("You are not in a voice channel"));

            return;
        }

        var player = await _lavaNode.GetPlayerAsync(Context.Guild.Id);
        if (player is null)
        {
            await ReplyAsync(embed: Embeds.Error("Nothing is playing"));

            return;
        }

        var queue = player.GetCustomQueue();
        queue.IsLooped = !queue.IsLooped;

        await ReplyAsync(embed: Embeds.Success($"I will {(queue.IsLooped ? "now" : "no longer")} repeat the queue"));
    }

    [Command("move")]
    [Alias("mv")]
    public async Task MoveAsync(int from, int to)
    {
        var voiceState = (IVoiceState) Context.User;
        if (voiceState.VoiceChannel is null)
        {
            await ReplyAsync(embed: Embeds.Error("You are not in a voice channel"));

            return;
        }

        var player = await _lavaNode.GetPlayerAsync(Context.Guild.Id);
        if (player is null)
        {
            await ReplyAsync(embed: Embeds.Error("Nothing is playing"));

            return;
        }

        var queue = player.GetCustomQueue();
        if (from < 1 || from > queue.Length || to < 1 || to > queue.Length)
        {
            await ReplyAsync(embed: Embeds.Error("Invalid indices"));

            return;
        }

        queue.Move(from, to);

        await ReplyAsync(embed: Embeds.Success("Track moved"));
    }

    [Command("play")]
    [Alias("p")]
    [Summary("Queue a track or playlist from a search term or url")]
    public async Task PlayAsync([Remainder] string song)
    {
        var voiceState = (IVoiceState) Context.User;
        if (voiceState.VoiceChannel is null)
        {
            await ReplyAsync(embed: Embeds.Error("You are not in a voice channel"));

            return;
        }

        var player = await _lavaNode.GetPlayerAsync(Context.Guild.Id);
        if (player is {State: {IsConnected: true}} && player.VoiceState.SessionId != voiceState.VoiceSessionId)
        {
            await ReplyAsync(embed: Embeds.Error("Music is already playing in another channel"));
            
            return;
        }
        
        player = await _lavaNode.JoinAsync(voiceState.VoiceChannel);
        
        SearchResponse searchResponse;
        if (Uri.IsWellFormedUriString(song, UriKind.Absolute))
        {
            searchResponse = await _lavaNode.LoadTrackAsync(song);
        }
        else
        {
            var search = new VideoSearch();
            var firstMatch = (await search.GetVideos(song, 1)).FirstOrDefault()?.getUrl();
            if (firstMatch is null)
            {
                await ReplyAsync("No songs matched");
            }
            
            searchResponse = await _lavaNode.LoadTrackAsync(firstMatch);
        }

        var queue = player.GetCustomQueue();
        switch (searchResponse.Type)
        {
            // case SearchType.LoadFailed:
            // {
            //     var reason = !string.IsNullOrWhiteSpace(searchResponse.Exception.Message)
            //         ? searchResponse.Exception.Message
            //         : "unknown";
            //
            //     await ReplyAsync(embed: Embeds.Error($"Error loading track: {reason}"));
            //
            //     break;
            // }
            case SearchType.Empty:
            {
                await ReplyAsync(embed: Embeds.Error("No tracks that match your search"));

                break;
            }
            case SearchType.Playlist:
            {
                var track = searchResponse.Tracks.First();
                queue.Add(searchResponse.Tracks);

                var embed = new EmbedBuilder()
                    .WithTitle("Added Playlist")
                    .WithThumbnailUrl($"https://img.youtube.com/vi/{track.Id}/0.jpg")
                    .AddField("Track", $"[{track.Title}]({track.Url})")
                    // .AddField("Estimated time until played", "n")
                    .AddField("Playlist length", track.Duration, true)
                    // .AddField("Position in upcoming", queue.Next == track ? "Next" : queue.Length)
                    .AddField("Number of tracks", queue.Length, true)
                    .WithFooter($"Requested by {Context.User.Username}", Context.User.GetAvatarUrl())
                    .Build();

                await ReplyAsync(embed: embed);

                break;
            }
            default:
            {
                var bestMatch = searchResponse.Tracks.First();
                queue.Add(bestMatch);

                if (queue.Length > 1)
                {
                    var embed = new EmbedBuilder()
                        .WithTitle("Added Track")
                        .WithThumbnailUrl($"https://img.youtube.com/vi/{bestMatch.Id}/0.jpg")
                        .AddField("Track", $"[{bestMatch.Title}]({bestMatch.Url})")
                        // .AddField("Estimated time until played", "n")
                        .AddField("Track length", bestMatch.Duration, true)
                        // .AddField("Position in upcoming", queue.Next == track ? "Next" : queue.Length)
                        .AddField("Position in queue", queue.Length, true)
                        .WithFooter($"Requested by {Context.User.Username}", Context.User.GetAvatarUrl())
                        .Build();

                    await ReplyAsync(embed: embed);
                }

                break;
            }
        }

        if (player.IsPaused)
        {
            await player.PlayAsync(_lavaNode, queue.GetNext());
        }
    }

    [Command("remove")]
    [Alias("rm", "delete", "del")]
    [Summary("Removes a track from the queue")]
    [Remarks("remove <track number (1, 2, 3)>")]
    public async Task RemoveAsync(int index)
    {
        var voiceState = (IVoiceState) Context.User;
        if (voiceState.VoiceChannel is null)
        {
            await ReplyAsync(embed: Embeds.Error("You are not in a voice channel"));

            return;
        }

        var player = await _lavaNode.GetPlayerAsync(Context.Guild.Id);
        if (player is null || player.GetQueue().Count is 0)
        {
            await ReplyAsync(embed: Embeds.Error("Nothing is playing"));

            return;
        }

        var queue = player.GetCustomQueue();
        var track = queue.Remove(index - 1)!;

        await ReplyAsync(embed: Embeds.Success($"Removed track [{track.Title}]({track.Url})"));
    }
}