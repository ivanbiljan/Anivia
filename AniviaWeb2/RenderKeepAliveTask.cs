using System.Runtime.CompilerServices;
using Discord;
using Discord.WebSocket;

namespace Anivia;

public class RenderKeepAliveTask : BackgroundService
{
    private readonly PeriodicTimer _timer = new(TimeSpan.FromSeconds(59));
    private readonly HttpClient _httpClient;

    public RenderKeepAliveTask(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (await _timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
        {
            var response = await _httpClient.GetAsync("https://anivia.onrender.com");
            Console.WriteLine($"Render ping returned {response.StatusCode}");
        }
    }
}