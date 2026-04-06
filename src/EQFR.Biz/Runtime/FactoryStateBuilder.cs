using EQFR.Biz.Runtime;
using EQFR.Common;
using EQFR.EIFData.Layout;
using EQFR.EIFData.Routes;
using EQFR.IO.Config;

namespace EQFR.Biz.Runtime;

public static class FactoryStateBuilder
{
    public static FactoryState BuildInitialState(ConfigBundle bundle)
    {
        var tickOptions = new TickOptions(
            TickInterval: TimeSpan.FromMilliseconds(bundle.Simulation.TickIntervalMs),
            MaxRecentEvents: bundle.Simulation.MaxRecentEvents
        );

        var locations = BuildLocations(bundle.Layout);
        var edges = BuildEdges(bundle.Routes);
        var machines = BuildMachines(bundle.Process);
        var transports = BuildTransports(bundle.Transport);
        var lots = BuildLots(bundle.Simulation, locations);

        return new FactoryState(
            SimulationStatus: SimulationStatus.Stopped,
            TickOptions: tickOptions,
            Locations: locations,
            RouteEdges: edges,
            Transports: transports,
            Machines: machines,
            Lots: lots,
            Tasks: new Dictionary<string, TransportTask>(StringComparer.OrdinalIgnoreCase),
            Reservations: new Dictionary<string, RouteReservation>(StringComparer.OrdinalIgnoreCase),
            RecentEvents: new List<EventLogItem>(capacity: Math.Max(0, tickOptions.MaxRecentEvents))
        );
    }

    private static Dictionary<string, LocationNode> BuildLocations(FactoryLayoutConfig layout)
    {
        var dict = new Dictionary<string, LocationNode>(StringComparer.OrdinalIgnoreCase);

        foreach (var loc in layout.Locations.OrderBy(l => l.Id, StringComparer.OrdinalIgnoreCase))
        {
            var ports = new Dictionary<string, PortNode>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in loc.Ports.OrderBy(p => p.Id, StringComparer.OrdinalIgnoreCase))
            {
                ports[p.Id] = new PortNode(p.Id, p.Type);
            }

            dict[loc.Id] = new LocationNode(
                Id: loc.Id,
                DisplayName: loc.DisplayName,
                Type: loc.Type,
                Position: loc.Position,
                Ports: ports
            );
        }

        return dict;
    }

    private static List<RouteEdge> BuildEdges(FactoryRoutesConfig routes)
    {
        var edges = new List<RouteEdge>();

        for (var i = 0; i < routes.Edges.Count; i++)
        {
            var e = routes.Edges[i];
            edges.Add(new RouteEdge(
                Id: $"E{i + 1:D3}",
                From: new NodeRef(e.From.LocationId, e.From.PortId),
                To: new NodeRef(e.To.LocationId, e.To.PortId),
                IsBidirectional: e.IsBidirectional,
                Distance: e.Distance
            ));
        }

        return edges;
    }

    private static Dictionary<string, MachineUnit> BuildMachines(EQFR.EIFData.Process.FactoryProcessConfig process)
    {
        var machines = new Dictionary<string, MachineUnit>(StringComparer.OrdinalIgnoreCase);

        foreach (var m in process.Machines.OrderBy(m => m.Id, StringComparer.OrdinalIgnoreCase))
        {
            machines[m.Id] = new MachineUnit(
                Id: m.Id,
                Status: MachineStatus.Idle,
                InputPortId: m.InputPortId,
                OutputPortId: m.OutputPortId,
                CurrentLotId: null,
                CurrentStepIndex: 0,
                TotalSteps: m.Steps.Count,
                StepStartTime: null,
                CurrentStepDurationMs: 0,
                NeedsInput: false,
                OutputReady: false
            );
        }

        return machines;
    }

    private static Dictionary<string, TransportUnit> BuildTransports(EQFR.EIFData.Transport.FactoryTransportConfig transport)
    {
        var transports = new Dictionary<string, TransportUnit>(StringComparer.OrdinalIgnoreCase);

        foreach (var t in transport.Transports.OrderBy(t => t.Id, StringComparer.OrdinalIgnoreCase))
        {
            var home = new NodeRef(t.Start.LocationId, t.Start.PortId);
            transports[t.Id] = new TransportUnit(
                Id: t.Id,
                Status: TransportStatus.Idle,
                CurrentNode: home,
                HomeNode: home,
                CurrentTaskId: null,
                CurrentLotId: null
            );
        }

        return transports;
    }

    private static Dictionary<string, Lot> BuildLots(
        EQFR.EIFData.Simulation.FactorySimulationConfig simulation,
        Dictionary<string, LocationNode> locations)
    {
        var lots = new Dictionary<string, Lot>(StringComparer.OrdinalIgnoreCase);

        if (simulation.InitialLots is null) return lots;

        foreach (var l in simulation.InitialLots.OrderBy(l => l.LotId, StringComparer.OrdinalIgnoreCase))
        {
            var portId = l.PortId;
            if (string.IsNullOrWhiteSpace(portId))
            {
                // Best-effort default to first port for the location. Validator should prevent bad cases.
                portId = locations[l.LocationId].Ports.Keys.First();
            }

            lots[l.LotId] = new Lot(
                LotId: l.LotId,
                MaterialType: l.MaterialType,
                Status: LotStatus.Available,
                CurrentNode: new NodeRef(l.LocationId, portId!)
            );
        }

        return lots;
    }
}

