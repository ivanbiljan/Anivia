using Anivia.Infrastructure;
using Lavalink4NET;
using Lavalink4NET.NetCord;
using Lavalink4NET.Players;
using NetCord.Rest;
using NetCord.Services.Commands;

namespace Anivia.CommandModules;

public sealed class AniviaCommandAttribute(
    string name,
    string category,
    string? description = null,
    params string[] aliases
) : CommandAttribute(new[] {name}.Concat(aliases).ToArray())
{
    public string Name { get; } = name;

    public string Category { get; } = category;

    public string? Description { get; } = description;
}

public abstract class AniviaCommandModule(IAudioService lavalinkAudioService) : CommandModule<CommandContext>
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

            await ReplyAsync(new ReplyMessageProperties {Embeds = [Embeds.Error(errorMessage)]}).ConfigureAwait(false);

            return null;
        }

        return result.Player;

        static ValueTask<AniviaPlayer> CreatePlayerAsync(
            IPlayerProperties<AniviaPlayer, LavalinkPlayerOptions> properties,
            CancellationToken cancellationToken = default
        )
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentNullException.ThrowIfNull(properties);

            return ValueTask.FromResult(new AniviaPlayer(properties));
        }
    }
}