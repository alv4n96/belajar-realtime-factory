namespace EQFR.Biz.Runtime;

public static class FactoryStateRoutingExtensions
{
    public static IReadOnlyDictionary<string, string> GetEdgeOccupancy(this FactoryState state)
    {
        // EdgeId -> TransportId
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var r in state.Reservations.Values)
        {
            dict[r.EdgeId] = r.TransportId;
        }

        return dict;
    }
}

