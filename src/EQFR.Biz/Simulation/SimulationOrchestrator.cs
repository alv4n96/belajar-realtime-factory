using EQFR.Biz.Dispatching;
using EQFR.Biz.Machines;
using EQFR.Biz.Runtime;
using EQFR.Biz.Transport;

namespace EQFR.Biz.Simulation;

public sealed class SimulationOrchestrator
{
    private readonly RollPressMachineEngine _machineEngine;
    private readonly Dispatcher _dispatcher;
    private readonly TransportEngine _transportEngine;

    public SimulationOrchestrator(
        RollPressMachineEngine machineEngine,
        Dispatcher dispatcher,
        TransportEngine transportEngine)
    {
        _machineEngine = machineEngine;
        _dispatcher = dispatcher;
        _transportEngine = transportEngine;
    }

    public void Tick(FactoryState state, DateTimeOffset now)
    {
        // Required deterministic tick order for MVP.
        _machineEngine.Tick(state, now);
        _dispatcher.Tick(state, now);
        _transportEngine.Tick(state, now);
    }
}

