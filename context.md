# README-CODEX-CONTEXT

## 1. Project Overview

This repository builds a **real-time factory simulation and monitoring system**.

The system models a factory flow where:
- raw material starts from an input warehouse/stocker
- transport units wait at charging stations
- a machine (example: Roll Press) requests input material
- a transport unit picks the material from warehouse/stocker and delivers it to the machine
- the machine runs multiple process steps
- after processing is complete, a transport unit picks the output from the machine and delivers it to the next warehouse/stocker
- transport units can only move on predefined routes/lines
- the UI must show real-time machine state, transport state, current lot/material, routes, legend, and event log

This project is intentionally designed as:
- **real-time**
- **in-memory runtime state**
- **no database**
- **JSON config driven**
- **issue-by-issue implementation**
- **modular and cleanly separated**

---

## 2. Core Goal

Build a system that can simulate and visualize this flow:

1. Roll Press is empty
2. Roll Press requests input material
3. One transport unit leaves charging station
4. Transport picks a lot from input warehouse/stocker
5. Transport delivers that lot to Roll Press input port
6. Transport returns to charging station
7. Roll Press processes the lot through multiple steps
8. Roll Press marks output ready
9. One transport unit picks output from Roll Press output port
10. Transport delivers output to next warehouse/stocker input port
11. Transport returns to charging station
12. UI shows all of the above in real time

---

## 3. Non-Goals for Current Phase

The following are intentionally out of scope for the first implementation:
- database persistence
- authentication/authorization
- external message broker
- PLC integration
- MES integration
- historian/replay storage
- advanced optimization scheduler
- battery optimization logic
- multi-factory support
- advanced collision avoidance beyond basic reservation
- production hardening for enterprise deployment

These can be added later after the core engine is stable.

---

## 4. Architecture Summary

The solution name is:

- `FR`

The main project structure is:

- `FR.Common`
- `FR.EIFData`
- `FR.IO`
- `FR.Biz`
- `FR.UI`

### Project responsibilities

#### FR.Common
Shared primitives and cross-project abstractions:
- enums
- constants
- shared DTOs
- shared result/error objects
- shared geometry/value objects
- simple reusable helpers

#### FR.EIFData
Configuration contracts / external input data definitions:
- layout config DTOs
- route config DTOs
- machine config DTOs
- transport config DTOs
- simulation config DTOs

#### FR.IO
Input/output and config loading layer:
- JSON config loader
- config validation
- file reader/writer
- optional file-based logging
- config parsing helpers

#### FR.Biz
Core business/runtime engine:
- in-memory factory state
- machine engine
- transport engine
- dispatcher
- route graph
- route reservation
- simulation orchestrator
- snapshot builder

#### FR.UI
Presentation/host layer:
- Blazor UI
- dashboard pages
- reusable UI components
- MVVM-inspired presentation structure
- SignalR realtime bridge
- background simulation hosting
- control endpoints (start/pause/reset)

---

## 5. High-Level Runtime Design

### 5.1 Runtime Model
The system runs entirely in memory.

There is no database in the first version.

At startup, the app:
- reads JSON config files
- validates config
- generates runtime state
- starts a background simulation loop
- streams snapshot updates to the UI

### 5.2 Main Runtime State
The runtime state includes:
- locations
- ports
- routes
- transports
- machines
- lots/materials
- tasks
- reservations
- recent event log
- current simulation status

### 5.3 Realtime Flow
Every simulation tick:
1. machine engine progresses
2. dispatcher checks pending requests
3. transport engine progresses
4. route reservations are updated
5. a snapshot is built
6. snapshot is sent to UI

---

## 6. Factory Domain Model

### 6.1 Locations
Factory locations may include:
- input warehouse/stocker
- charging station 1
- machine area (example: Roll Press)
- charging station 2
- output warehouse/stocker

Each location may have:
- unique id
- display name
- type
- X/Y position for UI
- port definitions

### 6.2 Ports
Each relevant node can expose ports:
- input port
- output port

Examples:
- input warehouse -> output port
- charging station -> input and output port
- Roll Press -> input and output port
- output warehouse -> input port

### 6.3 Routes
Transport units may only move through configured routes.

Routes are modeled as a graph:
- node/port = position in logical route network
- edge = valid path between nodes

Transport must not move freely outside configured edges.

