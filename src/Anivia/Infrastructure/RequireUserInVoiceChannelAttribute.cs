using Anivia.CommandModules;
using NetCord.Rest;
using NetCord.Services;
using NetCord.Services.Commands;

namespace Anivia.Infrastructure;

internal sealed class RequireUserInVoiceChannelAttribute : PreconditionAttribute<CommandContext>
{
    public override ValueTask<PreconditionResult> EnsureCanExecuteAsync(
        CommandContext context,
        IServiceProvider? serviceProvider
    )
    {
        var guild = context.Guild;
        if (guild is null)
        {
            return new ValueTask<PreconditionResult>(
                PreconditionResult.Fail("This command can only be used in a guild.")
            );
        }

        var userId = context.User.Id;
        var voiceState = guild.VoiceStates.GetValueOrDefault(userId);

        if (voiceState?.ChannelId is null)
        {
            _ = context.Message.ReplyAsync(
                new ReplyMessageProperties
                {
                    Embeds = [Embeds.Error("You are not in a voice channel")]
                }
            );

            return new ValueTask<PreconditionResult>(
                PreconditionResult.Fail("You must be in a voice channel to use this command.")
            );
        }

        return new ValueTask<PreconditionResult>(PreconditionResult.Success);
    }
}