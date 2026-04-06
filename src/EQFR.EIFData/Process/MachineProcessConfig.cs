namespace EQFR.EIFData.Process;

public sealed record MachineProcessConfig(
    string Id,
    IReadOnlyList<ProcessStepConfig> Steps,
    string InputPortId = "IN",
    string OutputPortId = "OUT"
);

