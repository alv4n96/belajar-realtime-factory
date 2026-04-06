using EQFR.Common;

namespace EQFR.UI.Services;

public sealed class SimulationControlService
{
    private readonly object _gate = new();
    private SimulationStatus _desiredStatus = SimulationStatus.Stopped;
    private bool _resetRequested;

    public SimulationStatus DesiredStatus
    {
        get
        {
            lock (_gate)
            {
                return _desiredStatus;
            }
        }
    }

    public void Start()
    {
        lock (_gate)
        {
            _desiredStatus = SimulationStatus.Running;
        }
    }

    public void Pause()
    {
        lock (_gate)
        {
            _desiredStatus = SimulationStatus.Paused;
        }
    }

    public void Stop()
    {
        lock (_gate)
        {
            _desiredStatus = SimulationStatus.Stopped;
        }
    }

    public void Reset()
    {
        lock (_gate)
        {
            _resetRequested = true;
            _desiredStatus = SimulationStatus.Stopped;
        }
    }

    public bool ConsumeResetRequested()
    {
        lock (_gate)
        {
            if (!_resetRequested) return false;
            _resetRequested = false;
            return true;
        }
    }
}

