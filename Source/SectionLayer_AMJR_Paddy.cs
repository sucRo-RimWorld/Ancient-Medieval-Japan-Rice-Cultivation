using RimWorld;
using UnityEngine;
using Verse;

namespace AncientMedievalJapan.RiceCultivation
{
    // A static, low-opacity overlay rather than changing terrain or creating
    // one Thing per bund edge. Regenerated on plot/water state changes only.
    public sealed class SectionLayer_AMJR_Paddy : SectionLayer
    {
        private static Material water;
        private static Material bund;

        public override bool Visible
        {
            get { return DebugViewSettings.drawTerrain; }
        }

        public SectionLayer_AMJR_Paddy(Section section) : base(section)
        {
            relevantChangeTypes = MapMeshFlagDefOf.Terrain;
        }

        private static void InitMaterials()
        {
            if (water != null) return;
            water = SolidColorMaterials.NewSolidColorMaterial(
                new Color(0.30f, 0.48f, 0.53f, 0.24f), ShaderDatabase.Transparent);
            bund = SolidColorMaterials.NewSolidColorMaterial(
                new Color(0.35f, 0.26f, 0.16f, 0.88f), ShaderDatabase.Transparent);
            water.renderQueue = 2402;
            bund.renderQueue = 2404;
        }

        public override void Regenerate()
        {
            ClearSubMeshes(MeshParts.All);
            InitMaterials();
            PaddyMapComponent state = Map.GetComponent<PaddyMapComponent>();
            float y = AltitudeLayer.TerrainScatter.AltitudeFor();

            foreach (IntVec3 cell in section.CellRect)
            {
                PaddyPlot plot = state.PlotAt(cell);
                if (plot == null || !plot.complete) continue;

                if (state.IsWet(plot))
                    Quad(GetSubMesh(water), cell, 0f, 0f, 1f, 1f, y + 0.001f);

                // No full-tile bund walls: strips follow the *edge* of each
                // cultivable cell and do not change walkability/fertility.
                // A shared border between independently owned completed plots
                // is drawn once (lower ID), not twice.
                foreach (IntVec3 d in GenAdj.CardinalDirections)
                {
                    PaddyPlot adjacent = state.PlotAt(cell + d);
                    if (adjacent == plot) continue;
                    if (adjacent != null && adjacent.complete &&
                        adjacent.id < plot.id) continue;

                    const float edge = 0.075f;
                    if (d == IntVec3.North)
                        Quad(GetSubMesh(bund), cell, 0f, 1f - edge,
                            1f, 1f, y + 0.004f);
                    else if (d == IntVec3.South)
                        Quad(GetSubMesh(bund), cell, 0f, 0f,
                            1f, edge, y + 0.004f);
                    else if (d == IntVec3.East)
                        Quad(GetSubMesh(bund), cell, 1f - edge, 0f,
                            1f, 1f, y + 0.004f);
                    else if (d == IntVec3.West)
                        Quad(GetSubMesh(bund), cell, 0f, 0f,
                            edge, 1f, y + 0.004f);
                }
            }
            FinalizeMesh(MeshParts.All);
        }

        private static void Quad(LayerSubMesh mesh, IntVec3 cell,
            float x0, float z0, float x1, float z1, float y)
        {
            int start = mesh.verts.Count;
            mesh.verts.Add(new Vector3(cell.x + x0, y, cell.z + z0));
            mesh.verts.Add(new Vector3(cell.x + x0, y, cell.z + z1));
            mesh.verts.Add(new Vector3(cell.x + x1, y, cell.z + z1));
            mesh.verts.Add(new Vector3(cell.x + x1, y, cell.z + z0));
            mesh.uvs.Add(new Vector3(0f, 0f, 0f));
            mesh.uvs.Add(new Vector3(0f, 1f, 0f));
            mesh.uvs.Add(new Vector3(1f, 1f, 0f));
            mesh.uvs.Add(new Vector3(1f, 0f, 0f));
            for (int i = 0; i < 4; i++)
                mesh.colors.Add(new Color32(255, 255, 255, 255));
            mesh.tris.Add(start);
            mesh.tris.Add(start + 1);
            mesh.tris.Add(start + 2);
            mesh.tris.Add(start);
            mesh.tris.Add(start + 2);
            mesh.tris.Add(start + 3);
        }
    }
}
