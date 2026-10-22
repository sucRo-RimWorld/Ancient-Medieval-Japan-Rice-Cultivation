# Rice Cultivation coordination

**2026-10-22 / Owner:** sucRo-RimWorld/Ancient-Medieval-Japan-Rice-Cultivation.

**Source of truth:** [Docs/Design.md](Design.md). Project [prior-art entry](https://github.com/sucRo-RimWorld/Ancient-Medieval-Japan-Project/blob/main/Docs/Research/RicePaddyIndependentImplementationDecision-2026-10-22.json) is READY, with live 1.6 RFE/ZP XML evidence and checks.

**Implementation target:** approved B variant, 1 saved work progress per plot, unique plot ID, first-pass 1 job per plot via common stable anchor reservation. Workgiver uses existing Construction category. Vanilla Zone_Growing only, without rewriting existing zones. Mandatory Waterworks exposed wet canal / natural source queries, Grains optional.

**Current implementation / native acceptance (2026-10-22):** Rice main `c63d3f872e39aa157d0fa31e6dc694e4a501ea36`. Real original Linux 1.6 [Project #39789707915](https://github.com/sucRo-RimWorld/Ancient-Medieval-Japan-Project/actions/runs/39789707915) confirmed Vanilla+Rice+Waterworks and Vanilla+Rice+Waterworks+Grains, each Pickle 1/1 PASS, no skips, game exit 0 and Unity ERROR 0. One real disk save/reload per profile. The unchanged production DLL SHA-256 is `e905e32f2691046c265484165ba122eca12296a38e9a74078b11a2c5cc77ecb7`. Definitive assertions and exclusions: [Docs/Validation/RiceNative16-20261022.md](Validation/RiceNative16-20261022.md).

**Unapproved / deferred:** construction/material balance coefficients, rice growth bonus (unimplemented), full 1.7 support, rendered-art acceptance, whole-season rice economy, MO/CCTO/arbitrary profiles, existing-player-save addition, and public release. The prototype work constant is engineering-only, not accepted balance. Do not repeat the accepted scenario without a concrete affected change or defect.
