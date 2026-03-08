using HarmonyLib;
using HutongGames.PlayMaker;
using System;
using System.Collections.Generic;
using System.Text;
using TeamCherry.Localization;
using static HutongGames.PlayMaker.FsmEventTarget;

namespace SilkenSisters.Patches
{
    internal class UtilityPatches
    {

        [HarmonyPrefix]
        [HarmonyPatch(typeof(HeroController), "Die")]
        private static void setDeathListener(HeroController __instance, ref bool nonLethal, ref bool frostDeath)
        {
            SilkenSisters.Log.LogDebug($"[DeathListener] Hornet died / isMemory? Mod:{SilkenSisters.isMemory()} Scene:{GameManager._instance.IsMemoryScene()}");
            if (SilkenSisters.isMemory() && GameManager._instance.IsMemoryScene())
            {

                PlayerData._instance.defeatedPhantom = true;
                PlayerData._instance.blackThreadWorld = true;
                if (SilkenSisters.hornetConstrain != null)
                {
                    SilkenSisters.hornetConstrain.enabled = false;
                }

                SilkenSisters.Log.LogDebug($"[DeathListener] Hornet died in memory, variable reset: defeatedPhantom:{PlayerData._instance.defeatedPhantom}, blackThreadWorld:{PlayerData._instance.blackThreadWorld}");

            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameManager), "SaveGame", new Type[] { typeof(int), typeof(Action<bool>), typeof(bool), typeof(AutoSaveName) })]
        private static bool setSaveListener(GameManager __instance, ref int saveSlot, ref Action<bool> ogCallback, ref bool withAutoSave, ref AutoSaveName autoSaveName)
        {
            ogCallback?.Invoke(true);
            SilkenSisters.Log.LogDebug($"[SaveListener] Trying to save game. isMemory? Mod:{SilkenSisters.isMemory()} Scene:{GameManager._instance.IsMemoryScene()}. Skipping?:{SilkenSisters.isMemory() || GameManager._instance.IsMemoryScene()}");
            return !(SilkenSisters.isMemory() || GameManager._instance.IsMemoryScene());
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(FsmState), "OnEnter")]
        private static void setStateListener(FsmState __instance)
        {

            if (__instance.Fsm.GameObject.name == "Lace Boss2 New" && __instance.Fsm.Name == "Control")
            {
                //SilkenSisters.Log.LogDebug($"[StateListen] {__instance.Name}");
            }
            if (__instance.Fsm.GameObject.name == "Boss Scene" && __instance.Fsm.Name == "Silken Sisters Sync Control")
            {
                SilkenSisters.Log.LogDebug($"[StateListen] {__instance.Name}");
            }

            bool logDeepMemory = false;
            if (logDeepMemory && (__instance.Fsm.GameObject.name == $"{SilkenSisters.plugin.deepMemoryInstance}" || __instance.Fsm.GameObject.name == $"before" || __instance.Fsm.GameObject.name == $"thread_memory"))
            {
                SilkenSisters.Log.LogDebug($"{__instance.Fsm.GameObject.name}, {__instance.fsm.name}, Entering state {__instance.Name}");
                if (__instance.Actions.Length > 0)
                {
                    foreach (FsmTransition transi in __instance.transitions)
                    {
                        SilkenSisters.Log.LogDebug($"    transitions for state {__instance.Name}: {transi.EventName} to {transi.toState}");
                    }

                    foreach (FsmStateAction action in __instance.Actions)
                    {
                        SilkenSisters.Log.LogDebug($"        Action for state {__instance.Name}: {action.GetType()}");
                    }
                }
            }
        }

    }

    [HarmonyPatch(typeof(Fsm), "Event")]
    [HarmonyPatch(new[] { typeof(FsmEventTarget), typeof(FsmEvent) })]
    public static class FSM_Event_Patch
    {
        private static void Prefix(Fsm __instance, ref FsmEventTarget eventTarget, ref FsmEvent fsmEvent)
        {
            if (__instance.Name == "Silken Sisters Sync Control")
            {
                SilkenSisters.Log.LogDebug(fsmEvent.Name);
            }

        }
    }
}
