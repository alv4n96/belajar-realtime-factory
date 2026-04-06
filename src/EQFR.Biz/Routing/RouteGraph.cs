using EQFR.Biz.Runtime;

namespace EQFR.Biz.Routing;

public sealed class RouteGraph
{
    public sealed record DirectedEdge(string EdgeId, NodeRef From, NodeRef To, double Cost);

    private readonly Dictionary<NodeRef, List<DirectedEdge>> _outgoing;

    private RouteGraph(Dictionary<NodeRef, List<DirectedEdge>> outgoing)
    {
        _outgoing = outgoing;
    }

    public static RouteGraph FromEdges(IReadOnlyList<RouteEdge> edges)
    {
        var outgoing = new Dictionary<NodeRef, List<DirectedEdge>>();

        foreach (var e in edges)
        {
            var cost = e.Distance is > 0 ? e.Distance.Value : 1d;

            Add(outgoing, new DirectedEdge(e.Id, e.From, e.To, cost));
            if (e.IsBidirectional)
            {
                Add(outgoing, new DirectedEdge(e.Id, e.To, e.From, cost));
            }
        }

        return new RouteGraph(outgoing);
    }

    public IReadOnlyList<DirectedEdge> GetOutgoing(NodeRef from)
        => _outgoing.TryGetValue(from, out var list) ? list : Array.Empty<DirectedEdge>();

    public IEnumerable<NodeRef> Nodes => _outgoing.Keys;

    private static void Add(Dictionary<NodeRef, List<DirectedEdge>> outgoing, DirectedEdge edge)
    {
        if (!outgoing.TryGetValue(edge.From, out var list))
        {
            list = new List<DirectedEdge>();
            outgoing[edge.From] = list;
        }

        list.Add(edge);
    }
}

