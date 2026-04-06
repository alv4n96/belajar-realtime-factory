using EQFR.Common;

namespace EQFR.Biz.Runtime;

public sealed record TransportUnit(
    string Id,
    TransportStatus Status,
    NodeRef CurrentNode,
    NodeRef HomeNode,
    string? CurrentTaskId = null,
    string? CurrentLotId = null
);

