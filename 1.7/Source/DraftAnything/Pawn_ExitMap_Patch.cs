using HarmonyLib;
using Verse;

namespace DraftAnything
{
    [HarmonyPatch(typeof(Pawn), "ExitMap")]
    public static class Pawn_ExitMap_Patch
    {
        public static void Prefix(Pawn __instance, ref bool allowedToJoinOrCreateCaravan)
        {
            if (__instance != null && __instance.IsControllable(out var comp) && comp != null)
            {
                bool isPlayerFaction = __instance.Faction != null && __instance.Faction.IsPlayer;
                __instance.RestoreOriginalState();

                if (!isPlayerFaction)
                {
                    allowedToJoinOrCreateCaravan = false;
                }
            }
        }
    }
}
