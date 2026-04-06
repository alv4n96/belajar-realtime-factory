using EQFR.Biz.Snapshots;

namespace EQFR.UI.ViewModels;

public static class TransportPanelViewModel
{
    public sealed record Item(
        string Id,
        string Status,
        string LocationId,
        string PortId,
        string? CurrentTaskId,
        string? CurrentLotId
    );

    public static IReadOnlyList<Item> Build(FactorySnapshot? snapshot)
    {
        if (snapshot is null) return Array.Empty<Item>();

        return snapshot.Transports
            .OrderBy(t => t.Id, StringComparer.OrdinalIgnoreCase)
            .Select(t => new Item(
                Id: t.Id,
                Status: t.Status.ToString(),
                LocationId: t.LocationId,
                PortId: t.PortId,
                CurrentTaskId: t.CurrentTaskId,
                CurrentLotId: t.CurrentLotId
            ))
            .ToList();
    }
}

