using Verse;

namespace ProgressionWaste
{
    public class Settings : ModSettings
    {
        public bool generateTrash = true;
        public int packAfter = 30;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref generateTrash, "generateTrash", true);
            Scribe_Values.Look(ref packAfter, "packAfter", 30);
        }
    }
}
