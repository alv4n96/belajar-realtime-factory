using EQFR.Biz.Snapshots;

namespace EQFR.UI.Services;

public sealed class FactorySnapshotStore
{
    private readonly object _gate = new();
    private FactorySnapshot? _latest;

    public event Action? Changed;

    public FactorySnapshot? Latest
    {
        get
        {
            lock (_gate)
            {
                return _latest;
            }
        }
    }

    public void Update(FactorySnapshot snapshot)
    {
        lock (_gate)
        {
            _latest = snapshot;
        }

        Changed?.Invoke();
    }
}

