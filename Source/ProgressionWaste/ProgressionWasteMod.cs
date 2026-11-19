using UnityEngine;
using Verse;

namespace ProgressionWaste
{
    public class ProgressionWasteMod : Mod
    {
        public static Settings Settings;

        public ProgressionWasteMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<Settings>();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var listing = new Listing_Standard();
            listing.Begin(inRect);
            listing.Gap(36f);
            listing.CheckboxLabeled("ProgressionWaste.GenerateTrash".Translate().CapitalizeFirst(), ref Settings.generateTrash,
                "ProgressionWaste.GenerateTrashDesc".Translate());
            listing.Gap(12f);
            // Greyed out and inert while trash generation is off
            bool enabled = Settings.generateTrash;
            float top = listing.CurHeight;
            if (!enabled)
            {
                GUI.color = Color.gray;
            }
            listing.Label($"{"ProgressionWaste.PackAfter".Translate().CapitalizeFirst()}: {Settings.packAfter}");
            int packAfter = (int)listing.Slider(Settings.packAfter, 1, 100);
            if (enabled)
            {
                Settings.packAfter = packAfter;
            }
            else
            {
                TooltipHandler.TipRegion(new Rect(0f, top, listing.ColumnWidth, listing.CurHeight - top), "ProgressionWaste.PackAfterDisabled".Translate());
            }
            GUI.color = Color.white;
            listing.End();
        }

        public override string SettingsCategory() => "ProgressionWaste.ModName".Translate();
    }
}
