using RimWorld;
using Verse;

namespace AncientMedievalJapan.RiceCultivation
{
    // Plot geometry is rectangular by design; keep saves small.
    public sealed class PaddyPlot : IExposable
    {
        public int id;
        public int minX;
        public int minZ;
        public int width;
        public int height;
        public IntVec3 workAnchor;
        public float workLeft;
        public bool complete;

        public PaddyPlot() { }

        public PaddyPlot(int uniqueId, CellRect rect, float work)
        {
            id = uniqueId;
            minX = rect.minX;
            minZ = rect.minZ;
            width = rect.Width;
            height = rect.Height;
            workAnchor = new IntVec3(minX, 0, minZ);
            workLeft = work;
        }

        public CellRect Bounds
        {
            get { return new CellRect(minX, minZ, width, height); }
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref id, "id", 0);
            Scribe_Values.Look(ref minX, "minX", 0);
            Scribe_Values.Look(ref minZ, "minZ", 0);
            Scribe_Values.Look(ref width, "width", 0);
            Scribe_Values.Look(ref height, "height", 0);
            Scribe_Values.Look(ref workAnchor, "workAnchor");
            Scribe_Values.Look(ref workLeft, "workLeft", 0f);
            Scribe_Values.Look(ref complete, "complete", false);
        }
    }
}