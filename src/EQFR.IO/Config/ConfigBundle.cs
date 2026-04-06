using EQFR.EIFData.Layout;
using EQFR.EIFData.Process;
using EQFR.EIFData.Routes;
using EQFR.EIFData.Simulation;
using EQFR.EIFData.Transport;

namespace EQFR.IO.Config;

public sealed record ConfigBundle(
    FactoryLayoutConfig Layout,
    FactoryRoutesConfig Routes,
    FactoryProcessConfig Process,
    FactorySimulationConfig Simulation,
    FactoryTransportConfig Transport
);

