using System;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace DraftAnything
{
    public class DraftAnythingMod : Mod
    {
        public static DraftAnythingSettings settings;

        public DraftAnythingMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<DraftAnythingSettings>();
            var harmony = new Harmony("Hamrick.DraftAnything");
            harmony.PatchAll();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            if (settings != null)
            {
                Listing_Standard listingStandard = new Listing_Standard();
                listingStandard.Begin(inRect);
                listingStandard.CheckboxLabeled("DA_PermanentDrafting".Translate(), ref settings.permanentDrafting, "DA_PermanentDraftingTooltip".Translate());
                listingStandard.End();
            }
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "Draft Anything";
        }
    }
}