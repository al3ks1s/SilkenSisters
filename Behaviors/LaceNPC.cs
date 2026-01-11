using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Silksong.FsmUtil;
using Silksong.UnityHelper.Extensions;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SilkenSisters.Behaviors
{
    internal class LaceNPC : MonoBehaviour
    {

        private PlayMakerFSM _control = null;
        private Transform _npcTransform = null;

        private void Awake()
        {
            Setup();
        }

        private async Task Setup()
        {
            try
            {
                SilkenSisters.Log.LogMessage($"[LaceNPC.Setup] Spawning lace on the organ bench");
                register();
                getComponents();
                disableRangeDetection();
                setPosition();
                editFSMAnimations();
                SilkenSisters.Log.LogMessage($"[LaceNPC.Setup] Finished setting up LaceNPC");
            }
            catch (Exception e)
            {
                SilkenSisters.Log.LogError($"{e} {e.Message}");
            }
        }

        private void register()
        {
            SilkenSisters.plugin.laceNPCFSMOwner = new FsmOwnerDefault();
            SilkenSisters.plugin.laceNPCFSMOwner.OwnerOption = OwnerDefaultOption.SpecifyGameObject;
            SilkenSisters.plugin.laceNPCFSMOwner.GameObject = gameObject;
        }

        private void getComponents()
        {
            _control = FsmUtil.GetFsmPreprocessed(gameObject, "Control");
            _npcTransform = gameObject.transform;
        }

        private void disableRangeDetection()
        {
            gameObject.FindChild("Start Range").SetActive(false);
            SilkenSisters.Log.LogInfo($"[LaceNPC.disableRangeDetection] LaceNPCDetection?:{gameObject.FindChild("Start Range").activeSelf}");
        }

        private void setPosition()
        {
            _npcTransform.SetPosition3D(81.9569f, 106.2943f, 2.7723f);
            //_npcTransform.SetScaleX(-0.9f);
            //_npcTransform.SetScaleY(0.9f);
            //_npcTransform.SetScaleZ(0.9f);
            SilkenSisters.Log.LogInfo($"[LaceNPC.setPosition] position:{_npcTransform.position}");
        }

        private void editFSMAnimations()
        {
            SilkenSisters.Log.LogMessage("[LaceNPC.editFSMAnimations] Editing Lace NPC FSM");
            _control.ChangeTransition("Take Control", "LAND", "Sit Up");
            _control.ChangeTransition("Take Control", "LAND", "Sit Up");
            _control.GetTransition("Take Control", "LAND").fsmEvent = FsmEvent.GetFsmEvent("FINISHED");
            _control.DisableAction("Take Control", 3);

            _control.ChangeTransition("Sit Up", "FINISHED", "Jump Antic");

            Wait w2 = new Wait();
            w2.time = 2f;
            _control.DisableAction("Sit Up", 4);
            _control.AddAction("Sit Up", w2);

            SetPosition laceTargetPos = _control.GetAction<SetPosition>("Sit Up", 3);
            laceTargetPos.vector = new Vector3(81.9569f, 106.7942f, 2.7021f);
            laceTargetPos.x = 81.9569f;
            laceTargetPos.y = 106.7942f;
            laceTargetPos.z = 2.7021f;

            InvokeMethod toggleChall = new InvokeMethod(toggleChallenge);
            _control.AddAction("Jump Away", toggleChall);

            InvokeMethod constrainHornet = new InvokeMethod(startConstrainHornet);
            _control.AddAction("Jump Away", constrainHornet);

            _control.DisableAction("Jump Antic", 4);

            _control.DisableAction("Jump Away", 7);
            _control.DisableAction("Look Up End", 0);

            _control.DisableAction("End", 1);
            _control.DisableAction("End", 4);
            _control.DisableAction("End", 5);

        }

        private void toggleChallenge()
        {
            SilkenSisters.plugin.challengeDialogInstance.SetActive(!SilkenSisters.plugin.challengeDialogInstance.activeSelf);
            SilkenSisters.Log.LogInfo($"[LaceNPC.toggleChallenge] challenge?:{SilkenSisters.plugin.challengeDialogInstance.activeSelf}");
        }
    
        private void startConstrainHornet()
        {
            SilkenSisters.hornetConstrain.enabled = true;
            SilkenSisters.Log.LogInfo($"[LaceNPC.startConstrainHornet] constrainHornet?:{SilkenSisters.hornetConstrain.enabled}");
        }

    }
}
