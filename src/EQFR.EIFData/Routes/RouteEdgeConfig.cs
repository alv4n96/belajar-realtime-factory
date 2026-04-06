namespace EQFR.EIFData.Routes;

public sealed record RouteEdgeConfig(
    NodeRefConfig From,
    NodeRefConfig To,
    bool IsBidirectional = true,
    double? Distance = null
);

