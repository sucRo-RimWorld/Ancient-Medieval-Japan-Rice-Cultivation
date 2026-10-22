using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using AncientMedievalJapan.RiceCultivation;
using AncientMedievalJapan.Waterworks;
using RimWorld;
using RimWorks.Pickle;
using Verse;
using Verse.AI;

namespace AncientMedievalJapan.RiceCultivation.E2E
{
    internal static class OnGameThread
    {
        public static Task Run(Action action)
        {
            var completed = new TaskCompletionSource<bool>();
            Type driver = typeof(PickleContext).Assembly.GetType("RimWorks.Pickle.Runtime.PickleDriver", true);
            MethodInfo post = driver.GetMethod("Post", BindingFlags.Public | BindingFlags.Static,
                null, new[] { typeof(Action) }, null);
            if (post == null) throw new MissingMethodException(driver.FullName, "Post");
            post.Invoke(null, new object[] { (Action)delegate
            {
                try
                {
                    if (!UnityData.IsInMainThread) throw new InvalidOperationException("Not game thread");
                    action();
                    completed.SetResult(true);
                }
                catch (Exception ex) { completed.SetException(ex); }
            }});
            return completed.Task;
        }
    }

    [PickleSteps]
    public sealed class RiceSteps
    {
        private IntVec3 position = IntVec3.Invalid;
        private IntVec3 inlet = IntVec3.Invalid;
        private IntVec3 source = IntVec3.Invalid;
        private int constructedId, existingZoneId, incompleteId;
        private float incompleteWork;

        private IntVec3 At(int x, int z) { return position + new IntVec3(x, 0, z); }

