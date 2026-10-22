# Rice Cultivation coordination

**2026-10-22 / Owner:** sucRo-RimWorld/Ancient-Medieval-Japan-Rice-Cultivation.

**Source of truth:** [Docs/Design.md](Design.md). Project [prior-art entry](https://github.com/sucRo-RimWorld/Ancient-Medieval-Japan-Project/blob/main/Docs/Research/RicePaddyIndependentImplementationDecision-2026-10-22.json) is READY, with live 1.6 RFE/ZP XML evidence and checks.

**Implementation target:** approved B variant, 1 saved work progress per plot, unique plot ID, first-pass 1 job per plot via common stable anchor reservation. Workgiver uses existing Construction category. Vanilla Zone_Growing only, without rewriting existing zones. Mandatory Waterworks exposed wet canal / natural source queries, Grains optional.

**Unapproved / deferred:** construction/material coefficients, rice growth bonus, full 1.7 support, complex graphics/art acceptance, live game acceptance. No load/save or build PASS assumed from source review. Construction workload constant, if present in prototype, is an engineering placeholder and not approved balance.
