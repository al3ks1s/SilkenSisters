using HarmonyLib;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Silksong.FsmUtil;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SilkenSisters.Patches
{
    internal class LaceCorpsePatch
    {

        [HarmonyPrefix]
        [HarmonyPatch(typeof(FsmState), "OnEnter")]
        private static void PatchLaceDeath(FsmState __instance)
        {
            // Enable Corpse Lace Hooking
            if (__instance.Fsm.GameObject.name == "Corpse Lace2(Clone)" && __instance.Name == "Start" && SilkenSisters.isMemory())
            {
                SilkenSisters.Log.LogDebug("Started setting corpse handler");
                GameObject laceCorpse = __instance.Fsm.GameObject;
                GameObject laceCorpseNPC = GameObject.Find($"{__instance.Fsm.GameObject.name}/NPC");
                SilkenSisters.Log.LogDebug($"{laceCorpseNPC}");

                SilkenSisters.Log.LogDebug("Fixing facing");
                PlayMakerFSM laceCorpseFSM = FsmUtil.GetFsmPreprocessed(laceCorpse, "Control");
                laceCorpseFSM.GetAction<CheckXPosition>("Set Facing", 0).compareTo = 72;
                laceCorpseFSM.GetAction<CheckXPosition>("Set Facing", 1).compareTo = 96;

                SilkenSisters.Log.LogDebug("Disabling interact action");
                laceCorpseFSM.DisableAction("NPC Ready", 0);
                SendEventByName lace_death_event = new SendEventByName();
                lace_death_event.sendEvent = "INTERACT";
                lace_death_event.delay = 0f;
                FsmOwnerDefault owner = new FsmOwnerDefault();
                owner.gameObject = laceCorpseNPC;
                owner.ownerOption = OwnerDefaultOption.SpecifyGameObject;

                FsmEventTarget target = new FsmEventTarget();
                target.gameObject = owner;
                target.target = FsmEventTarget.EventTarget.GameObject;
                lace_death_event.eventTarget = target;
                laceCorpseFSM.AddAction("NPC Ready", lace_death_event);

                SilkenSisters.Log.LogDebug("Editing NPC routes to skip dialogue");
                PlayMakerFSM laceCorpseNPCFSM = FsmUtil.GetFsmPreprocessed(laceCorpseNPC, "Control");
                SilkenSisters.Log.LogDebug("Editing Idle");
                laceCorpseNPCFSM.ChangeTransition("Idle", "INTERACT", "Drop Pause");
                SilkenSisters.Log.LogDebug("Drop Pause");
                laceCorpseNPCFSM.DisableAction("Drop Pause", 0);
                laceCorpseNPCFSM.ChangeTransition("Drop Down", "FINISHED", "End Pause");
                laceCorpseNPCFSM.ChangeTransition("Drop Down", "IS_HURT", "End Pause");
                SilkenSisters.Log.LogDebug("End Pause");
                laceCorpseNPCFSM.DisableAction("End Pause", 0);
                laceCorpseNPCFSM.GetAction<Wait>("End Pause", 1).time = 0.5f;

                SilkenSisters.Log.LogDebug("Disabling end actions");
                laceCorpseNPCFSM.DisableAction("End", 5);
                laceCorpseNPCFSM.DisableAction("End", 6);
                laceCorpseNPCFSM.DisableAction("End", 7);
                laceCorpseNPCFSM.DisableAction("End", 8);
                laceCorpseNPCFSM.DisableAction("End", 9);
                laceCorpseNPCFSM.DisableAction("End", 10);
                laceCorpseNPCFSM.DisableAction("End", 11);
                laceCorpseNPCFSM.DisableAction("End", 12);
                laceCorpseNPCFSM.DisableAction("End", 13);

                SilkenSisters.Log.LogDebug("Disabling audio cutting");
                laceCorpseFSM.DisableAction("Start", 0);
                laceCorpseNPCFSM.DisableAction("Talk 1 Start", 3);
                laceCorpseNPCFSM.DisableAction("End", 0);

                SilkenSisters.Log.LogDebug("Finished setting up corpse handler");
            }
        }


    }
}
