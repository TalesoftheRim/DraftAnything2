using HarmonyLib;
using RimWorld;
using Verse;

namespace DraftAnything
{
    [HarmonyPatch(typeof(AutoUndrafter), "ShouldAutoUndraft")]
    public static class AutoUndrafter_ShouldAutoUndraft_Patch
    {
        public static bool Prefix(AutoUndrafter __instance, ref bool __result)
        {
            if (DraftAnythingMod.settings != null && DraftAnythingMod.settings.permanentDrafting
                && __instance?.pawn != null && __instance.pawn.Drafted)
            {
                __result = false;
                return false;
            }
            return true;
        }
    }
}
