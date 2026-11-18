using HarmonyLib;
using RimWorld;
using Verse;

namespace DraftAnything
{
    [HarmonyPatch(typeof(AutoUndrafter), "ShouldAutoUndraft")]
    public static class AutoUndrafter_ShouldAutoUndraft_Patch
    {
        public static bool Prefix(AutoUndrafter __instance)
        {
            if (DraftAnythingMod.settings.permanentDrafting && __instance.pawn.Drafted)
            {
                return false;
            }
            return true;
        }
    }
}
