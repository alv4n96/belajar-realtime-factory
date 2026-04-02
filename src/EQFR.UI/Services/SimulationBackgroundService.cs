namespace EQFR.UI.Services;

public sealed class SimulationBackgroundService(ILogger<SimulationBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("EQFR simulation background service started.");

        // Placeholder only: the simulation loop will be implemented in later issues.
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }

        logger.LogInformation("EQFR simulation background service stopped.");
    }
}

