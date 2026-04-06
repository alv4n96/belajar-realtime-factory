using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using EQFR.Biz.Snapshots;
using EQFR.UI.Realtime;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace EQFR.UI.Services;

public sealed class SimulationBackgroundService(
    ILogger<SimulationBackgroundService> logger,
    IHubContext<FactoryHub> hubContext,
    FactorySnapshotStore snapshotStore,
    IHttpClientFactory httpClientFactory,
    IOptions<MockBackendOptions> options) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        var client = httpClientFactory.CreateClient("MockBackend");
        client.BaseAddress = new Uri(settings.BaseUrl, UriKind.Absolute);

        logger.LogInformation("EQFR UI backend polling service started. Backend: {BaseUrl}", client.BaseAddress);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var response = await client.GetAsync("/api/snapshot", stoppingToken);
                if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(Math.Max(100, settings.PollIntervalMs)), stoppingToken);
                    continue;
                }

                response.EnsureSuccessStatusCode();

                var payload = await response.Content.ReadFromJsonAsync<SnapshotEnvelope>(JsonOptions, stoppingToken);
                if (payload?.Snapshot is not null)
                {
                    snapshotStore.Update(payload.Snapshot);
                    await hubContext.Clients.All.SendAsync("factorySnapshot", payload.Snapshot, cancellationToken: stoppingToken);

                    var nextDelayMs = payload.Snapshot.TickOptions.TickInterval.TotalMilliseconds;
                    var delay = TimeSpan.FromMilliseconds(Math.Max(100, nextDelayMs > 0 ? nextDelayMs : settings.PollIntervalMs));
                    await Task.Delay(delay, stoppingToken);
                    continue;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to poll backend snapshot from {BaseUrl}", client.BaseAddress);
            }

            try
            {
                await Task.Delay(TimeSpan.FromMilliseconds(Math.Max(250, settings.PollIntervalMs)), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        logger.LogInformation("EQFR UI backend polling service stopped.");
    }

    private sealed record SnapshotEnvelope(DateTimeOffset? LastUpdatedUtc, FactorySnapshot? Snapshot);
}
