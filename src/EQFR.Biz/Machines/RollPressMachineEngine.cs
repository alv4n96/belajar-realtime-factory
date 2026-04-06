using EQFR.Biz.Runtime;
using EQFR.Common;
using EQFR.EIFData.Process;

namespace EQFR.Biz.Machines;

public sealed class RollPressMachineEngine
{
    private readonly Dictionary<string, MachineProcessConfig> _processByMachineId;

    public RollPressMachineEngine(FactoryProcessConfig processConfig)
    {
        _processByMachineId = processConfig.Machines.ToDictionary(m => m.Id, StringComparer.OrdinalIgnoreCase);
    }

    public void Tick(FactoryState state, DateTimeOffset now)
    {
        foreach (var machineId in state.Machines.Keys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
        {
            state.Machines[machineId] = TickMachine(state, state.Machines[machineId], now);
        }
    }

    private MachineUnit TickMachine(FactoryState state, MachineUnit machine, DateTimeOffset now)
    {
        if (!_processByMachineId.TryGetValue(machine.Id, out var process))
        {
            // No process config means the machine can't run.
            if (machine.Status != MachineStatus.Error)
            {
                EventLogWriter.Add(state, now, $"Machine '{machine.Id}' has no process config.", machine.Id, "machine.process.missing");
            }
            return machine with { Status = MachineStatus.Error };
        }

        var inputNode = new NodeRef(machine.Id, process.InputPortId);
        var outputNode = new NodeRef(machine.Id, process.OutputPortId);

        // If we were output-ready and the output lot is no longer at the output port, reset the machine.
        if (machine.OutputReady && machine.CurrentLotId is not null)
        {
            if (!state.Lots.TryGetValue(machine.CurrentLotId, out var outLot) || !outLot.CurrentNode.Equals(outputNode))
            {
                EventLogWriter.Add(state, now, $"Machine '{machine.Id}' output cleared.", machine.Id, "machine.output.cleared");
                return machine with
                {
                    Status = MachineStatus.Idle,
                    CurrentLotId = null,
                    CurrentStepIndex = 0,
                    TotalSteps = process.Steps.Count,
                    StepStartTime = null,
                    CurrentStepDurationMs = 0,
                    NeedsInput = false,
                    OutputReady = false
                };
            }
        }

        // If machine is empty (and not waiting for output pickup), request input.
        if (machine.CurrentLotId is null && !machine.OutputReady)
        {
            if (!machine.NeedsInput || machine.Status != MachineStatus.WaitingForInput)
            {
                EventLogWriter.Add(state, now, $"Machine '{machine.Id}' requests input.", machine.Id, "machine.input.request");
            }

            machine = machine with
            {
                Status = MachineStatus.WaitingForInput,
                NeedsInput = true,
                OutputReady = false,
                TotalSteps = process.Steps.Count
            };

            // Start processing if an available lot is already at the input port.
            var lotAtInput = state.Lots.Values
                .Where(l => l.Status == LotStatus.Available && l.CurrentNode.Equals(inputNode))
                .OrderBy(l => l.LotId, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();

            if (lotAtInput is not null)
            {
                return StartProcessing(state, machine, lotAtInput, process, now);
            }

            return machine;
        }

        // If we're processing, advance steps based on elapsed time.
        if (machine.Status == MachineStatus.Processing && machine.CurrentLotId is not null)
        {
            if (!state.Lots.TryGetValue(machine.CurrentLotId, out var lot))
            {
                EventLogWriter.Add(state, now, $"Machine '{machine.Id}' lost lot '{machine.CurrentLotId}'.", machine.Id, "machine.lot.missing");
                return machine with { Status = MachineStatus.Error };
            }

            if (machine.StepStartTime is null)
            {
                // Repair missing start time by restarting the current step.
                var step = process.Steps[Math.Clamp(machine.CurrentStepIndex, 0, Math.Max(0, process.Steps.Count - 1))];
                EventLogWriter.Add(state, now, $"Machine '{machine.Id}' resumes step '{step.Name}'.", machine.Id, "machine.step.resume");
                return machine with { StepStartTime = now, CurrentStepDurationMs = step.DurationMs };
            }

            var elapsedMs = (now - machine.StepStartTime.Value).TotalMilliseconds;
            if (elapsedMs < machine.CurrentStepDurationMs)
            {
                return machine;
            }

            var nextStepIndex = machine.CurrentStepIndex + 1;
            if (nextStepIndex >= process.Steps.Count)
            {
                // Completed processing.
                var movedLot = lot with { Status = LotStatus.Available, CurrentNode = outputNode };
                state.Lots[movedLot.LotId] = movedLot;

                EventLogWriter.Add(state, now, $"Machine '{machine.Id}' completed lot '{movedLot.LotId}'. Output ready.", machine.Id, "machine.complete");
                return machine with
                {
                    Status = MachineStatus.OutputReady,
                    OutputReady = true,
                    NeedsInput = false,
                    CurrentStepIndex = process.Steps.Count,
                    StepStartTime = null,
                    CurrentStepDurationMs = 0
                };
            }

            // Start next step.
            var nextStep = process.Steps[nextStepIndex];
            EventLogWriter.Add(state, now, $"Machine '{machine.Id}' started step {nextStepIndex + 1}/{process.Steps.Count}: '{nextStep.Name}'.", machine.Id, "machine.step.start");

            return machine with
            {
                CurrentStepIndex = nextStepIndex,
                StepStartTime = now,
                CurrentStepDurationMs = nextStep.DurationMs
            };
        }

        // If output ready, just wait (input is not requested).
        if (machine.OutputReady)
        {
            return machine with { Status = MachineStatus.OutputReady, NeedsInput = false };
        }

        // Default: keep current state.
        return machine;
    }

    private static MachineUnit StartProcessing(
        FactoryState state,
        MachineUnit machine,
        Lot lot,
        MachineProcessConfig process,
        DateTimeOffset now)
    {
        if (process.Steps.Count == 0)
        {
            EventLogWriter.Add(state, now, $"Machine '{machine.Id}' has no process steps.", machine.Id, "machine.steps.empty");
            return machine with { Status = MachineStatus.Error };
        }

        var step0 = process.Steps[0];
        state.Lots[lot.LotId] = lot with { Status = LotStatus.InProcess, CurrentNode = new NodeRef(machine.Id, process.InputPortId) };

        EventLogWriter.Add(state, now, $"Machine '{machine.Id}' started lot '{lot.LotId}'. Step 1/{process.Steps.Count}: '{step0.Name}'.", machine.Id, "machine.start");

        return machine with
        {
            Status = MachineStatus.Processing,
            NeedsInput = false,
            OutputReady = false,
            CurrentLotId = lot.LotId,
            CurrentStepIndex = 0,
            TotalSteps = process.Steps.Count,
            StepStartTime = now,
            CurrentStepDurationMs = step0.DurationMs
        };
    }
}

