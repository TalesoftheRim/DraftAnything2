using RimWorld;
using Verse;

namespace DraftAnything
{
    public static class Utils
    {
        public static bool IsControllable(this Pawn pawn)
        {
            return pawn.IsControllable(out _);
        }

        public static bool IsControllable(this Pawn pawn, out CompControlPawn comp)
        {
            comp = null;
            if (pawn.Spawned && CompControlPawn.instances.TryGetValue(pawn, out comp) && comp != null && comp.isControled)
            {
                return true;
            }
            return false;
        }

        public static void AssignComponents(this Pawn pawn)
        {
            if (pawn.playerSettings == null)
            {
                pawn.playerSettings = new Pawn_PlayerSettings(pawn);
            }
            if (pawn.drafter == null)
            {
                pawn.drafter = new Pawn_DraftController(pawn);
            }
            if (pawn.equipment == null)
            {
                pawn.equipment = new Pawn_EquipmentTracker(pawn);
            }

            RaceProperties race = pawn.RaceProps;
            bool isMechanoidWithoutSkills = (race != null && race.IsMechanoid && !race.Humanlike);
            if (isMechanoidWithoutSkills && pawn.kindDef != null && pawn.kindDef.skills != null)
            {
                isMechanoidWithoutSkills = false;
            }

            if (!isMechanoidWithoutSkills)
            {
                if (pawn.skills == null)
                {
                    pawn.skills = new Pawn_SkillTracker(pawn);
                }
                if (pawn.story == null)
                {
                    pawn.story = new Pawn_StoryTracker(pawn);
                }
            }

            if (pawn.royalty == null)
            {
                pawn.royalty = new Pawn_RoyaltyTracker(pawn);
            }
        }

        public static void RestoreOriginalState(this Pawn pawn)
        {
            if (!CompControlPawn.instances.TryGetValue(pawn, out var comp) || comp == null)
                return;

            comp.isControled = false;

            if (comp.originalFaction != null && pawn.Faction != comp.originalFaction)
            {
                pawn.SetFaction(comp.originalFaction);
            }

            if (pawn.drafter != null)
            {
                pawn.drafter.Drafted = false;
                pawn.drafter = null;
            }

            pawn.playerSettings = null;
        }
    }
}
