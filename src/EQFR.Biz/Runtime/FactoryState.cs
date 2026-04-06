using EQFR.Common;

namespace EQFR.Biz.Runtime;

public sealed record FactoryState(
    SimulationStatus SimulationStatus,
    TickOptions TickOptions,
    IReadOnlyDictionary<string, LocationNode> Locations,
    IReadOnlyList<RouteEdge> RouteEdges,
    IReadOnlyDictionary<string, TransportUnit> Transports,
    IReadOnlyDictionary<string, MachineUnit> Machines,
    IReadOnlyDictionary<string, Lot> Lots,
    IReadOnlyDictionary<string, TransportTask> Tasks,
    IReadOnlyDictionary<string, RouteReservation> Reservations,
    IReadOnlyList<EventLogItem> RecentEvents
);

