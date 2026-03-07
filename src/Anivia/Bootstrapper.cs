using Anivia.CommandModules;
using Anivia.Infrastructure;
using Lavalink4NET;
using NetCord;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;
using NetCord.Rest;

namespace Anivia;

public sealed class VoiceStateHandler(
    IAudioService lavalinkAudioService,
    GatewayClient gatewayClient,
    IAuditableOptionsSnapshot<DiscordOptions> discordOptions
) : IVoiceStateUpdateGatewayHandler
{
    public async ValueTask HandleAsync(VoiceState voiceState)
    {
        if (voiceState.ChannelId is not { } channelId)
        {
            return;
        }

        var guildId = voiceState.GuildId;
        var guild = gatewayClient.Cache.Guilds.GetValueOrDefault(guildId);
        if (guild is null)
        {
            return;
        }

        var usersInChannel = guild.VoiceStates.Values.Count(vs => vs.ChannelId == channelId);
        if (usersInChannel >= 2)
        {
            return;
        }

        var player = await lavalinkAudioService.Players.GetPlayerAsync(guildId);
        if (player is not null)
        {
            await player.DisconnectAsync();

            var textChannelId = discordOptions.CurrentValue.TextChannels.GetValueOrDefault(guildId);
            if (textChannelId == 0)
            {
                return;
            }

            var textChannel = guild.Channels.GetValueOrDefault(textChannelId) as TextGuildChannel;
            if (textChannel is null)
            {
                return;
            }

            await textChannel.SendMessageAsync(
                new MessageProperties {Embeds = [Embeds.Error("Stopping because everyone left")]}
            );
        }
    }
}

public sealed class ReadyHandler(
    PlaybackEventListener playbackEventListener,
    ILogger<ReadyHandler> logger
) : IReadyGatewayHandler
{
    public ValueTask HandleAsync(ReadyEventArgs args)
    {
        logger.LogInformation("Discord gateway ready");
        playbackEventListener.Subscribe();

        return ValueTask.CompletedTask;
    }
}