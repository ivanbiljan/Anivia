using Discord.Commands;
using Lavalink4NET;
using Lavalink4NET.Clients;
using Lavalink4NET.DiscordNet;
using Lavalink4NET.Players;
using Lavalink4NET.Players.Queued;

namespace Anivia.CommandModules;

public abstract class AniviaCommandModule(IAudioService lavalinkAudioService) : ModuleBase
{
    private readonly IAudioService _lavalinkAudioService = lavalinkAudioService;

    protected async ValueTask<QueuedLavalinkPlayer?> GetPlayerAsync(bool connectToVoiceChannel = true)
    {
        var channelBehavior = connectToVoiceChannel
            ? PlayerChannelBehavior.Join
            : PlayerChannelBehavior.None;

        var retrieveOptions = new PlayerRetrieveOptions(
            ChannelBehavior: channelBehavior,
            VoiceStateBehavior: MemberVoiceStateBehavior.Ignore
        );

        var result = await _lavalinkAudioService.Players
            .RetrieveAsync(Context, playerFactory: PlayerFactory.Queued, retrieveOptions)
            .ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            var errorMessage = result.Status switch
            {
                PlayerRetrieveStatus.VoiceChannelMismatch => "Music is already playing in another channel",
                PlayerRetrieveStatus.BotNotConnected => "Nothing is playing",
                _ => $"An unknown error has occurred: {result.Status}"
            };

            await ReplyAsync(embed: Embeds.Error(errorMessage)).ConfigureAwait(false);

            return null;
        }

        return result.Player;
    }
}