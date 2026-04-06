const SIMULATION_STATUS = {
  Stopped: 0,
  Running: 1,
  Paused: 2
};

const MACHINE_STATUS = {
  Idle: 0,
  WaitingForInput: 1,
  Processing: 2,
  OutputReady: 3,
  Error: 99
};

const TRANSPORT_STATUS = {
  Idle: 0,
  Moving: 1,
  Waiting: 2,
  Loading: 3,
  Unloading: 4,
  Returning: 5,
  Error: 99
};

const LOT_STATUS = {
  Available: 0,
  Reserved: 1,
  InTransit: 2,
  InProcess: 3,
  Completed: 4,
  Error: 99
};

const PORT_TYPE = {
  Input: 0,
  Output: 1
};

function createMockFactoryRuntime({ configLoader, snapshotStore, controlStore, logger = console }) {
  let runtime = null;
  let timer = null;
  let configBundle = null;
  let isTicking = false;

  async function start() {
    await refreshConfigBundle();
    rebuildRuntime();
    publishSnapshot();

    const intervalMs = runtime.tickIntervalMs;
    timer = setInterval(async () => {
      if (isTicking) return;
      isTicking = true;
      try {
        await tick();
      } catch (error) {
        logger.error("[backend-eqfr] mock runtime tick failed", error);
      } finally {
        isTicking = false;
      }
    }, intervalMs);
  }

  function stop() {
    if (timer) {
      clearInterval(timer);
      timer = null;
    }
  }

  async function tick() {
    if (!runtime) return;

    if (controlStore.consumeReset()) {
      await refreshConfigBundle();
      rebuildRuntime();
    }

    const controls = controlStore.get();
    runtime.simulationStatus = toSimulationStatus(controls.desiredStatus);

    if (runtime.simulationStatus === SIMULATION_STATUS.Running) {
      advanceScenario();
    }

    publishSnapshot();
  }

  async function refreshConfigBundle() {
    const loaded = await configLoader.loadBundle();
    configBundle = loaded.bundle;
  }

  function rebuildRuntime() {
    const layout = configBundle.layout;
    const routes = configBundle.routes;
    const process = configBundle.process;
    const simulation = configBundle.simulation;
    const transport = configBundle.transport;

    const sourceLotSeed = simulation.initialLots?.[0] || {
      lotId: "LOT_001",
      materialType: "RAW",
      locationId: layout.locations[0].id,
      portId: layout.locations[0].ports?.[0]?.id || "OUT"
    };

    const machineConfig = process.machines[0];
    const primaryTransport = transport.transports[0];
    const secondaryTransport = transport.transports[1] || null;
    const sourceLocationId = sourceLotSeed.locationId;
    const sourcePortId = sourceLotSeed.portId;
    const outputLocationId = findOutputWarehouse(layout.locations, sourceLocationId);
    const outputPortId = findWarehouseInputPort(layout.locations, outputLocationId);

    const routeEdges = routes.edges.map((edge, index) => ({
      id: `EDGE_${String(index + 1).padStart(3, "0")}`,
      fromLocationId: edge.from.locationId,
      fromPortId: edge.from.portId,
      toLocationId: edge.to.locationId,
      toPortId: edge.to.portId,
      isBidirectional: Boolean(edge.isBidirectional),
      distance: edge.distance ?? null
    }));

    const graph = createGraph(routeEdges);

    runtime = {
      layout,
      routeEdges,
      graph,
      simulationStatus: toSimulationStatus(controlStore.get().desiredStatus),
      tickIntervalMs: simulation.tickIntervalMs || 250,
      maxRecentEvents: simulation.maxRecentEvents || 200,
      machineConfig,
      machine: {
        id: machineConfig.id,
        inputPortId: machineConfig.inputPortId,
        outputPortId: machineConfig.outputPortId,
        status: MACHINE_STATUS.WaitingForInput,
        currentLotId: null,
        currentStepIndex: 0,
        totalSteps: machineConfig.steps.length,
        needsInput: true,
        outputReady: false
      },
      transports: [
        createTransportState(primaryTransport),
        ...(secondaryTransport ? [createTransportState(secondaryTransport)] : [])
      ],
      primaryTransportId: primaryTransport.id,
      secondaryTransportId: secondaryTransport?.id || primaryTransport.id,
      sourceTemplate: sourceLotSeed,
      sourceLocationId,
      sourcePortId,
      outputLocationId,
      outputPortId,
      activeLot: null,
      completedLots: [],
      recentEvents: [],
      edgeOccupancy: {},
      cycleNumber: 0,
      taskCounter: 0,
      processing: null,
      travel: null,
      phase: null
    };

    spawnNextLot();
    transitionTo("announce-input");
    addEvent(`Mock backend runtime initialized. Machine '${runtime.machine.id}' siap menerima input.`, runtime.machine.id, "mock.runtime.ready");
  }

  function createTransportState(config) {
    return {
      id: config.id,
      status: TRANSPORT_STATUS.Idle,
      locationId: config.start.locationId,
      portId: config.start.portId,
      currentTaskId: null,
      currentLotId: null,
      homeLocationId: config.start.locationId,
      homePortId: config.start.portId
    };
  }

  function transitionTo(kind) {
    runtime.phase = { kind, remainingTicks: 0 };
    runtime.edgeOccupancy = {};
    runtime.travel = null;

    switch (kind) {
      case "announce-input":
        runtime.phase.remainingTicks = 2;
        runtime.machine.status = MACHINE_STATUS.WaitingForInput;
        runtime.machine.currentLotId = null;
        runtime.machine.currentStepIndex = 0;
        runtime.machine.needsInput = true;
        runtime.machine.outputReady = false;
        inputTransport().status = TRANSPORT_STATUS.Idle;
        inputTransport().currentTaskId = null;
        inputTransport().currentLotId = null;
        addEvent(`Machine '${runtime.machine.id}' requests input.`, runtime.machine.id, "machine.request_input");
        break;

      case "travel-to-input": {
        const taskId = nextTaskId();
        const path = findPath(inputTransport().id, inputTransport().locationId, runtime.sourceLocationId);
        inputTransport().status = TRANSPORT_STATUS.Moving;
        inputTransport().currentTaskId = taskId;
        inputTransport().currentLotId = null;
        runtime.phase.taskId = taskId;
        runtime.travel = beginTravel(inputTransport().id, path, TRANSPORT_STATUS.Moving);
        addEvent(`Dispatcher assigned '${inputTransport().id}' to deliver input lot '${runtime.activeLot.lotId}' -> machine '${runtime.machine.id}'.`, inputTransport().id, "dispatch.input.assigned");
        addEvent(`Transport '${inputTransport().id}' started task '${taskId}'.`, inputTransport().id, "transport.task.started");
        break;
      }

      case "pickup-input":
        runtime.phase.remainingTicks = 2;
        inputTransport().status = TRANSPORT_STATUS.Loading;
        inputTransport().locationId = runtime.sourceLocationId;
        inputTransport().portId = runtime.sourcePortId;
        runtime.activeLot.status = LOT_STATUS.Reserved;
        runtime.activeLot.locationId = runtime.sourceLocationId;
        runtime.activeLot.portId = runtime.sourcePortId;
        break;

      case "travel-to-machine": {
        const path = composePath(inputTransport().id, [runtime.sourceLocationId, inputTransport().homeLocationId, runtime.machine.id]);
        inputTransport().status = TRANSPORT_STATUS.Moving;
        inputTransport().currentLotId = runtime.activeLot.lotId;
        runtime.activeLot.status = LOT_STATUS.InTransit;
        runtime.travel = beginTravel(inputTransport().id, path, TRANSPORT_STATUS.Moving);
        break;
      }

      case "drop-to-machine":
        runtime.phase.remainingTicks = 2;
        inputTransport().status = TRANSPORT_STATUS.Unloading;
        inputTransport().locationId = runtime.machine.id;
        inputTransport().portId = runtime.machine.inputPortId;
        runtime.activeLot.locationId = runtime.machine.id;
        runtime.activeLot.portId = runtime.machine.inputPortId;
        runtime.activeLot.status = LOT_STATUS.InTransit;
        break;

      case "processing-step": {
        const stepIndex = runtime.processing?.stepIndex ?? 0;
        const step = runtime.machineConfig.steps[stepIndex];
        runtime.phase.remainingTicks = Math.max(1, Math.ceil(step.durationMs / runtime.tickIntervalMs));
        runtime.machine.status = MACHINE_STATUS.Processing;
        runtime.machine.currentLotId = runtime.activeLot.lotId;
        runtime.machine.currentStepIndex = stepIndex;
        runtime.machine.needsInput = false;
        runtime.activeLot.status = LOT_STATUS.InProcess;
        runtime.activeLot.locationId = runtime.machine.id;
        runtime.activeLot.portId = runtime.machine.inputPortId;
        if (stepIndex === 0) {
          addEvent(`Machine '${runtime.machine.id}' started lot '${runtime.activeLot.lotId}'. Step 1/${runtime.machine.totalSteps}: '${step.name}'.`, runtime.machine.id, "machine.lot.started");
        } else {
          addEvent(`Machine '${runtime.machine.id}' started step ${stepIndex + 1}/${runtime.machine.totalSteps}: '${step.name}'.`, runtime.machine.id, "machine.step.started");
        }
        break;
      }

      case "output-ready":
        runtime.phase.remainingTicks = 2;
        runtime.machine.status = MACHINE_STATUS.OutputReady;
        runtime.machine.outputReady = true;
        runtime.machine.needsInput = false;
        runtime.activeLot.status = LOT_STATUS.Available;
        runtime.activeLot.locationId = runtime.machine.id;
        runtime.activeLot.portId = runtime.machine.outputPortId;
        addEvent(`Machine '${runtime.machine.id}' completed lot '${runtime.activeLot.lotId}'. Output ready.`, runtime.machine.id, "machine.output.ready");
        addEvent(`Dispatcher assigned '${outputTransport().id}' to move output lot '${runtime.activeLot.lotId}' -> warehouse '${runtime.outputLocationId}'.`, outputTransport().id, "dispatch.output.assigned");
        break;

      case "pickup-output": {
        runtime.phase.remainingTicks = 2;
        const taskId = nextTaskId();
        const path = findPath(outputTransport().id, outputTransport().locationId, runtime.machine.id);
        outputTransport().status = TRANSPORT_STATUS.Moving;
        outputTransport().currentTaskId = taskId;
        outputTransport().currentLotId = null;
        runtime.phase.taskId = taskId;
        runtime.travel = beginTravel(outputTransport().id, path, TRANSPORT_STATUS.Moving);
        addEvent(`Transport '${outputTransport().id}' started task '${taskId}'.`, outputTransport().id, "transport.task.started");
        break;
      }

      case "load-output":
        runtime.phase.remainingTicks = 2;
        outputTransport().status = TRANSPORT_STATUS.Loading;
        outputTransport().locationId = runtime.machine.id;
        outputTransport().portId = runtime.machine.outputPortId;
        runtime.machine.outputReady = false;
        break;

      case "travel-to-output": {
        const taskId = outputTransport().currentTaskId || nextTaskId();
        const path = composePath(outputTransport().id, [runtime.machine.id, outputTransport().homeLocationId, runtime.outputLocationId]);
        outputTransport().status = TRANSPORT_STATUS.Moving;
        outputTransport().currentTaskId = taskId;
        outputTransport().currentLotId = runtime.activeLot.lotId;
        runtime.activeLot.status = LOT_STATUS.InTransit;
        runtime.travel = beginTravel(outputTransport().id, path, TRANSPORT_STATUS.Moving);
        break;
      }

      case "drop-to-output":
        runtime.phase.remainingTicks = 2;
        outputTransport().status = TRANSPORT_STATUS.Unloading;
        outputTransport().locationId = runtime.outputLocationId;
        outputTransport().portId = runtime.outputPortId;
        runtime.activeLot.locationId = runtime.outputLocationId;
        runtime.activeLot.portId = runtime.outputPortId;
        break;

      case "return-home": {
        const path = findPath(outputTransport().id, runtime.outputLocationId, outputTransport().homeLocationId);
        outputTransport().status = TRANSPORT_STATUS.Returning;
        outputTransport().currentLotId = null;
        outputTransport().currentTaskId = null;
        runtime.travel = beginTravel(outputTransport().id, path, TRANSPORT_STATUS.Returning);
        break;
      }
    }
  }

  function advanceScenario() {
    switch (runtime.phase.kind) {
      case "announce-input":
        runtime.phase.remainingTicks -= 1;
        if (runtime.phase.remainingTicks <= 0) transitionTo("travel-to-input");
        break;

      case "travel-to-input":
        if (advanceTravel()) transitionTo("pickup-input");
        break;

      case "pickup-input":
        runtime.phase.remainingTicks -= 1;
        if (runtime.phase.remainingTicks <= 0) {
          runtime.activeLot.status = LOT_STATUS.InTransit;
          inputTransport().currentLotId = runtime.activeLot.lotId;
          addEvent(`Transport '${inputTransport().id}' picked lot '${runtime.activeLot.lotId}'.`, inputTransport().id, "transport.pickup");
          transitionTo("travel-to-machine");
        }
        break;

      case "travel-to-machine":
        if (advanceTravel()) transitionTo("drop-to-machine");
        break;

      case "drop-to-machine":
        runtime.phase.remainingTicks -= 1;
        if (runtime.phase.remainingTicks <= 0) {
          addEvent(`Transport '${inputTransport().id}' dropped lot '${runtime.activeLot.lotId}'.`, inputTransport().id, "transport.dropoff");
          addEvent(`Transport '${inputTransport().id}' completed task '${inputTransport().currentTaskId}'.`, inputTransport().id, "transport.task.completed");
          runtime.processing = { stepIndex: 0 };
          inputTransport().currentTaskId = null;
          inputTransport().currentLotId = null;
          transitionTo("processing-step");
        }
        break;

      case "processing-step":
        runtime.phase.remainingTicks -= 1;
        if (runtime.phase.remainingTicks <= 0) {
          const nextStepIndex = runtime.processing.stepIndex + 1;
          if (nextStepIndex < runtime.machine.totalSteps) {
            runtime.processing.stepIndex = nextStepIndex;
            transitionTo("processing-step");
          } else {
            runtime.processing = null;
            transitionTo("output-ready");
          }
        }
        break;

      case "output-ready":
        runtime.phase.remainingTicks -= 1;
        if (runtime.phase.remainingTicks <= 0) transitionTo("pickup-output");
        break;

      case "pickup-output":
        if (advanceTravel()) transitionTo("load-output");
        break;

      case "load-output":
        runtime.phase.remainingTicks -= 1;
        if (runtime.phase.remainingTicks <= 0) {
          outputTransport().currentLotId = runtime.activeLot.lotId;
          addEvent(`Transport '${outputTransport().id}' picked lot '${runtime.activeLot.lotId}'.`, outputTransport().id, "transport.pickup");
          addEvent(`Machine '${runtime.machine.id}' output cleared.`, runtime.machine.id, "machine.output.cleared");
          transitionTo("travel-to-output");
        }
        break;

      case "travel-to-output":
        if (advanceTravel()) transitionTo("drop-to-output");
        break;

      case "drop-to-output":
        runtime.phase.remainingTicks -= 1;
        if (runtime.phase.remainingTicks <= 0) {
          runtime.activeLot.status = LOT_STATUS.Completed;
          runtime.completedLots = [runtime.activeLot, ...runtime.completedLots].slice(0, 5);
          addEvent(`Transport '${outputTransport().id}' dropped lot '${runtime.activeLot.lotId}'.`, outputTransport().id, "transport.dropoff");
          addEvent(`Transport '${outputTransport().id}' completed task '${outputTransport().currentTaskId}'.`, outputTransport().id, "transport.task.completed");
          outputTransport().currentTaskId = null;
          outputTransport().currentLotId = null;
          spawnNextLot();
          transitionTo("return-home");
        }
        break;

      case "return-home":
        if (advanceTravel()) transitionTo("announce-input");
        break;
    }
  }

  function advanceTravel() {
    if (!runtime.travel) return true;

    const travel = runtime.travel;
    const transport = activeTravelTransport();
    const currentIndex = travel.edgeIndex;
    const fromLocationId = travel.path[currentIndex];
    const toLocationId = travel.path[currentIndex + 1] || fromLocationId;
    const edgeId = findEdgeId(fromLocationId, toLocationId);
    runtime.edgeOccupancy = edgeId && transport ? { [edgeId]: transport.id } : {};

    travel.ticksIntoEdge += 1;
    if (travel.ticksIntoEdge < travel.ticksPerEdge || currentIndex + 1 >= travel.path.length) {
      return false;
    }

    travel.ticksIntoEdge = 0;
    travel.edgeIndex += 1;

    if (transport) {
      transport.locationId = travel.path[travel.edgeIndex];
      transport.portId = resolvePortForLocation(transport, transport.locationId);
    }

    if (travel.edgeIndex >= travel.path.length - 1) {
      runtime.edgeOccupancy = {};
      runtime.travel = null;
      return true;
    }

    return false;
  }

  function beginTravel(transportId, path, status) {
    const transport = getTransportById(transportId);
    const normalizedPath = Array.isArray(path) && path.length > 0
      ? path
      : [transport?.locationId || runtime.sourceLocationId];

    if (transport) {
      transport.status = status;
      transport.locationId = normalizedPath[0];
      transport.portId = resolvePortForLocation(transport, normalizedPath[0]);
    }

    return {
      transportId,
      path: normalizedPath,
      edgeIndex: 0,
      ticksIntoEdge: 0,
      ticksPerEdge: 5
    };
  }

  function composePath(transportId, checkpoints) {
    const filtered = checkpoints.filter(Boolean);
    if (filtered.length <= 1) {
      return filtered;
    }

    const segments = [];
    for (let i = 0; i < filtered.length - 1; i += 1) {
      const segment = findPath(transportId, filtered[i], filtered[i + 1]);
      if (segments.length === 0) {
        segments.push(...segment);
      } else {
        segments.push(...segment.slice(1));
      }
    }

    return segments;
  }

  function publishSnapshot() {
    snapshotStore.set(buildSnapshot());
  }

  function buildSnapshot() {
    const now = new Date().toISOString();
    return {
      serverTime: now,
      simulationStatus: runtime.simulationStatus,
      tickOptions: {
        tickInterval: msToTimeSpan(runtime.tickIntervalMs),
        maxRecentEvents: runtime.maxRecentEvents
      },
      locations: runtime.layout.locations.map((location) => ({
        id: location.id,
        displayName: location.displayName,
        type: location.type,
        x: location.position.x,
        y: location.position.y,
        ports: (location.ports || []).map((port) => ({
          id: port.id,
          type: PORT_TYPE[port.type] ?? PORT_TYPE.Output
        }))
      })),
      routeEdges: runtime.routeEdges,
      edgeOccupancy: runtime.edgeOccupancy,
      machines: [
        {
          id: runtime.machine.id,
          status: runtime.machine.status,
          inputPortId: runtime.machine.inputPortId,
          outputPortId: runtime.machine.outputPortId,
          currentLotId: runtime.machine.currentLotId,
          currentStepIndex: runtime.machine.currentStepIndex,
          totalSteps: runtime.machine.totalSteps,
          needsInput: runtime.machine.needsInput,
          outputReady: runtime.machine.outputReady
        }
      ],
      transports: runtime.transports.map((transport) => ({
        id: transport.id,
        status: transport.status,
        locationId: transport.locationId,
        portId: transport.portId,
        currentTaskId: transport.currentTaskId,
        currentLotId: transport.currentLotId
      })),
      lots: [
        ...(runtime.activeLot ? [runtime.activeLot] : []),
        ...runtime.completedLots
      ].map((lot) => ({
        lotId: lot.lotId,
        materialType: lot.materialType,
        status: lot.status,
        locationId: lot.locationId,
        portId: lot.portId
      })),
      recentEvents: runtime.recentEvents
    };
  }

  function spawnNextLot() {
    runtime.cycleNumber += 1;
    const lotId = runtime.cycleNumber === 1
      ? runtime.sourceTemplate.lotId
      : nextLotId(runtime.sourceTemplate.lotId, runtime.cycleNumber);

    runtime.activeLot = {
      lotId,
      materialType: runtime.sourceTemplate.materialType,
      status: LOT_STATUS.Available,
      locationId: runtime.sourceLocationId,
      portId: runtime.sourcePortId
    };
  }

  function nextTaskId() {
    runtime.taskCounter += 1;
    return `TASK_${String(runtime.taskCounter).padStart(4, "0")}`;
  }

  function nextLotId(seedLotId, cycleNumber) {
    const match = /^(.*?)(\d+)$/.exec(seedLotId);
    if (!match) return `${seedLotId}_${String(cycleNumber).padStart(3, "0")}`;
    const [, prefix, numericPart] = match;
    const nextValue = Number.parseInt(numericPart, 10) + cycleNumber - 1;
    return `${prefix}${String(nextValue).padStart(numericPart.length, "0")}`;
  }

  function primaryTransport() {
    return runtime.transports.find((item) => item.id === runtime.primaryTransportId);
  }

  function inputTransport() {
    return primaryTransport();
  }

  function outputTransport() {
    return runtime.transports.find((item) => item.id === runtime.secondaryTransportId) || primaryTransport();
  }

  function getTransportById(transportId) {
    return runtime.transports.find((item) => item.id === transportId) || null;
  }

  function activeTravelTransport() {
    return runtime.travel ? getTransportById(runtime.travel.transportId) : null;
  }

  function resolvePortForLocation(transport, locationId) {
    if (locationId === runtime.machine.id) {
      return transport?.id === runtime.secondaryTransportId
        ? runtime.machine.outputPortId
        : runtime.machine.inputPortId;
    }

    if (locationId === runtime.sourceLocationId) return runtime.sourcePortId;
    if (locationId === runtime.outputLocationId) return runtime.outputPortId;
    if (locationId === transport?.homeLocationId) return transport.homePortId;

    const location = runtime.layout.locations.find((item) => item.id === locationId);
    return location?.ports?.[0]?.id || "IN";
  }

  function addEvent(message, entityId = null, eventType = null) {
    runtime.recentEvents.unshift({
      timestamp: new Date().toISOString(),
      message,
      entityId,
      eventType
    });
    runtime.recentEvents = runtime.recentEvents.slice(0, runtime.maxRecentEvents);
  }

  function findPath(transportId, fromLocationId, toLocationId) {
    if (fromLocationId === toLocationId) return [fromLocationId];

    const allowedLocations = getAllowedLocationsForTransport(transportId);
    const queue = [[fromLocationId]];
    const visited = new Set([fromLocationId]);

    while (queue.length > 0) {
      const currentPath = queue.shift();
      const current = currentPath[currentPath.length - 1];
      const neighbors = runtime.graph.get(current) || [];
      for (const next of neighbors) {
        if (allowedLocations && !allowedLocations.has(next)) continue;
        if (visited.has(next)) continue;
        const nextPath = [...currentPath, next];
        if (next === toLocationId) return nextPath;
        visited.add(next);
        queue.push(nextPath);
      }
    }

    return [fromLocationId, toLocationId];
  }

  function getAllowedLocationsForTransport(transportId) {
    if (transportId === runtime.primaryTransportId) {
      return new Set([runtime.sourceLocationId, "CS_1", runtime.machine.id]);
    }

    if (transportId === runtime.secondaryTransportId) {
      return new Set([runtime.machine.id, "CS_2", runtime.outputLocationId]);
    }

    return null;
  }

  function findEdgeId(fromLocationId, toLocationId) {
    const direct = runtime.routeEdges.find((edge) => edge.fromLocationId === fromLocationId && edge.toLocationId === toLocationId);
    if (direct) return direct.id;
    const reverse = runtime.routeEdges.find((edge) => edge.isBidirectional && edge.fromLocationId === toLocationId && edge.toLocationId === fromLocationId);
    return reverse?.id || null;
  }

  return { start, stop };
}

