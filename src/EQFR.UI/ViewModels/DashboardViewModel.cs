using EQFR.Biz.Snapshots;
using EQFR.UI.Services;

namespace EQFR.UI.ViewModels;

public sealed class DashboardViewModel : IDisposable
{
    private readonly FactorySnapshotStore _store;

    public DashboardViewModel(FactorySnapshotStore store)
    {
        _store = store;
        _store.Changed += OnStoreChanged;

        Snapshot = _store.Latest;
    }

    public event Action? Changed;

    public FactorySnapshot? Snapshot { get; private set; }

    public DateTimeOffset? LastUpdated => Snapshot?.ServerTime;

    public void Dispose()
    {
        _store.Changed -= OnStoreChanged;
    }

    private void OnStoreChanged()
    {
        Snapshot = _store.Latest;
        Changed?.Invoke();
    }
}

