using Anivia.Infrastructure;
using Discord.Commands;
using Lavalink4NET;
using Lavalink4NET.DiscordNet;
using Lavalink4NET.Players;
using Lavalink4NET.Players.Queued;

namespace Anivia.CommandModules;

public abstract class AniviaCommandModule(IAudioService lavalinkAudioService) : ModuleBase
{
    private readonly IAudioService _lavalinkAudioService = lavalinkAudioService;

    protected async ValueTask<AniviaPlayer?> GetPlayerAsync(bool connectToVoiceChannel = true)
    {
        var channelBehavior = connectToVoiceChannel
            ? PlayerChannelBehavior.Join
            : PlayerChannelBehavior.None;

        var retrieveOptions = new PlayerRetrieveOptions(
            channelBehavior
        );

        var result = await _lavalinkAudioService.Players
            .RetrieveAsync<AniviaPlayer, LavalinkPlayerOptions>(Context, CreatePlayerAsync, retrieveOptions)
            .ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            var errorMessage = result.Status switch
            {
                PlayerRetrieveStatus.VoiceChannelMismatch => "Music is already playing in another channel",
                PlayerRetrieveStatus.BotNotConnected      => "Nothing is playing",
                _                                         => $"An unknown error has occurred: {result.Status}"
            };

            await ReplyAsync(embed: Embeds.Error(errorMessage)).ConfigureAwait(false);

            return null;
        }

        return result.Player;
        
        static ValueTask<AniviaPlayer> CreatePlayerAsync(IPlayerProperties<AniviaPlayer, LavalinkPlayerOptions> properties, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentNullException.ThrowIfNull(properties);

            return ValueTask.FromResult(new AniviaPlayer(properties));
        }
    }
}