function createGraph(routeEdges) {
  const graph = new Map();
  for (const edge of routeEdges) {
    if (!graph.has(edge.fromLocationId)) graph.set(edge.fromLocationId, []);
    graph.get(edge.fromLocationId).push(edge.toLocationId);
    if (edge.isBidirectional) {
      if (!graph.has(edge.toLocationId)) graph.set(edge.toLocationId, []);
      graph.get(edge.toLocationId).push(edge.fromLocationId);
    }
  }
  return graph;
}

function toSimulationStatus(value) {
  return SIMULATION_STATUS[value] ?? SIMULATION_STATUS.Running;
}

function findOutputWarehouse(locations, sourceLocationId) {
  const candidate = locations.find((location) => location.type === "Warehouse" && location.id !== sourceLocationId && (location.ports || []).some((port) => port.type === "Input"));
  return candidate?.id || sourceLocationId;
}

function findWarehouseInputPort(locations, warehouseId) {
  const warehouse = locations.find((location) => location.id === warehouseId);
  return warehouse?.ports?.find((port) => port.type === "Input")?.id || warehouse?.ports?.[0]?.id || "IN";
}

function msToTimeSpan(ms) {
  const totalMilliseconds = Math.max(0, Number(ms) || 0);
  const hours = Math.floor(totalMilliseconds / 3600000);
  const minutes = Math.floor((totalMilliseconds % 3600000) / 60000);
  const seconds = Math.floor((totalMilliseconds % 60000) / 1000);
  const milliseconds = totalMilliseconds % 1000;
  return `${String(hours).padStart(2, "0")}:${String(minutes).padStart(2, "0")}:${String(seconds).padStart(2, "0")}.${String(milliseconds).padStart(3, "0")}0000`;
}

module.exports = { createMockFactoryRuntime };