### 6.4 Transport Units
Each transport unit has:
- id
- current location
- current port
- current X/Y UI position
- status
- current task
- current lot id
- route progress
- planned path
- optional battery value placeholder

### 6.5 Machines
Each machine has:
- id
- status
- current lot id
- current step index
- total steps
- current step start time
- current step duration
- needs input flag
- output ready flag

### 6.6 Lots / Materials
Each lot has:
- lot id
- material type
- source
- current location
- destination
- status

---

## 7. Core Factory Scenario

The baseline scenario for the first working demo is:

### Nodes
- `WH_IN` = input warehouse/stocker
- `CS_1` = charging station 1
- `ROLL_PRESS_1` = machine
- `CS_2` = charging station 2
- `WH_OUT` = output warehouse/stocker

### Transport
- `TR_01`
- `TR_02`

### Machine
- `ROLL_PRESS_1`

### Example flow
- machine starts empty
- machine requests input lot
- one transport is assigned
- transport moves from charging station to warehouse
- transport picks lot
- transport moves to machine input
- lot is unloaded to machine
- machine starts step 1
- machine progresses through all configured steps
- machine marks output ready
- one transport is assigned to output pickup
- transport moves to machine output
- transport picks finished lot
- transport moves to output warehouse
- transport unloads finished lot
- transport returns to charging station

---

## 8. Route and Reservation Rules

### 8.1 Basic route rule
A transport unit may only move through configured route edges.

### 8.2 Basic collision prevention
For the first version:
- only one transport is allowed on one reserved edge at a time

### 8.3 Reservation rule
Before moving:
- transport requests a route
- route segments are reserved
- if the route is blocked, transport waits

After movement:
- reservation is released

### 8.4 Initial simplicity
Do not build advanced optimization in the first version.
Use deterministic and simple rules first.

---

## 9. Machine Logic Rules

The first machine is Roll Press.

### Behavior
- if empty, machine may request input
- when lot arrives, processing starts
- processing goes through configured steps
- after final step, machine sets output ready
- output waits until a transport picks it

### Simplification
For first implementation:
- one machine processes one active lot at a time
- input lot becomes output lot with same lot id
- one output lot per input lot
- no scrap/rework branch yet

---

## 10. Transport Dispatch Rules

Use simple deterministic dispatching.

### Input dispatch
When machine needs input:
- choose the first idle transport that:
  - is available
  - is not carrying a lot
  - has a valid route
  - is not blocked by reservation

### Output dispatch
When machine output is ready:
- choose the first idle transport that:
  - is available
  - is not carrying a lot
  - has a valid route
  - can reserve the required route

### Return rule
After task completion:
- transport returns to charging station
- use default charging station or nearest configured one

---

## 11. UI Style and Presentation Rules

## Important:
The UI reference previously discussed is used **only as an abstract design pattern**.

Do not copy any raw text, project names, code fragments, screenshots, or source-specific implementation details from any external reference.

Only keep these abstract ideas:
- strict separation between View and ViewModel
- modular dashboard
- panel-based UI
- popup/detail windows separated from main dashboard
- event-driven UI updates
- business logic must not live inside UI components

### UI goals
The dashboard must show:
- factory canvas
- routes
- warehouse/stocker nodes
- charging station nodes
- machine nodes
- transport movement
- transport status
- machine status
- current lot id
- event log
- legend
- control buttons

### Main dashboard areas
1. Factory canvas
2. Machine detail panel
3. Transport detail panel
4. Event log panel
5. Legend panel
6. Control panel

---

## 12. Presentation Structure in FR.UI

FR.UI should follow an MVVM-inspired presentation structure.

Suggested folders:

- `Pages/`
- `Components/`
- `ViewModels/Base/`
- `ViewModels/Dashboard/`
- `ViewModels/Panels/`
- `ViewModels/Popup/`
- `ViewModels/Navigation/`
- `Services/`
- `Realtime/`
- `State/`

### Rules
- UI components must not contain business logic
- realtime updates must go through presentation services
- page state must come from ViewModels
- panel state must be isolated in panel ViewModels
- popup/detail state must be isolated from dashboard state
- commands must be separated from rendering
- Razor/component code should stay thin

---

## 13. Config-Driven System

All runtime generation comes from JSON config files under `/config`.

Suggested files:
- `factory-layout.json`
- `factory-routes.json`
- `factory-process.json`
- `factory-simulation.json`

