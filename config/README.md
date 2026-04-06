# EQFR Config

This folder is reserved for JSON configuration files used to generate the EQFR runtime state at startup.

Config files:
- `factory-layout.json`
- `factory-routes.json`
- `factory-process.json`
- `factory-simulation.json`
- `factory-transport.json`

Notes:
- The first MVP is config-driven and runs fully in memory.
- Avoid adding database or external broker dependencies in early phases.
- `factory-routes.json` defines the only valid transport paths. Node refs are `(locationId, portId)`, so make sure the ports in routes match the ports used by transports, lots, and machine input/output.

