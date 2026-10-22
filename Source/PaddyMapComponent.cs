using System.Collections.Generic;
using AncientMedievalJapan.Waterworks;
using RimWorld;
using Verse;

namespace AncientMedievalJapan.RiceCultivation
{
    // Single authority for plot identity, labor, cell lookup, and a cached binary water state.
    public sealed class PaddyMapComponent : MapComponent
    {
        private List<PaddyPlot> plots = new List<PaddyPlot>();
        private int nextId = 1;
        private readonly Dictionary<IntVec3, PaddyPlot> byCell = new Dictionary<IntVec3, PaddyPlot>();
        private readonly Dictionary<int, bool> connected = new Dictionary<int, bool>();
        private int lastWaterCheck = -1;
        // Engineering prototype only, not a user-approved labor balance.
        internal const float ProvisionalWorkPerCell = 300f;
        private const int WaterCheckInterval = 250;

        public PaddyMapComponent(Map map) : base(map) { }
        public IEnumerable<PaddyPlot> Plots => plots;
        public override void ExposeData()
        {
            Scribe_Values.Look(ref nextId, "AMJRC_nextPlotId", 1);
            Scribe_Collections.Look(ref plots, "AMJRC_plots", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (plots == null) plots = new List<PaddyPlot>();
                RebuildIndex();
                lastWaterCheck = -1;
            }
        }
        public override void FinalizeInit()
        {
            RebuildIndex();
            lastWaterCheck = -1;
        }
        private void RebuildIndex()
        {
            byCell.Clear();
            if (plots == null) plots = new List<PaddyPlot>();
            foreach (PaddyPlot plot in plots)
            {
                if (plot == null) continue;
                foreach (IntVec3 c in plot.Cells)
                    if (c.InBounds(map)) byCell[c] = plot;
                if (nextId <= plot.id) nextId = plot.id + 1;
            }
        }
        public PaddyPlot At(IntVec3 c)
        {
            PaddyPlot p;
            return byCell.TryGetValue(c, out p) ? p : null;
        }
        public PaddyPlot ById(int id)
        {
            foreach (PaddyPlot p in plots)
                if (p != null && p.id == id) return p;
            return null;
        }
        public bool CanAdd(CellRect rect)
        {
            if (rect.Width < 1 || rect.Height < 1) return false;
            foreach (IntVec3 c in rect.Cells)
            {
                if (!c.InBounds(map) || c.Fogged(map) || At(c) != null ||
                    !Designator_ZoneAdd.IsZoneableCell(c, map).Accepted ||
                    c.GetFertility(map) < ThingDefOf.Plant_Rice.plant.fertilityMin ||
                    map.terrainGrid.FoundationAt(c) != null || c.GetFirstBuilding(map) != null)
                    return false;
                Zone z = map.zoneManager.ZoneAt(c);
                if (z != null && !(z is Zone_Growing)) return false;
            }
            return true;
        }
        public PaddyPlot Add(CellRect rect)
        {
            if (!CanAdd(rect)) return null;
            var p = new PaddyPlot(nextId++, rect, rect.Area * ProvisionalWorkPerCell);
            plots.Add(p);
            foreach (IntVec3 c in rect.Cells) byCell[c] = p;
            MarkVisuals(p);
            InvalidateWater();
            return p;
        }
        public bool Remove(int id)
        {
            PaddyPlot p = ById(id);
            if (p == null) return false;
            plots.Remove(p);
            foreach (IntVec3 c in p.Cells) byCell.Remove(c);
            MarkVisuals(p);
            InvalidateWater();
            // Never change original TerrainDef or player Growing Zones on removal.
            return true;
        }
        public bool CanWork(PaddyPlot p)
        {
            if (p == null || p.completed || p.workLeft <= 0 || !p.Anchor.InBounds(map))
                return false;
            // Never cut cultivated or wild plants as an implicit side-effect of building.
            foreach (IntVec3 c in p.Cells)
                if (c.GetPlant(map) != null) return false;
            return true;
        }
        public void PerformWork(int plotId, float work)
        {
            PaddyPlot p = ById(plotId);
            if (!CanWork(p) || work <= 0f) return;
            p.workLeft -= work;
            if (p.workLeft > 0f) return;
            p.workLeft = 0f;
            p.completed = true;
            CreateZoneOnlyIfUnzoned(p);
            MarkVisuals(p);
            InvalidateWater();
        }
        private void CreateZoneOnlyIfUnzoned(PaddyPlot p)
        {
            // No partial merge, splitting, auto-recovery, or modification of existing Zones.
            foreach (IntVec3 c in p.Cells)
                if (map.zoneManager.ZoneAt(c) != null ||
                    !Designator_ZoneAdd.IsZoneableCell(c, map).Accepted)
                    return;
            var zone = new Zone_Growing(map.zoneManager);
            map.zoneManager.RegisterZone(zone);
            foreach (IntVec3 c in p.Cells) zone.AddCell(c);
            zone.SetPlantDefToGrow(ThingDefOf.Plant_Rice);
        }
        public override void MapComponentTick()
        {
            if (lastWaterCheck < 0 ||
                Find.TickManager.TicksGame - lastWaterCheck >= WaterCheckInterval)
                RefreshWater();
        }
        public bool HasWater(PaddyPlot p)
        {
            if (p == null || !p.completed) return false;
            if (lastWaterCheck < 0) RefreshWater();
            bool value;
            return connected.TryGetValue(p.id, out value) && value;
        }
        private void InvalidateWater()
        {
            lastWaterCheck = -1;
            connected.Clear();
        }
        private void MarkVisuals(PaddyPlot p)
        {
            if (map.mapDrawer == null) return;
            foreach (IntVec3 c in p.Cells)
                map.mapDrawer.MapMeshDirty(c, MapMeshFlagDefOf.Terrain, true, true);
        }
        private void RefreshWater()
        {
            CanalMapComponent canals = map.GetComponent<CanalMapComponent>();
            var next = new Dictionary<int, bool>();
            foreach (PaddyPlot p in plots)
            {
                if (p == null || !p.completed) continue;
                bool wet = false;
                foreach (IntVec3 c in p.Cells)
                {
                    if (c.x != p.minX && c.x != p.maxX && c.z != p.minZ && c.z != p.maxZ)
                        continue;
                    foreach (IntVec3 direction in GenAdj.CardinalDirections)
                    {
                        IntVec3 source = c + direction;
                        if (!source.InBounds(map) || p.Contains(source)) continue;
                        if (canals != null &&
                            (canals.CanDrawWaterFromCanalAt(source) || canals.HasNaturalSourceAt(source)))
                        { wet = true; break; }
                    }
                    if (wet) break;
                }
                next[p.id] = wet;
                bool before;
                if (!connected.TryGetValue(p.id, out before) || before != wet)
                    MarkVisuals(p);
            }
            connected.Clear();
            foreach (var item in next) connected.Add(item.Key, item.Value);
            lastWaterCheck = Find.TickManager.TicksGame;
        }
    }
}
