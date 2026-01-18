using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Silksong.AssetHelper.ManagedAssets;
using Silksong.FsmUtil;
using Silksong.UnityHelper.Extensions;
using System;
using System.Threading.Tasks;
using UnityEngine;

namespace SilkenSisters.Behaviors
{
    internal class PhantomBoss : MonoBehaviour
    {

        private PlayMakerFSM _control;

        private void Awake()
        {
            Setup();
        }

        private async Task Setup()
        {
            try
            {
                SilkenSisters.Log.LogMessage($"[PhantomBoss.Setup] Started setting phantom boss up");
                gameObject.transform.SetPositionX(77.1797f);
                getComponents();
                triggerLace();
                listenForLaceDead();

                if (SilkenSisters.isMemory()) { 
                    skipCutscene();
                    prepareExitMemoryEffect();
                }

                prepareSync();

                InvokeMethod constrainHornet = new InvokeMethod(endHornetConstrain);
                _control.AddAction("Final Parry", constrainHornet);

                SilkenSisters.Log.LogMessage($"[PhantomBoss.Setup] Finished setting phantom boss up");
            }
            catch (Exception e)
            {
                SilkenSisters.Log.LogError($"{e} {e.Message}");
            }
        }

        private void getComponents()
        {
            _control = gameObject.GetFsmPreprocessed("Control");
        }

        private void triggerLace()
        {
            // FG Column - enable LaceBoss Object
            SilkenSisters.Log.LogInfo($"[PhantomBoss.skipCutscene] Enable laceBoss {SilkenSisters.plugin.laceBossFSMOwner} {SilkenSisters.plugin.laceBossFSMOwner.gameObject}");
            ActivateGameObject activate_lace_boss = new ActivateGameObject();
            activate_lace_boss.activate = true;
            activate_lace_boss.gameObject = SilkenSisters.plugin.laceBossFSMOwner;
            activate_lace_boss.recursive = false;

            _control.AddAction("Appear", activate_lace_boss);

            // Trigger lace 
            SilkenSisters.Log.LogMessage($"[PhantomBoss.skipCutscene] Trigger lace boss");
            SendEventByName lace_boss_start = new SendEventByName();
            lace_boss_start.sendEvent = "BATTLE START FIRST";
            lace_boss_start.delay = 0;
            FsmEventTarget target_boss = new FsmEventTarget();
            target_boss.gameObject = SilkenSisters.plugin.laceBossFSMOwner;
            target_boss.target = FsmEventTarget.EventTarget.GameObject;
            lace_boss_start.eventTarget = target_boss;
            
            _control.AddAction("To Idle", lace_boss_start);
        }

        private void skipCutscene()
        {
            // Skip 
            SilkenSisters.Log.LogMessage($"[PhantomBoss.skipCutscene] Skip cutscene interaction");
            _control.GetAction<Wait>("Time Freeze", 4).time = 0.001f;
            _control.GetAction<ScaleTime>("Time Freeze", 5).timeScale = 1f;

            _control.DisableAction("Parry Ready", 0);
            _control.DisableAction("Parry Ready", 1);
            _control.GetAction<Wait>("Parry Ready", 4).time = 0.001f;
            _control.GetAction<Wait>("Parry Ready", 4).finishEvent = FsmEvent.GetFsmEvent("PARRY");

            _control.ChangeTransition("Death Explode", "FINISHED", "End Recover");
            _control.AddAction("End Recover", _control.GetAction<SetPositionToObject2D>("Get Control", 2));
            _control.AddAction("End Recover", _control.GetAction<SetPositionToObject2D>("Get Control", 4));

            _control.DisableAction("Set Data", 0);
            _control.DisableAction("Set Data", 1);
            _control.DisableAction("Set Data", 2);
            _control.DisableAction("Set Data", 7);
        }

        private void listenForLaceDead()
        {
            FsmGameObject laceBossVar = _control.AddGameObjectVariable("LaceBoss2");
            laceBossVar.SetName("LaceBoss2");

            FindGameObject laceObject = new FindGameObject();
            laceObject.objectName = $"{SilkenSisters.plugin.laceBossInstance.name}";
            laceObject.store = laceBossVar;
            laceObject.withTag = "Untagged";

            GameObjectIsNull laceIsNull = new GameObjectIsNull();
            laceIsNull.gameObject = laceBossVar;
            laceIsNull.isNotNull = FsmEvent.GetFsmEvent("BLOCKED HIT");

            _control.AddTransition("Final Parry", "BLOCKED HIT", "Counter Stance");
            _control.InsertAction("Final Parry", laceIsNull, 0);
            _control.InsertAction("Final Parry", laceObject, 0);
        }

