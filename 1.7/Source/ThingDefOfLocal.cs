using RimWorld;
using Verse;

namespace ProgressionWaste
{
    [DefOf]
    public static class ThingDefOfLocal
    {
        public static ThingDef VRecyclingE_Trash;

        static ThingDefOfLocal() => DefOfHelper.EnsureInitializedInCtor(typeof(ThingDefOfLocal));
    }
}