### factory-layout.json
Defines:
- nodes/locations
- display names
- coordinates
- ports
- types

### factory-routes.json
Defines:
- valid route edges
- source node/port
- destination node/port
- optional distance/cost

### factory-process.json
Defines:
- machine configs
- step list
- durations
- processing behavior

### factory-simulation.json
Defines:
- transport units
- startup positions
- tick interval
- initial scenario settings
- optional seeded lots

---

## 14. Example Initial Feature Set

The first build should support:
- 1 input warehouse/stocker
- 1 output warehouse/stocker
- 2 charging stations
- 1 Roll Press machine
- 2 transport units
- 1 route network
- 1 lot flow
- 8 machine steps
- 1 dashboard page
- realtime updates
- start/pause/reset controls

This is the first MVP.

---

## 15. Project Coding Rules

### General rules
- no database
- no ORM
- no external queue/broker
- in-memory runtime state only
- JSON config only
- small focused classes
- deterministic behavior first
- buildable after each issue
- no speculative abstractions unless needed
- keep naming consistent
- prefer composition over large god-classes

### Error handling
- fail fast on invalid config
- surface clear validation messages
- log simulation-level events
- avoid swallowing exceptions silently

### Testing mindset
- keep engines testable
- separate config DTOs from runtime models
- separate planning/dispatch logic from UI
- keep deterministic scenario for repeatable testing

---

## 16. Expected Main Runtime Classes

### FR.Common
Possible types:
- TransportStatus
- MachineStatus
- LotStatus
- PortType
- TaskType
- Result
- ErrorDetail
- Point2D
- TickOptions
- CommonConstants

### FR.EIFData
Possible types:
- FactoryLayoutConfig
- LocationConfig
- PortConfig
- RouteConfig
- MachineConfig
- MachineStepConfig
- TransportConfig
- SimulationConfig

### FR.IO
Possible types:
- IConfigLoader
- JsonConfigLoader
- ConfigValidationResult

### FR.Biz
Possible types:
- FactoryState
- FactoryStateFactory
- LocationNode
- PortNode
- RouteGraph
- RouteEdge
- TransportUnit
- MachineUnit
- MachineStep
- Lot
- TransportTask
- EventLogItem
- IRoutePlanner
- RoutePlanner
- IRouteReservationManager
- RouteReservationManager
- ITransportEngine
- TransportEngine
- IMachineEngine
- RollPressMachineEngine
- ITransportDispatcher
- TransportDispatcher
- SimulationOrchestrator
- SnapshotBuilder

### FR.UI
Possible types:
- FactoryHub
- SimulationBackgroundService
- FactoryDashboardViewModel
- MachinePanelViewModel
- TransportPanelViewModel
- FactoryCanvasViewModel
- EventLogPanelViewModel
- LegendPanelViewModel
- Popup view models
- Presentation services
- dashboard pages/components

---

## 17. Expected Realtime Snapshot

The UI should receive a lightweight snapshot, not the entire engine internals.

Example contents:
- timestamp
- simulation state
- transport summaries
- machine summaries
- lot summaries
- route occupancy summary
- recent event log items

The snapshot should be optimized for UI rendering.

---

## 18. Realtime Event Philosophy

The engine may internally use event-driven transitions.

Example event categories:
- machine requested input
- transport assigned
- transport arrived at pickup
- lot picked
- transport arrived at drop
- lot dropped
- machine step changed
- machine completed processing
- output ready
- output picked
- output stored
- transport returned to charging

These events should also feed the event log shown in UI.

---

## 19. UI Detail Requirements

### Factory canvas
Must visually show:
- location blocks
- route lines
- current transport positions
- machine node state
- selected item highlight

### Machine panel
Must show:
- machine id
- status
- current lot id
- current step
- total steps
- progress if available
- needs input / output ready state

### Transport panel
Must show:
- transport id
- status
- current task
- current lot id
- current location
- route/waiting state

### Event log
Must show:
- latest events first
- readable timestamps
- readable event text

### Legend
Must show clear mapping for statuses such as:
- idle
- moving
- waiting
- carrying lot
- processing
- error

---

## 20. Control Actions

The dashboard must support:
- Start
- Pause
- Reset

Later phases may support:
- Step once
- Inject lot
- Manual dispatch
- Force machine state
- Simulation speed control

