using EQFR.Common;

namespace EQFR.Biz.Runtime;

public sealed record LocationNode(
    string Id,
    string DisplayName,
    string Type,
    Point2D Position,
    IReadOnlyDictionary<string, PortNode> Ports
);

