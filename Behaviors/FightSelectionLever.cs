using Silksong.AssetHelper.ManagedAssets;
using Silksong.UnityHelper.Extensions;
using System.Threading.Tasks;
using TeamCherry.Localization;
using UnityEngine;

namespace SilkenSisters.Behaviors
{
    internal class FightSelectionLever : MonoBehaviour
    {

        GameObject infoPrompt;
        BasicNPC promptnpc;

        private void Awake()
        {
            Setup();
        }

        private async Task Setup()
        {
            gameObject.transform.position = new Vector3(60.9817f, 51.961f, 1.8055f);

            gameObject.transform.SetRotationZ(320.1306f);
            gameObject.FindChild("Platform").SetActive(false);
            gameObject.FindChild("Enviro Region Simple (2)").SetActive(false);
            gameObject.FindChild("Hero Detector Close").SetActive(false);
            gameObject.FindChild("Hero Detector Scene Enter").SetActive(false);
            gameObject.FindChild("axel").SetActive(false);
            gameObject.FindChild("Levers/Lever Bottom").SetActive(false);

            gameObject.FindChild("Levers/Lever Top").GetComponent<Lever>().OnHit.AddListener(FlipSync);
            gameObject.FindChild("Levers/Lever Top").GetComponent<Lever>().OnHit.AddListener(DisablePrompt);
            gameObject.FindChild("Levers/Lever Top").GetComponent<Lever>().OnHitDelayed.AddListener(SetLeverText);

            addInfo();

        }

        private void addInfo()
        {
            infoPrompt = SilkenSisters.plugin.assetManager.gameObjectCache.InstantiateAsset<GameObject>("infoPromptCache");
            promptnpc = infoPrompt.GetComponent<BasicNPC>();
            infoPrompt.transform.SetParent(gameObject.FindChild("Levers/Lever Top/Lever").transform, true);
            infoPrompt.transform.position = new Vector3(62.6255f, 53.961f, 1.8055f);
            infoPrompt.FindChild("Prompt Marker").transform.position = new Vector3(62.6255f, 56.4146f, 2.0055f);
            SetLeverText();
        }

        private void SetLeverText()
        {
            promptnpc.talkText[0].Sheet = $"Mods.{SilkenSisters.Id}";
            if (SilkenSisters.plugin.configManager.syncedFight.Value) {
                promptnpc.talkText[0].Key = "SILKEN_SISTERS_SYNC_FIGHT_ON"; 
            } else { 
                promptnpc.talkText[0].Key = "SILKEN_SISTERS_SYNC_FIGHT_OFF"; 
            }
            infoPrompt.SetActive(true);
            
        }

        private void DisablePrompt()
        {
            infoPrompt.SetActive(false);
        }

        private void FlipSync()
        {
            SilkenSisters.plugin.configManager.syncedFight.Value = !SilkenSisters.plugin.configManager.syncedFight.Value;
        }

    
    }

}
