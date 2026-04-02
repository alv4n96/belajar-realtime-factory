# EQFR Config

This folder is reserved for JSON configuration files used to generate the EQFR runtime state at startup.

Planned config files (names may evolve):
- `factory-layout.json`
- `factory-routes.json`
- `factory-process.json`
- `factory-simulation.json`

Notes:
- The first MVP is config-driven and runs fully in memory.
- Avoid adding database or external broker dependencies in early phases.

