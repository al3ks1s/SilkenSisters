using HarmonyLib;
using HutongGames.PlayMaker;
using System;
using System.Collections.Generic;
using System.Text;

using SD = System.Diagnostics;
namespace SilkenSisters.Patches
{
    internal class DebugPatches
    {

        [SD.Conditional("DEBUG")]
        public static void CreateDebugPatch()
        {
            //SilkenSisters.instance._utilitypatches = Harmony.CreateAndPatchAll(typeof(DebugPatches));
        }

        [SD.Conditional("DEBUG")]
        public static void RemoveDebugPatch()
        {
            if (SilkenSisters.instance._utilitypatches != null)
                SilkenSisters.instance._utilitypatches.UnpatchSelf();
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
            if (logDeepMemory && (__instance.Fsm.GameObject.name == $"{SilkenSisters.instance.deepMemoryInstance}" || __instance.Fsm.GameObject.name == $"before" || __instance.Fsm.GameObject.name == $"thread_memory"))
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
}

