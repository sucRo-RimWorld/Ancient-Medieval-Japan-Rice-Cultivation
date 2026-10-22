using RimWorks.Quickstarts;
using Verse;
namespace AncientMedievalJapan.RiceCultivation.E2E
{
    public sealed class RiceQuickstart : AbstractQuickstart
    {
        public override TaggedString description { get { return "Rice paddy 1.6 native source acceptance map."; } }
        public override int mapSize { get { return 50; } }
        public override string seed { get { return "AMJ-Rice-B-16"; } }
    }
}
