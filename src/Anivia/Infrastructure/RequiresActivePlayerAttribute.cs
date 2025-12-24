using Discord.Commands;
using Victoria;

namespace Anivia.Infrastructure;

internal sealed class RequiresActivePlayerAttribute : PreconditionAttribute
{
    public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
    {
        var lavaNode = services.GetRequiredService<LavaNode<LavaPlayer<LavaTrack>, LavaTrack>>();
        if (await lavaNode.TryGetPlayerAsync(context.Guild.Id) is null)
        {
            return PreconditionResult.FromError("Nothing is playing");
        }

        return PreconditionResult.FromSuccess();
    }
}