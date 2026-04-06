using EQFR.Biz.Snapshots;

namespace EQFR.UI.ViewModels;

public static class EventLogPanelViewModel
{
    public sealed record Item(string Time, string Message, string? EntityId, string? EventType);

    public static IReadOnlyList<Item> Build(FactorySnapshot? snapshot, int take = 50)
    {
        if (snapshot is null) return Array.Empty<Item>();

        return snapshot.RecentEvents
            .OrderByDescending(e => e.Timestamp)
            .Take(Math.Max(0, take))
            .Select(e => new Item(
                Time: e.Timestamp.ToString("HH:mm:ss"),
                Message: e.Message,
                EntityId: e.EntityId,
                EventType: e.EventType
            ))
            .ToList();
    }
}