        [Given("a pawn constructs a rice paddy and zone interactions preserve their owners")]
        public async Task Prepare(PickleContext ctx)
        {
            Pawn first = null, second = null;
            float startingWork = 0f;
            TimeSpeed prior = TimeSpeed.Normal;
            await OnGameThread.Run(delegate
            {
                Map map = Find.CurrentMap;
                ctx.Require(map != null && map.Size.x >= 40, "Missing original 1.6 Quickstart map");
                var paddy = map.GetComponent<PaddyMapComponent>();
                var water = map.GetComponent<CanalMapComponent>();
                ctx.Require(paddy != null && water != null, "Missing production map components");
                bool found = false;
                for (int x = 10; x < map.Size.x - 12 && !found; x++)
                    for (int z = 10; z < map.Size.z - 9 && !found; z++)
                    {
                        bool open = true;
                        for (int dx = -3; dx <= 7 && open; dx++)
                            for (int dz = -1; dz <= 4; dz++)
                            {
                                var cell = new IntVec3(x + dx, 0, z + dz);
                                if (cell.Fogged(map) || cell.GetFirstBuilding(map) != null ||
                                    map.terrainGrid.FoundationAt(cell) != null ||
                                    map.terrainGrid.UnderTerrainAt(cell) != null ||
                                    map.zoneManager.ZoneAt(cell) != null)
                                { open = false; break; }
                            }
                        if (open) { position = new IntVec3(x, 0, z); found = true; }
                    }
                ctx.Require(found, "No empty revealed 1.6 fixture area");
                for (int dx = -3; dx <= 7; dx++)
                    for (int dz = -1; dz <= 4; dz++)
                    {
                        IntVec3 cell = At(dx, dz);
                        foreach (Thing thing in new List<Thing>(cell.GetThingList(map)))
                            if (thing is Plant) thing.Destroy(DestroyMode.Vanish);
                        map.terrainGrid.SetTerrain(cell, TerrainDefOf.Soil);
                    }
                inlet = At(-1, 0);
                source = At(-2, 0);
                map.terrainGrid.SetTerrain(source,
                    DefDatabase<TerrainDef>.GetNamed("WaterMovingShallow"));
                water.Recalculate();
                ctx.Require(water.Dig(inlet), "Could not dig Waterworks supply");
                water.Recalculate();
                ctx.Require(water.CanDrawWaterFromCanalAt(inlet), "Canal has no uncovered supply");
                CellRect main = new CellRect(position.x, position.z, 3, 3);
                ctx.Require(paddy.AddRectangle(main), "Could not designate 3x3 rice plot");
                constructedId = paddy.PlotAt(position).id;
                ctx.Assert(!paddy.PlotById(constructedId).complete &&
                    map.zoneManager.ZoneAt(position) == null,
                    "Plot designation unexpectedly completed or created zone");

                // An adjacent, independently identified field with a partial
                // existing growing zone must not rewrite that existing zone.
                var existing = new Zone_Growing(map.zoneManager);
                map.zoneManager.RegisterZone(existing);
                existing.AddCell(At(3, 0));
                existing.SetPlantDefToGrow(ThingDefOf.Plant_Potato);
                ctx.Require(paddy.AddRectangle(new CellRect(position.x + 3, position.z, 2, 2)),
                    "Could not designate adjacent partially zoned field");
                var partial = paddy.PlotAt(At(3, 0));
                existingZoneId = partial.id;
                ctx.Require(paddy.AdvanceWork(partial.id, partial.workAnchor, partial.workLeft + 1f),
                    "Could not complete adjacent zone fixture");
                ctx.Assert(existing.CellCount == 1 &&
                    existing.GetPlantDefToGrow() == ThingDefOf.Plant_Potato &&
                    map.zoneManager.ZoneAt(At(4, 0)) == null,
                    "Existing partial growing zone was changed");

                for (int i = 0; i < 30 && (first == null || second == null); i++)
                {
                    Pawn worker = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
                    if (worker.Downed || worker.skills == null ||
                        worker.WorkTypeIsDisabled(WorkTypeDefOf.Construction))
                    { worker.Destroy(DestroyMode.Vanish); continue; }
                    if (first == null) first = worker; else second = worker;
                }
                ctx.Require(first != null && second != null, "No two Construction pawns");
                GenSpawn.Spawn(first, At(0, 4), map);
                GenSpawn.Spawn(second, At(2, 4), map);
                first.workSettings.EnableAndInitializeIfNotAlreadyInitialized();
                second.workSettings.EnableAndInitializeIfNotAlreadyInitialized();
                first.skills.GetSkill(SkillDefOf.Construction).Level = 15;
                if (first.needs.food != null) first.needs.food.CurLevelPercentage = 1f;
                if (first.needs.rest != null) first.needs.rest.CurLevelPercentage = 1f;

                var giver = (WorkGiver_Scanner)
                    DefDatabase<WorkGiverDef>.GetNamed("AMJR_ConstructPaddyWork").Worker;
                Job job = giver.JobOnCell(first, position);
                ctx.Require(job != null && job.def.defName == "AMJR_ConstructPaddy" &&
                    job.count == constructedId && job.targetA.Cell == position,
                    "Incorrect production Construction job targets");
                prior = Find.TickManager.CurTimeSpeed;
                Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
                job.playerForced = true;
                first.jobs.StartJob(job, JobCondition.InterruptForced);
                ctx.Require(first.CurJob == job, "Construction pawn failed to start job");
                ctx.Assert(giver.JobOnCell(second, At(1, 0)) == null,
                    "Two pawns concurrently reserved the same plot");
                startingWork = paddy.PlotById(constructedId).workLeft;
            });
            bool advanced = false;
            for (int batch = 0; batch < 35 && !advanced; batch++)
            {
                await OnGameThread.Run(delegate
                {
                    var plot = Find.CurrentMap.GetComponent<PaddyMapComponent>().PlotById(constructedId);
                    for (int tick = 0; tick < 32 && plot.workLeft >= startingWork; tick++)
                        Find.TickManager.DoSingleTick();
                    advanced = plot.workLeft < startingWork;
                });
                if (!advanced) await Task.Delay(1);
            }
            await OnGameThread.Run(delegate
            {
                var manager = Find.CurrentMap.GetComponent<PaddyMapComponent>();
                ctx.Require(advanced && manager.PlotById(constructedId).workLeft > 0f,
                    "Pawn did not make partial progress");
                first.jobs.EndCurrentJob(JobCondition.InterruptForced, false);
                var giver = (WorkGiver_Scanner)
                    DefDatabase<WorkGiverDef>.GetNamed("AMJR_ConstructPaddyWork").Worker;
                Job resume = giver.JobOnCell(first, position);
                ctx.Require(resume != null && resume.count == constructedId,
                    "Interrupted plot job did not resume");
                resume.playerForced = true;
                first.jobs.StartJob(resume, JobCondition.InterruptForced);
                ctx.Require(first.CurJob == resume, "Resumed job did not start");
            });
            bool done = false;
            for (int batch = 0; batch < 100 && !done; batch++)
            {
                await OnGameThread.Run(delegate
                {
                    var plot = Find.CurrentMap.GetComponent<PaddyMapComponent>().PlotById(constructedId);
                    for (int tick = 0; tick < 64 && !plot.complete; tick++)
                        Find.TickManager.DoSingleTick();
                    done = plot.complete;
                });
                if (!done) await Task.Delay(1);
            }
            await OnGameThread.Run(delegate
            {
                Map map = Find.CurrentMap;
                var paddy = map.GetComponent<PaddyMapComponent>();
                ctx.Require(done, "Pawn failed to finish resumed plot");
                var zone = map.zoneManager.ZoneAt(position) as Zone_Growing;
                ctx.Require(zone != null && zone.CellCount == 9 &&
                    zone.GetPlantDefToGrow().defName == "Plant_Rice",
                    "Completed plot missing standard Vanilla rice Zone");
                ctx.Assert(map.terrainGrid.TopTerrainAt(position) == TerrainDefOf.Soil,
                    "Original soil was replaced");
                paddy.RefreshWater();
                ctx.Require(paddy.IsWet(paddy.PlotById(constructedId)) &&
                    !paddy.IsWet(paddy.PlotById(existingZoneId)),
                    "Individual supplied plot state incorrect");
                ctx.Require(map.mapDrawer.SectionAt(position)
                    .GetLayer(typeof(SectionLayer_AMJR_Paddy)) != null,
                    "Paddy render layer not installed in actual loaded map");
                map.terrainGrid.SetFoundation(inlet, TerrainDefOf.Bridge);
                paddy.RefreshWater();
                ctx.Assert(!paddy.IsWet(paddy.PlotById(constructedId)),
                    "Bridge-covered intake incorrectly wet");
                map.terrainGrid.RemoveFoundation(inlet, false);
                paddy.RefreshWater();
                ctx.Assert(paddy.IsWet(paddy.PlotById(constructedId)),
                    "Uncovered canal intake failed to restore wetness");
                map.terrainGrid.SetTerrain(source, TerrainDefOf.Soil);
                map.GetComponent<CanalMapComponent>().Recalculate();
                paddy.RefreshWater();
                ctx.Assert(!paddy.IsWet(paddy.PlotById(constructedId)),
                    "Disconnected natural water left plot wet");
                map.terrainGrid.SetTerrain(source,
                    DefDatabase<TerrainDef>.GetNamed("WaterMovingShallow"));
                map.GetComponent<CanalMapComponent>().Recalculate();
                paddy.RefreshWater();
                ctx.Assert(paddy.IsWet(paddy.PlotById(constructedId)),
                    "Reconnected water did not wet plot");
                // Build the unfinished fixture *after* the pawn Construction
                // period, not before: otherwise idle pawns naturally complete it
                // during 6400 working ticks, long before disk saving begins.
                Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
                ctx.Require(paddy.AddRectangle(new CellRect(position.x + 6, position.z, 2, 2)),
                    "Could not create unfinished save/reload fixture");
                var pending = paddy.PlotAt(At(6, 0));
                incompleteId = pending.id;
                ctx.Require(paddy.AdvanceWork(incompleteId, pending.workAnchor, 1f),
                    "Could not create resumable saved work");
                incompleteWork = pending.workLeft;
                if (first.CurJob != null) first.jobs.EndCurrentJob(JobCondition.InterruptForced, false);
                // Test-created workers must not autonomously build the *other*
                // unfinished plot while Pickle saves/reloads. That would make
                // comparison to its pre-save work snapshot a false failure.
                first.Destroy(DestroyMode.Vanish);
                second.Destroy(DestroyMode.Vanish);
                Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
                Log.Message("AMJR_NATIVE16_PENDING_BEFORE_SAVE id=" + incompleteId +
                    " left=" + paddy.PlotById(incompleteId).workLeft +
                    " complete=" + paddy.PlotById(incompleteId).complete);
                Log.Message("AMJR_NATIVE16_BEFORE_SAVE_OK");
            });
        }

