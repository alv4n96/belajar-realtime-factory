using EQFR.Common;

namespace EQFR.Biz.Runtime;

public sealed record Lot(
    string LotId,
    string MaterialType,
    LotStatus Status,
    NodeRef CurrentNode
);

