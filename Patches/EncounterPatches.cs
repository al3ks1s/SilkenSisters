using HarmonyLib;
using HutongGames.PlayMaker;
using Silksong.FsmUtil;
using UnityEngine.SceneManagement;

namespace SilkenSisters.Patches
{
    internal class EncounterPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(FsmState), "OnEnter")]
        private static void PatchLaceEncounter(FsmState __instance)
        {

            // Disable Lace's encounter in Sinner's road
            if ((__instance.Fsm.GameObject.name == "Lace Encounter Control") && 
                __instance.Name == "Init" && 
                SceneManager.GetActiveScene().name == "Dust_01")
            {
                __instance.fsm.DisableAction("Check", 3);
            }
        }


    }
}
