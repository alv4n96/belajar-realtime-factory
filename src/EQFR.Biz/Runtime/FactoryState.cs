using EQFR.Common;

namespace EQFR.Biz.Runtime;

public sealed record FactoryState(
    SimulationStatus SimulationStatus,
    TickOptions TickOptions,
    Dictionary<string, LocationNode> Locations,
    List<RouteEdge> RouteEdges,
    Dictionary<string, TransportUnit> Transports,
    Dictionary<string, MachineUnit> Machines,
    Dictionary<string, Lot> Lots,
    Dictionary<string, TransportTask> Tasks,
    Dictionary<string, RouteReservation> Reservations,
    List<EventLogItem> RecentEvents
);

