using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AncientMedievalJapan.RiceCultivation
{
    // FilledRectangle guarantees a single contiguous player-drawn plot.
    public sealed class Designator_CreatePaddy : Designator_Cells
    {
        public override DrawStyleCategoryDef DrawStyleCategory
        {
            get { return DrawStyleCategoryDefOf.FilledRectangle; }
        }

        public override bool DragDrawMeasurements { get { return true; } }

        public Designator_CreatePaddy()
        {
            defaultLabel = "AMJR_CreateLabel".Translate();
            defaultDesc = "AMJR_CreateDesc".Translate();
            icon = ContentFinder<Texture2D>.Get("UI/Designators/ZoneCreate_Growing");
            useMouseIcon = true;
            soundSucceeded = SoundDefOf.Designate_ZoneAdd;
        }

        public override AcceptanceReport CanDesignateCell(IntVec3 c)
        {
            return Map.GetComponent<PaddyMapComponent>().CanPlaceCell(c);
        }

        public override void DesignateSingleCell(IntVec3 c)
        {
            Map.GetComponent<PaddyMapComponent>().AddRectangle(new CellRect(c.x, c.z, 1, 1));
        }

        public override void DesignateMultiCell(IEnumerable<IntVec3> cells)
        {
            var list = new List<IntVec3>(cells);
            if (list.Count == 0) { Finalize(false); return; }

            int minX = int.MaxValue, minZ = int.MaxValue;
            int maxX = int.MinValue, maxZ = int.MinValue;
            var distinct = new HashSet<IntVec3>();
            foreach (IntVec3 c in list)
            {
                distinct.Add(c);
                if (c.x < minX) minX = c.x;
                if (c.z < minZ) minZ = c.z;
                if (c.x > maxX) maxX = c.x;
                if (c.z > maxZ) maxZ = c.z;
            }
            var rect = CellRect.FromLimits(minX, minZ, maxX, maxZ);
            bool valid = distinct.Count == rect.Width * rect.Height &&
                         Map.GetComponent<PaddyMapComponent>().CanPlaceRectangle(rect);
            if (valid)
                valid = Map.GetComponent<PaddyMapComponent>().AddRectangle(rect);
            if (!valid)
                Messages.Message("AMJR_InvalidRectangle".Translate(),
                    MessageTypeDefOf.RejectInput, false);
            Finalize(valid);
        }

        public override void SelectedUpdate()
        {
            base.SelectedUpdate();
            GenUI.RenderMouseoverBracket();
        }
    }

    public sealed class Designator_RemovePaddy : Designator_Cells
    {
        public override DrawStyleCategoryDef DrawStyleCategory
        {
            get { return DrawStyleCategoryDefOf.FilledRectangle; }
        }

        public Designator_RemovePaddy()
        {
            defaultLabel = "AMJR_RemoveLabel".Translate();
            defaultDesc = "AMJR_RemoveDesc".Translate();
            icon = ContentFinder<Texture2D>.Get("UI/Designators/RemoveFloor");
            useMouseIcon = true;
            soundSucceeded = SoundDefOf.Designate_ZoneDelete;
        }

        public override AcceptanceReport CanDesignateCell(IntVec3 c)
        {
            return Map.GetComponent<PaddyMapComponent>().PlotAt(c) != null;
        }

        public override void DesignateSingleCell(IntVec3 c)
        {
            PaddyPlot plot = Map.GetComponent<PaddyMapComponent>().PlotAt(c);
            if (plot != null)
                Map.GetComponent<PaddyMapComponent>().Remove(plot.id);
        }

        public override void DesignateMultiCell(IEnumerable<IntVec3> cells)
        {
            PaddyMapComponent component = Map.GetComponent<PaddyMapComponent>();
            var ids = new HashSet<int>();
            foreach (IntVec3 c in cells)
            {
                PaddyPlot plot = component.PlotAt(c);
                if (plot != null) ids.Add(plot.id);
            }
            foreach (int id in ids) component.Remove(id);
            Finalize(ids.Count != 0);
        }
    }
}