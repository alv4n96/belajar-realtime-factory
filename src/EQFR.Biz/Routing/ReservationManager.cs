using EQFR.Biz.Runtime;

namespace EQFR.Biz.Routing;

public sealed class ReservationManager
{
    private readonly object _gate = new();
    private readonly Dictionary<string, RouteReservation> _byEdgeId;

    public ReservationManager()
    {
        _byEdgeId = new Dictionary<string, RouteReservation>(StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyDictionary<string, RouteReservation> Snapshot()
    {
        lock (_gate)
        {
            return new Dictionary<string, RouteReservation>(_byEdgeId, StringComparer.OrdinalIgnoreCase);
        }
    }

    public bool IsReserved(string edgeId)
    {
        if (string.IsNullOrWhiteSpace(edgeId)) return false;
        lock (_gate) return _byEdgeId.ContainsKey(edgeId);
    }

    public bool TryReserve(string edgeId, string transportId, out RouteReservation? reservation, out string? blockingTransportId)
    {
        reservation = null;
        blockingTransportId = null;

        if (string.IsNullOrWhiteSpace(edgeId) || string.IsNullOrWhiteSpace(transportId))
        {
            return false;
        }

        lock (_gate)
        {
            if (_byEdgeId.TryGetValue(edgeId, out var existing))
            {
                if (string.Equals(existing.TransportId, transportId, StringComparison.OrdinalIgnoreCase))
                {
                    reservation = existing;
                    return true;
                }

                blockingTransportId = existing.TransportId;
                return false;
            }

            var created = new RouteReservation(edgeId, transportId, DateTimeOffset.UtcNow);
            _byEdgeId[edgeId] = created;
            reservation = created;
            return true;
        }
    }

    public bool Release(string edgeId, string transportId)
    {
        if (string.IsNullOrWhiteSpace(edgeId) || string.IsNullOrWhiteSpace(transportId))
        {
            return false;
        }

        lock (_gate)
        {
            if (!_byEdgeId.TryGetValue(edgeId, out var existing))
            {
                return false;
            }

            if (!string.Equals(existing.TransportId, transportId, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return _byEdgeId.Remove(edgeId);
        }
    }

    public int ReleaseAllForTransport(string transportId)
    {
        if (string.IsNullOrWhiteSpace(transportId)) return 0;

        lock (_gate)
        {
            var toRemove = _byEdgeId
                .Where(kvp => string.Equals(kvp.Value.TransportId, transportId, StringComparison.OrdinalIgnoreCase))
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var edgeId in toRemove)
            {
                _byEdgeId.Remove(edgeId);
            }

            return toRemove.Count;
        }
    }
}

