using HarmonyLib;
using RimWorld;
using Verse;

namespace DraftAnything
{
    [HarmonyPatch(typeof(FloatMenuOptionProvider), "SelectedPawnValid")]
    public static class FloatMenuOptionProvider_SelectedPawnValid_Patch
    {
        public static void Postfix(FloatMenuOptionProvider __instance, ref bool __result, Pawn pawn, FloatMenuContext context)
        {
            if (__instance is FloatMenuOptionProvider_DraftedMove)
            {
                if (pawn.IsControllable() && pawn.Drafted)
                {
                    __result = true;
                }
            }
        }
    }
}