        [Then("rice plots, source water and growing zones survive the real reload")]
        public Task Verify(PickleContext ctx)
        {
            return OnGameThread.Run(delegate
            {
                Map map = Find.CurrentMap;
                ctx.Require(map != null, "No map after Pickle disk save/reload");
                var manager = map.GetComponent<PaddyMapComponent>();
                var water = map.GetComponent<CanalMapComponent>();
                ctx.Require(manager != null && water != null, "Missing map components after reload");
                var finished = manager.PlotById(constructedId);
                var partial = manager.PlotById(existingZoneId);
                var pending = manager.PlotById(incompleteId);
                ctx.Require(finished != null && partial != null && pending != null,
                    "Unique plot IDs did not persist");
                Log.Message("AMJR_NATIVE16_PENDING_AFTER_RELOAD id=" + pending.id +
                    " left=" + pending.workLeft + " expected=" + incompleteWork +
                    " complete=" + pending.complete + " others=" +
                    finished.complete + "/" + partial.complete);
                ctx.Assert(finished.complete && partial.complete && !pending.complete &&
                    Math.Abs(pending.workLeft - incompleteWork) < 0.01f,
                    "Plot work progress/completion did not persist: current=" +
                    pending.workLeft + " expected=" + incompleteWork +
                    " complete=" + pending.complete);
                var riceZone = map.zoneManager.ZoneAt(position) as Zone_Growing;
                ctx.Require(riceZone != null && riceZone.CellCount == 9 &&
                    riceZone.GetPlantDefToGrow().defName == "Plant_Rice",
                    "Vanilla growing Zone missing or recreated after load");
                var potato = map.zoneManager.ZoneAt(At(3, 0)) as Zone_Growing;
                ctx.Assert(potato != null && potato.CellCount == 1 &&
                    potato.GetPlantDefToGrow() == ThingDefOf.Plant_Potato &&
                    map.zoneManager.ZoneAt(At(4, 0)) == null,
                    "Preexisting partial growing zone changed on load");
                ctx.Assert(map.terrainGrid.TopTerrainAt(position) == TerrainDefOf.Soil,
                    "Soil did not survive save/reload");
                water.Recalculate();
                manager.RefreshWater();
                ctx.Assert(manager.IsWet(finished) && !manager.IsWet(partial),
                    "Water supply and independent plot state incorrect after reload");
                ctx.Require(map.mapDrawer.SectionAt(position)
                    .GetLayer(typeof(SectionLayer_AMJR_Paddy)) != null,
                    "Loaded game lacks paddy drawing layer");
                Log.Message("AMJR_NATIVE16_RELOADED_OK");
            });
        }
    }
}
