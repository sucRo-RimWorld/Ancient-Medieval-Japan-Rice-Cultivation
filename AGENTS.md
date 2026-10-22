# AMJ Rice Cultivation agent rules

1. Read this file and `main:Docs/Coordination.md` before modifying this repository.
2. Read Project [AGENTS.md](https://github.com/sucRo-RimWorld/Ancient-Medieval-Japan-Project/blob/main/AGENTS.md), [Docs/SharedRules.md](https://github.com/sucRo-RimWorld/Ancient-Medieval-Japan-Project/blob/main/Docs/SharedRules.md) and the Project's completed [independent implementation decision](https://github.com/sucRo-RimWorld/Ancient-Medieval-Japan-Project/blob/main/Docs/Research/RicePaddyIndependentImplementationDecision-2026-10-22.json).
3. This repository owns paddy plots, construction jobs, wet/bund visualization, and rice-only paddy growth effects. Waterworks owns supplied canal/natural source state. Grains is optional and owns rice processing.
4. Author-approved B: player rectangle -> pawn construction -> Vanilla growing zone (only when entire footprint unzoned) -> automatic water-state check. No manual flood/drain, soil replacement, cell-sized bund walls, bespoke sow/harvest jobs, or independent Plant_Rice.
5. Unapproved numerical tuning (labor/material/fertility effect) MUST NOT be reported as accepted. Never enable a gameplay bonus solely from a guessed coefficient.
6. Follow protected-main topic branch -> PR -> checks -> merge -> verify SHA. Never write gameplay code in AMJ Project.
7. Static checks before runtime, one consolidated 1.6 loaded-game acceptance only after a playable slice; never auto-trigger RimWorld on PR or push.
