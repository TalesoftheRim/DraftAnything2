using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
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
        public bool shouldDisableSkills;
        public bool shouldDisableStory;
        public bool shouldDisableRoyalty;

        public Faction originalFaction;

        public static Dictionary<Pawn, CompControlPawn> instances = new Dictionary<Pawn, CompControlPawn>();

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            var pawn = this.parent as Pawn;
            if (pawn is null)
            {
                Log.Error($"CompControlPawn attached to non-Pawn {this.parent} of type {this.parent.GetType()}");
                return;
            }

            instances[pawn] = this;

            if (originalFaction == null)
            {
                originalFaction = pawn.Faction;
            }

            if (isControled)
            {
                pawn.AssignComponents();
            }
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref isControled, "isControled");
            Scribe_Values.Look(ref shouldDisableSkills, "shouldDisableSkills");
            Scribe_Values.Look(ref shouldDisableStory, "shouldDisableStory");
            Scribe_Values.Look(ref shouldDisableRoyalty, "shouldDisableRoyalty");
            Scribe_References.Look(ref originalFaction, "originalFaction");
        }
    }


    [StaticConstructorOnStartup]
    internal static class HarmonyInit
    {
        static HarmonyInit()
        {
            Harmony harmony = new Harmony("Hamrick.DraftAnything");
            harmony.PatchAll();
            foreach (var pawnDef in DefDatabase<ThingDef>.AllDefs.Where(x => x.race != null && x.IsCorpse is false))
            {
                pawnDef.comps.Add(new CompProperties_ControlPawn());
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
            if (__instance.IsControllable())
            {
                __result = true;
            }
        }
    }

    [HarmonyPatch(typeof(Thing), "Faction", MethodType.Getter)]
    public static class Faction_Patch
    {
        public static bool setToPlayer;
        private static void Postfix(Pawn __instance, ref Faction __result)
        {
            if (setToPlayer && __instance.IsControllable())
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
            Faction_Patch.setToPlayer = true;
        }
        
        public static void Postfix()
        {
            Faction_Patch.setToPlayer = false;
        }
    }
    
    [HarmonyPatch(typeof(Pawn), "GetGizmos")]
    public class Pawn_GetGizmos_Patch
    {
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Pawn __instance)
        {
            if (CompControlPawn.instances.TryGetValue(__instance, out var comp))
            {
                Command_Toggle command_Toggle = new Command_Toggle();
                command_Toggle.hotKey = KeyBindingDefOf.Command_ColonistDraft;
                command_Toggle.isActive = (() => __instance.Drafted);
                command_Toggle.toggleAction = delegate
                {
                    if (__instance.drafter == null)
                    {
                        __instance.AssignComponents();
                    }
                    
                    bool newState = !__instance.Drafted;

                    if (newState)
                    {
                        comp.isControled = true;

                        if (comp.originalFaction == null)
                        {
                            comp.originalFaction = __instance.Faction;
                        }
                    }

                    __instance.drafter.Drafted = newState;

                    if (!newState)
                    {
                        __instance.RestoreOriginalState();
                    }
                    else
                    {
                        __instance.AssignComponents();
                    }

                    PlayerKnowledgeDatabase.KnowledgeDemonstrated(
                        ConceptDefOf.Drafting,
                        KnowledgeAmount.SpecificInteraction
                    );
                };

                command_Toggle.defaultDesc = "CommandToggleDraftDesc".Translate();
                command_Toggle.icon = TexCommand.Draft;
                command_Toggle.turnOnSound = SoundDefOf.DraftOn;
                command_Toggle.turnOffSound = SoundDefOf.DraftOff;
                command_Toggle.groupKey = 81729172;
                command_Toggle.defaultLabel = (__instance.Drafted ? "CommandUndraftLabel" : "CommandDraftLabel").Translate();
                if (__instance.Downed)
                {
                    command_Toggle.Disable("IsIncapped".Translate(__instance.LabelShort, __instance));
                }
                if (!__instance.Drafted)
                {
                    command_Toggle.tutorTag = "Draft";
                }
                else
                {
                    command_Toggle.tutorTag = "Undraft";
                }
                yield return command_Toggle;

                if (comp.isControled)
                {
                    Faction_Patch.setToPlayer = true;
                }
                foreach (var g in __result)
                {
                    if (g is Command_Toggle command && command.defaultDesc == "CommandToggleDraftDesc".Translate())
                    {
                        continue;
                    }
                    yield return g;
                }
            }
            else
            {
                foreach (var g in __result)
                {
                    yield return g;
                }
            }


            Faction_Patch.setToPlayer = false;
        }
    }

    [HarmonyPatch(typeof(Pawn), "CanTakeOrder", MethodType.Getter)]
    public static class Pawn_CanTakeOrder_Patch
    {
        public static void Postfix(Pawn __instance, ref bool __result)
        {
            if (__result is false && __instance.IsControllable())
            {
                __result = true;
            }
        }
    }

    [HarmonyPatch(typeof(PawnComponentsUtility), "AddAndRemoveDynamicComponents")]
    public static class PawnComponentsUtility_AddAndRemoveDynamicComponents_Patch
    {
        public static void Postfix(Pawn pawn)
        {
            if (pawn.IsControllable(out var comp) && comp != null)
            {
                pawn.AssignComponents();
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
                            (node as ThinkNode_Subtree)?.treeDef.defName == "LordDuty");

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
