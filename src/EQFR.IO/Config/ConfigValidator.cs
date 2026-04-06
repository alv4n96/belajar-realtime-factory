using EQFR.EIFData.Layout;
using EQFR.EIFData.Routes;

namespace EQFR.IO.Config;

public static class ConfigValidator
{
    public static ConfigValidationResult Validate(ConfigBundle bundle)
    {
        var errors = new List<ConfigValidationError>();

        ValidateLayout(bundle.Layout, errors);
        ValidateRoutes(bundle.Layout, bundle.Routes, errors);
        ValidateProcess(bundle.Process, errors);
        ValidateSimulation(bundle.Layout, bundle.Simulation, errors);
        ValidateTransport(bundle.Layout, bundle.Transport, errors);

        return errors.Count == 0 ? ConfigValidationResult.Ok() : new ConfigValidationResult(errors);
    }

    private static void ValidateLayout(FactoryLayoutConfig layout, List<ConfigValidationError> errors)
    {
        if (layout.Locations is null || layout.Locations.Count == 0)
        {
            errors.Add(new ConfigValidationError("layout.locations.empty", "Layout must define at least one location."));
            return;
        }

        var locationIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var loc in layout.Locations)
        {
            if (string.IsNullOrWhiteSpace(loc.Id))
            {
                errors.Add(new ConfigValidationError("layout.location.id.empty", "A location has an empty id."));
                continue;
            }

            if (!locationIds.Add(loc.Id))
            {
                errors.Add(new ConfigValidationError("layout.location.id.duplicate", $"Duplicate location id: '{loc.Id}'."));
            }

            if (loc.Ports is null || loc.Ports.Count == 0)
            {
                errors.Add(new ConfigValidationError("layout.location.ports.empty", $"Location '{loc.Id}' must define at least one port."));
                continue;
            }

            var portIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in loc.Ports)
            {
                if (string.IsNullOrWhiteSpace(p.Id))
                {
                    errors.Add(new ConfigValidationError("layout.port.id.empty", $"Location '{loc.Id}' has a port with empty id."));
                    continue;
                }

                if (!portIds.Add(p.Id))
                {
                    errors.Add(new ConfigValidationError("layout.port.id.duplicate", $"Location '{loc.Id}' has duplicate port id '{p.Id}'."));
                }
            }
        }
    }

    private static void ValidateRoutes(FactoryLayoutConfig layout, FactoryRoutesConfig routes, List<ConfigValidationError> errors)
    {
        if (routes.Edges is null || routes.Edges.Count == 0)
        {
            errors.Add(new ConfigValidationError("routes.edges.empty", "Routes must define at least one edge."));
            return;
        }

        var locationById = layout.Locations.ToDictionary(l => l.Id, StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < routes.Edges.Count; i++)
        {
            var e = routes.Edges[i];
            ValidateNodeRef($"routes.edges[{i}].from", e.From, locationById, errors);
            ValidateNodeRef($"routes.edges[{i}].to", e.To, locationById, errors);

            if (e.Distance is <= 0)
            {
                errors.Add(new ConfigValidationError("routes.edge.distance.invalid", $"routes.edges[{i}].distance must be > 0 when specified."));
            }
        }
    }

    private static void ValidateProcess(EQFR.EIFData.Process.FactoryProcessConfig process, List<ConfigValidationError> errors)
    {
        if (process.Machines is null || process.Machines.Count == 0)
        {
            errors.Add(new ConfigValidationError("process.machines.empty", "Process must define at least one machine config."));
            return;
        }

        var machineIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var m in process.Machines)
        {
            if (string.IsNullOrWhiteSpace(m.Id))
            {
                errors.Add(new ConfigValidationError("process.machine.id.empty", "A process machine has an empty id."));
                continue;
            }

            if (!machineIds.Add(m.Id))
            {
                errors.Add(new ConfigValidationError("process.machine.id.duplicate", $"Duplicate process machine id '{m.Id}'."));
            }

            if (m.Steps is null || m.Steps.Count == 0)
            {
                errors.Add(new ConfigValidationError("process.machine.steps.empty", $"Machine '{m.Id}' must have at least one step."));
                continue;
            }

            for (var si = 0; si < m.Steps.Count; si++)
            {
                var s = m.Steps[si];
                if (string.IsNullOrWhiteSpace(s.Name))
                {
                    errors.Add(new ConfigValidationError("process.step.name.empty", $"Machine '{m.Id}' has a step[{si}] with empty name."));
                }

                if (s.DurationMs <= 0)
                {
                    errors.Add(new ConfigValidationError("process.step.duration.invalid", $"Machine '{m.Id}' step '{s.Name}' durationMs must be > 0."));
                }
            }
        }
    }

    private static void ValidateSimulation(FactoryLayoutConfig layout, EQFR.EIFData.Simulation.FactorySimulationConfig simulation, List<ConfigValidationError> errors)
    {
        if (simulation.TickIntervalMs <= 0)
        {
            errors.Add(new ConfigValidationError("sim.tick.invalid", "simulation.tickIntervalMs must be > 0."));
        }

        if (simulation.MaxRecentEvents <= 0)
        {
            errors.Add(new ConfigValidationError("sim.events.invalid", "simulation.maxRecentEvents must be > 0."));
        }

        if (simulation.InitialLots is null) return;

        var locationById = layout.Locations.ToDictionary(l => l.Id, StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < simulation.InitialLots.Count; i++)
        {
            var lot = simulation.InitialLots[i];
            if (string.IsNullOrWhiteSpace(lot.LotId))
            {
                errors.Add(new ConfigValidationError("sim.lot.id.empty", $"initialLots[{i}].lotId is empty."));
            }

            if (string.IsNullOrWhiteSpace(lot.MaterialType))
            {
                errors.Add(new ConfigValidationError("sim.lot.material.empty", $"initialLots[{i}].materialType is empty."));
            }

            if (string.IsNullOrWhiteSpace(lot.LocationId))
            {
                errors.Add(new ConfigValidationError("sim.lot.location.empty", $"initialLots[{i}].locationId is empty."));
                continue;
            }

            if (!locationById.TryGetValue(lot.LocationId, out var loc))
            {
                errors.Add(new ConfigValidationError("sim.lot.location.invalid", $"initialLots[{i}].locationId '{lot.LocationId}' not found in layout."));
                continue;
            }

            if (!string.IsNullOrWhiteSpace(lot.PortId))
            {
                var portOk = loc.Ports.Any(p => string.Equals(p.Id, lot.PortId, StringComparison.OrdinalIgnoreCase));
                if (!portOk)
                {
                    errors.Add(new ConfigValidationError("sim.lot.port.invalid", $"initialLots[{i}].portId '{lot.PortId}' not found on location '{loc.Id}'."));
                }
            }
        }
    }

    private static void ValidateTransport(FactoryLayoutConfig layout, EQFR.EIFData.Transport.FactoryTransportConfig transport, List<ConfigValidationError> errors)
    {
        if (transport.Transports is null || transport.Transports.Count == 0)
        {
            errors.Add(new ConfigValidationError("transport.units.empty", "Transport config must define at least one transport unit."));
            return;
        }

        var locationById = layout.Locations.ToDictionary(l => l.Id, StringComparer.OrdinalIgnoreCase);
        var transportIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < transport.Transports.Count; i++)
        {
            var t = transport.Transports[i];
            if (string.IsNullOrWhiteSpace(t.Id))
            {
                errors.Add(new ConfigValidationError("transport.id.empty", $"transports[{i}].id is empty."));
                continue;
            }

            if (!transportIds.Add(t.Id))
            {
                errors.Add(new ConfigValidationError("transport.id.duplicate", $"Duplicate transport id '{t.Id}'."));
            }

            if (t.Start is null)
            {
                errors.Add(new ConfigValidationError("transport.start.missing", $"Transport '{t.Id}' is missing start config."));
                continue;
            }

            if (string.IsNullOrWhiteSpace(t.Start.LocationId) || string.IsNullOrWhiteSpace(t.Start.PortId))
            {
                errors.Add(new ConfigValidationError("transport.start.invalid", $"Transport '{t.Id}' start must include locationId and portId."));
                continue;
            }

            if (!locationById.TryGetValue(t.Start.LocationId, out var loc))
            {
                errors.Add(new ConfigValidationError("transport.start.location.invalid", $"Transport '{t.Id}' start location '{t.Start.LocationId}' not found in layout."));
                continue;
            }

            var portOk = loc.Ports.Any(p => string.Equals(p.Id, t.Start.PortId, StringComparison.OrdinalIgnoreCase));
            if (!portOk)
            {
                errors.Add(new ConfigValidationError("transport.start.port.invalid", $"Transport '{t.Id}' start port '{t.Start.PortId}' not found on location '{loc.Id}'."));
            }
        }
    }

    private static void ValidateNodeRef(
        string path,
        NodeRefConfig nodeRef,
        IReadOnlyDictionary<string, LocationConfig> locationById,
        List<ConfigValidationError> errors)
    {
        if (nodeRef is null)
        {
            errors.Add(new ConfigValidationError("routes.node_ref.null", $"{path} is null."));
            return;
        }

        if (string.IsNullOrWhiteSpace(nodeRef.LocationId) || string.IsNullOrWhiteSpace(nodeRef.PortId))
        {
            errors.Add(new ConfigValidationError("routes.node_ref.invalid", $"{path} must include locationId and portId."));
            return;
        }

        if (!locationById.TryGetValue(nodeRef.LocationId, out var loc))
        {
            errors.Add(new ConfigValidationError("routes.node_ref.location.invalid", $"{path}.locationId '{nodeRef.LocationId}' not found in layout."));
            return;
        }

        var portOk = loc.Ports.Any(p => string.Equals(p.Id, nodeRef.PortId, StringComparison.OrdinalIgnoreCase));
        if (!portOk)
        {
            errors.Add(new ConfigValidationError("routes.node_ref.port.invalid", $"{path}.portId '{nodeRef.PortId}' not found on location '{loc.Id}'."));
        }
    }
}

