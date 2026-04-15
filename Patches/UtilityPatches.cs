using HarmonyLib;
using HutongGames.PlayMaker;
using PrepatcherPlugin;
using SilkenSisters.Utils;
using System;
using UnityEngine.SceneManagement;



namespace SilkenSisters.Patches
{
    internal class UtilityPatches
    {


        [HarmonyPrefix]
        [HarmonyPatch(typeof(HeroController), "Die")]
        private static void setDeathListener(HeroController __instance, ref bool nonLethal, ref bool frostDeath)
        {
            SilkenSisters.Log.LogDebug($"[DeathListener] Hornet died / isMemory? Mod:{SilkenSisters.isMemory()} Scene:{GameManager.instance.IsMemoryScene()}");
            if (SilkenSisters.isMemory() && GameManager.instance.IsMemoryScene())
            { 
                SilkenSisters.Log.LogDebug($"[DeathListener] Hornet died in memory, removing the Prepatcher hook");

                PlayerDataVariableEvents.OnGetBool -= PrepatcherUtils.SilkenSisterMonitor;
            }

            if (SilkenSisters.hornetConstrain != null)
            {
                SilkenSisters.hornetConstrain.enabled = false;
            }
        }
    }
}
