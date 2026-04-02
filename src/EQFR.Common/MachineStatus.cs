namespace EQFR.Common;

public enum MachineStatus
{
    Idle = 0,
    WaitingForInput = 1,
    Processing = 2,
    OutputReady = 3,
    Error = 99,
}

