using EQFR.Biz.Runtime;
using EQFR.Common;

namespace EQFR.Biz.Dispatching;

public sealed class Dispatcher
{
    public void Tick(FactoryState state, DateTimeOffset now)
    {
        foreach (var machineId in state.Machines.Keys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
        {
            var machine = state.Machines[machineId];

            if (machine.NeedsInput && machine.CurrentLotId is null && !machine.OutputReady)
            {
                TryDispatchInput(state, machine, now);
            }

            if (machine.OutputReady && machine.CurrentLotId is not null)
            {
                TryDispatchOutput(state, machine, now);
            }
        }
    }

    private static void TryDispatchInput(FactoryState state, MachineUnit machine, DateTimeOffset now)
    {
        var lot = FindFirstAvailableLotFromWarehouseOutput(state);
        if (lot is null)
        {
            return;
        }

        if (HasActiveTaskForLot(state, lot.LotId, TaskType.DeliverInputToMachine))
        {
            return;
        }

        var transport = FindFirstIdleTransport(state);
        if (transport is null)
        {
            return;
        }

        var taskId = NextTaskId(state);
        var from = lot.CurrentNode;
        var to = new NodeRef(machine.Id, machine.InputPortId);

        state.Tasks[taskId] = new TransportTask(
            Id: taskId,
            Type: TaskType.DeliverInputToMachine,
            Status: TransportTaskStatus.Pending,
            Phase: TransportTaskPhase.ToPickup,
            AssignedTransportId: transport.Id,
            LotId: lot.LotId,
            From: from,
            To: to
        );

        EventLogWriter.Add(state, now, $"Dispatcher assigned '{transport.Id}' to deliver input lot '{lot.LotId}' -> machine '{machine.Id}'.", machine.Id, "dispatch.input");
    }

    private static void TryDispatchOutput(FactoryState state, MachineUnit machine, DateTimeOffset now)
    {
        if (!state.Lots.TryGetValue(machine.CurrentLotId!, out var lot))
        {
            return;
        }

        if (HasActiveTaskForLot(state, lot.LotId, TaskType.MoveOutputToWarehouse))
        {
            return;
        }

        var transport = FindFirstIdleTransport(state);
        if (transport is null)
        {
            return;
        }

        var warehouseOut = FindFirstWarehouseInputNode(state);
        if (warehouseOut is null)
        {
            return;
        }

        var taskId = NextTaskId(state);
        var from = new NodeRef(machine.Id, machine.OutputPortId);
        var to = warehouseOut.Value;

        state.Tasks[taskId] = new TransportTask(
            Id: taskId,
            Type: TaskType.MoveOutputToWarehouse,
            Status: TransportTaskStatus.Pending,
            Phase: TransportTaskPhase.ToPickup,
            AssignedTransportId: transport.Id,
            LotId: lot.LotId,
            From: from,
            To: to
        );

        EventLogWriter.Add(state, now, $"Dispatcher assigned '{transport.Id}' to move output lot '{lot.LotId}' -> warehouse '{to.LocationId}'.", machine.Id, "dispatch.output");
    }

    private static TransportUnit? FindFirstIdleTransport(FactoryState state)
    {
        foreach (var id in state.Transports.Keys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
        {
            var t = state.Transports[id];
            if (t.Status == TransportStatus.Idle && t.CurrentTaskId is null && t.CurrentLotId is null)
            {
                return t;
            }
        }

        return null;
    }

    private static Lot? FindFirstAvailableLotFromWarehouseOutput(FactoryState state)
    {
        var candidates = state.Lots.Values
            .Where(l => l.Status == LotStatus.Available)
            .OrderBy(l => l.LotId, StringComparer.OrdinalIgnoreCase);

        foreach (var lot in candidates)
        {
            if (!state.Locations.TryGetValue(lot.CurrentNode.LocationId, out var loc))
            {
                continue;
            }

            if (!string.Equals(loc.Type, "Warehouse", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!loc.Ports.TryGetValue(lot.CurrentNode.PortId, out var port))
            {
                continue;
            }

            if (port.Type == PortType.Output)
            {
                return lot;
            }
        }

        return null;
    }

    private static NodeRef? FindFirstWarehouseInputNode(FactoryState state)
    {
        foreach (var locId in state.Locations.Keys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
        {
            var loc = state.Locations[locId];
            if (!string.Equals(loc.Type, "Warehouse", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (var portId in loc.Ports.Keys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                if (loc.Ports[portId].Type == PortType.Input)
                {
                    return new NodeRef(loc.Id, portId);
                }
            }
        }

        return null;
    }

    private static bool HasActiveTaskForLot(FactoryState state, string lotId, TaskType type)
    {
        foreach (var t in state.Tasks.Values)
        {
            if (!string.Equals(t.LotId, lotId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (t.Type != type)
            {
                continue;
            }

            if (t.Status is TransportTaskStatus.Pending or TransportTaskStatus.InProgress)
            {
                return true;
            }
        }

        return false;
    }

    private static string NextTaskId(FactoryState state)
    {
        var max = 0;
        foreach (var id in state.Tasks.Keys)
        {
            if (!id.StartsWith("TASK_", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var suffix = id["TASK_".Length..];
            if (int.TryParse(suffix, out var n) && n > max)
            {
                max = n;
            }
        }

        return $"TASK_{max + 1:0000}";
    }
}

