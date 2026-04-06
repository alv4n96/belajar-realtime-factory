namespace EQFR.Biz.Runtime;

public sealed record RouteEdge(
    string Id,
    NodeRef From,
    NodeRef To,
    bool IsBidirectional,
    double? Distance
);

