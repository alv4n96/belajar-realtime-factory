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
    IHubContext<FactoryHub> hubContext,
    FactorySnapshotStore snapshotStore,
    SimulationControlService controls) : BackgroundService
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

        var bundle = bundleResult.Value;
        var (state, orchestrator, reservations) = BuildRuntime(bundle);

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTimeOffset.UtcNow;

            if (controls.ConsumeResetRequested())
            {
                (state, orchestrator, reservations) = BuildRuntime(bundle);
            }

            var desired = controls.DesiredStatus;
            if (state.SimulationStatus != desired)
            {
                state = state with { SimulationStatus = desired };
            }

            if (state.SimulationStatus == EQFR.Common.SimulationStatus.Running)
            {
                orchestrator.Tick(state, now);
            }

            var snapshot = SnapshotBuilder.Build(state, now);
            snapshotStore.Update(snapshot);
            await hubContext.Clients.All.SendAsync("factorySnapshot", snapshot, cancellationToken: stoppingToken);

            try
            {
                var delay = state.SimulationStatus == EQFR.Common.SimulationStatus.Running
                    ? state.TickOptions.TickInterval
                    : TimeSpan.FromMilliseconds(Math.Max(250, state.TickOptions.TickInterval.TotalMilliseconds));

                await Task.Delay(delay, stoppingToken);
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

    private static (FactoryState State, SimulationOrchestrator Orchestrator, ReservationManager Reservations) BuildRuntime(ConfigBundle bundle)
    {
        var state = FactoryStateBuilder.BuildInitialState(bundle);

        var reservations = new ReservationManager();
        var transportEngine = new TransportEngine(reservations);
        var machineEngine = new RollPressMachineEngine(bundle.Process);
        var dispatcher = new Dispatcher();
        var orchestrator = new SimulationOrchestrator(machineEngine, dispatcher, transportEngine);

        state = state with { SimulationStatus = EQFR.Common.SimulationStatus.Stopped };
        return (state, orchestrator, reservations);
    }
}

