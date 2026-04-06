using EQFR.Common;

namespace EQFR.Biz.Runtime;

public sealed record TransportTask(
    string Id,
    TaskType Type,
    TaskStatus Status,
    string? AssignedTransportId,
    string? LotId,
    NodeRef From,
    NodeRef To
);

