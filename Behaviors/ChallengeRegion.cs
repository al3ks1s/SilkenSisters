using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Mono.Security.Authenticode;
using SilkenSisters.SceneManagement;
using Silksong.FsmUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SilkenSisters.Behaviors
{
    internal class ChallengeRegion : MonoBehaviour
    {

        private GameObject _challengeRegion;
        private PlayMakerFSM _regionControl;

        private PlayMakerFSM _control;

        private void Awake()
        {
            Setup();
        }

        private async Task Setup()
        {
            SilkenSisters.Log.LogMessage($"[ChallengeRegion.Setup] Finished setting up ChallengeRegion");
            getComponents();
            setPositions();
            disableCradleStuff();
            setPhantomTrigger();
            setGarama();
            SilkenSisters.Log.LogMessage($"[ChallengeRegion.Setup] Finished setting up ChallengeRegion");
        }

        private void getComponents()
        {
            _challengeRegion = SceneObjectManager.findChildObject(gameObject, "Challenge Region");
            _regionControl = _challengeRegion.GetFsmPreprocessed("Challenge");
            _control = gameObject.GetFsmPreprocessed("First Challenge");
        }

        private void setPositions()
        {
            // Challenge region 84.375 106.8835 3.64 - 84,2341 112,4307 4,9999
            // Challenge dialog 83,9299 105,8935 2,504
            gameObject.transform.position = new Vector3(84.45f, 105.8935f, 2.504f);
            _challengeRegion.transform.localPosition = new Vector3(-0.2145f, 1.1139f, 2.4959f);
            SilkenSisters.Log.LogInfo($"[ChallengeRegion.setPositions] Dialog position:{gameObject.transform.position}, RegionPosition:{_challengeRegion.transform.localPosition}");
        }

        private void disableCradleStuff()
        {
            SceneObjectManager.findChildObject(gameObject, "Challenge Glows/Cradle__0013_loom_strut_based (2)").SetActive(false);
            SceneObjectManager.findChildObject(gameObject, "Challenge Glows/Cradle__0013_loom_strut_based (3)").SetActive(false);
            _control.GetTransition("Idle", "CHALLENGE START").FsmEvent = FsmEvent.GetFsmEvent("QUICK START");

            SilkenSisters.Log.LogInfo($"[ChallengeRegion.disableCradleStuff] Cradle specific objects active?: " +
                $"CradleLoom1:{SceneObjectManager.findChildObject(gameObject, "Challenge Glows/Cradle__0013_loom_strut_based (2)").activeSelf}, " +
                $"CradleLoom2:{SceneObjectManager.findChildObject(gameObject, "Challenge Glows/Cradle__0013_loom_strut_based (3)").activeSelf}");
        }

        private void setPhantomTrigger()
        {
            // Trigger phantom boss scene
            SilkenSisters.Log.LogMessage($"[ChallengeRegion.setPhantomTrigger] Setting battle trigger");
            SendEventByName battle_begin_event = new SendEventByName();
            battle_begin_event.sendEvent = "ENTER";
            battle_begin_event.delay = 0;
            FsmEventTarget target = new FsmEventTarget();
            target.gameObject = SilkenSisters.plugin.phantomBossSceneFSMOwner;
            target.target = FsmEventTarget.EventTarget.GameObject;
            battle_begin_event.eventTarget = target;

            _regionControl.AddAction("Challenge Complete", battle_begin_event);
            _regionControl.GetAction<GetXDistance>("Straight Back?", 1).gameObject.ownerOption = OwnerDefaultOption.UseOwner;

        }

        private void setGarama()
        {
            PlayMakerFSM HornetSpecialSFM = SilkenSisters.hornet.GetComponents<PlayMakerFSM>().First(f => f.FsmName == "Silk Specials");
            _regionControl.DisableAction("Hornet Voice", 0);
            _regionControl.AddAction("Hornet Voice", HornetSpecialSFM.GetStateAction("Standard", 0));

        }

    }
}
