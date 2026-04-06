using EQFR.Biz.Routing;
using EQFR.Biz.Runtime;
using EQFR.Common;

namespace EQFR.Biz.Transport;

public sealed class TransportEngine
{
    private readonly ReservationManager _reservations;

    public TransportEngine(ReservationManager reservations)
    {
        _reservations = reservations;
    }

    public void Tick(FactoryState state, DateTimeOffset now)
    {
        var graph = RouteGraph.FromEdges(state.RouteEdges);
        var releases = new List<(string EdgeId, string TransportId)>();

        foreach (var transportId in state.Transports.Keys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
        {
            var transport = state.Transports[transportId];

            // Attach a pending assigned task if we don't have one.
            transport = EnsureCurrentTask(state, transport, now);

            if (transport.CurrentTaskId is null)
            {
                state.Transports[transport.Id] = transport with { Status = TransportStatus.Idle };
                continue;
            }

            if (!state.Tasks.TryGetValue(transport.CurrentTaskId, out var task))
            {
                state.Transports[transport.Id] = transport with { CurrentTaskId = null, Status = TransportStatus.Idle };
                AddEvent(state, now, $"Transport '{transport.Id}' cleared missing task '{transport.CurrentTaskId}'.", transport.Id, "transport.task.missing");
                continue;
            }

            (transport, task) = RunTask(state, graph, transport, task, now, releases);
            state.Transports[transport.Id] = transport;
            state.Tasks[task.Id] = task;
        }

        // Release edges reserved for this tick.
        foreach (var (edgeId, transportId) in releases)
        {
            _reservations.Release(edgeId, transportId);
        }

        // Keep a best-effort snapshot of reservations in runtime state.
        state.Reservations.Clear();
        foreach (var kvp in _reservations.Snapshot())
        {
            state.Reservations[kvp.Key] = kvp.Value;
        }
    }

    private static TransportUnit EnsureCurrentTask(FactoryState state, TransportUnit transport, DateTimeOffset now)
    {
        if (transport.CurrentTaskId is not null)
        {
            return transport;
        }

        var nextTask = state.Tasks.Values
            .Where(t =>
                t.Status == TransportTaskStatus.Pending &&
                t.AssignedTransportId is not null &&
                string.Equals(t.AssignedTransportId, transport.Id, StringComparison.OrdinalIgnoreCase))
            .OrderBy(t => t.Id, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        if (nextTask is null)
        {
            return transport;
        }

        var started = nextTask with { Status = TransportTaskStatus.InProgress, Phase = TransportTaskPhase.ToPickup };
        state.Tasks[started.Id] = started;

        AddEvent(state, now, $"Transport '{transport.Id}' started task '{started.Id}'.", transport.Id, "transport.task.start");
        return transport with { CurrentTaskId = started.Id };
    }

    private (TransportUnit Transport, TransportTask Task) RunTask(
        FactoryState state,
        RouteGraph graph,
        TransportUnit transport,
        TransportTask task,
        DateTimeOffset now,
        List<(string EdgeId, string TransportId)> releases)
    {
        if (task.Status is TransportTaskStatus.Completed or TransportTaskStatus.Failed)
        {
            return (
                transport with { CurrentTaskId = null, Status = TransportStatus.Idle },
                task
            );
        }

        if (task.Phase == TransportTaskPhase.ToPickup && transport.CurrentNode.Equals(task.From))
        {
            task = task with { Phase = TransportTaskPhase.Pickup };
        }

        if (task.Phase == TransportTaskPhase.ToDropoff && transport.CurrentNode.Equals(task.To))
        {
            task = task with { Phase = TransportTaskPhase.Dropoff };
        }

        if (task.Phase == TransportTaskPhase.ReturnHome && transport.CurrentNode.Equals(transport.HomeNode))
        {
            task = task with { Phase = TransportTaskPhase.Done, Status = TransportTaskStatus.Completed };
            AddEvent(state, now, $"Transport '{transport.Id}' completed task '{task.Id}'.", transport.Id, "transport.task.done");
            return (transport with { CurrentTaskId = null, Status = TransportStatus.Idle }, task);
        }

        switch (task.Phase)
        {
            case TransportTaskPhase.ToPickup:
                return MoveTowards(state, graph, transport, task, now, task.From, releases, TransportStatus.Moving);

            case TransportTaskPhase.Pickup:
                return DoPickup(state, transport, task, now);

            case TransportTaskPhase.ToDropoff:
                return MoveTowards(state, graph, transport, task, now, task.To, releases, TransportStatus.Moving);

            case TransportTaskPhase.Dropoff:
                return DoDropoff(state, transport, task, now);

            case TransportTaskPhase.ReturnHome:
                return MoveTowards(state, graph, transport, task, now, transport.HomeNode, releases, TransportStatus.Returning);

            case TransportTaskPhase.Done:
                return (transport with { CurrentTaskId = null, Status = TransportStatus.Idle }, task with { Status = TransportTaskStatus.Completed });

            default:
                return (transport, task);
        }
    }

    private static (TransportUnit Transport, TransportTask Task) DoPickup(FactoryState state, TransportUnit transport, TransportTask task, DateTimeOffset now)
    {
        if (transport.CurrentLotId is not null)
        {
            // Already carrying something; move on to dropoff to avoid deadlock.
            return (transport, task with { Phase = TransportTaskPhase.ToDropoff });
        }

        if (task.LotId is null || !state.Lots.TryGetValue(task.LotId, out var lot))
        {
            AddEvent(state, now, $"Transport '{transport.Id}' failed pickup: lot missing for task '{task.Id}'.", transport.Id, "transport.pickup.failed");
            return (transport with { Status = TransportStatus.Error }, task with { Status = TransportTaskStatus.Failed, Phase = TransportTaskPhase.Done });
        }

        if (!lot.CurrentNode.Equals(task.From))
        {
            AddEvent(state, now, $"Transport '{transport.Id}' failed pickup: lot '{lot.LotId}' not at pickup node.", transport.Id, "transport.pickup.mismatch");
            return (transport with { Status = TransportStatus.Error }, task with { Status = TransportTaskStatus.Failed, Phase = TransportTaskPhase.Done });
        }

        var pickedLot = lot with { Status = LotStatus.InTransit };
        state.Lots[pickedLot.LotId] = pickedLot;

        AddEvent(state, now, $"Transport '{transport.Id}' picked lot '{pickedLot.LotId}'.", transport.Id, "transport.pickup");
        return (transport with { CurrentLotId = pickedLot.LotId, Status = TransportStatus.Loading }, task with { Phase = TransportTaskPhase.ToDropoff });
    }

    private static (TransportUnit Transport, TransportTask Task) DoDropoff(FactoryState state, TransportUnit transport, TransportTask task, DateTimeOffset now)
    {
        if (transport.CurrentLotId is null)
        {
            AddEvent(state, now, $"Transport '{transport.Id}' dropoff skipped: no lot.", transport.Id, "transport.dropoff.empty");
            return (transport, task with { Phase = TransportTaskPhase.ReturnHome });
        }

        if (!state.Lots.TryGetValue(transport.CurrentLotId, out var lot))
        {
            AddEvent(state, now, $"Transport '{transport.Id}' dropoff failed: lot '{transport.CurrentLotId}' missing.", transport.Id, "transport.dropoff.failed");
            return (transport with { Status = TransportStatus.Error }, task with { Status = TransportTaskStatus.Failed, Phase = TransportTaskPhase.Done });
        }

        var dropped = lot with
        {
            Status = LotStatus.Available,
            CurrentNode = task.To
        };
        state.Lots[dropped.LotId] = dropped;

        AddEvent(state, now, $"Transport '{transport.Id}' dropped lot '{dropped.LotId}'.", transport.Id, "transport.dropoff");
        return (transport with { CurrentLotId = null, Status = TransportStatus.Unloading }, task with { Phase = TransportTaskPhase.ReturnHome });
    }

    private (TransportUnit Transport, TransportTask Task) MoveTowards(
        FactoryState state,
        RouteGraph graph,
        TransportUnit transport,
        TransportTask task,
        DateTimeOffset now,
        NodeRef target,
        List<(string EdgeId, string TransportId)> releases,
        TransportStatus movingStatus)
    {
        if (transport.CurrentNode.Equals(target))
        {
            var nextPhase = task.Phase switch
            {
                TransportTaskPhase.ToPickup => TransportTaskPhase.Pickup,
                TransportTaskPhase.ToDropoff => TransportTaskPhase.Dropoff,
                TransportTaskPhase.ReturnHome => TransportTaskPhase.Done,
                _ => task.Phase
            };

            return (transport with { Status = TransportStatus.Waiting }, task with { Phase = nextPhase });
        }

        if (!RoutePlanner.TryFindPath(graph, transport.CurrentNode, target, out var plan) || plan is null || plan.EdgeIds.Count == 0)
        {
            AddEvent(state, now, $"Transport '{transport.Id}' has no route from '{transport.CurrentNode}' to '{target}'.", transport.Id, "transport.route.not_found");
            return (transport with { Status = TransportStatus.Error }, task with { Status = TransportTaskStatus.Failed, Phase = TransportTaskPhase.Done });
        }

        var nextEdgeId = plan.EdgeIds[0];
        var nextNode = plan.Nodes[1];

        if (!_reservations.TryReserve(nextEdgeId, transport.Id, out var res, out var blocking))
        {
            var phase = task.Phase == TransportTaskPhase.ReturnHome ? "returning" : "moving";
            AddEvent(state, now, $"Transport '{transport.Id}' waiting ({phase}): edge '{nextEdgeId}' blocked by '{blocking}'.", transport.Id, "transport.wait");
            return (transport with { Status = TransportStatus.Waiting }, task);
        }

        // Mark as reserved in runtime state snapshot for this tick.
        if (res is not null)
        {
            state.Reservations[nextEdgeId] = res;
        }

        // Move one hop per tick.
        transport = transport with { CurrentNode = nextNode, Status = movingStatus };

        if (transport.CurrentLotId is not null && state.Lots.TryGetValue(transport.CurrentLotId, out var lot))
        {
            state.Lots[lot.LotId] = lot with { CurrentNode = nextNode, Status = LotStatus.InTransit };
        }

        releases.Add((nextEdgeId, transport.Id));
        return (transport, task);
    }

    private static void AddEvent(FactoryState state, DateTimeOffset now, string message, string? entityId, string eventType)
    {
        state.RecentEvents.Add(new EventLogItem(now, message, entityId, eventType));

        var max = state.TickOptions.MaxRecentEvents;
        while (state.RecentEvents.Count > max)
        {
            state.RecentEvents.RemoveAt(0);
        }
    }
}
