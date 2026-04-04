using HarmonyLib;
using HutongGames.PlayMaker;
using System;



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
    }
}
