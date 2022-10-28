using Discord;
using Discord.WebSocket;

namespace Anivia;

public static class RenderKeepAliveTask
{
    private static readonly CancellationTokenSource CancellationTokenSource = new();
    private static readonly PeriodicTimer Timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
    private static Task? _handler;

    public static void Start(DiscordSocketClient discordSocketClient) =>
        _handler = RenderKeepAliveAsync(discordSocketClient);
    
    private static async Task RenderKeepAliveAsync(DiscordSocketClient discordSocketClient)
    {
        while (await Timer.WaitForNextTickAsync(CancellationTokenSource.Token))
        {
            var aniviaRole = discordSocketClient.Guilds.First().GetRole(1032015477054115862);
            await aniviaRole.ModifyAsync(
                r =>
                {
                    r.Color = new Color(Random.Shared.Next(255), Random.Shared.Next(255), Random.Shared.Next(255));
                });
        }
    }
}