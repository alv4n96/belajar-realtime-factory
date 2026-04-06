using EQFR.Biz.Snapshots;
using EQFR.Common;

namespace EQFR.UI.ViewModels;

public static class FactoryCanvasViewModel
{
    public sealed record Scene(
        double MinX,
        double MinY,
        double Width,
        double Height,
        IReadOnlyList<Node> Nodes,
        IReadOnlyList<Edge> Edges,
        IReadOnlyList<MachineOverlay> Machines,
        IReadOnlyList<TransportMarker> Transports
    );

    public sealed record Node(
        string Id,
        string DisplayName,
        string Type,
        double X,
        double Y
    );

    public sealed record Edge(
        string Id,
        double X1,
        double Y1,
        double X2,
        double Y2,
        bool IsOccupied
    );

    public sealed record MachineOverlay(
        string Id,
        MachineStatus Status,
        double X,
        double Y
    );

    public sealed record TransportMarker(
        string Id,
        TransportStatus Status,
        double X,
        double Y
    );

    public static Scene? Build(FactorySnapshot? snapshot)
    {
        if (snapshot is null || snapshot.Locations.Count == 0)
        {
            return null;
        }

        var locById = snapshot.Locations.ToDictionary(l => l.Id, StringComparer.OrdinalIgnoreCase);

        var minX = snapshot.Locations.Min(l => l.X);
        var maxX = snapshot.Locations.Max(l => l.X);
        var minY = snapshot.Locations.Min(l => l.Y);
        var maxY = snapshot.Locations.Max(l => l.Y);

        var pad = 1.2;
        minX -= pad;
        maxX += pad;
        minY -= pad;
        maxY += pad;

        var nodes = snapshot.Locations
            .OrderBy(l => l.Id, StringComparer.OrdinalIgnoreCase)
            .Select(l => new Node(l.Id, string.IsNullOrWhiteSpace(l.DisplayName) ? l.Id : l.DisplayName, l.Type, l.X, l.Y))
            .ToList();

        var edges = snapshot.RouteEdges
            .OrderBy(e => e.Id, StringComparer.OrdinalIgnoreCase)
            .Select(e =>
            {
                if (!locById.TryGetValue(e.FromLocationId, out var from) || !locById.TryGetValue(e.ToLocationId, out var to))
                {
                    return null;
                }

                var occupied = snapshot.EdgeOccupancy.ContainsKey(e.Id);
                return new Edge(e.Id, from.X, from.Y, to.X, to.Y, occupied);
            })
            .Where(e => e is not null)
            .Select(e => e!)
            .ToList();

        var machines = snapshot.Machines
            .OrderBy(m => m.Id, StringComparer.OrdinalIgnoreCase)
            .Select(m =>
            {
                if (!locById.TryGetValue(m.Id, out var loc))
                {
                    return null;
                }

                return new MachineOverlay(m.Id, m.Status, loc.X, loc.Y);
            })
            .Where(m => m is not null)
            .Select(m => m!)
            .ToList();

        var transports = snapshot.Transports
            .OrderBy(t => t.Id, StringComparer.OrdinalIgnoreCase)
            .Select(t =>
            {
                if (!locById.TryGetValue(t.LocationId, out var loc))
                {
                    return null;
                }

                var (dx, dy) = OffsetFor(t.Id);
                return new TransportMarker(t.Id, t.Status, loc.X + dx, loc.Y + dy);
            })
            .Where(t => t is not null)
            .Select(t => t!)
            .ToList();

        var width = Math.Max(1, maxX - minX);
        var height = Math.Max(1, maxY - minY);

        return new Scene(minX, minY, width, height, nodes, edges, machines, transports);
    }

    private static (double Dx, double Dy) OffsetFor(string key)
    {
        unchecked
        {
            var hash = 17;
            foreach (var ch in key)
            {
                hash = (hash * 31) + ch;
            }

            var dx = (((hash >> 0) & 0xFF) / 255.0 - 0.5) * 0.36;
            var dy = (((hash >> 8) & 0xFF) / 255.0 - 0.5) * 0.36;
            return (dx, dy);
        }
    }
}
