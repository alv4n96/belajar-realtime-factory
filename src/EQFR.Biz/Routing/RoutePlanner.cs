using EQFR.Biz.Runtime;

namespace EQFR.Biz.Routing;

public static class RoutePlanner
{
    public sealed record RoutePlan(IReadOnlyList<string> EdgeIds, IReadOnlyList<NodeRef> Nodes, double TotalCost);

    public static bool TryFindPath(RouteGraph graph, NodeRef start, NodeRef goal, out RoutePlan? plan)
    {
        plan = null;

        if (start.Equals(goal))
        {
            plan = new RoutePlan(Array.Empty<string>(), new[] { start }, 0d);
            return true;
        }

        // Dijkstra (simple + deterministic): ties are resolved by insertion order of edges.
        var distances = new Dictionary<NodeRef, double> { [start] = 0d };
        var prev = new Dictionary<NodeRef, (NodeRef PrevNode, string EdgeId)>();
        var visited = new HashSet<NodeRef>();

        var queue = new PriorityQueue<NodeRef, double>();
        queue.Enqueue(start, 0d);

        while (queue.TryDequeue(out var current, out var currentCost))
        {
            if (!visited.Add(current))
            {
                continue;
            }

            if (current.Equals(goal))
            {
                plan = Reconstruct(start, goal, distances[goal], prev);
                return true;
            }

            foreach (var edge in graph.GetOutgoing(current))
            {
                var next = edge.To;
                if (visited.Contains(next))
                {
                    continue;
                }

                var nextCost = currentCost + edge.Cost;
                if (!distances.TryGetValue(next, out var best) || nextCost < best)
                {
                    distances[next] = nextCost;
                    prev[next] = (current, edge.EdgeId);
                    queue.Enqueue(next, nextCost);
                }
            }
        }

        return false;
    }

    private static RoutePlan Reconstruct(
        NodeRef start,
        NodeRef goal,
        double totalCost,
        Dictionary<NodeRef, (NodeRef PrevNode, string EdgeId)> prev)
    {
        var nodePath = new List<NodeRef> { goal };
        var edgePath = new List<string>();

        var current = goal;
        while (!current.Equals(start))
        {
            var link = prev[current];
            edgePath.Add(link.EdgeId);
            current = link.PrevNode;
            nodePath.Add(current);
        }

        nodePath.Reverse();
        edgePath.Reverse();

        return new RoutePlan(edgePath, nodePath, totalCost);
    }
}