        private void prepareExitMemoryEffect()
        {
            GameObject temp = SilkenSisters.plugin.deepMemoryCache.InstantiateAsset();
            PlayMakerFSM sourceFSM = temp.FindChild("before/thread_memory").GetFsmPreprocessed("FSM");
            FsmGameObject deepMemVar = _control.AddGameObjectVariable("Deep Memory Enter");

            _control.AddState("Deep Memory Enter");
            SpawnObjectFromGlobalPool deepMemoryEffect = new SpawnObjectFromGlobalPool();
            deepMemoryEffect.gameObject = sourceFSM.GetAction<SpawnObjectFromGlobalPool>("Deep Memory Enter", 3).gameObject;
            deepMemoryEffect.spawnPoint = sourceFSM.GetAction<SpawnObjectFromGlobalPool>("Deep Memory Enter", 3).spawnPoint;
            deepMemoryEffect.storeObject = deepMemVar;
            deepMemoryEffect.position = new FsmVector3();
            deepMemoryEffect.rotation = new FsmVector3();

            SetMainCameraFovOffset camOff = new SetMainCameraFovOffset();
            camOff.FovOffset = -1f;
            camOff.TransitionTime = 4.7f;
            camOff.TransitionCurve = sourceFSM.GetAction<SetMainCameraFovOffset>("Deep Memory Enter", 4).TransitionCurve;

            Wait deepEnterWait = new Wait();
            deepEnterWait.time = 4.7f;
            deepEnterWait.finishEvent = FsmEvent.GetFsmEvent("FINISHED");

            _control.AddAction("Deep Memory Enter", deepMemoryEffect);
            _control.AddAction("Deep Memory Enter", camOff);
            _control.AddAction("Deep Memory Enter", deepEnterWait);

            _control.AddState("Deep Memory Enter Fall");
            HeroControllerMethods heroC1 = new HeroControllerMethods();
            heroC1.method = HeroControllerMethods.Method.RelinquishControl;

            HeroControllerMethods heroC2 = new HeroControllerMethods();
            heroC2.method = HeroControllerMethods.Method.RelinquishControlNotVelocity;

            TransitionToAudioSnapshot audio1 = new TransitionToAudioSnapshot();
            audio1.snapshot = sourceFSM.GetAction<TransitionToAudioSnapshot>("Deep Memory Enter Fall", 3).snapshot;
            audio1.transitionTime = 2f;

            TransitionToAudioSnapshot audio2 = new TransitionToAudioSnapshot();
            audio2.snapshot = sourceFSM.GetAction<TransitionToAudioSnapshot>("Deep Memory Enter Fall", 4).snapshot;
            audio2.transitionTime = 2f;

            TransitionToAudioSnapshot audio3 = new TransitionToAudioSnapshot();
            audio3.snapshot = sourceFSM.GetAction<TransitionToAudioSnapshot>("Deep Memory Enter Fall", 5).snapshot;
            audio3.transitionTime = 2f;

            SetMainCameraFovOffset camOff2 = new SetMainCameraFovOffset();
            camOff2.FovOffset = -2f;
            camOff2.TransitionTime = 2f;
            camOff2.TransitionCurve = sourceFSM.GetAction<SetMainCameraFovOffset>("Deep Memory Enter Fall", 6).TransitionCurve;

            AudioPlayerOneShotSingle audio4 = new AudioPlayerOneShotSingle();
            audio4.audioPlayer = sourceFSM.GetAction<AudioPlayerOneShotSingle>("Deep Memory Enter Fall", 7).audioPlayer;
            audio4.spawnPoint = sourceFSM.GetAction<AudioPlayerOneShotSingle>("Deep Memory Enter Fall", 7).spawnPoint;
            audio4.audioClip = sourceFSM.GetAction<AudioPlayerOneShotSingle>("Deep Memory Enter Fall", 7).audioClip;
            audio4.pitchMax = 1f;
            audio4.pitchMin = 1f;
            audio4.volume = 1f;
            audio4.delay = 0f;
            audio4.storePlayer = sourceFSM.GetAction<AudioPlayerOneShotSingle>("Deep Memory Enter Fall", 7).storePlayer;

            Tk2dPlayAnimationWithEvents fallAnim = new Tk2dPlayAnimationWithEvents();
            fallAnim.gameObject = sourceFSM.GetAction<Tk2dPlayAnimationWithEvents>("Deep Memory Enter Fall", 8).gameObject;
            fallAnim.clipName = sourceFSM.GetAction<Tk2dPlayAnimationWithEvents>("Deep Memory Enter Fall", 8).clipName;
            fallAnim.animationTriggerEvent = sourceFSM.GetAction<Tk2dPlayAnimationWithEvents>("Deep Memory Enter Fall", 8).animationTriggerEvent;

            _control.AddAction("Deep Memory Enter Fall", sourceFSM.GetAction<HeroControllerMethods>("Deep Memory Enter Fall", 1));
            _control.AddAction("Deep Memory Enter Fall", sourceFSM.GetAction<HeroControllerMethods>("Deep Memory Enter Fall", 2));
            _control.AddAction("Deep Memory Enter Fall", audio1);
            _control.AddAction("Deep Memory Enter Fall", audio2);
            _control.AddAction("Deep Memory Enter Fall", audio3);
            _control.AddAction("Deep Memory Enter Fall", camOff2);
            _control.AddAction("Deep Memory Enter Fall", audio4);
            _control.AddAction("Deep Memory Enter Fall", fallAnim);

            _control.AddState("Collapse");

            AudioPlayerOneShotSingle audio5 = new AudioPlayerOneShotSingle();
            audio5.audioPlayer = sourceFSM.GetAction<AudioPlayerOneShotSingle>("Collapse", 2).audioPlayer;
            audio5.spawnPoint = sourceFSM.GetAction<AudioPlayerOneShotSingle>("Collapse", 2).spawnPoint;
            audio5.audioClip = sourceFSM.GetAction<AudioPlayerOneShotSingle>("Collapse", 2).audioClip;
            audio5.pitchMax = 1f;
            audio5.pitchMin = 1f;
            audio5.volume = 1f;
            audio5.delay = 0f;
            audio5.storePlayer = sourceFSM.GetAction<AudioPlayerOneShotSingle>("Collapse", 2).storePlayer;

            AudioPlayerOneShotSingle audio6 = new AudioPlayerOneShotSingle();
            audio6.audioPlayer = sourceFSM.GetAction<AudioPlayerOneShotSingle>("Collapse", 3).audioPlayer;
            audio6.spawnPoint = sourceFSM.GetAction<AudioPlayerOneShotSingle>("Collapse", 3).spawnPoint;
            audio6.audioClip = sourceFSM.GetAction<AudioPlayerOneShotSingle>("Collapse", 3).audioClip;
            audio6.pitchMax = 1f;
            audio6.pitchMin = 1f;
            audio6.volume = 1f;
            audio6.delay = 0f;
            audio6.storePlayer = sourceFSM.GetAction<AudioPlayerOneShotSingle>("Collapse", 3).storePlayer;

            ListenForAnimationEvent eventListen = new ListenForAnimationEvent();
            eventListen.Response = FsmEvent.GetFsmEvent("FINISHED");
            eventListen.Target = sourceFSM.GetAction<ListenForAnimationEvent>("Collapse", 4).Target;

            Wait waitForAnim = new Wait();
            waitForAnim.time = 1f;
            waitForAnim.finishEvent = FsmEvent.GetFsmEvent("FINISHED");

            _control.AddAction("Collapse", audio5);
            _control.AddAction("Collapse", audio6);
            _control.AddAction("Collapse", waitForAnim);

            FsmState exitMemory = new FsmState(SilkenSisters.plugin.ExitMemoryCache);
            exitMemory.GetAction<ScreenFader>(1).startColour = new Color(0, 0, 0, 0);
            exitMemory.GetAction<ScreenFader>(1).endColour = new Color(0, 0, 0, 1);

            exitMemory.GetAction<StartPreloadingScene>(0).SceneName = "Organ_01";
            exitMemory.GetAction<BeginSceneTransition>(4).sceneName = "Organ_01";
            exitMemory.GetAction<BeginSceneTransition>(4).entryGateName = $"{SilkenSisters.plugin.respawnPointInstance.name}";
            SilkenSisters.Log.LogInfo($"[PhantomBoss.prepareExitMemoryEffect] Transition Gate to exit memory: {SilkenSisters.plugin.respawnPointInstance.name}");

            exitMemory.GetAction<Wait>(2).time = 2f;

            _control.AddState(exitMemory);

            _control.AddTransition("Set Data", "FINISHED", "Deep Memory Enter");
            _control.AddTransition("Deep Memory Enter", "FINISHED", "Deep Memory Enter Fall");
            _control.AddTransition("Deep Memory Enter Fall", "FINISHED", "Collapse");
            _control.AddTransition("Collapse", "FINISHED", "Exit Memory");

            resetPlayerData();

        }

