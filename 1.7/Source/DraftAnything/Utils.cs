using System.Collections.Generic;
using RimWorld;
using Verse;

namespace DraftAnything
{
    public static class Utils
    {
        public static bool TryGetControlComp(this Pawn pawn, out CompControlPawn comp)
        {
            comp = null;
            if (pawn == null)
            {
                return false;
            }
            if (CompControlPawn.instances.TryGetValue(pawn, out comp) && comp != null)
            {
                return true;
            }
            comp = pawn.GetComp<CompControlPawn>();
            if (comp != null)
            {
                CompControlPawn.instances[pawn] = comp;
                return true;
            }
            return false;
        }

        public static bool IsControllable(this Pawn pawn)
        {
            return pawn.IsControllable(out _);
        }

        public static bool IsControllable(this Pawn pawn, out CompControlPawn comp)
        {
            comp = null;
            if (pawn == null || pawn.Destroyed || pawn.Dead || !pawn.Spawned)
            {
                return false;
            }
            if (pawn.TryGetControlComp(out comp) && comp.isControled)
            {
                return true;
            }
            return false;
        }

        public static bool IsNativelyPlayerControlled(this Pawn pawn, CompControlPawn comp)
        {
            if (pawn == null || pawn.Destroyed || pawn.Dead)
            {
                return false;
            }
            if (pawn.IsColonistPlayerControlled || pawn.IsColonyMechPlayerControlled)
            {
                return true;
            }
            if (comp != null && comp.isControled)
            {
                return false;
            }
            return pawn.IsColonySubhumanPlayerControlled
                || pawn.IsPlayerControlled
                || (pawn.drafter != null && pawn.drafter.ShowDraftGizmo);
        }

        public static void AssignComponents(this Pawn pawn, CompControlPawn comp = null)
        {
            if (pawn == null)
            {
                return;
            }
            if (comp == null)
            {
                pawn.TryGetControlComp(out comp);
            }

            if (pawn.playerSettings == null)
            {
                pawn.playerSettings = new Pawn_PlayerSettings(pawn);
            }
            if (pawn.drafter == null)
            {
                pawn.drafter = new Pawn_DraftController(pawn);
                if (comp != null)
                {
                    comp.shouldDisableDrafter = true;
                }
            }
            if (pawn.equipment == null)
            {
                pawn.equipment = new Pawn_EquipmentTracker(pawn);
                if (comp != null)
                {
                    comp.shouldDisableEquipment = true;
                }
            }

            RaceProperties race = pawn.RaceProps;
            bool isMechanoidWithoutSkills = race != null && race.IsMechanoid && !race.Humanlike;
            if (isMechanoidWithoutSkills && pawn.kindDef != null && pawn.kindDef.skills != null)
            {
                isMechanoidWithoutSkills = false;
            }

            if (!isMechanoidWithoutSkills)
            {
                if (pawn.skills == null)
                {
                    pawn.skills = new Pawn_SkillTracker(pawn);
                    if (comp != null)
                    {
                        comp.shouldDisableSkills = true;
                    }
                }
                if (pawn.story == null)
                {
                    pawn.story = new Pawn_StoryTracker(pawn);
                    if (comp != null)
                    {
                        comp.shouldDisableStory = true;
                    }
                }
            }

            if (pawn.royalty == null)
            {
                pawn.royalty = new Pawn_RoyaltyTracker(pawn);
                if (comp != null)
                {
                    comp.shouldDisableRoyalty = true;
                }
            }
        }

        public static void EnableControl(this Pawn pawn, CompControlPawn comp)
        {
            if (pawn == null || comp == null)
            {
                return;
            }
            comp.isControled = true;
            comp.pendingCleanup = false;
            pawn.AssignComponents(comp);
            if (pawn.drafter != null && !pawn.drafter.Drafted)
            {
                pawn.drafter.Drafted = true;
            }
        }

        public static void RestoreOriginalState(this Pawn pawn)
        {
            if (pawn == null || !pawn.TryGetControlComp(out var comp) || comp == null)
            {
                return;
            }

            comp.isControled = false;
            comp.pendingCleanup = false;

            if (pawn.drafter != null && pawn.drafter.Drafted)
            {
                pawn.drafter.Drafted = false;
            }

            bool isPlayerOwned = (pawn.Faction != null && pawn.Faction.IsPlayer)
                || pawn.IsColonistPlayerControlled
                || pawn.IsColonyMechPlayerControlled
                || pawn.IsColonySubhumanPlayerControlled
                || pawn.IsPlayerControlled;

            if (!isPlayerOwned)
            {
                if (comp.shouldDisableDrafter && pawn.drafter != null)
                {
                    pawn.drafter = null;
                }
                if (comp.shouldDisableEquipment && pawn.equipment != null)
                {
                    if (pawn.Spawned && pawn.equipment.AllEquipmentListForReading.Count > 0)
                    {
                        pawn.equipment.DropAllEquipment(pawn.Position, false);
                    }
                    pawn.equipment = null;
                }
                if (comp.shouldDisableSkills)
                {
                    pawn.skills = null;
                }
                if (comp.shouldDisableStory)
                {
                    pawn.story = null;
                }
                if (comp.shouldDisableRoyalty)
                {
                    pawn.royalty = null;
                }
            }

            comp.shouldDisableDrafter = false;
            comp.shouldDisableEquipment = false;
            comp.shouldDisableSkills = false;
            comp.shouldDisableStory = false;
            comp.shouldDisableRoyalty = false;
        }
    }
}
