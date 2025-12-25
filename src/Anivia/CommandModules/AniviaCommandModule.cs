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

    public async ValueTask<QueuedLavalinkPlayer?> GetPlayerAsync(bool connectToVoiceChannel = false)
    {
        var channelBehavior = connectToVoiceChannel ? PlayerChannelBehavior.Join : PlayerChannelBehavior.None;
        
        var retrieveOptions = new PlayerRetrieveOptions(
            ChannelBehavior: channelBehavior,
            VoiceStateBehavior: MemberVoiceStateBehavior.Ignore
        );
        
        var playerResult = await _lavalinkAudioService.Players
            .RetrieveAsync(Context, PlayerFactory.Queued, retrieveOptions);

        if (playerResult.IsSuccess)
        {
            return playerResult.Player;
        }

        var errorMessage = playerResult.Status switch
        {
            PlayerRetrieveStatus.VoiceChannelMismatch => "Music is already playing in another channel",
            PlayerRetrieveStatus.BotNotConnected => "Nothing is playing",
            _ => $"An unknown error has occurred: {playerResult.Status}"
        };

        await ReplyAsync(embed: Embeds.Error(errorMessage));

        return null;
    }
}