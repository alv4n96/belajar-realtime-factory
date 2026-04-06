using EQFR.Common;

namespace EQFR.Biz.Snapshots;

public sealed record FactorySnapshot(
    DateTimeOffset ServerTime,
    SimulationStatus SimulationStatus,
    TickOptions TickOptions,
    IReadOnlyList<LocationSnapshot> Locations,
    IReadOnlyList<RouteEdgeSnapshot> RouteEdges,
    IReadOnlyDictionary<string, string> EdgeOccupancy,
    IReadOnlyList<MachineSnapshot> Machines,
    IReadOnlyList<TransportSnapshot> Transports,
    IReadOnlyList<LotSnapshot> Lots,
    IReadOnlyList<EventSnapshot> RecentEvents
);

public sealed record LocationSnapshot(
    string Id,
    string DisplayName,
    string Type,
    double X,
    double Y,
    IReadOnlyList<PortSnapshot> Ports
);

public sealed record PortSnapshot(string Id, PortType Type);

public sealed record RouteEdgeSnapshot(
    string Id,
    string FromLocationId,
    string FromPortId,
    string ToLocationId,
    string ToPortId,
    bool IsBidirectional,
    double? Distance
);

public sealed record MachineSnapshot(
    string Id,
    MachineStatus Status,
    string InputPortId,
    string OutputPortId,
    string? CurrentLotId,
    int CurrentStepIndex,
    int TotalSteps,
    bool NeedsInput,
    bool OutputReady
);

public sealed record TransportSnapshot(
    string Id,
    TransportStatus Status,
    string LocationId,
    string PortId,
    string? CurrentTaskId,
    string? CurrentLotId
);

public sealed record LotSnapshot(
    string LotId,
    string MaterialType,
    LotStatus Status,
    string LocationId,
    string PortId
);

public sealed record EventSnapshot(
    DateTimeOffset Timestamp,
    string Message,
    string? EntityId,
    string? EventType
);

