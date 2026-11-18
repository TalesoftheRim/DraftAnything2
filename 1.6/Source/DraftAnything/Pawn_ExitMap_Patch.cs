using HarmonyLib;
using Verse;

namespace DraftAnything
{
    [HarmonyPatch(typeof(Pawn), "ExitMap")]
    public static class Pawn_ExitMap_Patch
    {
        public static void Prefix(Pawn __instance, ref bool allowedToJoinOrCreateCaravan)
        {
            if (__instance.IsControllable(out var comp) && comp != null)
            {
                __instance.RestoreOriginalState();

                allowedToJoinOrCreateCaravan = false;
            }
        }
    }
}
