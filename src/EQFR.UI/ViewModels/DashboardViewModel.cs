using EQFR.Biz.Snapshots;
using EQFR.Common;
using EQFR.UI.Services;

namespace EQFR.UI.ViewModels;

public sealed class DashboardViewModel : IDisposable
{
    private readonly FactorySnapshotStore _store;
    private readonly SimulationControlService _controls;

    public DashboardViewModel(FactorySnapshotStore store, SimulationControlService controls)
    {
        _store = store;
        _controls = controls;
        _store.Changed += OnStoreChanged;

        Snapshot = _store.Latest;
    }

    public event Action? Changed;

    public FactorySnapshot? Snapshot { get; private set; }

    public DateTimeOffset? LastUpdated => Snapshot?.ServerTime;

    public SimulationStatus SimulationStatus => Snapshot?.SimulationStatus ?? SimulationStatus.Stopped;

    public bool CanStart => SimulationStatus != SimulationStatus.Running;
    public bool CanPause => SimulationStatus == SimulationStatus.Running;
    public bool CanReset => true;

    public Task StartAsync()
    {
        _controls.Start();
        Changed?.Invoke();
        return Task.CompletedTask;
    }

    public Task PauseAsync()
    {
        _controls.Pause();
        Changed?.Invoke();
        return Task.CompletedTask;
    }

    public Task ResetAsync()
    {
        _controls.Reset();
        Changed?.Invoke();
        return Task.CompletedTask;
    }

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