        private void resetPlayerData()
        {
            HutongGames.PlayMaker.Actions.SetPlayerDataBool disablePhantom = new HutongGames.PlayMaker.Actions.SetPlayerDataBool();
            disablePhantom.boolName = "defeatedPhantom";
            disablePhantom.value = true;

            HutongGames.PlayMaker.Actions.SetPlayerDataBool world_black_thread = new HutongGames.PlayMaker.Actions.SetPlayerDataBool();
            world_black_thread.boolName = "blackThreadWorld";
            world_black_thread.value = true;

            _control.InsertAction("Collapse", disablePhantom, 0);
            _control.InsertAction("Collapse", world_black_thread, 0);
        }

        private void prepareSync()
        {
            if (SilkenSisters.syncedFight.Value && SilkenSisters.debugBuild)
            {
                _control.AddState("SilkenSync");

                if (FsmEvent.GetFsmEvent("PHANTOM_SYNC") == null)
                {
                    FsmEvent phantomSync = new FsmEvent("PHANTOM_SYNC");
                    FsmEvent.AddFsmEvent(phantomSync);
                }

                _control.ChangeTransition("Phase?", "FINISHED", "SilkenSync");
                _control.AddTransition("SilkenSync", "PHANTOM_SYNC", "Range Check");
            }
        }

