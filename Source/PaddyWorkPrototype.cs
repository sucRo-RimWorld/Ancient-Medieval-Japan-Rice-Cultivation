using Verse;

namespace AncientMedievalJapan.RiceCultivation
{
    // NOT an approved gameplay value. Centralized to prevent work remaining
    // and progress display from diverging during the prototype. Must be
    // replaced by independently audited labor values before release.
    internal static class PaddyWorkPrototype
    {
        internal static float Required(CellRect rect)
        {
            return rect.Width * rect.Height * 100f +
                   (rect.Width + rect.Height) * 50f;
        }
    }
}