But the first implementation only requires:
- Start
- Pause
- Reset

---

## 21. Implementation Workflow

Development is done **issue by issue**.

Each issue must:
- be focused
- keep the solution buildable
- not prematurely implement later issues
- include small clear deliverables
- leave the repository in a stable state

### Standard rule for Codex
When working on an issue:
- implement only that issue
- add only the minimum dependency needed
- do not redesign unrelated modules
- explain files changed
- explain build/run steps
- explain what remains for next issue

---

## 22. Canonical Issue Order

Recommended implementation order:

1. bootstrap solution
2. create shared primitives
3. create config DTOs
4. create JSON loader and validation
5. create runtime state models
6. create route graph and reservation
7. create transport engine
8. create machine engine
9. create dispatcher and orchestrator
10. create host backend
11. create UI shell
12. create factory canvas and legend
13. create machine/transport/event panels
14. add control actions
15. finalize end-to-end demo
16. cleanup and documentation

This order should be kept unless there is a strong reason to change it.

---

## 23. Mandatory Codex Working Rules

Codex must follow these rules:

- Work on one issue at a time
- Keep code compiling
- Prefer clear, readable code
- Keep classes focused
- Do not add database code
- Do not add auth
- Do not add broker/event bus
- Do not add advanced infra not required yet
- Do not copy external project/source text
- Use the abstract architecture only
- Keep UI logic separate from business logic
- Keep runtime state in memory
- Use JSON config as source of generation

---

## 24. Master Prompt for Codex

Use the following baseline instructions when starting a new issue:

"You are working in a .NET solution named FR.

Architecture rules:
- No database.
- Runtime state must be in-memory only.
- Config must come from JSON files under /config.
- Keep project names exactly:
  - FR.Common
  - FR.EIFData
  - FR.IO
  - FR.Biz
  - FR.UI
- FR.Common, FR.EIFData, FR.IO, FR.Biz are libraries.
- FR.UI is the UI host app.
- Keep the code modular and production-friendly.
- Do not implement authentication yet.
- Do not add external message brokers.
- Prefer clean interfaces and small classes.
- Every issue must end in a buildable state.

Presentation rules:
- Use an MVVM-inspired UI structure in FR.UI.
- Separate pages/components from presentation state.
- Create ViewModels under FR.UI/ViewModels.
- UI components must not contain business logic.
- Realtime data must flow through presentation services before reaching components.
- Popup/detail views must have their own view models.

Important domain:
- Factory has warehouse/stocker, charging stations, roll press, transport units, ports, routes.
- Transport moves only on predefined routes.
- Roll press requests input material, then processes multiple steps, then outputs finished lot.
- UI must show legend, machine status, transport status, and current lot id in realtime.

Return:
1. files created/updated
2. brief design note
3. how to run/build
4. what remains for the next issue"

### End every issue prompt with:
"Important:
- Work on this issue only.
- Do not implement future issues yet.
- Keep the code compiling.
- If a dependency is missing, add only the minimum needed stub.
- Explain any tradeoff briefly."

---

## 25. Definition of Done for the First MVP

The MVP is done when:
- solution builds successfully
- config loads successfully
- initial runtime state is generated from JSON
- simulation can start
- machine requests input
- transport picks and delivers input lot
- machine processes the lot through configured steps
- machine marks output ready
- transport picks and delivers output lot
- transport returns to charging station
- UI shows transport and machine state in real time
- event log is readable
- start/pause/reset works

---

## 26. What Must Be Preserved

The following must remain true across future refactors:
- no DB dependency in early phase
- route-constrained transport movement
- deterministic baseline demo
- strict separation of business logic and presentation logic
- config-driven factory generation
- issue-by-issue delivery discipline

---

## 27. What Can Be Added Later

Later backlog may include:
- multiple machines
- multiple production lines
- more transport units
- richer routing
- priority dispatching
- battery logic
- charging duration
- alarms
- operator manual override
- live integration
- history/replay
- analytics
- performance optimization

These are future phases, not MVP requirements.

---

## 28. Final Instruction to Codex

Build the project incrementally.
Do not skip foundations.
Do not over-engineer early issues.
Get the engine stable first.
Then get realtime delivery stable.
Then get UI stable.
Then improve polish.

This repository is for a clean, understandable, and extensible real-time factory simulation baseline.