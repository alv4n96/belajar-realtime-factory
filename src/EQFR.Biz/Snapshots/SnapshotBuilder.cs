using EQFR.Biz.Runtime;

namespace EQFR.Biz.Snapshots;

public static class SnapshotBuilder
{
    public static FactorySnapshot Build(FactoryState state, DateTimeOffset now)
    {
        var locations = state.Locations.Values
            .OrderBy(l => l.Id, StringComparer.OrdinalIgnoreCase)
            .Select(l => new LocationSnapshot(
                Id: l.Id,
                DisplayName: l.DisplayName,
                Type: l.Type,
                X: l.Position.X,
                Y: l.Position.Y,
                Ports: l.Ports.Values
                    .OrderBy(p => p.Id, StringComparer.OrdinalIgnoreCase)
                    .Select(p => new PortSnapshot(p.Id, p.Type))
                    .ToList()
            ))
            .ToList();

        var edges = state.RouteEdges
            .OrderBy(e => e.Id, StringComparer.OrdinalIgnoreCase)
            .Select(e => new RouteEdgeSnapshot(
                Id: e.Id,
                FromLocationId: e.From.LocationId,
                FromPortId: e.From.PortId,
                ToLocationId: e.To.LocationId,
                ToPortId: e.To.PortId,
                IsBidirectional: e.IsBidirectional,
                Distance: e.Distance
            ))
            .ToList();

        var machines = state.Machines.Values
            .OrderBy(m => m.Id, StringComparer.OrdinalIgnoreCase)
            .Select(m => new MachineSnapshot(
                Id: m.Id,
                Status: m.Status,
                InputPortId: m.InputPortId,
                OutputPortId: m.OutputPortId,
                CurrentLotId: m.CurrentLotId,
                CurrentStepIndex: m.CurrentStepIndex,
                TotalSteps: m.TotalSteps,
                NeedsInput: m.NeedsInput,
                OutputReady: m.OutputReady
            ))
            .ToList();

        var transports = state.Transports.Values
            .OrderBy(t => t.Id, StringComparer.OrdinalIgnoreCase)
            .Select(t => new TransportSnapshot(
                Id: t.Id,
                Status: t.Status,
                LocationId: t.CurrentNode.LocationId,
                PortId: t.CurrentNode.PortId,
                CurrentTaskId: t.CurrentTaskId,
                CurrentLotId: t.CurrentLotId
            ))
            .ToList();

        var lots = state.Lots.Values
            .OrderBy(l => l.LotId, StringComparer.OrdinalIgnoreCase)
            .Select(l => new LotSnapshot(
                LotId: l.LotId,
                MaterialType: l.MaterialType,
                Status: l.Status,
                LocationId: l.CurrentNode.LocationId,
                PortId: l.CurrentNode.PortId
            ))
            .ToList();

        var events = state.RecentEvents
            .OrderByDescending(e => e.Timestamp)
            .Take(state.TickOptions.MaxRecentEvents)
            .Select(e => new EventSnapshot(e.Timestamp, e.Message, e.EntityId, e.EventType))
            .ToList();

        return new FactorySnapshot(
            ServerTime: now,
            SimulationStatus: state.SimulationStatus,
            TickOptions: state.TickOptions,
            Locations: locations,
            RouteEdges: edges,
            EdgeOccupancy: state.GetEdgeOccupancy(),
            Machines: machines,
            Transports: transports,
            Lots: lots,
            RecentEvents: events
        );
    }
}

