using System.Collections.Generic;
using Verse;

namespace AncientMedievalJapan.RiceCultivation
{
    // Stable one-rectangle plot. Neither the terrain nor growing Zone is owned here.
    public sealed class PaddyPlot : IExposable
    {
        public int id;
        public int minX, minZ, maxX, maxZ;
        public int anchorX, anchorZ;
        public float workLeft;
        public bool completed;
        public PaddyPlot() { }
        public PaddyPlot(int id, CellRect area, float work)
        {
            this.id = id;
            minX = area.minX; maxX = area.maxX;
            minZ = area.minZ; maxZ = area.maxZ;
            anchorX = area.minX; anchorZ = area.minZ;
            workLeft = work;
        }
        public IntVec3 Anchor => new IntVec3(anchorX, 0, anchorZ);
        public CellRect Rect => new CellRect(minX, minZ, maxX - minX + 1, maxZ - minZ + 1);
        public IEnumerable<IntVec3> Cells => Rect.Cells;
        public bool Contains(IntVec3 cell) =>
            cell.x >= minX && cell.x <= maxX && cell.z >= minZ && cell.z <= maxZ;
        public void ExposeData()
        {
            Scribe_Values.Look(ref id, "id");
            Scribe_Values.Look(ref minX, "minX");
            Scribe_Values.Look(ref maxX, "maxX");
            Scribe_Values.Look(ref minZ, "minZ");
            Scribe_Values.Look(ref maxZ, "maxZ");
            Scribe_Values.Look(ref anchorX, "anchorX");
            Scribe_Values.Look(ref anchorZ, "anchorZ");
            Scribe_Values.Look(ref workLeft, "workLeft");
            Scribe_Values.Look(ref completed, "completed");
        }
    }
}
