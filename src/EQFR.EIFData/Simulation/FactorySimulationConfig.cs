namespace EQFR.EIFData.Simulation;

public sealed record FactorySimulationConfig(
    int TickIntervalMs,
    int MaxRecentEvents = 200,
    IReadOnlyList<LotSeedConfig>? InitialLots = null
);

