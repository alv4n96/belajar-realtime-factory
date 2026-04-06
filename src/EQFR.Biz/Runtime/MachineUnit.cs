using EQFR.Common;

namespace EQFR.Biz.Runtime;

public sealed record MachineUnit(
    string Id,
    MachineStatus Status,
    string InputPortId,
    string OutputPortId,
    string? CurrentLotId = null,
    int CurrentStepIndex = 0,
    int TotalSteps = 0,
    DateTimeOffset? StepStartTime = null,
    int CurrentStepDurationMs = 0,
    bool NeedsInput = false,
    bool OutputReady = false
);

