using Verse;

namespace DraftAnything
{
    public class DraftAnythingSettings : ModSettings
    {
        public bool permanentDrafting = false;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref permanentDrafting, "permanentDrafting", false);
            base.ExposeData();
        }
    }
}