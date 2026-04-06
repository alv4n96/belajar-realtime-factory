using EQFR.Common;

namespace EQFR.Biz.Runtime;

public sealed record TransportUnit(
    string Id,
    TransportStatus Status,
    NodeRef CurrentNode,
    string? CurrentTaskId = null,
    string? CurrentLotId = null
);