        private void endHornetConstrain()
        {
            SilkenSisters.hornetConstrain.enabled = false;
            SilkenSisters.Log.LogInfo($"[PhantomBoss.endHornetConstrain] HornetConstrain?:{SilkenSisters.hornetConstrain.enabled}");
        }

    }

    internal class PhantomScene : MonoBehaviour
    {

        private PlayMakerFSM _control;

        private void Awake()
        {
            Setup();
        }

        private async Task Setup()
        {
            try
            {
                getComponents();
                disableAreaDetection();
                editFSMEvents();
                editBossTitle();
                setupHornetControl();
            }
            catch (Exception e)
            {
                SilkenSisters.Log.LogError($"{e} {e.Message}");
            }
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

            FsmEventTarget target = new FsmEventTarget();
            target.gameObject = SilkenSisters.plugin.laceNPCFSMOwner;
            target.target = FsmEventTarget.EventTarget.GameObject;

            SilkenSisters.Log.LogMessage($"[PhantomBoss.editFSMEvents] Trigger lace sit up");
            SendEventByName lace_stand_event = new SendEventByName();
            lace_stand_event.sendEvent = "ENTER";
            lace_stand_event.delay = 0;
            lace_stand_event.eventTarget = target;
            _control.AddAction("Organ Hit", lace_stand_event);

            _control.GetAction<Tk2dPlayAnimationWithEvents>("Organ Hit", 0).animationTriggerEvent = FsmEvent.GetFsmEvent("SOMETHINGELSE");
            _control.GetAction<Tk2dPlayAnimationWithEvents>("Organ Hit", 0).animationCompleteEvent = FsmEvent.GetFsmEvent("FINISHED");
            _control.AddIntVariable("Dummy");

            FsmOwnerDefault PhantomOrganOwner = new FsmOwnerDefault();
            PhantomOrganOwner.OwnerOption = OwnerDefaultOption.SpecifyGameObject;
            PhantomOrganOwner.GameObject = gameObject.FindChild("Organ Phantom");

            Tk2dPauseAnimation pausePhantom = new Tk2dPauseAnimation();
            pausePhantom.gameObject = PhantomOrganOwner;
            pausePhantom.pause = true;
            _control.AddAction("Organ Hit", pausePhantom);


            SilkenSisters.Log.LogMessage($"[PhantomBoss.editFSMEvents] Trigger lace jump");
            SendEventByName lace_jump_event = new SendEventByName();
            lace_jump_event.sendEvent = "JUMP";
            lace_jump_event.delay = 0.2f;
            lace_jump_event.eventTarget = target;
            _control.AddAction("Organ Note", lace_jump_event);
            //_control.AddAction("BG Fog", lace_jump_event);

            FunctionCall fLeft = new FunctionCall();
            fLeft.FunctionName = "FaceLeft";
            SendMessage hornetFaceLeft = new SendMessage();
            hornetFaceLeft.gameObject = SilkenSisters.hornetFSMOwner;
            hornetFaceLeft.delivery = 0;
            hornetFaceLeft.options = SendMessageOptions.DontRequireReceiver;
            hornetFaceLeft.functionCall = fLeft;
            _control.AddAction("BG Fog", hornetFaceLeft);


            Tk2dPlayAnimation hornetChall = new Tk2dPlayAnimation();
            hornetChall.gameObject = SilkenSisters.hornetFSMOwner;
            hornetChall.clipName = "Challenge Talk Start";
            hornetChall.animLibName = "";
            _control.AddAction("BG Fog", hornetChall);

            _control.GetAction<Wait>("Organ Note", 3).time = 0.3f;

        }

        private void setupHornetControl()
        {
            SilkenSisters.Log.LogMessage("[PhantomBoss.setupHornetControl] Setting actions to give back hornet control");
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
            _control.GetAction<DisplayBossTitle>("Start Battle", 3).bossTitle = "SILKEN_SISTERS";
            SilkenSisters.Log.LogInfo($"[PhantomBoss.editBossTitle] NewTitleBase:{_control.GetAction<DisplayBossTitle>("Start Battle", 3).bossTitle}");
        }
        
    
    
    }

}
