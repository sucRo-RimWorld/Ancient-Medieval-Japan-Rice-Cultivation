using RimWorld;
using Verse;

namespace AncientMedievalJapan.RiceCultivation
{
    [DefOf]
    public static class AMJR_Defs
    {
        public static JobDef AMJR_ConstructPaddy;

        static AMJR_Defs()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(AMJR_Defs));
        }
    }
}