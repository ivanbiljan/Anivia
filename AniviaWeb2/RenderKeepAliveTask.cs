namespace Anivia;

public class RenderKeepAliveTask : BackgroundService
{
    private readonly HttpClient _httpClient;
    private readonly PeriodicTimer _timer = new(TimeSpan.FromSeconds(59));

    public RenderKeepAliveTask(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (await _timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
        {
            var response = await _httpClient.GetAsync("https://balls-hoth.onrender.com");
            Console.WriteLine($"Render ping returned {response.StatusCode}");
        }
    }
}