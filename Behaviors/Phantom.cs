using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Silksong.AssetHelper.ManagedAssets;
using Silksong.FsmUtil;
using Silksong.FsmUtil.Actions;
using Silksong.UnityHelper.Extensions;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;

namespace SilkenSisters.Behaviors
{
    internal class PhantomBoss : MonoBehaviour
    {

        private PlayMakerFSM _control;
        private HealthManager _healthManager;

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
                    prepareSync();
                }
                else
                {
                    TriggerLace1Jump();
                }


                addDamageDelegate();
                _control.AddMethod("Final Parry", endHornetConstrain);
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
            _healthManager = gameObject.GetComponent<HealthManager>();
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
            FsmGameObject laceBossVar = _control.AddGameObjectVariable("LaceBoss");
            laceBossVar.SetName("LaceBoss");

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

            _control.DisableAction("Set Data", 2);

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

        private void endHornetConstrain()
        {
            SilkenSisters.hornetConstrain.enabled = false;
            SilkenSisters.Log.LogInfo($"[PhantomBoss.endHornetConstrain] HornetConstrain?:{SilkenSisters.hornetConstrain.enabled}");
        }

        private void addDamageDelegate()
        {
            _healthManager.initHp = SilkenSisters.plugin.MaxHP.Value + 100;
            _healthManager.HealToMax();
            _control.GetIntVariable("Dragoon HP").Value = SilkenSisters.plugin.P2HP.Value;
            _control.GetIntVariable("Rage HP").Value = SilkenSisters.plugin.P3HP.Value;
            _healthManager.TookDamage += TransferDamage;
        }

        private void TransferDamage()
        {
            SilkenSisters.Log.LogInfo($"Phantom: {_healthManager.hp}");
            SilkenSisters.Log.LogInfo($"Phantom: {_healthManager.lastHitInstance.DamageDealt}");

            HealthManager laceManager = SilkenSisters.plugin.laceBossInstance.GetComponent<HealthManager>();

            if (laceManager.hp - _healthManager.lastHitInstance.DamageDealt > 0) { laceManager.ApplyExtraDamage(_healthManager.lastHitInstance.DamageDealt); }
            
        }

        private void TriggerLace1Jump()
        {
            _control.AddAction(
                "End",
                new SendEvent
                {
                    eventTarget = new FsmEventTarget
                    {
                        target = FsmEventTarget.EventTarget.BroadcastAll
                    },
                    sendEvent = FsmEvent.GetFsmEvent("LACE JUMP"),
                    delay = 0
                }
            );
        }

        private void prepareSync()
        {
            if (SilkenSisters.syncedFight.Value && SilkenSisters.isMemory())
            {

                AddVars();

                Synchronize();
                NukeStates();

                AddParryBait();
                AddDefensiveParry();
                AddMock();
                AddHorizontalDragoon();
                AddRunStates();
                AddPhaseStates();
                AddWaitDefense();

            }
        }

        private void AddVars()
        {
            _control.AddGameObjectVariable("Lace").Value = SilkenSisters.plugin.laceBossInstance;
            _control.AddStringVariable("NextPhaseEvent");
            _control.AddFloatVariable("Hornet Facing Right");
            
            _control.AddFloatVariable("Phase Left X").Value = 73;
            _control.AddFloatVariable("Phase Right X").Value = 96;

            _control.AddVector3Variable("Lace Pos");

            _control.AddFloatVariable("Wait Time").Value = SilkenSisters.plugin.syncWaitTime.Value;
            _control.AddFloatVariable("Async Delay").Value = SilkenSisters.plugin.syncDelay.Value;

            _control.AddFloatVariable("Gather Distance").Value = SilkenSisters.plugin.syncGatherDistance.Value;
            _control.AddFloatVariable("Tele Distance").Value = SilkenSisters.plugin.syncTeleDistance.Value;

            _control.AddBoolVariable("Stun Defense").Value = false;

        }

        private void Synchronize()
        {
            _control.AddState("SyncWait");

            /*
            _control.AddAction(
                "SyncWait",
                new Tk2dPlayAnimation
                {
                    animLibName = "",
                    clipName = "Idle",
                    gameObject = new FsmOwnerDefault { OwnerOption = OwnerDefaultOption.UseOwner }
                }
            );
            //*/

            _control.AddAction(
                "SyncWait",
                new SendEventByName { 
                    eventTarget = new FsmEventTarget { 
                        gameObject = SilkenSisters.plugin.phantomBossSceneFSMOwner, 
                        fsmName = "Silken Sisters Sync Control", 
                        target = FsmEventTarget.EventTarget.GameObjectFSM 
                    }, 
                    sendEvent = "PHANTOM READY", 
                    delay = 0f,
                    everyFrame = true
                }
            );

            _control.AddTransition("SyncWait", "STAB", "Stab Antic");
            _control.AddTransition("SyncWait", "EVADE", "Evade Antic");
            _control.AddTransition("SyncWait", "PARRY", "Parry Antic");
            _control.AddTransition("SyncWait", "GTHROW", "G Throw Antic");
            _control.AddTransition("SyncWait", "ATHROW", "Set A Throw");
            _control.AddTransition("SyncWait", "DRAGOON RAGE", "Dragoon Rage");
            _control.AddTransition("SyncWait", "DRAGOON", "Normal Dragoon");

            _control.ChangeTransition("Run To", "CLOSE RANGE", "To Idle");

        }

        private void NukeStates()
        {

            _control.RemoveState("Final Phase?");
            _control.RemoveState("Rage?");
            _control.RemoveState("Dragoon?");
            _control.RemoveState("Phase?");
            _control.RemoveState("Range Check");
            _control.RemoveState("Close Range");
            _control.RemoveState("Far Range");

            _control.ChangeTransition("To Idle", "FINISHED", "SyncWait");
            _control.ChangeTransition("Sing End", "FINISHED", "To Idle");
            _control.ChangeTransition("Phase In", "FINISHED", "To Idle");

            _control.AddTransition("To Idle", "SING", "Sing");
            //_control.AddAction("To Idle", _control.GetAction<CheckHeroPerformanceRegion>("Range Check", 0));

        }

        private void AddRunStates()
        {

            _control.AddState("Run To Lace").Position = new Rect(330.0859f, 1430.102f, 121.98f, 64f);
            _control.AddTransition("SyncWait", "RUN TO LACE", "Run To Lace");
            _control.AddTransition("SyncWait", "RUN TO", "Run To Lace");
            _control.AddTransition("Run To Lace", "FINISHED", "Run To Antic");

            _control.AddAction(
                "Run To Lace", 
                new SetBoolValue
                {
                    boolValue = false,
                    boolVariable = _control.GetBoolVariable("Ran")
                }
            );

            _control.DisableAction("Run To Lace", 3);
            _control.DisableAction("Run To Lace", 4);

            _control.AddAction(
                "Run To",
                new GetXDistance
                {
                    gameObject = SilkenSisters.plugin.phantomBossFSMOwner,
                    target = _control.GetGameObjectVariable("Lace"),
                    storeResult = _control.GetFloatVariable("Distance"),
                    everyFrame = true
                }
            );

            _control.AddActions(
                "Run To",
                new FloatCompare
                {
                    float1 = _control.GetFloatVariable("Distance"),
                    float2 = SilkenSisters.plugin.syncGatherDistance.Value,
                    tolerance = 0,
                    equal = FsmEvent.GetFsmEvent("FINISHED"),
                    lessThan = FsmEvent.GetFsmEvent("FINISHED"),
                    everyFrame = true
                }
            );

            _control.AddState("Run Away Lace").Position = new Rect(330.0859f, 1430.102f, 121.98f, 64f);
            _control.AddTransition("SyncWait", "RUN AWAY LACE", "Run Away Lace");
            _control.AddTransition("Run Away Lace", "FINISHED", "Run Away Antic");

            _control.AddAction(
                "Run Away Lace",
                new SetBoolValue
                {
                    boolValue = false,
                    boolVariable = _control.GetBoolVariable("Ran")
                }
            );

            _control.AddAction(
                "Run Away",
                new GetXDistance
                {
                    gameObject = SilkenSisters.plugin.phantomBossFSMOwner,
                    target = _control.GetGameObjectVariable("Lace"),
                    storeResult = _control.GetFloatVariable("Distance"),
                    everyFrame = true
                }
            );

            _control.AddActions(
                "Run Away",
                new FloatCompare
                {
                    float1 = _control.GetFloatVariable("Distance"),
                    float2 = SilkenSisters.plugin.syncGatherDistance.Value,
                    tolerance = 0,
                    equal = FsmEvent.GetFsmEvent("FINISHED"),
                    lessThan = FsmEvent.GetFsmEvent("FINISHED"),
                    everyFrame = true
                }
            );

        }

        private void AddPhaseStates()
        {

            _control.AddState("Phase Tele").Position = new Rect(1873.398f, 1200.492f, 110.63f, 48);
            _control.AddState("Phase Dragoon").Position = new Rect(1873.398f, 1200.492f, 110.63f, 48);
            _control.AddState("Phase Throw").Position = new Rect(1873.398f, 1200.492f, 110.63f, 48);

            _control.AddTransition("SyncWait", "PHASE TELE", "Phase Tele");

            _control.AddTransition("Phase Tele", "FINISHED", "Phase Antic");
            _control.AddTransition("Phase Dragoon", "FINISHED", "Phase Antic");
            _control.AddTransition("Phase Throw", "FINISHED", "Phase Antic");

            _control.DisableAction("Phase Move", 3);
            _control.DisableAction("Phase Move", 4);

            _control.AddAction("Phase Tele", new SetStringValue { stringValue = "TELE", stringVariable = _control.GetStringVariable("NextPhaseEvent") });
            _control.AddAction("Phase Dragoon", new SetStringValue { stringValue = "DRAGOON", stringVariable = _control.GetStringVariable("NextPhaseEvent") });
            _control.AddAction("Phase Throw", new SetStringValue { stringValue = "A THROW", stringVariable = _control.GetStringVariable("NextPhaseEvent") });

            _control.AddAction(
                "Phase Move", 
                new SendEventByName { 
                    eventTarget = new FsmEventTarget
                    {
                        gameObject = SilkenSisters.plugin.phantomBossFSMOwner,
                        fsmName = "Control",
                        target = FsmEventTarget.EventTarget.GameObjectFSM,
                    },
                    sendEvent = _control.GetStringVariable("NextPhaseEvent"),
                    delay = 0
                }
            );


            _control.AddState("Phase Tele Pos").Position = new Rect(1873.398f, 1200.492f, 110.63f, 48);
            _control.AddTransition("Phase Move", "TELE", "Phase Tele Pos");
            _control.AddTransition("Phase Tele Pos", "FINISHED", "Fog In");

            _control.AddMethod("Phase Tele Pos", GetTelePos);
            _control.AddAction(
                "Phase Tele Pos", 
                new SetPosition {
                    gameObject = new FsmOwnerDefault { OwnerOption = OwnerDefaultOption.UseOwner },
                    vector = new FsmVector3 { UseVariable = true },
                    x = _control.GetFloatVariable("Tele X"),
                    y = new FsmFloat { UseVariable = true },
                    z = new FsmFloat { UseVariable = true },
                }
            );

        }

        private void AddHorizontalDragoon()
        {
            
        }

