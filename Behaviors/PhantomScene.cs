using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using SilkenSisters.SceneManagement;
using Silksong.FsmUtil;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


namespace SilkenSisters.Behaviors
{
    internal class PhantomScene : MonoBehaviour
    {

        private PlayMakerFSM _control;

        private void Awake()
        {
            Setup();
        }

        private async Task Setup()
        {
            //register();
            getComponents();
            //waitForLace();
            disableAreaDetection();
            editFSMEvents();
            editBossTitle();
            setupHornetControl();
        }


        private void register()
        {
            SilkenSisters.Log.LogInfo($"Trying to register phantom");
            SilkenSisters.plugin.phantomBossScene = gameObject;
            SilkenSisters.Log.LogInfo($"{SilkenSisters.plugin.phantomBossScene}");

            SilkenSisters.Log.LogInfo($"Registering FSMOwner");
            SilkenSisters.plugin.phantomBossSceneFSMOwner = new FsmOwnerDefault();
            SilkenSisters.plugin.phantomBossSceneFSMOwner.OwnerOption = OwnerDefaultOption.SpecifyGameObject;
            SilkenSisters.plugin.phantomBossSceneFSMOwner.GameObject = SilkenSisters.plugin.phantomBossScene;
        }

        private void getComponents()
        {
            _control = gameObject.GetFsmPreprocessed("Control");
        }

        private void disableAreaDetection()
        {
            ((PlayMakerUnity2DProxy)GetComponent(typeof(PlayMakerUnity2DProxy))).enabled = false;
            ((BoxCollider2D)GetComponent(typeof(BoxCollider2D))).enabled = false;
        }

        private void editFSMEvents()
        {
            SilkenSisters.Log.LogInfo($"Trigger lace jump");
            SendEventByName lace_jump_event = new SendEventByName();
            lace_jump_event.sendEvent = "ENTER";
            lace_jump_event.delay = 0;

            FsmEventTarget target = new FsmEventTarget();
            target.gameObject = SilkenSisters.plugin.laceNPCFSMOwner;
            target.target = FsmEventTarget.EventTarget.GameObject;
            
            lace_jump_event.eventTarget = target;

            _control.AddAction("Organ Hit", lace_jump_event);


            FaceObjectV2 hornetFaceEnemies = new FaceObjectV2();
            hornetFaceEnemies.objectA = SilkenSisters.hornetFSMOwner;
            hornetFaceEnemies.objectB = SilkenSisters.plugin.lace2BossInstance;
            hornetFaceEnemies.spriteFacesRight = false;
            hornetFaceEnemies.playNewAnimation = false;
            hornetFaceEnemies.newAnimationClip = "";
            hornetFaceEnemies.resetFrame = false;
            hornetFaceEnemies.everyFrame = false;
            hornetFaceEnemies.pauseBetweenTurns = 0.1f;
            _control.AddAction("BG Fog", hornetFaceEnemies);


            Tk2dPlayAnimation hornetChall = new Tk2dPlayAnimation();
            hornetChall.gameObject = SilkenSisters.hornetFSMOwner;
            hornetChall.clipName = "Challenge Talk Start";
            hornetChall.animLibName = "";
            _control.AddAction("BG Fog", hornetChall);


        }

        private void setupHornetControl()
        {
            SilkenSisters.Log.LogDebug("Setting actions to give back hornet control");
            SendMessage message_control_idle = new SendMessage();
            FunctionCall fc_control_idle = new FunctionCall();
            fc_control_idle.FunctionName = "StartControlToIdle";

            message_control_idle.functionCall = fc_control_idle;
            message_control_idle.gameObject = SilkenSisters.hornetFSMOwner;
            message_control_idle.options = SendMessageOptions.DontRequireReceiver;

            SendMessage message_control_regain = new SendMessage();
            FunctionCall fc_control_regain = new FunctionCall();
            fc_control_regain.FunctionName = "RegainControl";
            message_control_regain.functionCall = fc_control_regain;
            message_control_regain.gameObject = SilkenSisters.hornetFSMOwner;
            message_control_regain.options = SendMessageOptions.DontRequireReceiver;

            HutongGames.PlayMaker.Actions.SetPlayerDataBool enablePause = new HutongGames.PlayMaker.Actions.SetPlayerDataBool();
            enablePause.boolName = "disablePause";
            enablePause.value = false;


            Tk2dPlayAnimation hornetChallEnd = new Tk2dPlayAnimation();
            hornetChallEnd.gameObject = SilkenSisters.hornetFSMOwner;
            hornetChallEnd.clipName = "Challenge Talk End";
            hornetChallEnd.animLibName = "";

            _control.AddAction("Start Battle", hornetChallEnd);
            _control.AddAction("Start Battle", message_control_regain);
            _control.AddAction("Start Battle", message_control_idle);
            _control.AddAction("Start Battle", enablePause);
        }

        private void editBossTitle()
        {
            SilkenSisters.Log.LogInfo($"Change boss title");
            _control.GetAction<DisplayBossTitle>("Start Battle", 3).bossTitle = "SILKEN_SISTERS";
        }
    }
}
