using Verse;

namespace ProgressionWaste
{
    public class CompProperties_WasteTally : CompProperties
    {
        public CompProperties_WasteTally()
        {
            compClass = typeof(CompWasteTally);
        }
    }

    /// <summary>Per-pawn tally of filth cleaned since the last time they packed trash.</summary>
    public class CompWasteTally : ThingComp
    {
        public int tally;

        public bool CanPack => ProgressionWasteMod.Settings.generateTrash && tally >= ProgressionWasteMod.Settings.packAfter;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref tally, "wasteTally");
        }
    }
}
