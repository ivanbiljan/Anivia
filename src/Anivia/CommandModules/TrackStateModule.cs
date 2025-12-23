using System.Text.RegularExpressions;
using Anivia.Extensions;
using Discord;
using Discord.Commands;
using Victoria;

namespace Anivia.CommandModules;

[Name("Track State")]
[Summary("Commands used for updating the state of the current track")]
public sealed class TrackStateModule(LavaNode<LavaPlayer<LavaTrack>, LavaTrack> lavaNode) : ModuleBase
{
    private readonly LavaNode<LavaPlayer<LavaTrack>, LavaTrack> _lavaNode = lavaNode;

    [Command("pause")]
    [Summary("Pauses the current track")]
    public async Task PauseCurrentTrackAsync()
    {
        var voiceState = (IVoiceState)Context.User;
        if (voiceState.VoiceChannel is null)
        {
            await ReplyAsync(embed: Embeds.Error("You are not in a voice channel"));

            return;
        }

        if (await _lavaNode.TryGetPlayerAsync(Context.Guild.Id) is not LavaPlayer player)
        {
            await ReplyAsync(embed: Embeds.Error("Nothing is playing"));

            return;
        }

        await player.PauseAsync(_lavaNode);
        await ReplyAsync(
            embed: Embeds.Success(
                $"Track has been paused at {player.Track.Position.ToShortString().AsBold()} :pause_button:"));
    }

    [Command("resume")]
    [Summary("Resumes the current track")]
    public async Task ResumeCurrentTrackAsync()
    {
        var voiceState = (IVoiceState)Context.User;
        if (voiceState.VoiceChannel is null)
        {
            await ReplyAsync(embed: Embeds.Error("You are not in a voice channel"));

            return;
        }

        if (await _lavaNode.TryGetPlayerAsync(Context.Guild.Id) is not LavaPlayer player)
        {
            await ReplyAsync(embed: Embeds.Error("Nothing is playing"));

            return;
        }

        await player.ResumeAsync(_lavaNode, player.Track);
        await ReplyAsync(embed: Embeds.Success("Track has been resumed :play_pause:"));
    }

    [Command("seek")]
    [Alias("wind to")]
    public async Task SeekAsync(string timestamp)
    {
        if (await _lavaNode.TryGetPlayerAsync(Context.Guild.Id) is not LavaPlayer player)
        {
            await ReplyAsync(embed: Embeds.Error("Nothing is playing"));

            return;
        }

        var position = ParseTimestamp(timestamp);
        if (position is null)
        {
            await ReplyAsync(embed: Embeds.Error("Invalid timestamp"));

            return;
        }

        await player.SeekAsync(_lavaNode, position.Value);

        await ReplyAsync(
            embed: Embeds.Success(
                $"Track has been wound to {position.Value.ToShortString()}/{player.Track.Duration.ToShortString()}"));

        return;

        static TimeSpan? ParseTimestamp(string input)
        {
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

    [Command("backward")]
    [Alias("back", "b", "rewind")]
    [Summary("Rewinds the current track by a number of seconds")]
    [Remarks("back 10")]
    public async Task WindCurrentTrackBackwardsAsync(int seconds)
    {
        if (await _lavaNode.TryGetPlayerAsync(Context.Guild.Id) is not LavaPlayer player)
        {
            await ReplyAsync(embed: Embeds.Error("Nothing is playing"));

            return;
        }

        var forwardedTimestamp = player.Track.Position.Subtract(TimeSpan.FromSeconds(seconds));
        await player.SeekAsync(_lavaNode, forwardedTimestamp);

        await ReplyAsync(
            embed: Embeds.Success(
                $"Track has been wound to {forwardedTimestamp.ToShortString()}/{player.Track.Duration} :rewind:"));
    }

    [Command("forward")]
    [Alias("f")]
    public async Task WindCurrentTrackForwardAsync(int seconds)
    {
        if (await _lavaNode.TryGetPlayerAsync(Context.Guild.Id) is not LavaPlayer player)
        {
            await ReplyAsync(embed: Embeds.Error("Nothing is playing"));

            return;
        }

        var forwardedTimestamp = player.Track.Position.Add(TimeSpan.FromSeconds(seconds));
        await player.SeekAsync(_lavaNode, forwardedTimestamp);

        await ReplyAsync(
            embed: Embeds.Success(
                $"Track has been wound to {forwardedTimestamp.ToShortString()}/{player.Track.Duration} :fast_forward:"));
    }
}