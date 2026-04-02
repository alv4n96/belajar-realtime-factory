using EQFR.Common;

namespace EQFR.EIFData.Layout;

public sealed record LocationConfig(
    string Id,
    string DisplayName,
    string Type,
    Point2D Position,
    IReadOnlyList<PortConfig> Ports
);

