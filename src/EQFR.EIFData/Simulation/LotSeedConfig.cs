namespace EQFR.EIFData.Simulation;

public sealed record LotSeedConfig(
    string LotId,
    string MaterialType,
    string LocationId,
    string? PortId = null
);

