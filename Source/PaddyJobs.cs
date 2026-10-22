using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace AncientMedievalJapan.RiceCultivation
{
    // Exactly one stable reserved anchor per paddy, so separate pawns cannot
    // work on different cells of the same plot at the same time.
    public sealed class WorkGiver_ConstructPaddy : WorkGiver_Scanner
    {
        public override IEnumerable<IntVec3> PotentialWorkCellsGlobal(Pawn pawn)
        {
            if (pawn.Map == null) yield break;
            PaddyMapComponent plots = pawn.Map.GetComponent<PaddyMapComponent>();
            foreach (PaddyPlot plot in plots.Plots)
            {
                if (plot.complete || !plots.IsClearForConstruction(plot)) continue;

                // The reserved anchor is stable for all pawns, but need not
                // itself be reachable. Scan from a reachable cell of this plot.
                foreach (IntVec3 workCell in plot.Bounds)
                {
                    if (workCell.IsForbidden(pawn) ||
                        !pawn.CanReach(workCell, PathEndMode.Touch, pawn.NormalMaxDanger()))
                        continue;
                    yield return workCell;
                    break;
                }
            }
        }

        public override Job JobOnCell(Pawn pawn, IntVec3 cell, bool forced = false)
        {
            if (pawn.Map == null) return null;
            PaddyMapComponent manager = pawn.Map.GetComponent<PaddyMapComponent>();
            PaddyPlot plot = manager.PlotAt(cell);
            if (plot == null || !manager.IsClearForConstruction(plot)) return null;
            if (!pawn.CanReserve(plot.workAnchor, 1, -1, null, forced)) return null;
            if (cell.IsForbidden(pawn) ||
                !pawn.CanReach(cell, PathEndMode.Touch, pawn.NormalMaxDanger())) return null;
            // TargetA: single canonical reservation per plot.
            // TargetB: an actually reachable cell for walking/working.
            Job job = JobMaker.MakeJob(AMJR_Defs.AMJR_ConstructPaddy, plot.workAnchor, cell);
            // Dedicated job: count is not an item-stack count. Immutable identity
            // guards cancellation followed by rebuilding at the same coordinates.
            job.count = plot.id;
            return job;
        }
    }

    public sealed class JobDriver_ConstructPaddy : JobDriver
    {
        private PaddyMapComponent Manager
        {
            get { return Map.GetComponent<PaddyMapComponent>(); }
        }

        private bool IsCurrent()
        {
            return Manager.IsCurrentWork(job.count, TargetLocA);
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return IsCurrent() &&
                   pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(() => !IsCurrent() ||
                !Manager.IsClearForConstruction(Manager.PlotById(job.count)));

            yield return Toils_Goto.GotoCell(TargetIndex.B, PathEndMode.Touch);

            Toil work = ToilMaker.MakeToil("AMJR_PaddyWork");
            work.defaultCompleteMode = ToilCompleteMode.Never;
            work.tickIntervalAction = delegate(int delta)
            {
                if (!IsCurrent()) { ReadyForNextToil(); return; }
                // This only drives work progress; final labor coefficients
                // remain separately unapproved and outside this JobDriver.
                float amount = pawn.GetStatValue(StatDefOf.ConstructionSpeed) * delta;
                if (!Manager.AdvanceWork(job.count, TargetLocA, amount))
                {
                    ReadyForNextToil();
                    return;
                }
                PaddyPlot current = Manager.PlotById(job.count);
                if (current == null || current.complete)
                    ReadyForNextToil();
            };
            work.WithProgressBar(TargetIndex.B, () =>
            {
                PaddyPlot plot = Manager.PlotById(job.count);
                if (plot == null) return 1f;
                float total = PaddyWorkPrototype.Required(plot.Bounds);
                return total <= 0f ? 1f :
                    UnityEngine.Mathf.Clamp01(1f - plot.workLeft / total);
            }, interpolateBetweenActorAndTarget: true);
            work.activeSkill = () => SkillDefOf.Construction;
            yield return work;
        }
    }
}