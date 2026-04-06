using EQFR.Biz.Snapshots;

namespace EQFR.UI.ViewModels;

public static class MachinePanelViewModel
{
    public sealed record Item(
        string Id,
        string Status,
        string? CurrentLotId,
        string StepText,
        string FlagsText
    );

    public static IReadOnlyList<Item> Build(FactorySnapshot? snapshot)
    {
        if (snapshot is null) return Array.Empty<Item>();

        return snapshot.Machines
            .OrderBy(m => m.Id, StringComparer.OrdinalIgnoreCase)
            .Select(m =>
            {
                var status = m.Status.ToString();
                var step = m.TotalSteps <= 0 ? "-" : $"{Math.Min(m.CurrentStepIndex + 1, m.TotalSteps)}/{m.TotalSteps}";
                var flags = BuildFlags(m.NeedsInput, m.OutputReady);

                return new Item(
                    Id: m.Id,
                    Status: status,
                    CurrentLotId: m.CurrentLotId,
                    StepText: step,
                    FlagsText: flags
                );
            })
            .ToList();
    }

    private static string BuildFlags(bool needsInput, bool outputReady)
    {
        var flags = new List<string>(2);
        if (needsInput) flags.Add("NeedsInput");
        if (outputReady) flags.Add("OutputReady");
        return flags.Count == 0 ? "-" : string.Join(", ", flags);
    }
}

