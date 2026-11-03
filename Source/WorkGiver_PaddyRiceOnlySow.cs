using RimWorld;
using Verse;
using Verse.AI;

namespace AncientMedievalJapan.RiceCultivation
{
    // Reuses Vanilla's sowing work giver and jobs; restricts only completed
    // paddy cells. No new growing zone, work type or Harmony dependency.
    public sealed class WorkGiver_PaddyRiceOnlySow : WorkGiver_GrowerSow
    {
        public override Job JobOnCell(Pawn pawn, IntVec3 cell, bool forced = false)
        {
            if (pawn != null && pawn.Map != null)
            {
                PaddyMapComponent component = pawn.Map.GetComponent<PaddyMapComponent>();
                PaddyPlot plot = component != null ? component.PlotAt(cell) : null;
                if (plot != null && plot.complete)
                {
                    ThingDef wanted = WorkGiver_Grower.CalculateWantedPlantDef(cell, pawn.Map);
                    if (wanted != null && wanted.defName != "Plant_Rice")
                    {
                        if (forced)
                            JobFailReason.Is("AMJR_RiceOnlySow".Translate());
                        return null;
                    }
                }
            }
            return base.JobOnCell(pawn, cell, forced);
        }
    }
}
