using Victoria.Node;

namespace Anivia;

public class RenderKeepAliveTask : BackgroundService
{
    private readonly LavaNode _lavaNode;
    private readonly HttpClient _httpClient;
    private readonly PeriodicTimer _timer = new(TimeSpan.FromSeconds(59));

    public RenderKeepAliveTask(IHttpClientFactory httpClientFactory, LavaNode lavaNode)
    {
        _lavaNode = lavaNode;
        _httpClient = httpClientFactory.CreateClient();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (await _timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
        {
            // var response = await _httpClient.GetAsync("https://balls-hoth.onrender.com");
            var response2 = await _httpClient.GetAsync("https://anivia-lavalink-server.onrender.com/");
            Console.WriteLine($"Render ping returned {response2.StatusCode}");

            var player = _lavaNode.Players.FirstOrDefault();
            if (player is null)
            {
                return;
            }

            await player.SetVolumeAsync(100);
        }
    }
}