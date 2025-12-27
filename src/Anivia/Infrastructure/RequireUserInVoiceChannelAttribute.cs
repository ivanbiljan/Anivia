using Anivia.CommandModules;
using Discord;
using Discord.Commands;

namespace Anivia.Infrastructure;

internal sealed class RequireUserInVoiceChannelAttribute : PreconditionAttribute
{
    public override async Task<PreconditionResult> CheckPermissionsAsync(
        ICommandContext context,
        CommandInfo command,
        IServiceProvider services
    )
    {
        if (context.User is not IVoiceState {VoiceChannel: not null})
        {
            await context.Channel.SendMessageAsync(embed: Embeds.Error("You are not in a voice channel"));
            
            return PreconditionResult.FromError("You must be in a voice channel to use this command.");
        }

        return PreconditionResult.FromSuccess();
    }
}