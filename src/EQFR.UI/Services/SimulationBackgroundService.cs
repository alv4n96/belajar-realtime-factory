using EQFR.Biz.Dispatching;
using EQFR.Biz.Machines;
using EQFR.Biz.Routing;
using EQFR.Biz.Runtime;
using EQFR.Biz.Simulation;
using EQFR.Biz.Snapshots;
using EQFR.Biz.Transport;
using EQFR.IO.Config;
using EQFR.UI.Realtime;
using Microsoft.AspNetCore.SignalR;

namespace EQFR.UI.Services;

public sealed class SimulationBackgroundService(
    ILogger<SimulationBackgroundService> logger,
    IHubContext<FactoryHub> hubContext) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("EQFR simulation background service started.");

        var configDir = FindConfigDirectory(AppContext.BaseDirectory);
        if (configDir is null)
        {
            logger.LogError("Config directory not found. Expected a 'config' folder in app directory or parent directories.");
            return;
        }

        var loader = new JsonConfigLoader();
        var bundleResult = loader.LoadFromDirectory(configDir);
        if (!bundleResult.IsSuccess || bundleResult.Value is null)
        {
            logger.LogError("Failed to load config from '{ConfigDir}': {Error}", configDir, bundleResult.Error?.Message);
            return;
        }

        var state = FactoryStateBuilder.BuildInitialState(bundleResult.Value);

        var reservations = new ReservationManager();
        var transportEngine = new TransportEngine(reservations);
        var machineEngine = new RollPressMachineEngine(bundleResult.Value.Process);
        var dispatcher = new Dispatcher();
        var orchestrator = new SimulationOrchestrator(machineEngine, dispatcher, transportEngine);

        state = state with { SimulationStatus = EQFR.Common.SimulationStatus.Running };

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTimeOffset.UtcNow;

            orchestrator.Tick(state, now);

            var snapshot = SnapshotBuilder.Build(state, now);
            await hubContext.Clients.All.SendAsync("factorySnapshot", snapshot, cancellationToken: stoppingToken);

            try
            {
                await Task.Delay(state.TickOptions.TickInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // ignore
            }
        }

        logger.LogInformation("EQFR simulation background service stopped.");
    }

    private static string? FindConfigDirectory(string baseDirectory)
    {
        var dir = new DirectoryInfo(baseDirectory);

        for (var i = 0; i < 8 && dir is not null; i++)
        {
            var candidate = Path.Combine(dir.FullName, "config");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        return null;
    }
}

