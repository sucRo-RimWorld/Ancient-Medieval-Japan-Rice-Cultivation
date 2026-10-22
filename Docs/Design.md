# AMJ Rice Cultivation — B construction / initial source design

Owner design migrated from [Project RicePaddyDesign §§1–5](https://github.com/sucRo-RimWorld/Ancient-Medieval-Japan-Project/blob/main/Docs/Research/RicePaddyDesign-2026-10-21.md). Project historical comparisons are not an alternate authority.

## Approved gameplay
- Requires Ancient & Medieval Japan Waterworks. Grains optional; common Vanilla `Plant_Rice` always; no duplicated rice processing.
- Create a rectangular plot with one drag operation; one plot ID even alongside touching plots; no implicit merging.
- A pawn must work before completion. Progress is persistent on the plot, pauses and resumes; only one worker can reserve its stable construction anchor.
- The original terrain and fertility remain intact; bund is at cell edges, not occupying entire planting cells.
- Only after completion, if **all** cells have no Zone, create a Vanilla `Zone_Growing` and select Vanilla rice. If fully/partly zoned, leave all existing zones untouched; user controls missing cells. No automatic zone repair.
- Completed plot detects a cardinal adjacent **exposed supplied canal** or accepted natural source using Waterworks queries. Covered canal withdrawal is prohibited; no manual controls or fluid volumes.
- Other crops may use Vanilla zone naturally and receive no custom water bonus or penalty.

## First-source implementation boundary
- `PaddyMapComponent` saves rectangular bounds, monotonic ID, remaining work, and anchor. Derive cell-index and visual edges on load; no custom plant inventories.
- Initial code uses a bounded construction-work **prototype constant** solely to make the work Job observable. It is not final balance and must be independently validated and author-approved before release.
- The component stores wet/dry connection as transient cache, recalculated at low frequency; drawing changes invalidate terrain mesh. No per-plant source graph queries.
- A `SectionLayer` renders a simple stationary wet overlay and edge bunds; its visual materials are placeholders pending asset review.
- Rice growth coefficient **not implemented** until CCTO/Grains crop numbers are settled; no invented bonus.
- If whole plot cells become occupied, work waits rather than destroying crops. Cancelling the plot does not delete Vanilla growing zones or revert soil.
- Read-only source inspection is not compile, game load, gameplay PASS or release acceptance.

## Acceptance after implementation
1. Whole unzoned rectangle -> build -> Vanilla rice zone; no numeric bonus until reviewed.
2. Fully/partially zoned rectangle -> no change to any existing zone.
3. Interrupt, reload, and resume construction; no double job or old-job resurrection after removal/recreate.
4. Exposed connected canal/water -> wet visual; covered/disconnected -> dry, and restored -> wet.
5. Adjacent independently designated plots remain separate; no soil/fertility changes.
6. Grains absent / present loading, Vanilla rice harvest uses respective existing recipe output. No game launch until functional slice ready.
