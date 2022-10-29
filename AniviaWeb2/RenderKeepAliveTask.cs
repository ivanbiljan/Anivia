using System.Runtime.CompilerServices;
using Discord;
using Discord.WebSocket;

namespace Anivia;

public static class RenderKeepAliveTask
{
    private static readonly HttpClient HttpClient = new();
    private static readonly CancellationTokenSource CancellationTokenSource = new();
    private static readonly PeriodicTimer Timer = new PeriodicTimer(TimeSpan.FromSeconds(15));
    private static Task? _handler;

    public static void Start(DiscordSocketClient discordSocketClient) =>
        _handler = RenderKeepAliveAsync(discordSocketClient);
    
    private static async Task RenderKeepAliveAsync(DiscordSocketClient discordSocketClient)
    {
        while (await Timer.WaitForNextTickAsync(CancellationTokenSource.Token))
        {
            var response = await HttpClient.GetAsync("https://anivia.onrender.com");
            Console.WriteLine($"Render ping returned {response.StatusCode}");
        }
    }
}