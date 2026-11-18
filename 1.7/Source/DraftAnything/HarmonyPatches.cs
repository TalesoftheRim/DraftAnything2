using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace DraftAnything
{
    public class CompProperties_ControlPawn : CompProperties
    {
        public CompProperties_ControlPawn()
        {
            this.compClass = typeof(CompControlPawn);
        }
    }

    public class CompControlPawn : ThingComp
    {
        public bool isControled;
        public bool shouldDisableDrafter;
        public bool shouldDisableEquipment;
        public bool shouldDisableSkills;
        public bool shouldDisableStory;
        public bool shouldDisableRoyalty;

        public bool pendingCleanup;

        public static Dictionary<Pawn, CompControlPawn> instances = new Dictionary<Pawn, CompControlPawn>();

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            var pawn = this.parent as Pawn;
            if (pawn is null)
            {
                Log.Error($"CompControlPawn attached to non-Pawn {this.parent} of type {this.parent?.GetType()}");
                return;
            }

            instances[pawn] = this;

            if (isControled)
            {
                if (pawn.Dead || !pawn.Drafted)
                {
                    pawn.RestoreOriginalState();
                }
                else
                {
                    pawn.AssignComponents(this);
                }
            }
            else if (pawn.Faction != null && pawn.Faction.IsPlayer && !pawn.Dead && pawn.playerSettings == null
                && (pawn.IsColonist || pawn.IsColonyMech || pawn.IsColonySubhuman || pawn.IsSlaveOfColony
                    || pawn.IsColonistPlayerControlled || pawn.IsColonyMechPlayerControlled || pawn.IsColonySubhumanPlayerControlled))
            {
                pawn.playerSettings = new Pawn_PlayerSettings(pawn);
            }
        }

        public override void CompTick()
        {
            base.CompTick();
            if (pendingCleanup && parent is Pawn pawn && !pawn.Drafted)
            {
                pawn.RestoreOriginalState();
            }
        }

        public override void CompTickInterval(int delta)
        {
            base.CompTickInterval(delta);
            if (pendingCleanup && parent is Pawn pawn && !pawn.Drafted)
            {
                pawn.RestoreOriginalState();
            }
        }

        public override void PostDeSpawn(Map map, DestroyMode mode)
        {
            base.PostDeSpawn(map, mode);
            if (pendingCleanup && parent is Pawn pawn && !pawn.Drafted)
            {
                pawn.RestoreOriginalState();
            }
        }

        public override void Notify_Killed(Map prevMap, DamageInfo? dinfo = null)
        {
            base.Notify_Killed(prevMap, dinfo);
            isControled = false;
            pendingCleanup = false;
            shouldDisableDrafter = false;
            shouldDisableEquipment = false;
            shouldDisableSkills = false;
            shouldDisableStory = false;
            shouldDisableRoyalty = false;
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
            if (parent is Pawn pawn)
            {
                instances.Remove(pawn);
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref isControled, "isControled");
            Scribe_Values.Look(ref shouldDisableDrafter, "shouldDisableDrafter");
            Scribe_Values.Look(ref shouldDisableEquipment, "shouldDisableEquipment");
            Scribe_Values.Look(ref shouldDisableSkills, "shouldDisableSkills");
            Scribe_Values.Look(ref shouldDisableStory, "shouldDisableStory");
            Scribe_Values.Look(ref shouldDisableRoyalty, "shouldDisableRoyalty");
        }
    }

    [StaticConstructorOnStartup]
    internal static class HarmonyInit
    {
        static HarmonyInit()
        {
            foreach (var pawnDef in DefDatabase<ThingDef>.AllDefs.Where(x => x.race != null && x.IsCorpse is false))
            {
                if (pawnDef.comps == null)
                {
                    pawnDef.comps = new List<CompProperties>();
                }
                if (!pawnDef.comps.Any(c => c is CompProperties_ControlPawn))
                {
                    pawnDef.comps.Add(new CompProperties_ControlPawn());
                }
            }
        }
    }

    [HarmonyPatch(typeof(PawnRelationUtility), "GetMostImportantColonyRelative")]
    public static class GetMostImportantColonyRelative_Patch
    {
        private static Exception Finalizer(Exception __exception)
        {
            if (__exception != null)
            {
                return null;
            }
            return null;
        }
    }

    [HarmonyPatch(typeof(Pawn_HealthTracker), "NotifyPlayerOfKilled")]
    public static class NotifyPlayerOfKilled_Patch
    {
        private static Exception Finalizer(Exception __exception)
        {
            if (__exception != null)
            {
                return null;
            }
            return null;
        }
    }

    [HarmonyPatch(typeof(MusicManagerPlay), "AppropriateNow")]
    public static class AppropriateNow_Patch
    {
        private static Exception Finalizer(Exception __exception)
        {
            if (__exception != null)
            {
                return null;
            }
            return null;
        }
    }

    [HarmonyPatch(typeof(Pawn), "IsColonySubhumanPlayerControlled", MethodType.Getter)]
    public static class IsColonist_Patch
    {
        private static void Postfix(Pawn __instance, ref bool __result)
        {
            if (!__result && __instance != null && __instance.IsControllable())
            {
                __result = true;
            }
        }
    }

    [HarmonyPatch(typeof(Thing), "Faction", MethodType.Getter)]
    public static class Faction_Patch
    {
        [ThreadStatic]
        private static int setToPlayerDepth;

        public static void BeginPlayerOverride()
        {
            setToPlayerDepth++;
        }

        public static void EndPlayerOverride()
        {
            if (setToPlayerDepth > 0)
            {
                setToPlayerDepth--;
            }
        }

        private static void Postfix(Thing __instance, ref Faction __result)
        {
            if (setToPlayerDepth > 0 && __instance is Pawn pawn && pawn.IsControllable())
            {
                __result = Faction.OfPlayer;
            }
        }
    }

    [HarmonyPatch(typeof(VerbTracker), "CreateVerbTargetCommand")]
    public static class VerbTracker_CreateVerbTargetCommand_Patch
    {
        public static void Prefix()
        {
            Faction_Patch.BeginPlayerOverride();
        }

        public static Exception Finalizer(Exception __exception)
        {
            Faction_Patch.EndPlayerOverride();
            return __exception;
        }
    }

    [HarmonyPatch(typeof(Pawn_DraftController), "Drafted", MethodType.Setter)]
    public static class Pawn_DraftController_Drafted_Patch
    {
        public static void Postfix(Pawn_DraftController __instance, bool value)
        {
            if (value)
            {
                return;
            }

            Pawn pawn = __instance?.pawn;
            if (pawn != null && pawn.TryGetControlComp(out var comp) && comp.isControled)
            {
                comp.isControled = false;
                comp.pendingCleanup = true;
            }
        }
    }

    [HarmonyPatch(typeof(Pawn), "GetGizmos")]
    public class Pawn_GetGizmos_Patch
    {
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Pawn __instance)
        {
            if (__instance == null || __instance.Destroyed || __instance.Dead
                || !__instance.TryGetControlComp(out var comp))
            {
                foreach (var g in __result)
                {
                    yield return g;
                }
                yield break;
            }

            if (comp.pendingCleanup && !__instance.Drafted)
            {
                __instance.RestoreOriginalState();
            }

            if (!comp.isControled && __instance.IsNativelyPlayerControlled(comp))
            {
                foreach (var g in __result)
                {
                    yield return g;
                }
                yield break;
            }

            string draftDesc = "CommandToggleDraftDesc".Translate();

            Command_Toggle command_Toggle = new Command_Toggle();
            command_Toggle.hotKey = KeyBindingDefOf.Command_ColonistDraft;
            command_Toggle.isActive = (() => __instance.Drafted);
            command_Toggle.toggleAction = delegate
            {
                bool newState = !__instance.Drafted;

                if (newState)
                {
                    __instance.EnableControl(comp);
                }
                else
                {
                    if (__instance.drafter != null && __instance.drafter.Drafted)
                    {
                        __instance.drafter.Drafted = false;
                    }
                    __instance.RestoreOriginalState();
                }

                PlayerKnowledgeDatabase.KnowledgeDemonstrated(
                    ConceptDefOf.Drafting,
                    KnowledgeAmount.SpecificInteraction
                );
                if (__instance.Drafted)
                {
                    LessonAutoActivator.TeachOpportunity(ConceptDefOf.QueueOrders, OpportunityType.GoodToKnow);
                }
            };

            command_Toggle.defaultDesc = draftDesc;
            command_Toggle.icon = TexCommand.Draft;
            command_Toggle.turnOnSound = SoundDefOf.DraftOn;
            command_Toggle.turnOffSound = SoundDefOf.DraftOff;
            command_Toggle.groupKey = 81729172;
            command_Toggle.defaultLabel = (__instance.Drafted ? "CommandUndraftLabel" : "CommandDraftLabel").Translate();
            if (__instance.Downed)
            {
                command_Toggle.Disable("IsIncapped".Translate(__instance.LabelShort, __instance));
            }
            command_Toggle.tutorTag = __instance.Drafted ? "Undraft" : "Draft";
            yield return command_Toggle;

            bool pushedOverride = false;
            try
            {
                if (comp.isControled)
                {
                    Faction_Patch.BeginPlayerOverride();
                    pushedOverride = true;
                }

                foreach (var g in __result)
                {
                    if (g is Command_Toggle command && command.defaultDesc == draftDesc)
                    {
                        continue;
                    }
                    yield return g;
                }
            }
            finally
            {
                if (pushedOverride)
                {
                    Faction_Patch.EndPlayerOverride();
                }
            }
        }
    }

    [HarmonyPatch(typeof(Pawn), "CanTakeOrder", MethodType.Getter)]
    public static class Pawn_CanTakeOrder_Patch
    {
        public static void Postfix(Pawn __instance, ref bool __result)
        {
            if (__result is false && __instance != null && __instance.IsControllable())
            {
                __result = true;
            }
        }
    }

    [HarmonyPatch(typeof(PawnComponentsUtility), "AddAndRemoveDynamicComponents")]
    public static class PawnComponentsUtility_AddAndRemoveDynamicComponents_Patch
    {
        public static void Prefix(Pawn pawn, out bool __state)
        {
            __state = pawn != null
                && !pawn.Dead
                && pawn.TryGetControlComp(out var comp)
                && comp.isControled
                && pawn.Drafted;
        }

        public static void Postfix(Pawn pawn, bool __state)
        {
            if (pawn == null || pawn.Dead)
            {
                return;
            }

            if (pawn.TryGetControlComp(out var comp))
            {
                if (comp.pendingCleanup && !pawn.Drafted)
                {
                    pawn.RestoreOriginalState();
                }
                else if (comp.isControled)
                {
                    pawn.AssignComponents(comp);
                    if (__state && pawn.drafter != null && !pawn.drafter.Drafted)
                    {
                        pawn.drafter.Drafted = true;
                    }
                    return;
                }
            }

            if (pawn.Faction != null && pawn.Faction.IsPlayer && pawn.playerSettings == null
                && (pawn.IsColonist || pawn.IsColonyMech || pawn.IsColonySubhuman || pawn.IsSlaveOfColony
                    || pawn.IsColonistPlayerControlled || pawn.IsColonyMechPlayerControlled || pawn.IsColonySubhumanPlayerControlled))
            {
                pawn.playerSettings = new Pawn_PlayerSettings(pawn);
            }
        }
    }

    [StaticConstructorOnStartup]
    public static class Startup
    {
        static Startup()
        {
            PatchThinkTreeDefs();
        }

        public static void PatchThinkTreeDefs()
        {
            var thinkTreeDefs = DefDatabase<ThinkTreeDef>.AllDefsListForReading;
            foreach (var thinkTreeDef in thinkTreeDefs)
            {
                if (thinkTreeDef.defName == "Downed") continue;
                var rootNode = thinkTreeDef.thinkRoot;
                if (rootNode == null || rootNode.subNodes == null) continue;

                int isColonist = rootNode.subNodes.FindIndex(node => node.GetType() == typeof(ThinkNode_ConditionalColonist));
                if (isColonist >= 0)
                {
                    InsertNodeAt(rootNode.subNodes, isColonist);
                }
                else
                {
                    int queuedJobNodeIndex = rootNode.subNodes.FindIndex(node => node.GetType() == typeof(ThinkNode_QueuedJob));
                    if (queuedJobNodeIndex >= 0)
                    {
                        InsertNodeAt(rootNode.subNodes, queuedJobNodeIndex);
                    }
                    else
                    {
                        int subtreeNodeIndex = rootNode.subNodes.FindIndex(node => node.GetType() == typeof(ThinkNode_Subtree) &&
                            (node as ThinkNode_Subtree)?.treeDef?.defName == "LordDuty");

                        if (subtreeNodeIndex >= 0)
                        {
                            InsertNodeAt(rootNode.subNodes, subtreeNodeIndex);
                        }
                        else
                        {
                            var revenantIndex = rootNode.subNodes.FindIndex(node => node.GetType() ==
                                typeof(ThinkNode_ConditionalRevenantState));
                            if (revenantIndex >= 0)
                            {
                                InsertNodeAt(rootNode.subNodes, revenantIndex);
                            }
                            else
                            {
                                if (thinkTreeDef.defName == "Shambler")
                                {
                                    int shamblerQueuedJobIndex = rootNode.subNodes.FindIndex(node => node.GetType() == typeof(ThinkNode_QueuedJob));
                                    if (shamblerQueuedJobIndex >= 0)
                                    {
                                        InsertNodeAt(rootNode.subNodes, shamblerQueuedJobIndex);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void InsertNodeAt(List<ThinkNode> subNodes, int index)
        {
            var newNode = new ThinkNode_IsControllable
            {
                subNodes = new List<ThinkNode>
                {
                    new ThinkNode_QueuedJob(),
                    new ThinkNode_Tagger
                    {
                        tagToGive = JobTag.DraftedOrder,
                        subNodes = new List<ThinkNode>
                        {
                            new JobGiver_MoveToStandable(),
                            new JobGiver_Orders()
                        }
                    }
                }
            };

            subNodes.Insert(index, newNode);
        }
    }
}