        private void AddParryBait()
        {
            _control.AddState("Phase Parry Bait").Position = new Rect(1873.398f, 1200.492f, 110.63f, 48);
            _control.CopyState("Fog In 2", "Parry Bait Fog In");
            _control.CopyState("Phase In Air", "Parry Bait In Air");

            _control.DisableAction("Parry Bait Fog In", 0);
            _control.DisableAction("Parry Bait Fog In", 1);

            _control.AddTransition("SyncWait", "PARRY BAIT", "Phase Parry Bait");
            _control.AddTransition("Phase Parry Bait", "FINISHED", "Phase Antic");

            _control.AddAction("Phase Parry Bait", new SetStringValue { stringValue = "PARRY", stringVariable = _control.GetStringVariable("NextPhaseEvent") });

            _control.AddState("Parry Bait Pos").Position = new Rect(1873.398f, 1200.492f, 110.63f, 48);
            _control.AddTransition("Phase Move", "PARRY", "Parry Bait Pos");
            _control.AddTransition("Parry Bait Pos", "FINISHED", "Parry Bait Fog In");
            _control.ChangeTransition("Parry Bait Fog In", "FINISHED", "Parry Bait In Air");
            _control.ChangeTransition("Parry Bait In Air", "FINISHED", "Parry Antic");


            _control.AddAction(
                "Parry Bait Pos", 
                new GetPosition { 
                    gameObject = SilkenSisters.hornetFSMOwner,
                    vector = _control.GetVector3Variable("Hornet Pos"),
                    x = new FsmFloat(),
                    y = new FsmFloat(),
                    z = new FsmFloat(),
                    everyFrame = false,
                }
            );
            
            _control.AddAction(
                "Parry Bait Pos", 
                new GetScale { 
                    gameObject = SilkenSisters.hornetFSMOwner,
                    vector = new FsmVector3(),
                    xScale = _control.GetFloatVariable("Hornet Facing Right"),
                    yScale = new FsmFloat(),
                    zScale = new FsmFloat(),
                    everyFrame = false,
                }
            );

            _control.AddMethod("Parry Bait Pos", SetParryBaitPos);

            _control.InsertAction(
                "Parry Bait Fog In",
                new SetPosition
                {
                    gameObject = new FsmOwnerDefault { OwnerOption = OwnerDefaultOption.UseOwner },
                    vector = _control.GetVector3Variable("Hornet Pos"),
                    x = new FsmFloat { UseVariable = true },
                    y = new FsmFloat { UseVariable = true },
                    z = new FsmFloat { UseVariable = true },
                },
                0
            );

            _control.AddAction(
                "Parry Antic",
                new SetIsKinematic2d
                {
                    gameObject = new FsmOwnerDefault { OwnerOption = OwnerDefaultOption.UseOwner },
                    isKinematic = false
                }
            );
        }

        private void AddDefensiveParry()
        {
            _control.AddState("Phase Defense Parry").Position = new Rect(1873.398f, 1200.492f, 110.63f, 48);
            _control.CopyState("Fog In 2", "Defense Parry Fog In");
            _control.CopyState("Phase In Air", "Defense Parry In Air");

            _control.DisableAction("Defense Parry Fog In", 0);
            _control.DisableAction("Defense Parry Fog In", 1);

            _control.AddTransition("SyncWait", "DEFEND", "Phase Defense Parry");
            _control.AddTransition("Phase Defense Parry", "FINISHED", "Phase Antic");

            _control.AddAction("Phase Defense Parry", new SetStringValue { stringValue = "DEFEND", stringVariable = _control.GetStringVariable("NextPhaseEvent") });

            _control.AddState("Defense Parry Pos").Position = new Rect(1873.398f, 1200.492f, 110.63f, 48);
            _control.AddTransition("Phase Move", "DEFEND", "Defense Parry Pos");
            _control.AddTransition("Defense Parry Pos", "FINISHED", "Defense Parry Fog In");
            _control.ChangeTransition("Defense Parry Fog In", "FINISHED", "Defense Parry In Air");
            _control.ChangeTransition("Defense Parry In Air", "FINISHED", "Parry Antic");


            _control.AddAction(
                "Defense Parry Pos",
                new GetPosition
                {
                    gameObject = SilkenSisters.plugin.laceBossFSMOwner,
                    vector = _control.GetVector3Variable("Lace Pos"),
                    x = new FsmFloat(),
                    y = new FsmFloat(),
                    z = new FsmFloat(),
                    everyFrame = false,
                }
            );

            _control.AddAction(
                "Defense Parry Pos",
                new GetPosition
                {
                    gameObject = SilkenSisters.hornetFSMOwner,
                    vector = _control.GetVector3Variable("Hornet Pos"),
                    x = new FsmFloat(),
                    y = new FsmFloat(),
                    z = new FsmFloat(),
                    everyFrame = false,
                }
            );

            _control.AddMethod("Defense Parry Pos", SetDefenseParryPos);

            _control.InsertAction(
                "Defense Parry Fog In",
                new SetPosition
                {
                    gameObject = SilkenSisters.plugin.phantomBossFSMOwner,
                    vector = _control.GetVector3Variable("Lace Pos"),
                    x = new FsmFloat { UseVariable = true },
                    y = new FsmFloat { UseVariable = true },
                    z = new FsmFloat { UseVariable = true },
                },
                0
            );


            _control.AddGlobalTransition("LACE STUN", "Phase Defense Parry");
            _control.InsertAction(
                "Stun Damage",
                new SendEventByNameV3
                {
                    eventTarget = new FsmEventTarget
                    {
                        gameObject = SilkenSisters.plugin.laceBossFSMOwner,
                        fsmName = "Control",
                        target = FsmEventTarget.EventTarget.GameObjectFSM
                    },
                    sendEvent = "PHANTOM STUN",
                    activeBool = _control.GetBoolVariable("Stun Defense"),
                    delay = 0
                }, 0
            );

            _control.InsertAction("Stun Damage", new SetBoolValue { boolValue = false, boolVariable = _control.GetBoolVariable("Stun Defense") }, 1);
            _control.InsertAction("Stun Recover", new SetBoolValue { boolValue = true, boolVariable = _control.GetBoolVariable("Stun Defense") }, 0);

        }

        private void AddMock()
        {
            _control.CopyState("Hornet Dead", "Mock Hornet");

            _control.AddTransition("SyncWait", "MOCK", "Mock Hornet");
            _control.AddTransition("Mock Hornet", "FINISHED", "SyncWait");
            _control.AddAction("Mock Hornet", new Wait { time = 1f });
        }

        private void AddWaitDefense()
        {

            _control.AddState("Wait Move");

            _control.AddTransition("SyncWait", "TOOK DAMAGE", "Wait Move");
            _control.AddTransition("Wait Move", "EVADE", "Evade Antic");
            _control.AddTransition("Wait Move", "PARRY", "Parry Antic");
            _control.AddTransition("Wait Move", "NONE", "SyncWait");

            _control.AddAction(
                "Wait Move",
                new SendRandomEventV4
                {
                    events = new FsmEvent[] { FsmEvent.GetFsmEvent("EVADE"), FsmEvent.GetFsmEvent("PARRY"), FsmEvent.GetFsmEvent("NONE") },
                    weights = new FsmFloat[] { 1, 1, 0 },
                    missedMax = new FsmInt[] { 10, 10, 10 },
                    eventMax = new FsmInt[] { 2, 2, 2 },
                    activeBool = true
                }
            );

        }


        private void Update()
        {
            if (SilkenSisters.plugin.syncWaitTime.Value != _control.GetFloatVariable("Wait Time").Value)
            {
                _control.GetFloatVariable("Wait Time").Value = SilkenSisters.plugin.syncWaitTime.Value;
            }

            if (SilkenSisters.plugin.syncDelay.Value != _control.GetFloatVariable("Async Delay").Value)
            {
                _control.GetFloatVariable("Async Delay").Value = SilkenSisters.plugin.syncDelay.Value;
            }

            if (SilkenSisters.plugin.syncGatherDistance.Value != _control.GetFloatVariable("Gather Distance").Value)
            {
                _control.GetFloatVariable("Gather Distance").Value = SilkenSisters.plugin.syncGatherDistance.Value;
            }

            if (SilkenSisters.plugin.syncTeleDistance.Value != _control.GetFloatVariable("Tele Distance").Value)
            {
                _control.GetFloatVariable("Tele Distance").Value = SilkenSisters.plugin.syncTeleDistance.Value;
            }

        }

        // Additional action methods
        private void SetParryBaitPos()
        {

            FsmVector3 hornet_pos = _control.GetVector3Variable("Hornet Pos");
            float hornet_facing = _control.GetFloatVariable("Hornet Facing Right").Value;

            float distance_offset = hornet_facing * SilkenSisters.plugin.ParryBaitDistance.Value;

            if (hornet_pos.value.x + distance_offset < _control.GetFloatVariable("Phase Left X").Value || 
                hornet_pos.value.x + distance_offset > _control.GetFloatVariable("Phase Right X").Value) {
                distance_offset *= -1;
            };

            hornet_pos.value.x += distance_offset;
            hornet_pos.value.y -= (0.5677f - 0.2494f);
            
        }
    
        private void SetDefenseParryPos()
        {
            FsmVector3 lace_pos = _control.GetVector3Variable("Lace Pos");
            FsmVector3 hornet_pos = _control.GetVector3Variable("Hornet Pos");

            if (lace_pos.Value.x > hornet_pos.Value.x)
            {
                lace_pos.value.x -= SilkenSisters.plugin.DefenseParryDistance.Value;
            } else if (lace_pos.Value.x < hornet_pos.Value.x) {
                lace_pos.value.x += SilkenSisters.plugin.DefenseParryDistance.Value;
            }

            lace_pos.value.y -= (0.5462f - 0.2494f);
        }

