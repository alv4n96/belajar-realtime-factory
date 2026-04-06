using EQFR.Common;

namespace EQFR.Biz.Runtime;

public sealed record TransportTask(
    string Id,
    TaskType Type,
    TransportTaskStatus Status,
    TransportTaskPhase Phase,
    string? AssignedTransportId,
    string? LotId,
    NodeRef From,
    NodeRef To
);

