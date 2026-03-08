using System;
using System.Threading.Tasks;
using TeamCherry.Localization;
using UnityEngine;

namespace SilkenSisters.Behaviors
{
    internal class InfoPrompt : MonoBehaviour
    {
        private BasicNPC _npc;

        private void Awake()
        {
            Setup();
        }

        private async Task Setup()
        {
            try
            {
                SilkenSisters.Log.LogMessage($"[ChallengeRegion.Setup] Started setting up info prompt");
                getComponents();
                setPosition();
                setText();
                SilkenSisters.Log.LogMessage($"[ChallengeRegion.Setup] Finished setting up info prompt");
            }
            catch (Exception e)
            {
                SilkenSisters.Log.LogError($"{e} {e.Message}");
            }
        }

        private void getComponents()
        {
            _npc = GetComponent<BasicNPC>();
        }

        private void setPosition()
        {
            //59.8327f, 54.3406f, 0.006f
            gameObject.transform.position = new Vector3(59.8327f, 54.3406f, 0.006f);
        }

        private void setText()
        {
            _npc.talkText[0].Sheet = $"Mods.{SilkenSisters.Id}";
            _npc.talkText[0].Key = "SILKEN_SISTERS_INFOPROMPT";
            SilkenSisters.Log.LogMessage(Language.Get("SILKEN_SISTERS_INFOPROMPT", $"Mods.{SilkenSisters.Id}"));

        }

    }
}
