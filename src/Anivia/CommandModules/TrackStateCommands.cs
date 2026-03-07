using System.Text.RegularExpressions;
using Anivia.Extensions;
using Anivia.Infrastructure;
using Lavalink4NET;
using NetCord.Rest;
using NetCord.Services.Commands;

namespace Anivia.CommandModules;

public sealed class TrackStateCommands(IAudioService lavalinkAudioService)
    : AniviaCommandModule(lavalinkAudioService)
{

    [AniviaCommand("pause", "Track state", "Pauses the current track")]
    [RequireUserInVoiceChannel]
    public async Task PauseCurrentTrackAsync()
    {
        var player = await GetPlayerAsync();
        if (player is not {CurrentTrack: not null})
        {
            return;
        }

        await player.PauseAsync();
        await ReplyAsync(
            new ReplyMessageProperties
            {
                Embeds =
                [
                    Embeds.Success(
                        $"Track has been paused at {player.Position!.Value.Position.ToShortString().AsBold()} :pause_button:"
                    )
                ]
            }
        );
    }

    [AniviaCommand("resume", "Track state", "Resumes the current track")]
    [RequireUserInVoiceChannel]
    public async Task ResumeCurrentTrackAsync()
    {
        var player = await GetPlayerAsync();
        if (player is not {CurrentTrack: not null})
        {
            return;
        }

        await player.ResumeAsync();

        await ReplyAsync(new ReplyMessageProperties {Embeds = [Embeds.Success("Track has been resumed :play_pause:")]});
    }

    [AniviaCommand("seek", "Track state", "Seeks the current track. seek <seconds|1m30s>")]
    public async Task SeekAsync([CommandParameter(Remainder = true)] string timestamp)
    {
        var player = await GetPlayerAsync();
        if (player is not {CurrentTrack: not null})
        {
            return;
        }

        var position = ParseTimestamp(timestamp);
        if (position is null || position.Value >= player.CurrentTrack.Duration)
        {
            await ReplyAsync(new ReplyMessageProperties {Embeds = [Embeds.Error("Invalid timestamp")]});

            return;
        }

        await player.SeekAsync(position.Value);
        await ReplyAsync(
            new ReplyMessageProperties
            {
                Embeds =
                [
                    Embeds.Success(
                        $"Track has been wound to {position.Value.ToShortString()}/{player.CurrentTrack!.Duration.ToShortString()} :fast_forward:"
                    )
                ]
            }
        );

        return;

        static TimeSpan? ParseTimestamp(string input)
        {
            if (int.TryParse(input, out var inputSeconds))
            {
                return TimeSpan.FromSeconds(inputSeconds);
            }

            if (TimeSpan.TryParse(input, out var result))
            {
                return result;
            }

            var match = Regex.Match(input, @"(?:(\d+)m)?(?:(\d+)s)?");
            if (!match.Groups[1].Success && !match.Groups[2].Success)
            {
                return null;
            }

            var minutes = match.Groups[1].Success ? int.Parse(match.Groups[1].Value) : 0;
            var seconds = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : 0;

            return new TimeSpan(0, minutes, seconds);
        }
    }

    [AniviaCommand("backward", "Track state", "Rewinds the current track by a number of seconds", "back", "b", "rewind")]
    public async Task WindCurrentTrackBackwardsAsync([CommandParameter] int seconds)
    {
        var player = await GetPlayerAsync();
        if (player is null)
        {
            return;
        }

        if (player.CurrentTrack is null)
        {
            return;
        }

        var forwardedTimestamp = player.Position!.Value.Position.Subtract(TimeSpan.FromSeconds(seconds));
        await player.SeekAsync(forwardedTimestamp);

        await ReplyAsync(
            new ReplyMessageProperties
            {
                Embeds =
                [
                    Embeds.Success(
                        $"Track has been wound to {forwardedTimestamp.ToShortString()}/{player.CurrentTrack.Duration} :rewind:"
                    )
                ]
            }
        );
    }

    [AniviaCommand("forward", "Track state", "Forwards the current track by a number of seconds", "f")]
    public async Task WindCurrentTrackForwardAsync(int seconds)
    {
        var player = await GetPlayerAsync();
        if (player is not {CurrentTrack: not null})
        {
            return;
        }

        var forwardedTimestamp = player.Position!.Value.Position.Add(TimeSpan.FromSeconds(seconds));
        await player.SeekAsync(forwardedTimestamp);

        await ReplyAsync(
            new ReplyMessageProperties
            {
                Embeds =
                [
                    Embeds.Success(
                        $"Track has been wound to {forwardedTimestamp.ToShortString()}/{player.CurrentTrack.Duration} :fast_forward:"
                    )
                ]
            }
        );
    }
}