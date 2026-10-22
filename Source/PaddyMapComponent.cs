using System.Collections.Generic;
using AncientMedievalJapan.Waterworks;
using RimWorld;
using Verse;

namespace AncientMedievalJapan.RiceCultivation
{
    public sealed class PaddyMapComponent : MapComponent
    {
        private List<PaddyPlot> plots = new List<PaddyPlot>();
        private readonly Dictionary<int, PaddyPlot> byId = new Dictionary<int, PaddyPlot>();
        private readonly Dictionary<IntVec3, PaddyPlot> byCell = new Dictionary<IntVec3, PaddyPlot>();
        private readonly Dictionary<IntVec3, PaddyPlot> byAnchor = new Dictionary<IntVec3, PaddyPlot>();
        private readonly HashSet<int> wetIds = new HashSet<int>();
        private int nextId = 1;

        public IEnumerable<PaddyPlot> Plots { get { return plots; } }

        public PaddyMapComponent(Map map) : base(map) { }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref plots, "AMJR_plots", LookMode.Deep);
            Scribe_Values.Look(ref nextId, "AMJR_nextId", 1);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && plots == null)
                plots = new List<PaddyPlot>();
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            RebuildIndices();
            RefreshWater();
        }

        private void RebuildIndices()
        {
            byId.Clear();
            byCell.Clear();
            byAnchor.Clear();
            wetIds.Clear();
            int highest = 0;
            foreach (PaddyPlot plot in plots)
            {
                if (plot == null || plot.id <= 0 || plot.width <= 0 || plot.height <= 0)
                    continue;
                byId[plot.id] = plot;
                byAnchor[plot.workAnchor] = plot;
                foreach (IntVec3 c in plot.Bounds)
                    if (c.InBounds(map)) byCell[c] = plot;
                if (plot.id > highest) highest = plot.id;
            }
            if (nextId <= highest) nextId = highest + 1;
        }

        public PaddyPlot PlotAt(IntVec3 c)
        {
            PaddyPlot plot;
            byCell.TryGetValue(c, out plot);
            return plot;
        }

        public PaddyPlot PlotById(int id)
        {
            PaddyPlot plot;
            byId.TryGetValue(id, out plot);
            return plot;
        }

        public PaddyPlot PlotAtAnchor(IntVec3 c)
        {
            PaddyPlot plot;
            byAnchor.TryGetValue(c, out plot);
            return plot;
        }

        public bool IsWet(PaddyPlot plot)
        {
            return plot != null && plot.complete && wetIds.Contains(plot.id);
        }

        public bool CanPlaceCell(IntVec3 c)
        {
            if (!c.InBounds(map) || byCell.ContainsKey(c) ||
                !Designator_ZoneAdd.IsZoneableCell(c, map).Accepted)
                return false;
            Zone zone = map.zoneManager.ZoneAt(c);
            if (zone != null && !(zone is Zone_Growing)) return false;
            TerrainDef ground = map.terrainGrid.TopTerrainAt(c);
            if (ground == null || ground.IsWater || ground.IsIce || ground.IsFloor)
                return false;
            CanalMapComponent canals = map.GetComponent<CanalMapComponent>();
            if (canals != null && canals.IsCanal(c)) return false;
            ThingDef rice = DefDatabase<ThingDef>.GetNamedSilentFail("Plant_Rice");
            return rice != null && rice.plant != null &&
                c.GetFertility(map) >= rice.plant.fertilityMin;
        }

        public bool CanPlaceRectangle(CellRect area)
        {
            if (area.Width <= 0 || area.Height <= 0) return false;
            foreach (IntVec3 c in area)
                if (!CanPlaceCell(c)) return false;
            return true;
        }

        public bool AddRectangle(CellRect area)
        {
            if (!CanPlaceRectangle(area)) return false;
            // PROTOTYPE ONLY. This work amount is NOT approved labor balance.
            float work = area.Width * area.Height * 100f +
                         (area.Width + area.Height) * 50f;
            var plot = new PaddyPlot(nextId++, area, work);
            plots.Add(plot);
            byId.Add(plot.id, plot);
            byAnchor.Add(plot.workAnchor, plot);
            foreach (IntVec3 c in area)
                byCell.Add(c, plot);
            DirtyVisual(plot);
            return true;
        }

        public bool Remove(int id)
        {
            PaddyPlot plot = PlotById(id);
            if (plot == null) return false;
            plots.Remove(plot);
            byId.Remove(id);
            byAnchor.Remove(plot.workAnchor);
            foreach (IntVec3 c in plot.Bounds) byCell.Remove(c);
            wetIds.Remove(id);
            DirtyVisual(plot);
            // The original Terrain and Vanilla Growing Zone are never modified.
            return true;
        }

        public bool IsClearForConstruction(PaddyPlot plot)
        {
            if (plot == null || plot.complete) return false;
            foreach (IntVec3 c in plot.Bounds)
            {
                if (!c.InBounds(map) || c.GetPlant(map) != null ||
                    c.GetFirstBuilding(map) != null)
                    return false;
                // A formerly suitable field might have been replaced by a canal.
                if (map.GetComponent<CanalMapComponent>().IsCanal(c))
                    return false;
            }
            return true;
        }

        public bool IsCurrentWork(int id, IntVec3 anchor)
        {
            PaddyPlot plot = PlotById(id);
            return plot != null && !plot.complete && plot.workAnchor == anchor;
        }

        public bool AdvanceWork(int id, IntVec3 anchor, float amount)
        {
            if (!IsCurrentWork(id, anchor)) return false;
            PaddyPlot plot = PlotById(id);
            if (!IsClearForConstruction(plot)) return false;
            plot.workLeft -= amount;
            if (plot.workLeft <= 0f)
            {
                plot.workLeft = 0f;
                plot.complete = true;
                FinishPlot(plot);
            }
            return true;
        }

        private void FinishPlot(PaddyPlot plot)
        {
            // Never modify or merge any existing Zone, including partial overlap.
            foreach (IntVec3 c in plot.Bounds)
                if (map.zoneManager.ZoneAt(c) != null) { DirtyVisual(plot); RefreshWater(); return; }

            // A changed/blocked footprint is left for the player to zone manually.
            foreach (IntVec3 c in plot.Bounds)
                if (!Designator_ZoneAdd.IsZoneableCell(c, map).Accepted)
                { DirtyVisual(plot); RefreshWater(); return; }

            var zone = new Zone_Growing(map.zoneManager);
            map.zoneManager.RegisterZone(zone);
            foreach (IntVec3 c in plot.Bounds) zone.AddCell(c);
            ThingDef rice = DefDatabase<ThingDef>.GetNamedSilentFail("Plant_Rice");
            if (rice != null) zone.SetPlantDefToGrow(rice);
            DirtyVisual(plot);
            RefreshWater();
        }

        public override void MapComponentTick()
        {
            if (Find.TickManager.TicksGame % 251 == 0) RefreshWater();
        }

        public void RefreshWater()
        {
            CanalMapComponent canal = map.GetComponent<CanalMapComponent>();
            if (canal == null) return;
            foreach (PaddyPlot plot in plots)
            {
                bool wet = plot.complete && TouchesSource(plot, canal);
                if (wet)
                {
                    if (wetIds.Add(plot.id)) DirtyVisual(plot);
                }
                else if (wetIds.Remove(plot.id))
                    DirtyVisual(plot);
            }
        }

        private bool TouchesSource(PaddyPlot plot, CanalMapComponent canal)
        {
            foreach (IntVec3 cell in plot.Bounds)
            {
                foreach (IntVec3 d in GenAdj.CardinalDirections)
                {
                    IntVec3 near = cell + d;
                    if (!near.InBounds(map) || byCell.ContainsKey(near)) continue;
                    if (canal.CanDrawWaterFromCanalAt(near) ||
                        (map.terrainGrid.FoundationAt(near) == null &&
                         canal.HasNaturalSourceAt(near)))
                        return true;
                }
            }
            return false;
        }

        private void DirtyVisual(PaddyPlot plot)
        {
            if (map.mapDrawer == null) return;
            foreach (IntVec3 c in plot.Bounds)
                if (c.InBounds(map))
                    map.mapDrawer.MapMeshDirty(c, MapMeshFlagDefOf.Terrain, false, true);
        }
    }
}