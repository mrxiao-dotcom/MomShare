using System.Text.Json;
using Microsoft.Extensions.Hosting;

namespace MomShares.Api.Services;

public class RankingsRefreshService : IHostedService, IDisposable
{
    private Timer? _timer;
    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RankingsRefreshService> _logger;

    public RankingsRefreshService(IHttpClientFactory httpFactory, IConfiguration configuration, ILogger<RankingsRefreshService> logger)
    {
        _httpFactory = httpFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Schedule first run at configured hour, then every 24h
        var hour = _configuration.GetValue<int?>("RankingsSettings:AutoRefreshHour") ?? 3;
        var now = DateTime.Now;
        var next = new DateTime(now.Year, now.Month, now.Day, hour, 0, 0);
        if (now >= next) next = next.AddDays(1);
        var due = next - now;
        _timer = new Timer(async _ => await DoRefreshAsync(), null, due, TimeSpan.FromDays(1));

        // Also trigger one immediate refresh in background
        _ = Task.Run(async () =>
        {
            try
            {
                await DoRefreshAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Initial rankings refresh failed");
            }
        }, cancellationToken);

        _logger.LogInformation("[RankingsRefreshService] scheduled first refresh at {next}", next);
        return Task.CompletedTask;
    }

    private async Task DoRefreshAsync()
    {
        var url = _configuration["RankingsSettings:AutoRefreshUrl"];
        if (string.IsNullOrWhiteSpace(url))
        {
            _logger.LogDebug("[RankingsRefreshService] No AutoRefreshUrl configured, skipping refresh");
            return;
        }

        try
        {
            var client = _httpFactory.CreateClient();
            _logger.LogInformation("[RankingsRefreshService] fetching rankings from {url}", url);
            var resp = await client.GetAsync(url);
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("[RankingsRefreshService] remote returned {status}", resp.StatusCode);
                return;
            }
            var content = await resp.Content.ReadAsStringAsync();
            // Validate JSON
            using var doc = JsonDocument.Parse(content);

            // Write to wwwroot/rankings.json
            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "rankings.json");
            await System.IO.File.WriteAllTextAsync(path, JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = true }));
            _logger.LogInformation("[RankingsRefreshService] rankings refreshed and saved to {path}", path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RankingsRefreshService] refresh failed");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}