        private void GetTelePos()
        {
            _control.GetFloatVariable("Tele X").Value = SilkenSisters.plugin.phantomBossScene.GetFsm("Silken Sisters Sync Control").GetFloatVariable("Phantom X").Value;
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
            if (SilkenSisters.isMemory()) ((BoxCollider2D)GetComponent(typeof(BoxCollider2D))).enabled = false;
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

    internal class SyncControl : MonoBehaviour
    {

        private PlayMakerFSM _control;
        private FsmOwnerDefault lacefsmowner;
        private FsmOwnerDefault phantomfsmowner;
        private FsmOwnerDefault hornetfsmowner;

        private FsmEventTarget LaceTarget;
        private FsmEventTarget PhantomTarget;

        private void Awake()
        {
            if (SilkenSisters.syncedFight.Value)
            {
                Setup();
            } else
            {
                enabled = false;
            }
        }

        private void Setup()
        {
            lacefsmowner = SilkenSisters.plugin.laceBossFSMOwner;
            phantomfsmowner = SilkenSisters.plugin.phantomBossFSMOwner;
            hornetfsmowner = SilkenSisters.hornetFSMOwner;

            LaceTarget = new FsmEventTarget
            {
                gameObject = SilkenSisters.plugin.laceBossFSMOwner,
                fsmName = "Control",
                target = FsmEventTarget.EventTarget.GameObjectFSM
            };

            PhantomTarget = new FsmEventTarget
            {
                gameObject = SilkenSisters.plugin.phantomBossFSMOwner,
                fsmName = "Control",
                target = FsmEventTarget.EventTarget.GameObjectFSM
            };

            CreateControlFSM();
            CreateVariables();
            CreateStates();
            MakeTransition();
            Synchronize();
            AddBusyActions();
            SetupP3Check();
            setupHealthCheck();
            SetupRangeCheck();
            SetupRangeCheckHornet();
            SetupGatherRunMovements();
            SetupGatherTeleMovements();
            SetupSplitRunMovements();
            SetupSplitTeleMovements();
            SetupAttackChoice();
            OrderAround();

            StartFSM();
        }

        private void StartFSM()
        {
            _control.enabled = true;
            //_control.MakeLog();
        }

        private void CreateControlFSM()
        {
            _control = gameObject.AddComponent<PlayMakerFSM>();
            _control.enabled = false;
            _control.Reset();

            _control.FsmName = "Silken Sisters Sync Control";
            _control.GetState("State 1").Name = "Init";
            _control.GetState("Init").Position = new Rect(0,0,20,10);
            _control.fsm.StartState = "Init";
            
        }

        private void CreateVariables()
        {

            _control.AddIntVariable("P2 HP").Value = SilkenSisters.plugin.P2HP.Value;
            _control.AddIntVariable("P3 HP").Value = SilkenSisters.plugin.P3HP.Value;

            _control.AddBoolVariable("Did P2").Value = false;
            _control.AddBoolVariable("Did P3").Value = false;
            _control.AddBoolVariable("First Rage").Value = false;

            _control.AddBoolVariable("Synchronous").Value = true;
            _control.AddBoolVariable("Keep Close").Value = true;
            _control.AddBoolVariable("Should Hop").Value = false;

            _control.AddBoolVariable("Lace Closest").Value = false;
            _control.AddBoolVariable("Hornet Between").Value = false;

            _control.AddBoolVariable("Phantom Alternate Attack").Value = false;
            _control.AddBoolVariable("Lace Alternate Attack").Value = false;

            _control.AddBoolVariable("Can Run").Value = false;

            _control.AddBoolVariable("Can Parry").Value = true;
            _control.AddIntVariable("Parry Cooldown").Value = 0;

            _control.AddStringVariable("Phantom Alternate Attack String").Value = "";
            _control.AddStringVariable("Lace Alternate Attack String").Value = "";

            _control.AddFloatVariable("Wait Time").Value = SilkenSisters.plugin.syncWaitTime.Value;
            _control.AddFloatVariable("Async Delay").Value = SilkenSisters.plugin.syncDelay.Value;

            _control.AddFloatVariable("Gather Distance").Value = SilkenSisters.plugin.syncGatherDistance.Value;
            _control.AddFloatVariable("Tele Distance").Value = SilkenSisters.plugin.syncTeleDistance.Value;
            _control.AddFloatVariable("Range Distance").Value = SilkenSisters.plugin.syncRangeDistance.Value;

            _control.AddFloatVariable("Hornet Distance");
            _control.AddFloatVariable("Lace Hornet Distance");
            _control.AddFloatVariable("Phantom Hornet Distance");

            _control.AddFloatVariable("Phantom Lace Distance");

            _control.AddFloatVariable("Lace X");
            _control.AddFloatVariable("Phantom X");

            _control.AddGameObjectVariable("Phantom").Value = phantomfsmowner.gameObject.Value;
            _control.AddGameObjectVariable("Lace").Value = lacefsmowner.gameObject.Value;
            _control.AddGameObjectVariable("Hornet").Value = hornetfsmowner.gameObject.Value;

            _control.AddBoolVariable("Close enough");
            _control.AddBoolVariable("Should Tele");
            _control.AddBoolVariable("Should Run");
            
        }

        private void CreateStates()
        {

            _control.AddState("Wait Synchro").Position = new Rect(100, 40, 20,10);
            _control.AddState("Wait Lace").Position = new Rect(325, 150, 20, 10);
            _control.AddState("Wait Phantom").Position = new Rect(325, 250, 20, 10);
            
            // -------------
            // General position and movement
            _control.AddState("Range Check").Position = new Rect(300, -100, 20, 10);
            _control.AddState("Range Check Hornet").Position = new Rect(650, -100, 20, 10);
            _control.AddState("Health Check").Position = new Rect(300, -200, 20, 10);
            _control.AddState("P2 Shift").Position = new Rect(400, -200, 20, 10);
            _control.AddState("P3 Shift").Position = new Rect(500, -200, 20, 10);

            //_control.AddState("Close").Position = new Rect(400, 15, 20, 10);
            //_control.AddState("Far").Position = new Rect(400, 300, 20, 10);

            _control.AddState("Hornet Close").Position = new Rect(900, 15, 20, 10);
            _control.AddState("Hornet Far").Position = new Rect(900, 300, 20, 10);

            _control.AddState("Gather Run").Position = new Rect(500, 0, 20, 10);
            _control.AddState("Split Run").Position = new Rect(500, 100, 20, 10);

            _control.AddState("Gather Tele").Position = new Rect(500, 200, 20, 10);
            _control.AddState("Split Tele").Position = new Rect(500, 300, 20, 10);

            
            // -------------
            _control.AddState("Lace Busy").Position = new Rect(50, 300, 20, 10);
            _control.AddState("Phantom Busy").Position = new Rect(50, 200, 20, 10);
            
            _control.AddState("Phantom Busy Mock").Position = new Rect(-50, 200, 20, 10);
            _control.AddState("Phantom Busy Defend").Position = new Rect(-50, 200, 20, 10);
            _control.AddState("Lace Busy Mock").Position = new Rect(-50, 200, 20, 10);
            _control.AddState("Lace Busy Defend").Position = new Rect(-50, 200, 20, 10);


            _control.AddState("P3?").Position = new Rect(500, -100, 20, 10);
            _control.AddState("P3!").Position = new Rect(900, 600, 20, 10);

            // -------------
            int x = 2;
            int y = 40;
            int i = -200;
            int z = 1250;

            _control.AddState("Duo Charge Stab").Position = new Rect(z, i += y * x, 20, 10);
            _control.AddState("Duo Charge A Throw").Position = new Rect(z, i += y * x, 20, 10);
            _control.AddState("Duo Charge Dragoon").Position = new Rect(z, i += y * x, 20, 10);      // P2
            _control.AddState("Duo Charge Parry Bait").Position = new Rect(z, i += y * x, 20, 10);
            _control.AddState("Duo Charge AH Dragoon").Position = new Rect(z, i += y * x, 20, 10);   // P3

            _control.AddState("Duo J Slash Stab").Position = new Rect(z, i += y * x, 20, 10);
            _control.AddState("Duo J Slash Parry").Position = new Rect(z, i += y * x, 20, 10);
            _control.AddState("Duo J Slash G Throw").Position = new Rect(z, i += y * x, 20, 10);
            _control.AddState("Duo J Slash GH Dragoon").Position = new Rect(z, i += y * x, 20, 10);  // P3

            _control.AddState("Duo Counter Stab").Position = new Rect(z, i += y * x, 20, 10);
            _control.AddState("Duo Counter Parry").Position = new Rect(z, i += y * x, 20, 10);

            _control.AddState("Duo Combo Slash Parry").Position = new Rect(z, i += y * x, 20, 10);
            _control.AddState("Duo Combo Slash G Throw").Position = new Rect(z, i += y * x, 20, 10);
            _control.AddState("Duo Combo Slash A Throw").Position = new Rect(z, i += y * x, 20, 10);
            _control.AddState("Duo Combo Slash Dragoon").Position = new Rect(z, i += y * x, 20, 10);
            _control.AddState("Duo Combo Slash Parry Bait").Position = new Rect(z, i += y * x, 20, 10);
            _control.AddState("Duo Combo Slash AH Dragoon").Position = new Rect(z, i += y * x, 20, 10);

            _control.AddState("Duo Cross Slash Parry Bait").Position = new Rect(z, i += y * x, 20, 10);
            _control.AddState("Duo Cross Slash AH Dragoon").Position = new Rect(z, i += y * x, 20, 10);
            _control.AddState("Duo Cross Slash GH Dragoon").Position = new Rect(z, i += y * x, 20, 10);

            _control.AddState("Duo Parry Bait Stab").Position = new Rect(z, i += y * x, 20, 10);
            _control.AddState("Duo Parry Bait A Throw").Position = new Rect(z, i += y * x, 20, 10);
            _control.AddState("Duo Parry Bait Dragoon").Position = new Rect(z, i += y * x, 20, 10);
            _control.AddState("Duo Counter Parry Bait").Position = new Rect(z, i += y * x, 20, 10);

            _control.AddState("RAAAAAAGE").Position = new Rect(z, i += y * x, 20, 10);

            _control.AddState("Attack End").Position = new Rect(z + 200, -200, 20, 10);

            // -------------
            // -------------
            // -------------


            //SendRandomEventV3ActiveBool

        }

        private void MakeTransition()
        {
            _control.AddTransition("Init", "FINISHED", "Wait Synchro");

            _control.AddTransition("Wait Synchro", "PHANTOM READY", "Wait Lace");
            _control.AddTransition("Wait Synchro", "LACE READY", "Wait Phantom");

            _control.AddTransition("Wait Lace", "LACE READY", "Range Check");
            _control.AddTransition("Wait Phantom", "PHANTOM READY", "Range Check");
            
            _control.AddTransition("Wait Lace", "LACE BUSY", "Lace Busy");
            _control.AddTransition("Wait Phantom", "PHANTOM BUSY", "Phantom Busy");
            
            _control.AddTransition("Lace Busy", "MOCK", "Lace Busy Mock");
            _control.AddTransition("Lace Busy", "DEFEND", "Lace Busy Defend");
            _control.AddTransition("Phantom Busy", "MOCK", "Phantom Busy Mock");
            _control.AddTransition("Phantom Busy", "DEFEND", "Phantom Busy Defend");
            
            _control.AddTransition("Lace Busy Mock", "FINISHED", "Wait Synchro");
            _control.AddTransition("Lace Busy Defend", "FINISHED", "Wait Synchro");
            _control.AddTransition("Phantom Busy Mock", "FINISHED", "Wait Synchro");
            _control.AddTransition("Phantom Busy Defend", "FINISHED", "Wait Synchro");

            _control.AddTransition("Range Check", "GATHER R", "Gather Run");
            _control.AddTransition("Range Check", "SPLIT R", "Split Run");
            _control.AddTransition("Range Check", "GATHER T", "Gather Tele");
            _control.AddTransition("Range Check", "SPLIT T", "Split Tele");

            _control.AddTransition("Range Check", "FINISHED", "Health Check");
            _control.AddTransition("Health Check", "FINISHED", "P3?");

            _control.AddTransition("Health Check", "TO P2", "P2 Shift");
            _control.AddTransition("Health Check", "TO P3", "P3 Shift");

            _control.AddTransition("P2 Shift", "FINISHED", "Wait Synchro");
            _control.AddTransition("P3 Shift", "FINISHED", "RAAAAAAGE");

            _control.AddTransition("Gather Run", "FINISHED", "Wait Synchro");
            _control.AddTransition("Split Run", "FINISHED", "Wait Synchro");
            _control.AddTransition("Gather Tele", "FINISHED", "Wait Synchro");
            _control.AddTransition("Split Tele", "FINISHED", "Wait Synchro");

            _control.AddTransition("Range Check Hornet", "FAR", "Hornet Far");
            _control.AddTransition("Range Check Hornet", "CLOSE", "Hornet Close");

            _control.AddTransition("P3?", "NO", "Range Check Hornet");
            _control.AddTransition("P3?", "YES", "P3!");


            // Close range attacks
            _control.AddTransition("Hornet Close", "DUO JSLASH STAB", "Duo J Slash Stab");
            _control.AddTransition("Hornet Close", "DUO JSLASH PARRY", "Duo J Slash Parry");

            _control.AddTransition("Hornet Close", "DUO COUNTER STAB", "Duo Counter Stab");
            _control.AddTransition("Hornet Close", "DUO COUNTER PARRY", "Duo Counter Parry");
            _control.AddTransition("Hornet Close", "DUO COUNTER PARRY BAIT", "Duo Counter Parry Bait");

            _control.AddTransition("Hornet Close", "DUO COMBOSLASH PARRY", "Duo Combo Slash Parry");
            _control.AddTransition("Hornet Close", "DUO COMBOSLASH GTHROW", "Duo Combo Slash G Throw");
            _control.AddTransition("Hornet Close", "DUO COMBOSLASH ATHROW", "Duo Combo Slash A Throw");
            _control.AddTransition("Hornet Close", "DUO COMBOSLASH DRAGOON", "Duo Combo Slash Dragoon");
            _control.AddTransition("Hornet Close", "DUO COMBOSLASH PARRYBAIT", "Duo Combo Slash Parry Bait");
            _control.AddTransition("Hornet Close", "DUO COMBOSLASH AHDRAGOON", "Duo Combo Slash AH Dragoon");

            _control.AddTransition("Hornet Close", "DUO PARRYBAIT STAB", "Duo Parry Bait Stab");
            _control.AddTransition("Hornet Close", "DUO PARRYBAIT ATHROW", "Duo Parry Bait A Throw");
            _control.AddTransition("Hornet Close", "DUO PARRYBAIT DRAGOON", "Duo Parry Bait Dragoon");


            // Long range attacks
            _control.AddTransition("Hornet Far", "DUO CHARGE STAB", "Duo Charge Stab");
            _control.AddTransition("Hornet Far", "DUO CHARGE ATHROW", "Duo Charge A Throw");
            _control.AddTransition("Hornet Far", "DUO CHARGE DRAGOON", "Duo Charge Dragoon");
            _control.AddTransition("Hornet Far", "DUO CHARGE PARRYBAIT", "Duo Charge Parry Bait");
            _control.AddTransition("Hornet Far", "DUO CHARGE AHDRAGOON", "Duo Charge AH Dragoon");

            _control.AddTransition("Hornet Far", "DUO JSLASH STAB", "Duo J Slash Stab");
            _control.AddTransition("Hornet Far", "DUO JSLASH GTHROW", "Duo J Slash G Throw");

            _control.AddTransition("Hornet Far", "DUO COMBOSLASH GTHROW", "Duo Combo Slash G Throw");
            _control.AddTransition("Hornet Far", "DUO COMBOSLASH ATHROW", "Duo Combo Slash A Throw");
            _control.AddTransition("Hornet Far", "DUO COMBOSLASH AHDRAGOON", "Duo Combo Slash AH Dragoon");

            _control.AddTransition("Hornet Far", "DUO COMBOSLASH PARRYBAIT", "Duo Combo Slash Parry Bait");

            _control.AddTransition("Hornet Far", "DUO COUNTER PARRY BAIT", "Duo Counter Parry Bait");


            // P3 Only attacks
            _control.AddTransition("P3!", "RAGE", "RAAAAAAGE");
            _control.AddTransition("P3!", "DUO CHARGE AHDRAGOON", "Duo Charge AH Dragoon");
            _control.AddTransition("P3!", "DUO JSLASH GHDRAGOON", "Duo J Slash GH Dragoon");

            _control.AddTransition("P3!", "DUO COMBOSLASH AHDRAGOON", "Duo Combo Slash AH Dragoon");
            _control.AddTransition("P3!", "DUO CROSSSLASH PARRYBAIT", "Duo Cross Slash Parry Bait");
            _control.AddTransition("P3!", "DUO CROSSSLASH AHDRAGOON", "Duo Cross Slash AH Dragoon");
            _control.AddTransition("P3!", "DUO CROSSSLASH GHDRAGOON", "Duo Cross Slash GH Dragoon");


            // Back to Wait Sync
            _control.AddTransition("Attack End", "FINISHED", "Wait Synchro");


            _control.AddTransition("Duo Charge Stab", "FINISHED", "Attack End");
            _control.AddTransition("Duo Charge A Throw", "FINISHED", "Attack End");
            _control.AddTransition("Duo Charge Dragoon", "FINISHED", "Attack End");      // P2
            _control.AddTransition("Duo Charge Parry Bait", "FINISHED", "Attack End");
            _control.AddTransition("Duo Charge Parry Bait", "RETRY", "Range Check Hornet");
            _control.AddTransition("Duo Charge AH Dragoon", "FINISHED", "Attack End");   // P3

            _control.AddTransition("Duo J Slash Stab", "FINISHED", "Attack End");
            _control.AddTransition("Duo J Slash Parry", "FINISHED", "Attack End");
            _control.AddTransition("Duo J Slash Parry", "RETRY", "Range Check Hornet");
            _control.AddTransition("Duo J Slash G Throw", "FINISHED", "Attack End");
            _control.AddTransition("Duo J Slash GH Dragoon", "FINISHED", "Attack End");  // P3

            _control.AddTransition("Duo Counter Stab", "FINISHED", "Attack End");
            _control.AddTransition("Duo Counter Stab", "RETRY", "Range Check Hornet");
            _control.AddTransition("Duo Counter Parry", "FINISHED", "Attack End");
            _control.AddTransition("Duo Counter Parry", "RETRY", "Range Check Hornet");

            _control.AddTransition("Duo Combo Slash Parry", "FINISHED", "Attack End");
            _control.AddTransition("Duo Combo Slash Parry", "RETRY", "Range Check Hornet");
            _control.AddTransition("Duo Combo Slash G Throw", "FINISHED", "Attack End");
            _control.AddTransition("Duo Combo Slash A Throw", "FINISHED", "Attack End");
            _control.AddTransition("Duo Combo Slash Dragoon", "FINISHED", "Attack End");
            _control.AddTransition("Duo Combo Slash Parry Bait", "FINISHED", "Attack End");
            _control.AddTransition("Duo Combo Slash Parry Bait", "RETRY", "Range Check Hornet");
            _control.AddTransition("Duo Combo Slash AH Dragoon", "FINISHED", "Attack End");

            _control.AddTransition("Duo Cross Slash Parry Bait", "FINISHED", "Attack End");
            _control.AddTransition("Duo Cross Slash Parry Bait", "RETRY", "Range Check Hornet");

            _control.AddTransition("Duo Cross Slash AH Dragoon", "FINISHED", "Attack End");
            _control.AddTransition("Duo Cross Slash GH Dragoon", "FINISHED", "Attack End");

            _control.AddTransition("Duo Parry Bait Stab", "FINISHED", "Attack End");
            _control.AddTransition("Duo Parry Bait Stab", "RETRY", "Range Check Hornet");

            _control.AddTransition("Duo Parry Bait A Throw", "FINISHED", "Attack End");
            _control.AddTransition("Duo Parry Bait A Throw", "RETRY", "Range Check Hornet");

            _control.AddTransition("Duo Parry Bait Dragoon", "FINISHED", "Attack End");
            _control.AddTransition("Duo Parry Bait Dragoon", "RETRY", "Range Check Hornet");

            _control.AddTransition("Duo Counter Parry Bait", "FINISHED", "Attack End");
            _control.AddTransition("Duo Counter Parry Bait", "RETRY", "Range Check Hornet");

            _control.AddTransition("RAAAAAAGE", "FINISHED", "Attack End");

        }

        private void Synchronize()
        {

            _control.AddAction("Wait Lace", new Wait { finishEvent = FsmEvent.GetFsmEvent("LACE BUSY"), time = _control.GetFloatVariable("Wait Time") });
            _control.AddAction("Wait Phantom", new Wait { finishEvent = FsmEvent.GetFsmEvent("PHANTOM BUSY"), time = _control.GetFloatVariable("Wait Time") });

        }

        private void AddBusyActions()
        {

            _control.AddAction(
                "Lace Busy",
                new SendRandomEventV4 {
                    events = new FsmEvent[] { FsmEvent.GetFsmEvent("MOCK"), FsmEvent.GetFsmEvent("DEFEND") },
                    weights = new FsmFloat[] { 1, 3 },
                    eventMax = new FsmInt[] { 1, 10 },
                    missedMax = new FsmInt[] { 10, 10 },
                    activeBool = true
                }
            );
            
            _control.AddAction(
                "Phantom Busy",
                new SendRandomEventV4 {
                    events = new FsmEvent[] { FsmEvent.GetFsmEvent("MOCK"), FsmEvent.GetFsmEvent("DEFEND") },
                    weights = new FsmFloat[] { 1, 1 },
                    eventMax = new FsmInt[] { 2, 2 },
                    missedMax = new FsmInt[] { 2, 2 },
                    activeBool = true
                }
            );

            _control.AddAction("Lace Busy Mock", new SendEventByName { eventTarget = PhantomTarget, sendEvent = "MOCK", delay = 0 });
            _control.AddAction("Lace Busy Defend", new SendEventByName { eventTarget = PhantomTarget, sendEvent = "DEFEND", delay = 0 });
            _control.AddAction("Phantom Busy Mock", new SendEventByName { eventTarget = LaceTarget, sendEvent = "MOCK", delay = 0 });
            _control.AddAction("Phantom Busy Defend", new SendEventByName { eventTarget = LaceTarget, sendEvent = "DEFEND", delay = 0 });

        }

        private void SetupRangeCheck()
        {

            _control.AddAction("Range Check", new BoolTest
            {
                boolVariable = _control.GetBoolVariable("Can Run"),
                isFalse = FsmEvent.GetFsmEvent("FINISHED")
            });

            _control.AddAction(
                "Range Check",
                new SetBoolValue
                {
                    boolValue = false,
                    boolVariable = _control.GetBoolVariable("Can Run"),
                    everyFrame = false,
                }
            );

            // Reset the run state
            _control.AddAction(
                "Attack End",
                new SetBoolValue
                {
                    boolValue = true,
                    boolVariable = _control.GetBoolVariable("Can Run"),
                    everyFrame = false,
                }
            );

            _control.AddMethod("Range Check", IsHornetBetween);

            _control.AddAction(
                "Range Check",
                new GetXDistance
                {
                    gameObject = lacefsmowner,
                    target = _control.GetGameObjectVariable("Phantom"),
                    storeResult = _control.GetFloatVariable("Phantom Lace Distance")
                }
            );

            _control.AddAction(
                "Range Check",
                new GetXDistance
                {
                    gameObject = lacefsmowner,
                    target = _control.GetGameObjectVariable("Hornet"),
                    storeResult = _control.GetFloatVariable("Lace Hornet Distance")
                }
            );
            
            _control.AddAction(
                "Range Check",
                new GetXDistance
                {
                    gameObject = phantomfsmowner,
                    target = _control.GetGameObjectVariable("Hornet"),
                    storeResult = _control.GetFloatVariable("Phantom Hornet Distance")
                }
            );

            _control.AddAction(
                "Range Check",
                new SetBoolValue
                {
                    boolValue = false,
                    boolVariable = _control.GetBoolVariable("Lace Closest"),
                    everyFrame = false,
                }
            );

            _control.AddAction(
                "Range Check",
                new FloatTestToBool
                {
                    float1 = _control.GetFloatVariable("Lace Hornet Distance"),
                    float2 = _control.GetFloatVariable("Phantom Hornet Distance"),
                    lessThanBool = _control.GetBoolVariable("Lace Closest"),
                    equalBool = _control.GetBoolVariable("Lace Closest"),
                    greaterThanBool = new FsmBool(),
                    tolerance = 0,
                    everyFrame = false
                }
            );

            // ---------------------------

            _control.AddAction(
                "Range Check",
                new SetBoolValue
                {
                    boolValue = false,
                    boolVariable = _control.GetBoolVariable("Should Tele"),
                    everyFrame = false,
                }
            );

            _control.AddAction(
                "Range Check",
                new SetBoolValue
                {
                    boolValue = false,
                    boolVariable = _control.GetBoolVariable("Should Run"),
                    everyFrame = false,
                }
            );

            _control.AddAction(
                "Range Check",
                new SetBoolValue
                {
                    boolValue = false,
                    boolVariable = _control.GetBoolVariable("Close Enough"),
                    everyFrame = false,
                }
            );

            // ---------------------------

            _control.AddAction(
                "Range Check",
                new FloatTestToBool
                {
                    float1 = _control.GetFloatVariable("Phantom Lace Distance"),
                    float2 = _control.GetFloatVariable("Gather Distance"),
                    lessThanBool = _control.GetBoolVariable("Close Enough"),
                    equalBool = _control.GetBoolVariable("Close Enough"),
                    greaterThanBool = new FsmBool(),
                    tolerance = 0,
                    everyFrame = false
                }
            );

            _control.AddAction(
                "Range Check",
                new FloatTestToBool
                {
                    float1 = _control.GetFloatVariable("Phantom Lace Distance"),
                    float2 = _control.GetFloatVariable("Tele Distance"),
                    lessThanBool = _control.GetBoolVariable("Should Run"),
                    equalBool = _control.GetBoolVariable("Should Run"),
                    greaterThanBool = _control.GetBoolVariable("Should Tele"),
                    tolerance = 0,
                    everyFrame = false
                }
            );

            // ---------------------------

            _control.AddAction(
                "Range Check",
                new BoolAllTrue
                {
                    boolVariables = new FsmBool[] { _control.GetBoolVariable("Keep Close"), _control.GetBoolVariable("Close Enough") },
                    sendEvent = FsmEvent.GetFsmEvent("FINISHED"),
                    storeResult = new FsmBool(),
                    everyFrame = false
                }
            );

            _control.AddAction(
                "Range Check",
                new BoolAllTrue
                {
                    boolVariables = new FsmBool[] { _control.GetBoolVariable("Keep Close"), _control.GetBoolVariable("Should Run") },
                    sendEvent = FsmEvent.GetFsmEvent("GATHER R"),
                    storeResult = new FsmBool(),
                    everyFrame = false
                }
            );

            _control.AddAction(
                "Range Check",
                new BoolAllTrue
                {
                    boolVariables = new FsmBool[] { _control.GetBoolVariable("Keep Close"), _control.GetBoolVariable("Should Tele") },
                    sendEvent = FsmEvent.GetFsmEvent("GATHER T"),
                    storeResult = new FsmBool(),
                    everyFrame = false
                }
            );

            // ---------------------------

            _control.AddAction(
                "Range Check",
                new BoolTestMulti
                {
                    boolVariables = new FsmBool[] { _control.GetBoolVariable("Keep Close"), _control.GetBoolVariable("Close Enough") },
                    boolStates = new FsmBool[] { false, true },
                    trueEvent = FsmEvent.GetFsmEvent("SPLIT T"),
                    storeResult = new FsmBool(),
                    everyFrame = false
                }
            );

            _control.AddAction(
                "Range Check",
                new BoolTestMulti
                {
                    boolVariables = new FsmBool[] { _control.GetBoolVariable("Keep Close"), _control.GetBoolVariable("Should Run") },
                    boolStates = new FsmBool[] { false, true },
                    trueEvent = FsmEvent.GetFsmEvent("SPLIT R"),
                    storeResult = new FsmBool(),
                    everyFrame = false
                }
            );

            _control.AddAction(
                "Range Check",
                new BoolTestMulti
                {
                    boolVariables = new FsmBool[] { _control.GetBoolVariable("Keep Close"), _control.GetBoolVariable("Should Tele") },
                    boolStates = new FsmBool[] { false, true },
                    trueEvent = FsmEvent.GetFsmEvent("FINISHED"),
                    storeResult = new FsmBool(),
                    everyFrame = false
                }
            );

        }

        private void SetupGatherRunMovements()
        {

            _control.AddAction(
                "Gather Run",
                new SendEventByNameV3
                {
                    eventTarget = LaceTarget,
                    sendEvent = "EVADE PHANTOM",
                    everyFrame = false,
                    activeBool = _control.GetBoolVariable("Lace Closest"),
                    delay = 0
                }
            );
        
            _control.AddAction(
                "Gather Run",
                new SendEventByNameV3
                {
                    eventTarget = PhantomTarget,
                    sendEvent = "RUN TO LACE",
                    everyFrame = false,
                    activeBool = _control.GetBoolVariable("Lace Closest"),
                    delay = 0
                }
            );

            _control.AddAction(
                "Gather Run",
                new SendEventByNameV3
                {
                    eventTarget = LaceTarget,
                    sendEvent = "HOP PHANTOM",
                    everyFrame = false,
                    activeBool = true,
                    delay = 0
                }
            );
        
            _control.AddAction(
                "Gather Run",
                new SendEventByNameV3
                {
                    eventTarget = PhantomTarget,
                    sendEvent = "RUN AWAY LACE",
                    everyFrame = false,
                    activeBool = true,
                    delay = 0
                }
            );
        
        
        }

        private void SetupGatherTeleMovements()
        {

            _control.AddMethod("Gather Tele", SelectGatherTelePosX);

            _control.AddAction(
                "Gather Tele",
                new SendEventByNameV3
                {
                    eventTarget = LaceTarget,
                    sendEvent = "TELE",
                    everyFrame = false,
                    activeBool = true,
                    delay = 0
                }
            );
        
            _control.AddAction(
                "Gather Tele",
                new SendEventByNameV3
                {
                    eventTarget = PhantomTarget,
                    sendEvent = "PHASE TELE",
                    everyFrame = false,
                    activeBool = true,
                    delay = 0
                }
            );      
        
        }

        private void SetupSplitRunMovements()
        {
            _control.AddAction(
                "Split Run",
                new SendEventByNameV3
                {
                    eventTarget = LaceTarget,
                    sendEvent = "EVADE",
                    everyFrame = false,
                    activeBool = _control.GetBoolVariable("Hornet Between"),
                    delay = 0
                }
            );

            _control.AddAction(
                "Split Run",
                new SendEventByNameV3
                {
                    eventTarget = PhantomTarget,
                    sendEvent = "EVADE",
                    everyFrame = false,
                    activeBool = _control.GetBoolVariable("Hornet Between"),
                    delay = 0
                }
            );

            _control.AddAction(
                "Split Run",
                new SendEventByNameV3
                {
                    eventTarget = FsmEventTarget.Self,
                    sendEvent = "FINISHED",
                    everyFrame = false,
                    activeBool = _control.GetBoolVariable("Hornet Between"),
                    delay = 0
                }
            );

            _control.AddAction(
                "Split Run",
                new SendEventByNameV3
                {
                    eventTarget = LaceTarget,
                    sendEvent = "HOP NONE",
                    everyFrame = false,
                    activeBool = _control.GetBoolVariable("Lace Closest"),
                    delay = 0
                }
            );

            _control.AddAction(
                "Split Run",
                new SendEventByNameV3
                {
                    eventTarget = PhantomTarget,
                    sendEvent = "EVADE",
                    everyFrame = false,
                    activeBool = _control.GetBoolVariable("Lace Closest"),
                    delay = 0
                }
            );
            
            _control.AddAction(
                "Split Run",
                new SendEventByNameV3
                {
                    eventTarget = LaceTarget,
                    sendEvent = "EVADE",
                    everyFrame = false,
                    activeBool = true,
                    delay = 0
                }
            );

            _control.AddAction(
                "Split Run",
                new SendEventByNameV3
                {
                    eventTarget = PhantomTarget,
                    sendEvent = "RUN TO",
                    everyFrame = false,
                    activeBool = true,
                    delay = 0
                }
            );
        }

        private void SetupSplitTeleMovements()
        {
            _control.AddMethod("Split Tele", SelectSplitTelePosX);

            _control.AddAction(
                "Split Tele",
                new SendEventByNameV3
                {
                    eventTarget = LaceTarget,
                    sendEvent = "TELE",
                    everyFrame = false,
                    activeBool = true,
                    delay = 0
                }
            );

            _control.AddAction(
                "Split Tele",
                new SendEventByNameV3
                {
                    eventTarget = PhantomTarget,
                    sendEvent = "PHASE TELE",
                    everyFrame = false,
                    activeBool = true,
                    delay = 0
                }
            );
        }

        private void SetupRangeCheckHornet()
        {

            _control.AddMethod("Range Check Hornet", GetDuoDistanceToHornet);
            _control.AddAction(
                "Range Check Hornet",
                new FloatCompare
                {
                    float1 = _control.GetFloatVariable("Hornet Distance"),
                    float2 = _control.GetFloatVariable("Range Distance"),
                    lessThan = FsmEvent.GetFsmEvent("CLOSE"),
                    equal = FsmEvent.GetFsmEvent("CLOSE"),
                    greaterThan = FsmEvent.GetFsmEvent("FAR"),
                    tolerance = 0
                }
            );

        }

        private void setupHealthCheck()
        {

            _control.AddAction(
                "Health Check",
                new BoolTest
                {
                    boolVariable = _control.GetBoolVariable("Did P3"),
                    isTrue = FsmEvent.GetFsmEvent("FINISHED")
                }
            );
            _control.AddAction(
                "Health Check",
                new CompareHP
                {
                    enemy = _control.GetGameObjectVariable("Phantom"),
                    integer2 = _control.GetIntVariable("P3 HP"),
                    lessThan = FsmEvent.GetFsmEvent("TO P3"),
                    equal = FsmEvent.GetFsmEvent("TO P3")
                }
            );

            _control.AddAction(
                "Health Check",
                new BoolTest
                {
                    boolVariable = _control.GetBoolVariable("Did P2"),
                    isTrue = FsmEvent.GetFsmEvent("FINISHED")
                }
            );
            _control.AddAction(
                "Health Check",
                new CompareHP
                {
                    enemy = _control.GetGameObjectVariable("Phantom"),
                    integer2 = _control.GetIntVariable("P2 HP"),
                    lessThan = FsmEvent.GetFsmEvent("TO P2"),
                    equal = FsmEvent.GetFsmEvent("TO P2")
                }
            );


            _control.AddAction(
                "P2 Shift",
                new SetBoolValue
                {
                    boolVariable = _control.GetBoolVariable("Did P2"),
                    boolValue = true
                }
            );
            _control.AddAction(
                "P2 Shift",
                new SetBoolValue
                {
                    boolVariable = _control.GetBoolVariable("Keep Close"),
                    boolValue = false
                }
            );
            _control.AddAction(
                "P2 Shift",
                new SendEventByName
                {
                    eventTarget = PhantomTarget,
                    sendEvent = "DRAGOON",
                    delay = 0
                }
            );
            _control.AddAction(
                "P2 Shift",
                new SendEventByName
                {
                    eventTarget = LaceTarget,
                    sendEvent = "TO P2",
                    delay = 0
                }
            );


            _control.AddAction(
                "P3 Shift",
                new SetBoolValue
                {
                    boolVariable = _control.GetBoolVariable("Did P3"),
                    boolValue = true
                }
            );

        }

        private void SetupP3Check()
        {

            _control.AddAction(
                "P3?",
                new SendRandomEventV4
                {
                    events = new FsmEvent[] { FsmEvent.GetFsmEvent("YES"), FsmEvent.GetFsmEvent("NO") },
                    weights = new FsmFloat[] { 1, 6 },
                    eventMax = new FsmInt[] { 1, 10 },
                    missedMax = new FsmInt[] { 10, 10 },
                    activeBool = _control.GetBoolVariable("Did P3")
                }
            );

            _control.AddAction(
                "P3?",
                new SendEventByName
                {
                    eventTarget = FsmEventTarget.Self,
                    sendEvent = "NO",
                    delay = 0f,
                    everyFrame = false
                }
            );
        }

        private void SetupAttackChoice()
        {

            _control.AddAction(
                "Hornet Close",
                new SetBoolValue
                {
                    boolVariable = _control.GetBoolVariable("Should Hop"),
                    boolValue = false
                }
            );

           _control.AddAction(
                "Hornet Close",
                new SendRandomEventV4
                {
                    events = new FsmEvent[] { 
                        FsmEvent.GetFsmEvent("DUO JSLASH STAB"),
                        FsmEvent.GetFsmEvent("DUO JSLASH PARRY"),
                        FsmEvent.GetFsmEvent("DUO COUNTER STAB"),
                        FsmEvent.GetFsmEvent("DUO COUNTER PARRY"),
                        FsmEvent.GetFsmEvent("DUO COMBOSLASH PARRY"),
                        FsmEvent.GetFsmEvent("DUO COMBOSLASH GTHROW"),
                        FsmEvent.GetFsmEvent("DUO COMBOSLASH ATHROW"),
                        FsmEvent.GetFsmEvent("DUO COMBOSLASH DRAGOON"),
                        FsmEvent.GetFsmEvent("DUO COMBOSLASH PARRYBAIT"),
                        //FsmEvent.GetFsmEvent("DUO COMBOSLASH AHDRAGOON"),
                        FsmEvent.GetFsmEvent("DUO PARRYBAIT STAB"),
                        FsmEvent.GetFsmEvent("DUO PARRYBAIT ATHROW"),
                        FsmEvent.GetFsmEvent("DUO PARRYBAIT DRAGOON"),
                        FsmEvent.GetFsmEvent("DUO COUNTER PARRY BAIT"),
                    },
                    weights = new FsmFloat[] { 1, 1, 1, 1, 1, 1, 1, 0.5f, 1, 1, 1, 0.5f, 1},
                    eventMax = new FsmInt[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,1, 1, 1},
                    missedMax = new FsmInt[] { 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13 },
                    activeBool = _control.GetBoolVariable("Did P2")
                }
            );
            _control.AddAction(
                "Hornet Close",
                new SendRandomEventV4
                {
                    events = new FsmEvent[] { 
                        FsmEvent.GetFsmEvent("DUO JSLASH STAB"),
                        FsmEvent.GetFsmEvent("DUO JSLASH PARRY"),
                        FsmEvent.GetFsmEvent("DUO COUNTER STAB"),
                        FsmEvent.GetFsmEvent("DUO COUNTER PARRY"),
                        FsmEvent.GetFsmEvent("DUO COMBOSLASH PARRY"),
                        FsmEvent.GetFsmEvent("DUO COMBOSLASH GTHROW"),
                        FsmEvent.GetFsmEvent("DUO COMBOSLASH ATHROW"),
                        FsmEvent.GetFsmEvent("DUO COMBOSLASH PARRYBAIT"),
                        FsmEvent.GetFsmEvent("DUO PARRYBAIT STAB"),
                        FsmEvent.GetFsmEvent("DUO PARRYBAIT ATHROW"),
                        FsmEvent.GetFsmEvent("DUO COUNTER PARRY BAIT"),
                    },
                    weights = new FsmFloat[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
                    eventMax = new FsmInt[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, },
                    missedMax = new FsmInt[] { 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13 },
                    activeBool = true
                }
            );

            _control.AddAction(
                "Hornet Far",
                new SetBoolValue
                {
                    boolVariable = _control.GetBoolVariable("Should Hop"),
                    boolValue = true
                }
            );

            _control.AddAction(
                "Hornet Far",
                new SendRandomEventV4
                {
                    events = new FsmEvent[] { 
                        FsmEvent.GetFsmEvent("DUO CHARGE STAB"),
                        FsmEvent.GetFsmEvent("DUO CHARGE ATHROW"),
                        FsmEvent.GetFsmEvent("DUO CHARGE DRAGOON"),
                        FsmEvent.GetFsmEvent("DUO CHARGE PARRYBAIT"),
                        //FsmEvent.GetFsmEvent("DUO CHARGE AHDRAGOON"),
                        FsmEvent.GetFsmEvent("DUO JSLASH STAB"),
                        FsmEvent.GetFsmEvent("DUO JSLASH GTHROW"),
                        FsmEvent.GetFsmEvent("DUO COMBOSLASH GTHROW"),
                        FsmEvent.GetFsmEvent("DUO COMBOSLASH ATHROW"),
                        //FsmEvent.GetFsmEvent("DUO COMBOSLASH AHDRAGOON"),
                        FsmEvent.GetFsmEvent("DUO COMBOSLASH PARRYBAIT"),
                        FsmEvent.GetFsmEvent("DUO COUNTER PARRY BAIT"),
                    },
                    weights = new FsmFloat[] { 1, 1, 0.5f, 1, 1f, 1, 1, 1f, 1, 1},
                    eventMax = new FsmInt[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, },
                    missedMax = new FsmInt[] { 10, 10, 10, 10, 10, 10, 10, 10, 10, 10 },
                    activeBool = _control.GetBoolVariable("Did P2")
                }
            );
            _control.AddAction(
                "Hornet Far",
                new SendRandomEventV4
                {
                    events = new FsmEvent[] {
                        FsmEvent.GetFsmEvent("DUO CHARGE STAB"),
                        FsmEvent.GetFsmEvent("DUO CHARGE ATHROW"),
                        FsmEvent.GetFsmEvent("DUO CHARGE PARRYBAIT"),
                        FsmEvent.GetFsmEvent("DUO JSLASH STAB"),
                        FsmEvent.GetFsmEvent("DUO JSLASH GTHROW"),
                        FsmEvent.GetFsmEvent("DUO COMBOSLASH PARRYBAIT"),
                        FsmEvent.GetFsmEvent("DUO COUNTER PARRY BAIT"),
                    },
                    weights = new FsmFloat[] { 1, 1, 1, 1, 1, 1, 1 },
                    eventMax = new FsmInt[] { 1, 1, 1, 1, 1, 1, 1, },
                    missedMax = new FsmInt[] { 10, 10, 10, 10, 10, 10, 10},
                    activeBool = true
                }
            );


            _control.AddAction("P3!", new BoolFlip { boolVariable = _control.GetBoolVariable("Keep Close") });

            _control.AddAction(
                "P3!",
                new SendRandomEventV4
                {
                    events = new FsmEvent[] {
                        FsmEvent.GetFsmEvent("RAGE"),
                        FsmEvent.GetFsmEvent("DUO CHARGE AHDRAGOON"),
                        FsmEvent.GetFsmEvent("DUO JSLASH GHDRAGOON"),
                        FsmEvent.GetFsmEvent("DUO COMBOSLASH AHDRAGOON"),
                        FsmEvent.GetFsmEvent("DUO CROSSSLASH PARRYBAIT"),
                        FsmEvent.GetFsmEvent("DUO CROSSSLASH AHDRAGOON"),
                        FsmEvent.GetFsmEvent("DUO CROSSSLASH GHDRAGOON"),
                    },
                    weights = new FsmFloat[] { 0.05f, 1, 1, 1, 1, 1, 1 },
                    eventMax = new FsmInt[] { 1, 1, 1, 1, 1, 1, 1, },
                    missedMax = new FsmInt[] { 10, 10, 10, 10, 10, 10, 10 },
                    activeBool = true
                }
            );

        }

        private void OrderAround()
        {

            // ---------------------
            _control.AddAction(
                "Duo Charge Stab",
                new ConvertBoolToString
                {
                    boolVariable = _control.GetBoolVariable("Should Hop"),
                    stringVariable = _control.GetStringVariable("Lace Alternate Attack String"),
                    trueString = "HOP CHARGE",
                    falseString = "CHARGE"
                }
            );
            _control.AddAction(
                "Duo Charge Stab",
                new SendEventByNameV3
                {
                    eventTarget = LaceTarget,
                    sendEvent = _control.GetStringVariable("Lace Alternate Attack String"),
                    delay = 0,
                    activeBool = true
                }
            );
            _control.AddAction("Duo Charge Stab", new SendEventByName { eventTarget = PhantomTarget, sendEvent = "STAB", delay = 0 });

            // ---------------------

            _control.AddAction(
                "Duo Charge A Throw",
                new ConvertBoolToString
                {
                    boolVariable = _control.GetBoolVariable("Should Hop"),
                    stringVariable = _control.GetStringVariable("Lace Alternate Attack String"),
                    trueString = "HOP CHARGE",
                    falseString = "CHARGE"
                }
            );
            _control.AddAction(
                "Duo Charge A Throw",
                new SendEventByNameV3
                {
                    eventTarget = LaceTarget,
                    sendEvent = _control.GetStringVariable("Lace Alternate Attack String"),
                    delay = 0,
                    activeBool = true
                }
            );
                        
            _control.AddAction("Duo Charge A Throw", new RandomBool { storeResult = _control.GetBoolVariable("Phantom Alternate Attack") });
            _control.AddAction(
                "Duo Charge A Throw", 
                new ConvertBoolToString { 
                    boolVariable = _control.GetBoolVariable("Phantom Alternate Attack"), 
                    stringVariable = _control.GetStringVariable("Phantom Alternate Attack String") ,
                    trueString = "PHASE THROW",
                    falseString = "ATHROW"
                }
            );
            _control.AddAction(
                "Duo Charge A Throw", 
                new SendEventByNameV3 { 
                    eventTarget = PhantomTarget, 
                    sendEvent = _control.GetStringVariable("Phantom Alternate Attack String"), 
                    delay = 0,
                    activeBool = _control.GetBoolVariable("Did P2")
                }
            );
            
            _control.AddAction("Duo Charge A Throw", new SendEventByName { eventTarget = PhantomTarget, sendEvent = "ATHROW", delay = 0 });

            // ---------------------

            _control.AddAction(
                "Duo Charge Dragoon",
                new ConvertBoolToString
                {
                    boolVariable = _control.GetBoolVariable("Should Hop"),
                    stringVariable = _control.GetStringVariable("Lace Alternate Attack String"),
                    trueString = "HOP CHARGE",
                    falseString = "CHARGE"
                }
            );
            _control.AddAction(
                "Duo Charge Dragoon",
                new SendEventByNameV3
                {
                    eventTarget = LaceTarget,
                    sendEvent = _control.GetStringVariable("Lace Alternate Attack String"),
                    delay = 0,
                    activeBool = true
                }
            );
            _control.AddAction("Duo Charge Dragoon", new SendEventByName { eventTarget = PhantomTarget, sendEvent = "DRAGOON", delay = 0 });      // P2

            // ---------------------
            _control.AddAction("Duo Charge Parry Bait", new BoolTest { boolVariable = _control.GetBoolVariable("Can Parry"), isFalse = FsmEvent.GetFsmEvent("RETRY") });
            _control.AddAction("Duo Charge Parry Bait", new SetIntValue { intVariable = _control.GetIntVariable("Parry Cooldown"), intValue = SilkenSisters.plugin.ParryCooldown.Value });
            _control.AddAction(
                "Duo Charge Parry Bait",
                new ConvertBoolToString
                {
                    boolVariable = _control.GetBoolVariable("Should Hop"),
                    stringVariable = _control.GetStringVariable("Lace Alternate Attack String"),
                    trueString = "HOP CHARGE",
                    falseString = "CHARGE"
                }
            );
            _control.AddAction(
                "Duo Charge Parry Bait",
                new SendEventByNameV3
                {
                    eventTarget = LaceTarget,
                    sendEvent = _control.GetStringVariable("Lace Alternate Attack String"),
                    delay = 0,
                    activeBool = true
                }
            );
            _control.AddAction("Duo Charge Parry Bait", new SendEventByName { eventTarget = PhantomTarget, sendEvent = "PARRY BAIT", delay = 0 });

            // ---------------------
            _control.AddAction(
                "Duo Charge AH Dragoon",
                new ConvertBoolToString
                {
                    boolVariable = _control.GetBoolVariable("Should Hop"),
                    stringVariable = _control.GetStringVariable("Lace Alternate Attack String"),
                    trueString = "HOP CHARGE",
                    falseString = "CHARGE"
                }
            );
            _control.AddAction(
                "Duo Charge AH Dragoon",
                new SendEventByNameV3
                {
                    eventTarget = LaceTarget,
                    sendEvent = _control.GetStringVariable("Lace Alternate Attack String"),
                    delay = 0,
                    activeBool = true
                }
            );
            _control.AddAction("Duo Charge AH Dragoon", new SendEventByName { eventTarget = PhantomTarget, sendEvent = "DRAGOON", delay = 0 });   // P3
                                                                                                                                                  //_control.AddAction("Duo Charge AH Dragoon", new SendEventByName { eventTarget = PhantomTarget, sendEvent = "AHDRAGOON", delay = 0 });   // P3

            // ---------------------
            _control.AddAction(
                "Duo J Slash Stab",
                new ConvertBoolToString
                {
                    boolVariable = _control.GetBoolVariable("Should Hop"),
                    stringVariable = _control.GetStringVariable("Lace Alternate Attack String"),
                    trueString = "HOP JSLASH",
                    falseString = "JSLASH"
                }
            );
            _control.AddAction(
                "Duo J Slash Stab",
                new SendEventByNameV3
                {
                    eventTarget = LaceTarget,
                    sendEvent = _control.GetStringVariable("Lace Alternate Attack String"),
                    delay = 0,
                    activeBool = true
                }
            );
            _control.AddAction("Duo J Slash Stab", new SendEventByName { eventTarget = PhantomTarget, sendEvent = "STAB", delay = 0 });

            // ---------------------       
            _control.AddAction("Duo J Slash Parry", new BoolTest { boolVariable = _control.GetBoolVariable("Can Parry"), isFalse = FsmEvent.GetFsmEvent("RETRY") });
            _control.AddAction("Duo J Slash Parry", new SetIntValue { intVariable = _control.GetIntVariable("Parry Cooldown"), intValue = SilkenSisters.plugin.ParryCooldown.Value });
            _control.AddAction(
                "Duo J Slash Parry",
                new ConvertBoolToString
                {
                    boolVariable = _control.GetBoolVariable("Should Hop"),
                    stringVariable = _control.GetStringVariable("Lace Alternate Attack String"),
                    trueString = "HOP JSLASH",
                    falseString = "JSLASH"
                }
            );
            _control.AddAction(
                "Duo J Slash Parry",
                new SendEventByNameV3
                {
                    eventTarget = LaceTarget,
                    sendEvent = _control.GetStringVariable("Lace Alternate Attack String"),
                    delay = 0,
                    activeBool = true
                }
            );
            _control.AddAction("Duo J Slash Parry", new SendEventByName { eventTarget = PhantomTarget, sendEvent = "PARRY", delay = 0 });

            // ---------------------
            _control.AddAction(
                "Duo J Slash G Throw",
                new ConvertBoolToString
                {
                    boolVariable = _control.GetBoolVariable("Should Hop"),
                    stringVariable = _control.GetStringVariable("Lace Alternate Attack String"),
                    trueString = "HOP JSLASH",
                    falseString = "JSLASH"
                }
            );
            _control.AddAction(
                "Duo J Slash G Throw",
                new SendEventByNameV3
                {
                    eventTarget = LaceTarget,
                    sendEvent = _control.GetStringVariable("Lace Alternate Attack String"),
                    delay = 0,
                    activeBool = true
                }
            );
            _control.AddAction("Duo J Slash G Throw", new SendEventByName { eventTarget = PhantomTarget, sendEvent = "GTHROW", delay = 0 });

            // ---------------------
            _control.AddAction(
                "Duo J Slash GH Dragoon",
                new ConvertBoolToString
                {
                    boolVariable = _control.GetBoolVariable("Should Hop"),
                    stringVariable = _control.GetStringVariable("Lace Alternate Attack String"),
                    trueString = "HOP JSLASH",
                    falseString = "JSLASH"
                }
            );
            _control.AddAction(
                "Duo J Slash GH Dragoon",
                new SendEventByNameV3
                {
                    eventTarget = LaceTarget,
                    sendEvent = _control.GetStringVariable("Lace Alternate Attack String"),
                    delay = 0,
                    activeBool = true
                }
            );
            _control.AddAction("Duo J Slash GH Dragoon", new SendEventByName { eventTarget = PhantomTarget, sendEvent = "DRAGOON", delay = 0 });  // P3
                                                                                                                                                  //_control.AddAction("Duo J Slash GH Dragoon", new SendEventByName { eventTarget = PhantomTarget, sendEvent = "GHDRAGOON", delay = 0 });  // P3


            // ---------------------
            _control.AddAction("Duo Counter Stab", new BoolTest { boolVariable = _control.GetBoolVariable("Can Parry"), isFalse = FsmEvent.GetFsmEvent("RETRY") });
            _control.AddAction("Duo Counter Stab", new SetIntValue { intVariable = _control.GetIntVariable("Parry Cooldown"), intValue = SilkenSisters.plugin.ParryCooldown.Value });
            _control.AddAction("Duo Counter Stab", new SendEventByName { eventTarget = LaceTarget, sendEvent = "COUNTER", delay = 0 });
            _control.AddAction("Duo Counter Stab", new SendEventByName { eventTarget = PhantomTarget, sendEvent = "STAB", delay = 0 });

            // ---------------------
            _control.AddAction("Duo Counter Parry", new BoolTest { boolVariable = _control.GetBoolVariable("Can Parry"), isFalse = FsmEvent.GetFsmEvent("RETRY") });
            _control.AddAction("Duo Counter Parry", new SetIntValue { intVariable = _control.GetIntVariable("Parry Cooldown"), intValue = SilkenSisters.plugin.ParryCooldown.Value });
            _control.AddAction("Duo Counter Parry", new SendEventByName { eventTarget = LaceTarget, sendEvent = "COUNTER", delay = 0 });
            _control.AddAction("Duo Counter Parry", new SendEventByName { eventTarget = PhantomTarget, sendEvent = "PARRY", delay = 0 });

            // ---------------------
            _control.AddAction("Duo Combo Slash Parry", new BoolTest { boolVariable = _control.GetBoolVariable("Can Parry"), isFalse = FsmEvent.GetFsmEvent("RETRY") });
            _control.AddAction("Duo Combo Slash Parry", new SetIntValue { intVariable = _control.GetIntVariable("Parry Cooldown"), intValue = SilkenSisters.plugin.ParryCooldown.Value });
            _control.AddAction(
                "Duo Combo Slash Parry",
                new ConvertBoolToString
                {
                    boolVariable = _control.GetBoolVariable("Should Hop"),
                    stringVariable = _control.GetStringVariable("Lace Alternate Attack String"),
                    trueString = "HOP COMBO",
                    falseString = "COMBO"
                }
            );
            _control.AddAction(
                "Duo Combo Slash Parry",
                new SendEventByNameV3
                {
                    eventTarget = LaceTarget,
                    sendEvent = _control.GetStringVariable("Lace Alternate Attack String"),
                    delay = 0,
                    activeBool = true
                }
            );

            _control.AddAction("Duo Combo Slash Parry", new SendEventByName { eventTarget = PhantomTarget, sendEvent = "PARRY", delay = 0 });

            // ---------------------
            _control.AddAction(
                "Duo Combo Slash G Throw",
                new ConvertBoolToString
                {
                    boolVariable = _control.GetBoolVariable("Should Hop"),
                    stringVariable = _control.GetStringVariable("Lace Alternate Attack String"),
                    trueString = "HOP COMBO",
                    falseString = "COMBO"
                }
            );
            _control.AddAction(
                "Duo Combo Slash G Throw",
                new SendEventByNameV3
                {
                    eventTarget = LaceTarget,
                    sendEvent = _control.GetStringVariable("Lace Alternate Attack String"),
                    delay = 0,
                    activeBool = true
                }
            );
            _control.AddAction("Duo Combo Slash G Throw", new SendEventByName { eventTarget = PhantomTarget, sendEvent = "GTHROW", delay = 0 });

            // ---------------------
            _control.AddAction(
                "Duo Combo Slash A Throw",
                new ConvertBoolToString
                {
                    boolVariable = _control.GetBoolVariable("Should Hop"),
                    stringVariable = _control.GetStringVariable("Lace Alternate Attack String"),
                    trueString = "HOP COMBO",
                    falseString = "COMBO"
                }
            );
            _control.AddAction(
                "Duo Combo Slash A Throw",
                new SendEventByNameV3
                {
                    eventTarget = LaceTarget,
                    sendEvent = _control.GetStringVariable("Lace Alternate Attack String"),
                    delay = 0,
                    activeBool = true
                }
            );
            _control.AddAction("Duo Combo Slash A Throw", new RandomBool { storeResult = _control.GetBoolVariable("Phantom Alternate Attack") });
            _control.AddAction(
                "Duo Combo Slash A Throw",
                new ConvertBoolToString
                {
                    boolVariable = _control.GetBoolVariable("Phantom Alternate Attack"),
                    stringVariable = _control.GetStringVariable("Phantom Alternate Attack String"),
                    trueString = "PHASE THROW",
                    falseString = "ATHROW"
                }
            );
            _control.AddAction(
                "Duo Combo Slash A Throw",
                new SendEventByNameV3
                {
                    eventTarget = LaceTarget,
                    sendEvent = _control.GetStringVariable("Phantom Alternate Attack String"),
                    delay = 0,
                    activeBool = _control.GetBoolVariable("Did P2")
                }
            );
            _control.AddAction("Duo Combo Slash A Throw", new SendEventByName { eventTarget = PhantomTarget, sendEvent = "ATHROW", delay = 0 });

            // ---------------------
            _control.AddAction(
                "Duo Combo Slash Dragoon",
                new ConvertBoolToString
                {
                    boolVariable = _control.GetBoolVariable("Should Hop"),
                    stringVariable = _control.GetStringVariable("Lace Alternate Attack String"),
                    trueString = "HOP COMBO",
                    falseString = "COMBO"
                }
            );
            _control.AddAction(
                "Duo Combo Slash Dragoon",
                new SendEventByNameV3
                {
                    eventTarget = LaceTarget,
                    sendEvent = _control.GetStringVariable("Lace Alternate Attack String"),
                    delay = 0,
                    activeBool = true
                }
            );
            _control.AddAction("Duo Combo Slash Dragoon", new SendEventByName { eventTarget = PhantomTarget, sendEvent = "DRAGOON", delay = 0 });

            // ---------------------
            _control.AddAction("Duo Combo Slash Parry Bait", new BoolTest { boolVariable = _control.GetBoolVariable("Can Parry"), isFalse = FsmEvent.GetFsmEvent("RETRY") });
            _control.AddAction("Duo Combo Slash Parry Bait", new SetIntValue { intVariable = _control.GetIntVariable("Parry Cooldown"), intValue = SilkenSisters.plugin.ParryCooldown.Value });
            _control.AddAction(
                "Duo Combo Slash Parry Bait",
                new ConvertBoolToString
                {
                    boolVariable = _control.GetBoolVariable("Should Hop"),
                    stringVariable = _control.GetStringVariable("Lace Alternate Attack String"),
                    trueString = "HOP COMBO",
                    falseString = "COMBO"
                }
            );
            _control.AddAction(
                "Duo Combo Slash Parry Bait",
                new SendEventByNameV3
                {
                    eventTarget = LaceTarget,
                    sendEvent = _control.GetStringVariable("Lace Alternate Attack String"),
                    delay = 0,
                    activeBool = true
                }
            );
            _control.AddAction("Duo Combo Slash Parry Bait", new SendEventByName { eventTarget = PhantomTarget, sendEvent = "PARRY BAIT", delay = 0 });

            // ---------------------
            _control.AddAction(
                "Duo Combo Slash AH Dragoon",
                new ConvertBoolToString
                {
                    boolVariable = _control.GetBoolVariable("Should Hop"),
                    stringVariable = _control.GetStringVariable("Lace Alternate Attack String"),
                    trueString = "HOP COMBO",
                    falseString = "COMBO"
                }
            );
            _control.AddAction(
                "Duo Combo Slash AH Dragoon",
                new SendEventByNameV3
                {
                    eventTarget = LaceTarget,
                    sendEvent = _control.GetStringVariable("Lace Alternate Attack String"),
                    delay = 0,
                    activeBool = true
                }
            );
            _control.AddAction("Duo Combo Slash AH Dragoon", new SendEventByName { eventTarget = PhantomTarget, sendEvent = "DRAGOON", delay = 0 });
            //_control.AddAction("Duo Combo Slash AH Dragoon", new SendEventByName { eventTarget = PhantomTarget, sendEvent = "AHDRAGOON", delay = 0 });

            // ---------------------
            _control.AddAction("Duo Cross Slash Parry Bait", new BoolTest { boolVariable = _control.GetBoolVariable("Can Parry"), isFalse = FsmEvent.GetFsmEvent("RETRY") });
            _control.AddAction("Duo Cross Slash Parry Bait", new SetIntValue { intVariable = _control.GetIntVariable("Parry Cooldown"), intValue = SilkenSisters.plugin.ParryCooldown.Value });
            _control.AddAction("Duo Cross Slash Parry Bait", new SendEventByName { eventTarget = LaceTarget, sendEvent = "CROSS SLASH", delay = 0 });
            _control.AddAction("Duo Cross Slash Parry Bait", new SendEventByName { eventTarget = PhantomTarget, sendEvent = "PARRY BAIT", delay = 0 });

            // ---------------------
            _control.AddAction("Duo Cross Slash AH Dragoon", new SendEventByName { eventTarget = LaceTarget, sendEvent = "CROSS SLASH", delay = 0 });
            _control.AddAction("Duo Cross Slash AH Dragoon", new SendEventByName { eventTarget = PhantomTarget, sendEvent = "DRAGOON", delay = 0 });
            //_control.AddAction("Duo Cross Slash AH Dragoon", new SendEventByName { eventTarget = PhantomTarget, sendEvent = "AHDRAGOON", delay = 0 });

            // ---------------------
            _control.AddAction("Duo Cross Slash GH Dragoon", new SendEventByName { eventTarget = LaceTarget, sendEvent = "CROSS SLASH", delay = 0 });
            _control.AddAction("Duo Cross Slash GH Dragoon", new SendEventByName { eventTarget = PhantomTarget, sendEvent = "DRAGOON", delay = 0 });
            //_control.AddAction("Duo Cross Slash GH Dragoon", new SendEventByName { eventTarget = PhantomTarget, sendEvent = "GHDRAGOON", delay = 0 });

            // ---------------------
            _control.AddAction("Duo Parry Bait Stab", new BoolTest { boolVariable = _control.GetBoolVariable("Can Parry"), isFalse = FsmEvent.GetFsmEvent("RETRY") });
            _control.AddAction("Duo Parry Bait Stab", new SetIntValue { intVariable = _control.GetIntVariable("Parry Cooldown"), intValue = SilkenSisters.plugin.ParryCooldown.Value });
            _control.AddAction("Duo Parry Bait Stab", new SendEventByName { eventTarget = LaceTarget, sendEvent = "PARRY BAIT", delay = 0 });
            _control.AddAction("Duo Parry Bait Stab", new SendEventByName { eventTarget = PhantomTarget, sendEvent = "STAB", delay = 0 });

            // ---------------------
            _control.AddAction("Duo Parry Bait A Throw", new BoolTest { boolVariable = _control.GetBoolVariable("Can Parry"), isFalse = FsmEvent.GetFsmEvent("RETRY") });
            _control.AddAction("Duo Parry Bait A Throw", new SetIntValue { intVariable = _control.GetIntVariable("Parry Cooldown"), intValue = SilkenSisters.plugin.ParryCooldown.Value });
            _control.AddAction("Duo Parry Bait A Throw", new SendEventByName { eventTarget = LaceTarget, sendEvent = "PARRY BAIT", delay = 0 });
            _control.AddAction("Duo Parry Bait A Throw", new RandomBool { storeResult = _control.GetBoolVariable("Phantom Alternate Attack") });
            _control.AddAction(
                "Duo Parry Bait A Throw",
                new ConvertBoolToString
                {
                    boolVariable = _control.GetBoolVariable("Phantom Alternate Attack"),
                    stringVariable = _control.GetStringVariable("Phantom Alternate Attack String"),
                    trueString = "PHASE THROW",
                    falseString = "ATHROW"
                }
            );
            _control.AddAction(
                "Duo Parry Bait A Throw",
                new SendEventByNameV3
                {
                    eventTarget = PhantomTarget,
                    sendEvent = _control.GetStringVariable("Phantom Alternate Attack String"),
                    delay = 0,
                    activeBool = _control.GetBoolVariable("Did P2")
                }
            );
            _control.AddAction("Duo Parry Bait A Throw", new SendEventByName { eventTarget = PhantomTarget, sendEvent = "ATHROW", delay = 0 });

            // ---------------------
            _control.AddAction("Duo Parry Bait Dragoon", new BoolTest { boolVariable = _control.GetBoolVariable("Can Parry"), isFalse = FsmEvent.GetFsmEvent("RETRY") });
            _control.AddAction("Duo Parry Bait Dragoon", new SetIntValue { intVariable = _control.GetIntVariable("Parry Cooldown"), intValue = SilkenSisters.plugin.ParryCooldown.Value });
            _control.AddAction("Duo Parry Bait Dragoon", new SendEventByName { eventTarget = LaceTarget, sendEvent = "PARRY BAIT", delay = 0 });
            _control.AddAction("Duo Parry Bait Dragoon", new SendEventByName { eventTarget = PhantomTarget, sendEvent = "DRAGOON", delay = 0 });

            // ---------------------
            _control.AddAction("Duo Counter Parry Bait", new BoolTest { boolVariable = _control.GetBoolVariable("Can Parry"), isFalse = FsmEvent.GetFsmEvent("RETRY") });
            _control.AddAction("Duo Counter Parry Bait", new SetIntValue { intVariable = _control.GetIntVariable("Parry Cooldown"), intValue = SilkenSisters.plugin.ParryCooldown.Value });
            _control.AddAction("Duo Counter Parry Bait", new SendEventByName { eventTarget = LaceTarget, sendEvent = "PARRY BAIT", delay = 0 });
            _control.AddAction("Duo Counter Parry Bait", new SendEventByName { eventTarget = PhantomTarget, sendEvent = "PARRY BAIT", delay = 0 });

            // ---------------------
            _control.AddAction("RAAAAAAGE", new SendEventByName { eventTarget = LaceTarget, sendEvent = "TO P3", delay = 0 });
            _control.AddAction("RAAAAAAGE", new SendEventByName { eventTarget = PhantomTarget, sendEvent = "DRAGOON RAGE", delay = 0 });



            _control.AddAction("Attack End", new IntSubtract { intVariable = _control.GetIntVariable("Parry Cooldown"), subtract = 1, perSecond = false, everyFrame = false });
            _control.AddAction("Attack End", new IntCompareToBool { 
                integer1 = _control.GetIntVariable("Parry Cooldown"), 
                integer2 = 0, 
                lessThanBool = _control.GetBoolVariable("Can Parry"),
                greaterThanBool = new FsmBool(),
                equalBool = new FsmBool(),
                everyFrame = false
            });


        }
        
        private void Update()
        {

            if (SilkenSisters.plugin.syncWaitTime.Value != _control.GetFloatVariable("Wait Time").Value)
            {
                _control.GetFloatVariable("Wait Time").Value = SilkenSisters.plugin.syncWaitTime.Value;
            }

            if (SilkenSisters.plugin.syncDelay.Value != _control.GetFloatVariable("Async Delay").Value)
            {
                _control.GetFloatVariable("Async Delay").Value = SilkenSisters.plugin.syncDelay.Value;
            }

            if (SilkenSisters.plugin.syncGatherDistance.Value != _control.GetFloatVariable("Gather Distance").Value)
            {
                _control.GetFloatVariable("Gather Distance").Value = SilkenSisters.plugin.syncGatherDistance.Value;
            }

            if (SilkenSisters.plugin.syncTeleDistance.Value != _control.GetFloatVariable("Tele Distance").Value)
            {
                _control.GetFloatVariable("Tele Distance").Value = SilkenSisters.plugin.syncTeleDistance.Value;
            }

            if (SilkenSisters.plugin.syncRangeDistance.Value != _control.GetFloatVariable("Range Distance").Value)
            {
                _control.GetFloatVariable("Range Distance").Value = SilkenSisters.plugin.syncRangeDistance.Value;
            }

            if (SilkenSisters.plugin.syncTeleDistance.Value != _control.GetFloatVariable("P2 HP").Value)
            {
                _control.GetFloatVariable("P2 HP").Value = SilkenSisters.plugin.syncTeleDistance.Value;
            }

            if (SilkenSisters.plugin.syncTeleDistance.Value != _control.GetFloatVariable("P3 HP").Value)
            {
                _control.GetFloatVariable("P3 HP").Value = SilkenSisters.plugin.syncTeleDistance.Value;
            }

        }



        // Custom Action methods
        private void GetDuoDistanceToHornet()
        {

            float laceX = lacefsmowner.gameObject.Value.transform.position.x;
            float phantomX = phantomfsmowner.gameObject.Value.transform.position.x;
            float hornetX = hornetfsmowner.gameObject.Value.transform.position.x;

            float laceDistance = Math.Abs(hornetX - laceX);
            float phantomDistance = Math.Abs(hornetX - phantomX);

            float average = (laceDistance + phantomDistance) / 2;

            SilkenSisters.Log.LogInfo($"Lace:{laceDistance}, Phantom:{phantomDistance}, Average:{average}");

            _control.GetFloatVariable("Hornet Distance").Value = average;
        }

        private void SelectGatherTelePosX()
        {

            float gatherTeleX = UnityEngine.Random.Range(76, 93);
            //SilkenSisters.Log.LogInfo($"{gatherTeleX} {hornetfsmowner.gameObject.Value.transform.position.x} {hornetfsmowner.gameObject.Value.transform.position.x - gatherTeleX}");

            while (Math.Abs(hornetfsmowner.gameObject.Value.transform.position.x - gatherTeleX) < 3)
            {
                gatherTeleX = UnityEngine.Random.Range(76, 93);
                //SilkenSisters.Log.LogInfo($"{gatherTeleX} {hornetfsmowner.gameObject.Value.transform.position.x} {hornetfsmowner.gameObject.Value.transform.position.x - gatherTeleX}");
            }
            
            if (gatherTeleX < hornetfsmowner.gameObject.Value.transform.position.x)
            {
                _control.GetFloatVariable("Lace X").Value = gatherTeleX + 0.75f;
                _control.GetFloatVariable("Phantom X").Value = gatherTeleX - 0.75f;
            } else
            {
                _control.GetFloatVariable("Lace X").Value = gatherTeleX - 0.75f;
                _control.GetFloatVariable("Phantom X").Value = gatherTeleX + 0.75f;
            }

        }

        private void SelectSplitTelePosX()
        {

            float splitTeleXLace = UnityEngine.Random.Range(76, 93);
            float splitTeleXPhantom = UnityEngine.Random.Range(76, 93);
            //SilkenSisters.Log.LogInfo($"{gatherTeleX} {hornetfsmowner.gameObject.Value.transform.position.x} {hornetfsmowner.gameObject.Value.transform.position.x - gatherTeleX}");

            while (Math.Abs(splitTeleXPhantom - splitTeleXLace) < 4)
            {
                splitTeleXLace = UnityEngine.Random.Range(76, 93);
                splitTeleXPhantom = UnityEngine.Random.Range(76, 93);
                //SilkenSisters.Log.LogInfo($"{gatherTeleX} {hornetfsmowner.gameObject.Value.transform.position.x} {hornetfsmowner.gameObject.Value.transform.position.x - gatherTeleX}");
            }
            
            _control.GetFloatVariable("Lace X").Value = splitTeleXLace;
            _control.GetFloatVariable("Phantom X").Value = splitTeleXPhantom;

        }

        private void IsHornetBetween()
        {
            _control.GetBoolVariable("Hornet Between").Value = (
                    hornetfsmowner.gameObject.Value.transform.position.x < lacefsmowner.GameObject.Value.transform.position.x &&
                    hornetfsmowner.gameObject.Value.transform.position.x > phantomfsmowner.GameObject.Value.transform.position.x
                ) || (
                    hornetfsmowner.gameObject.Value.transform.position.x > lacefsmowner.GameObject.Value.transform.position.x &&
                    hornetfsmowner.gameObject.Value.transform.position.x < phantomfsmowner.GameObject.Value.transform.position.x
                );
        }


    }

}
