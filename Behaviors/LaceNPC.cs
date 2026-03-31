using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Silksong.FsmUtil;
using Silksong.UnityHelper.Extensions;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Silksong.AssetHelper.ManagedAssets;
using System.Collections;
using HarmonyLib;

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
                SilkenSisters.Log.LogDebug($"[LaceNPC.Setup] Spawning lace on the organ bench");
                register();
                getComponents();
                AddVariables();
                disableRangeDetection();
                setPosition();
                editFSMAnimations();
                EditTransitions();
                SetConductAnimation();
                SkipDialogue();
                EditDialog();
                TakeHornetControl();
                resumePhantom();
                HornetLookAtLace();
                SilkenSisters.Log.LogDebug($"[LaceNPC.Setup] Finished setting up LaceNPC");
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
            SilkenSisters.Log.LogDebug($"[LaceNPC.disableRangeDetection] LaceNPCDetection?:{gameObject.FindChild("Start Range").activeSelf}");
        }

        private void setPosition()
        {
            _npcTransform.SetPosition3D(81.9569f, 106.2943f, 2.7723f);
            SilkenSisters.Log.LogDebug($"[LaceNPC.setPosition] position:{_npcTransform.position}");
        }

        private void SetConductAnimation()
        {

            _control.AddAction("Init", new RandomBool { storeResult = _control.GetBoolVariable("IsConducting") });
            _control.AddAction("Init", new BoolTest { boolVariable = _control.GetBoolVariable("IsConducting"), isTrue = FsmEvent.GetFsmEvent("CONDUCT") });
            _control.AddAction("Init", new BoolTest { boolVariable = _control.GetBoolVariable("IsMemory"), isFalse = FsmEvent.GetFsmEvent("CONDUCT"), isTrue = FsmEvent.GetFsmEvent("FINISHED") });

            _control.AddState("Conduct");
            _control.AddTransition("Init", "CONDUCT", "Conduct");
            _control.AddTransition("Conduct", "FINISHED", "Dormant");
            _control.AddMethod("Conduct", setConductPosition);
            _control.AddMethod("Conduct", SpawnFlies);
            
            _control.AddAction("Conduct", new Tk2dPlayAnimation { gameObject = SilkenSisters.plugin.laceNPCFSMOwner, animLibName = "", clipName = "Conduct" });

            _control.DisableAction("Take Control", 1);
            _control.AddAction("Take Control", new tk2dPlayAnimationConditional { Target = SilkenSisters.plugin.laceNPCFSMOwner, AnimName = "NPC Idle Turn Left", Condition = _control.GetBoolVariable("IsConducting") });
            
            _control.DisableAction("Sit Up", 1);
            _control.InsertAction("Sit Up", new tk2dPlayAnimationConditional { Target = SilkenSisters.plugin.laceNPCFSMOwner, AnimName = "TurnToIdle", Condition = _control.GetBoolVariable("IsConducting") }, 1);
            
            _control.AddAction("Sit Up", new tk2dPlayAnimationConditional { Target = SilkenSisters.plugin.laceNPCFSMOwner, AnimName = "SitToIdle", Condition = _control.GetBoolVariable("IsNotConducting") });
            
        }

        private void AddVariables()
        {

            _control.AddBoolVariable("IsConducting").Value = false;
            _control.AddBoolVariable("IsNotConducting").Value = true;
            //_control.AddBoolVariable("IsMemory").Value = false;
            _control.AddBoolVariable("IsMemory").Value = SilkenSisters.isMemory();
            _control.AddBoolVariable("IsNotMemory").Value = !SilkenSisters.isMemory();
            _control.AddBoolVariable("EncounteredLace1").Value = PlayerData.instance.encounteredLace1;

        }

        private void EditDialog()
        {

            //_control.DisableAction("Take Control", 2);
            _control.DisableAction("Take Control", 3);
            _control.DisableAction("Start Pause", 2);
            _control.DisableAction("Sit Up", 5);
            _control.DisableAction("Convo 3", 2);
            _control.DisableAction("Convo 3", 3);
            _control.DisableAction("Convo 3", 4);
            _control.DisableAction("Convo 3", 5);
            _control.DisableAction("Convo 4", 2);

            _control.GetAction<RunDialogue>("Convo 1", 0).PreventHeroAnimation = true;
            _control.GetAction<RunDialogue>("Convo 4", 0).Key = "LACE_MEET_4";
            _control.GetAction<EndDialogue>("End", 3).ReturnControl = false;
            _control.DisableAction("To Idle Anim", 0);
            _control.DisableAction("End Dialogue", 1);
        }

        private void EditTransitions()
        {

            _control.GetTransition("Take Control", "LAND").fsmEvent = FsmEvent.GetFsmEvent("FINISHED");

            _control.AddState("Lace Ready");
            _control.AddTransition("Lace Ready", "JUMP", "Jump Antic");
            _control.ChangeTransition("End Dialogue", "FINISHED", "Lace Ready");
            _control.ChangeTransition("Take Control", "FINISHED", "Convo 1");
        
        }

        private void editFSMAnimations()
        {
            SilkenSisters.Log.LogDebug("[LaceNPC.editFSMAnimations] Editing Lace NPC FSM");

            _control.AddMethod("Take Control", makeFliesLeave);

            SetPosition laceTargetPos = _control.GetAction<SetPosition>("Sit Up", 3);
            laceTargetPos.vector = new Vector3(81.9569f, 106.7942f, 2.7021f);
            laceTargetPos.x = 81.9569f;
            laceTargetPos.y = 106.7942f;
            laceTargetPos.z = 2.7021f;

            _control.AddMethod("Jump Away", toggleChallenge);
            _control.AddMethod("Jump Away", startConstrainHornet);

            _control.DisableAction("Jump Antic", 4);

            _control.DisableAction("Jump Away", 7);
            _control.DisableAction("Look Up End", 0);

            _control.DisableAction("End", 1);
            _control.DisableAction("End", 4);
            _control.DisableAction("End", 5);

            _control.AddAction("Look Up End", new HutongGames.PlayMaker.Actions.SetPlayerDataBool { boolName = "encounteredLace1", value = true });

        }

        private void SkipDialogue()
        {

            _control.AddTransition("Take Control", "SKIP", "Sit Up");
            _control.AddAction("Take Control", new BoolTestDelay { boolVariable = _control.GetBoolVariable("IsMemory"), isTrue = FsmEvent.GetFsmEvent("SKIP"), delay = 0.5f });
            _control.AddAction("Take Control", new BoolTestDelay { boolVariable = _control.GetBoolVariable("EncounteredLace1"), isTrue = FsmEvent.GetFsmEvent("SKIP"), delay = 0.5f });

            _control.AddTransition("Sit Up", "SKIP", "Lace Ready");
            _control.AddAction("Sit Up", new BoolTest { boolVariable = _control.GetBoolVariable("IsMemory"), isTrue = FsmEvent.GetFsmEvent("SKIP") });
            _control.AddAction("Sit Up", new BoolTest { boolVariable = _control.GetBoolVariable("EncounteredLace1"), isTrue = FsmEvent.GetFsmEvent("SKIP") });
            
        }

        private void resumePhantom()
        {
            FsmOwnerDefault PhantomOrganOwner = new FsmOwnerDefault();
            PhantomOrganOwner.OwnerOption = OwnerDefaultOption.SpecifyGameObject;
            PhantomOrganOwner.GameObject = SilkenSisters.plugin.phantomBossScene.FindChild("Organ Phantom");
            _control.AddAction("Lace Ready", new Tk2dResumeAnimation { gameObject = PhantomOrganOwner });
        }

        private void setConductPosition()
        {
            _npcTransform.position = new Vector3(81.9569f, 106.9124f, 2.9723f);
            _control.GetBoolVariable("IsConducting").Value = true;
            _control.GetBoolVariable("IsNotConducting").Value = false;
        }

        private void TakeHornetControl()
        {
            SendMessage relinquishControl = new SendMessage
            {
                gameObject = SilkenSisters.hornetFSMOwner,
                delivery = 0,
                options = SendMessageOptions.DontRequireReceiver,
                functionCall = new FunctionCall { FunctionName = "RelinquishControl" }
            };
            _control.AddAction("Take Control", relinquishControl);
            SendMessage stopAnimation = new SendMessage
            {
                gameObject = SilkenSisters.hornetFSMOwner,
                delivery = 0,
                options = SendMessageOptions.DontRequireReceiver,
                functionCall = new FunctionCall { FunctionName = "StopAnimationControl" }
            };
            _control.AddAction("Take Control", stopAnimation);
        }

        private void HornetLookAtLace()
        {
            _control.AddAction("Take Control", new FaceObjectV2
            {
                objectA = SilkenSisters.hornetFSMOwner,
                objectB = SilkenSisters.plugin.laceNPCInstance,
                newAnimationClip = "",
                playNewAnimation = false,
                spriteFacesRight = false,
                resetFrame = false,
                everyFrame = false,
                pauseBetweenTurns = 0
            });
            _control.AddAction("Take Control", new tk2dPlayAnimationConditional { Target = SilkenSisters.hornetFSMOwner, AnimName = "Turn Back Three Quarter", Condition = _control.GetBoolVariable("IsNotMemory") });
        }

        private void toggleChallenge()
        {
            if (SilkenSisters.isMemory()) { 
                SilkenSisters.plugin.challengeDialogInstance.SetActive(false);
                SilkenSisters.Log.LogDebug($"[LaceNPC.toggleChallenge] challenge?:{SilkenSisters.plugin.challengeDialogInstance.activeSelf}");
            }
        }

        private void startConstrainHornet()
        {
            SilkenSisters.hornetConstrain.enabled = true;
            SilkenSisters.Log.LogDebug($"[LaceNPC.startConstrainHornet] constrainHornet?:{SilkenSisters.hornetConstrain.enabled}");
        }

        private void SpawnFlies()
        {
            SilkenSisters.plugin.silkflies = SilkenSisters.plugin.assetManager.gameObjectCache.InstantiateAsset<GameObject>("silkfliesCache");
            SilkenSisters.plugin.silkflies.SetActive(false);
            SilkenSisters.plugin.silkflies.AddComponent<SilkFlies>();
            SilkenSisters.plugin.silkflies.SetActive(true);
        }

        private void makeFliesLeave()
        {
            if (SilkenSisters.plugin.silkflies != null) { 
                SilkenSisters.plugin.silkflies.GetComponent<SilkFlies>().Leave();
            }
        }

    }

    internal class SilkFlies : MonoBehaviour
    {

        List<GameObject> _flies = new();
        List<PlayMakerFSM> _controls = null;

        List<Vector3> _positions = [
            new Vector3(81.5f, 108.8f, 2.7723f),
            new Vector3(76,109, 2.7723f),
            new Vector3(79.5f, 106, 2.7723f),
            new Vector3(77f, 106f, 2.7723f),
            new Vector3(78.5f, 110, 2.7723f),
        ];

        private void Awake()
        {
            Setup();
        }

        private void Setup()
        {
            GetComponents();
            SpawnNewFlies(); 
            SpawnNewFlies();
            SetPositions();
            ReduceBuzz();
        }

        private void GetComponents()
        {
            for (int i = 0; i < gameObject.transform.childCount; i++) 
            {
                _flies.Add(gameObject.transform.GetChild(i).gameObject);
            }

            _controls = _flies.Select(f => f.GetFsmPreprocessed("Control")).ToList();
        }

        private void SetPositions()
        {
            for (int i = 0; i < _flies.Count; i++)
            {
                _flies[i].transform.position = _positions[i];
                _flies[i].transform.SetScaleX(0.9f);
                _flies[i].transform.SetScaleY(0.9f);
                _controls[i].GetAction<IdleBuzzV3>("Idle", 0).manualStartPos.Value = _positions[i];
            }
        }

        private void ReduceBuzz()
        {
            foreach(var fly in _controls)
            {
                fly.GetAction<IdleBuzzV3>("Idle", 0).roamingRangeX = 0.35f;
                fly.GetAction<IdleBuzzV3>("Idle", 0).roamingRangeY = 0.25f;
            }
        }

        private void SpawnNewFlies()
        {
            GameObject newfly = GameObject.Instantiate(_flies[0]);
            newfly.transform.parent = gameObject.transform;
            _flies.Add(newfly);
            _controls.Add(newfly.GetFsmPreprocessed("Control"));
        }


        public void Leave()
        {
            foreach (var fsm in _controls)
            {
                fsm.SendEvent("LEAVE");
            }
        }

    }

    internal class LaceMourning : MonoBehaviour
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
                SilkenSisters.Log.LogDebug($"[LaceNPC.Setup] Spawning lace on the organ bench");
                register();
                getComponents();
                AddVariables();

                CreateSleepAnim();
                disableRangeDetection();
                setPosition();
                SetSleepingAnimation();
                disableAnim();
                HornetLookAtLace();
                SpawnFlies();

                SetDialogue();
                WaitNeedolin();
                LaceSing();
                PunishHornet();
                SetLaceLeave();
                AddOutOfRange();

                SilkenSisters.Log.LogDebug($"[LaceNPC.Setup] Finished setting up LaceNPC");
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
            _control = gameObject.GetFsmPreprocessed("Control");
            _npcTransform = gameObject.transform;
            gameObject.FindChild("black_fader_moon").SetActive(false);

        }

        private void AddVariables()
        {
            _control.AddStringVariable("NextState");
            _control.AddIntVariable("Lace Pissed Off Count").Value = 0;
            _control.AddBoolVariable("Scoff").Value = true;
            _control.AddFloatVariable("TeleX").Value = 0;
            _control.AddFloatVariable("Needolin Range").Value = 8;
            _control.AddFloatVariable("Distance").Value = 0;
            _control.AddFloatVariable("HitDistance").Value = 2.8f;

            _control.AddGameObjectVariable("Hornet").Value = SilkenSisters.hornet;
            _control.AddGameObjectVariable("Lace").Value = gameObject;
        }

        private void disableRangeDetection()
        {
            //gameObject.FindChild("Start Range").SetActive(false);
            gameObject.FindChild("Start Range").GetComponent<BoxCollider2D>().size = new Vector2((_control.GetFloatVariable("Needolin Range").Value - 1) * 2, 55);
            SilkenSisters.Log.LogDebug($"[LaceNPC.disableRangeDetection] LaceNPCDetection?:{gameObject.FindChild("Start Range").activeSelf}");
        }

        private void setPosition()
        {
            _npcTransform.SetPosition3D(83.5752f, 107.0726f, 3.4021f);
            _npcTransform.SetScaleX(1);
            SilkenSisters.Log.LogDebug($"[LaceNPC.setPosition] position:{_npcTransform.position}");
        }

        private void CreateSleepAnim()
        {
            tk2dSpriteAnimationClip lieToWakeClip = gameObject.GetComponent<tk2dSpriteAnimator>().Library.GetClipByName("LieToWake");

            tk2dSpriteAnimationClip backToSleep = new tk2dSpriteAnimationClip();
            backToSleep.CopyFrom(lieToWakeClip);
            backToSleep.name = "BackToSleep";
            backToSleep.frames = System.Linq.Enumerable.Reverse(backToSleep.frames).Skip(2).ToArray();

            gameObject.GetComponent<tk2dSpriteAnimator>().Library.clips = gameObject.GetComponent<tk2dSpriteAnimator>().Library.clips.AddToArray(backToSleep);
            gameObject.GetComponent<tk2dSpriteAnimator>().Library.lookup["BackToSleep"] = new tk2dSpriteAnimation.AnimationInfo { clip = backToSleep, id = gameObject.GetComponent<tk2dSpriteAnimator>().Library.clips.Length };


            tk2dSpriteAnimationClip crouch = new tk2dSpriteAnimationClip();
            crouch.CopyFrom(lieToWakeClip);
            crouch.name = "Crouch";
            crouch.frames = System.Linq.Enumerable.Reverse(crouch.frames).Take(4).ToArray();

            gameObject.GetComponent<tk2dSpriteAnimator>().Library.clips = gameObject.GetComponent<tk2dSpriteAnimator>().Library.clips.AddToArray(backToSleep);
            gameObject.GetComponent<tk2dSpriteAnimator>().Library.lookup["BackToSleep"] = new tk2dSpriteAnimation.AnimationInfo { clip = backToSleep, id = gameObject.GetComponent<tk2dSpriteAnimator>().Library.clips.Length };
            
            gameObject.GetComponent<tk2dSpriteAnimator>().Library.clips = gameObject.GetComponent<tk2dSpriteAnimator>().Library.clips.AddToArray(crouch);
            gameObject.GetComponent<tk2dSpriteAnimator>().Library.lookup["Crouch"] = new tk2dSpriteAnimation.AnimationInfo { clip = crouch, id = gameObject.GetComponent<tk2dSpriteAnimator>().Library.clips.Length };
        }

        private void SetSleepingAnimation()
        {
            _control.AddAction("Dormant", new Tk2dPlayAnimation { gameObject = SilkenSisters.plugin.laceNPCFSMOwner, animLibName = "", clipName = "Lie" });
        }

        private void disableAnim()
        {

            _control.DisableAction("Jump Antic", 4);
            //_control.AddAction("Look Up End", new HutongGames.PlayMaker.Actions.SetPlayerDataBool { boolName = "encounteredLace1", value = true });

        }

        private void HornetLookAtLace()
        {
            _control.InsertActions(
                "Take Control",
                3,
                new FsmStateAction[] { 
                    new FaceObjectV2
                    {
                        objectA = SilkenSisters.hornetFSMOwner,
                        objectB = SilkenSisters.plugin.laceNPCInstance,
                        newAnimationClip = "",
                        playNewAnimation = false,
                        spriteFacesRight = false,
                        resetFrame = false,
                        everyFrame = false,
                        pauseBetweenTurns = 0
                    },
                    //new tk2dPlayAnimationConditional { Target = SilkenSisters.hornetFSMOwner, AnimName = "Taunt Back", Condition = true }
                }
            );
        }

        private void SetDialogue()
        {
            // Change TakeControl event
            _control.DisableAction("Take Control", 2);
            _control.GetTransition("Take Control", "LAND").fsmEvent = FsmEvent.GetFsmEvent("FINISHED");

            _control.AddState("Mourn Dialogue").Position = new Rect(0, 0, 20, 20);
            _control.ChangeTransition("Start Pause", "FINISHED", "Mourn Dialogue");
            _control.AddAction("Start Pause", new FaceObjectV2
                {
                    objectA = SilkenSisters.hornetFSMOwner,
                    objectB = SilkenSisters.plugin.laceNPCInstance,
                    newAnimationClip = "",
                    playNewAnimation = false,
                    spriteFacesRight = false,
                    resetFrame = false,
                    everyFrame = false,
                    pauseBetweenTurns = 0
                }
            );
            
            _control.AddAction("Start Pause", new tk2dPlayAnimationConditional { Target = SilkenSisters.hornetFSMOwner, AnimName = "Taunt Back", Condition = true });
            _control.AddAction(
                "Start Pause",
                new AudioPlayerOneShotSingle
                {
                    spawnPoint = _control.GetGameObjectVariable("Hornet"),
                    delay = 0,
                    pitchMax = 1,
                    pitchMin = 1,
                    audioClip = SilkenSisters.plugin.assetManager.audioClipCache.InstantiateAsset<AudioClip>("hornetSword"),
                    audioPlayer = SilkenSisters.plugin.assetManager.prefabCache["AudioPlayerActor"].Result,
                    volume = 1,
                    storePlayer = new FsmGameObject { name = "", useVariable = true },
                }
            );
            _control.GetState("Start Pause").GetAction<Wait>(1).time = 0.7f;

            _control.AddAction(
                "Mourn Dialogue", 
                new AudioPlayRandomVoiceFromTableV2
                {
                    gameObject = SilkenSisters.plugin.laceNPCFSMOwner,
                    audioClipTable = SilkenSisters.plugin.assetManager.audioClipTableCache["LaceWeakTalk"].Result,
                    pitchOffset = 0,
                    forcePlay = false,
                    stopPreviousSound = true
                }
            );
            _control.AddAction(
                "Mourn Dialogue",
                new RunDialogue
                {
                    Sheet = $"Mods.{SilkenSisters.Id}",
                    Key = "SILKEN_SISTERS_LACE_MOURN_1",
                    PlayerVoiceTableOverride = new FsmObject(),
                    OverrideContinue = false,
                    PreventHeroAnimation = false,
                    HideDecorators = false,
                    TextAlignment = TextAlignment.Left,
                    OffsetY = 0,
                    Target = SilkenSisters.plugin.laceNPCFSMOwner
                }
            );

            _control.AddState("End Dlg").Position = new Rect(0, 40, 20, 20);
            _control.AddTransition("Mourn Dialogue", "CONVO_END", "End Dlg");
            _control.AddAction(
                "End Dlg",
                new EndDialogue
                {
                    ReturnControl = false,
                    ReturnHUD = true,
                    Target = SilkenSisters.plugin.laceNPCFSMOwner,
                    UseChildren = false
                }
            );

            _control.AddMethod("End Dlg", DisableNpc);
            _control.AddAction(
                "End Dlg",
                new Tk2dPlayAnimationWithEvents{ gameObject = SilkenSisters.hornetFSMOwner, clipName = "Challenge Talk End", animationCompleteEvent = FsmEvent.GetFsmEvent("FINISHED") }
            );


        }

        private void WaitNeedolin()
        {
            // Remember to replace the needolin audioclip
            _control.AddState("Needolin Ready").Position = new Rect(0, 80, 20, 20);
            _control.AddState("Regain Control").Position = new Rect(0, 80, 20, 20);
            _control.AddMethod("Regain Control", DisableNpc);
            _control.AddTransition("End Dlg", "FINISHED", "Regain Control");
            _control.AddTransition("Regain Control", "FINISHED", "Needolin Ready");

            _control.AddAction("Regain Control", new SendMessage
            {
                gameObject = SilkenSisters.hornetFSMOwner,
                delivery = 0,
                options = SendMessageOptions.DontRequireReceiver,
                functionCall = new FunctionCall { FunctionName = "RegainControl" }
            });
            _control.AddAction("Regain Control", new SendMessage
            {
                gameObject = SilkenSisters.hornetFSMOwner,
                delivery = 0,
                options = SendMessageOptions.DontRequireReceiver,
                functionCall = new FunctionCall { FunctionName = "StartAnimationControl" }
            });
            _control.AddAction("Regain Control", new Wait { time = 1f, finishEvent = FsmEvent.GetFsmEvent("FINISHED") });

            _control.AddAction(
                "Needolin Ready",
                new CheckHeroPerformanceRegionV2
                {
                    Target = SilkenSisters.plugin.laceNPCFSMOwner,
                    Radius = _control.GetFloatVariable("Needolin Range"),
                    MinReactDelay = 0.5f,
                    MaxReactDelay = 1f,
                    None = new FsmEvent(""),
                    ActiveInner = FsmEvent.GetFsmEvent("NEEDOLIN"),
                    ActiveOuter = new FsmEvent(""),
                    IgnoreNeedolinRange = false,
                    UseActiveBool = false,
                    ActiveBool = false,
                    StoreState = HeroPerformanceRegion.AffectedState.None,
                    EveryFrame = true,
                }
            );

            _control.AddAction("Needolin Ready", new SendEventToRegister { eventName = "REMINDER NEEDOLIN" });
            _control.AddMethod("Needolin Ready", makeFliesPerch);
            _control.AddActions(
                "Needolin Ready",
                new FsmStateAction[]
                {
                    new GetDistanceV2
                    {
                        gameObject = new FsmOwnerDefault(),
                        target = SilkenSisters.hornet,
                        targetOffsetX = 0,
                        targetOffsetY = 0,
                        storeResult = _control.GetFloatVariable("Distance"),
                        everyFrame = true
                    },
                    new FloatCompare
                    {
                        float1 = _control.GetFloatVariable("Distance"),
                        float2 = _control.GetFloatVariable("Needolin Range"),
                        greaterThan = FsmEvent.GetFsmEvent("EXIT"),
                        equal = FsmEvent.GetFsmEvent(""),
                        lessThan = FsmEvent.GetFsmEvent(""),
                        everyFrame = true,
                        tolerance = 0
                    }
                }
            );

            _control.AddState("Hornet Playing").Position = new Rect(0, 120, 20, 20);
            _control.AddTransition("Needolin Ready", "NEEDOLIN", "Hornet Playing");
            _control.AddTransition("Hornet Playing", "END", "Needolin Ready");
            _control.AddMethod("Hornet Playing", WakeFlies);
            _control.AddAction("Hornet Playing", new SendEventToRegister { eventName = "REMINDER NEEDOLIN END" });
            _control.AddAction("Hornet Playing", new Wait { time = 5f, finishEvent = FsmEvent.GetFsmEvent("FINISHED") });
            _control.AddAction(
                "Hornet Playing",
                new CheckHeroPerformanceRegionV2
                {
                    Target = SilkenSisters.plugin.laceNPCFSMOwner,
                    Radius = _control.GetFloatVariable("Needolin Range"),
                    MinReactDelay = 0.5f,
                    MaxReactDelay = 1f,
                    None = FsmEvent.GetFsmEvent("END"),
                    ActiveInner = new FsmEvent(""),
                    ActiveOuter = new FsmEvent(""),
                    IgnoreNeedolinRange = false,
                    UseActiveBool = false,
                    ActiveBool = false,
                    StoreState = HeroPerformanceRegion.AffectedState.None,
                    EveryFrame = true
                }
            );


        }

        private void LaceSing()
        {
            _control.AddState("Lace Rise").Position = new Rect(0, 160, 20, 20);
            _control.AddTransition("Hornet Playing", "FINISHED", "Lace Rise");
            _control.AddActions(
                "Lace Rise",
                new FsmStateAction[]
                {
                    new SetStringValue { stringValue = "SLEEP", stringVariable = _control.GetStringVariable("NextState") },
                    new HutongGames.PlayMaker.Actions.SetPosition {
                        gameObject = SilkenSisters.plugin.laceNPCFSMOwner,
                        x = 83.5752f,
                        y = 106.847f,
                        z = 3.4021f,
                        vector = new Vector3(83.5752f,106.847f,3.4021f),
                        everyFrame = false
                    },
                    new AudioPlayerOneShotSingle
                    {
                        spawnPoint = _control.GetGameObjectVariable("Lace"),
                        delay = 0,
                        pitchMax = 1,
                        pitchMin = 1,
                        audioClip = SilkenSisters.plugin.assetManager.audioClipCache.InstantiateAsset<AudioClip>("laceWake"),
                        audioPlayer = SilkenSisters.plugin.assetManager.prefabCache["AudioPlayerActor"].Result,
                        volume = 1,
                        storePlayer = new FsmGameObject { name = "", useVariable = true },
                    },
                    new Tk2dPlayAnimationWithEvents{ gameObject = SilkenSisters.plugin.laceNPCFSMOwner, clipName = "LieToWake", animationCompleteEvent = FsmEvent.GetFsmEvent("FINISHED") },

                }
            );

            _control.AddState("Lace Stand").Position = new Rect(0, 240, 20, 20);
            _control.AddTransition("Lace Rise", "FINISHED", "Lace Stand");
            _control.AddActions(
                "Lace Stand",
                new FsmStateAction[]
                {
                    new Tk2dPlayAnimation { gameObject = SilkenSisters.plugin.laceNPCFSMOwner, clipName = "NPC Idle Right", animLibName = "" },
                    new SetStringValue { stringValue = "CROUCH", stringVariable = _control.GetStringVariable("NextState") },
                    new CheckHeroPerformanceRegionV2
                    {
                        Target = SilkenSisters.plugin.laceNPCFSMOwner,
                        Radius = _control.GetFloatVariable("Needolin Range"),
                        MinReactDelay = 0.5f,
                        MaxReactDelay = 0.8f,
                        None = FsmEvent.GetFsmEvent("END"),
                        ActiveInner = new FsmEvent(""),
                        ActiveOuter = new FsmEvent(""),
                        IgnoreNeedolinRange = false,
                        UseActiveBool = false,
                        ActiveBool = false,
                        StoreState = HeroPerformanceRegion.AffectedState.None,
                        EveryFrame = true
                    },
                    new Wait { time = 1f, finishEvent = FsmEvent.GetFsmEvent("FINISHED") }
                }
            );

            _control.AddState("Lace Sing").Position = new Rect(0, 300, 20, 20);
            _control.AddTransition("Lace Stand", "FINISHED", "Lace Sing");
            _control.AddActions(
                "Lace Sing",
                new FsmStateAction[]
                {
                    new Tk2dPlayAnimation{ gameObject = SilkenSisters.plugin.laceNPCFSMOwner, clipName = "Sing", animLibName = "" },
                    new SetAudioPitch
                    {
                        gameObject = new FsmOwnerDefault(),
                        pitch = 1.1f
                    },
                    new CheckHeroPerformanceRegionV2
                    {
                        Target = SilkenSisters.plugin.laceNPCFSMOwner,
                        Radius = _control.GetFloatVariable("Needolin Range"),
                        MinReactDelay = 0f,
                        MaxReactDelay = 0.5f,
                        None =  FsmEvent.GetFsmEvent("END"),
                        ActiveInner = new FsmEvent(""),
                        ActiveOuter = new FsmEvent(""),
                        IgnoreNeedolinRange = false,
                        UseActiveBool = false,
                        ActiveBool = false,
                        StoreState = HeroPerformanceRegion.AffectedState.None,
                        EveryFrame = true
                    },
                    new SetAudioClip
                    {
                        gameObject = new FsmOwnerDefault(),
                        audioClip = SilkenSisters.plugin.assetManager.audioClipCache.InstantiateAsset<AudioClip>("phantomSing"),
                        autoPlay = true,
                        stopOnExit = true
                    },
                    new SetStringValue { stringValue = "STOP SING", stringVariable = _control.GetStringVariable("NextState") },
                    new Wait { time = 15f, finishEvent = FsmEvent.GetFsmEvent("FINISHED") }
                }
            );

            _control.AddState("Lace Scoff").Position = new Rect(0, 200, 20, 20);
            _control.AddState("Lace Crouch").Position = new Rect(0, 200, 20, 20);
            _control.AddState("Lace Lays").Position = new Rect(0, 200, 20, 20);
            _control.AddState("Lace Idle").Position = new Rect(0, 200, 20, 20);
            _control.AddTransition("Lace Rise", "END", "Lace Scoff");
            _control.AddTransition("Lace Stand", "END", "Lace Scoff");
            _control.AddTransition("Lace Sing", "END", "Lace Scoff");

            _control.AddTransition("Lace Scoff", "SLEEP", "Lace Lays");
            _control.AddTransition("Lace Scoff", "CROUCH", "Lace Crouch");
            _control.AddTransition("Lace Scoff", "STOP SING", "Lace Idle");

            _control.AddTransition("Lace Idle", "FINISHED", "Lace Crouch");
            _control.AddTransition("Lace Crouch", "FINISHED", "Lace Lays");
            _control.AddTransition("Lace Lays", "FINISHED", "Needolin Ready");

            _control.AddMethod("Lace Scoff", makeFliesPerch);

            _control.AddActions(
                "Lace Scoff",
                new FsmStateAction[]
                {
                    new SetAudioPitch
                    {
                        gameObject = new FsmOwnerDefault(),
                        pitch = 1f
                    },
                    new SetBoolValue
                    {
                        boolValue = true,
                        boolVariable = _control.GetBoolVariable("Scoff")
                    },
                }
            );
            _control.AddMethod("Lace Scoff", makeFliesPerch);
            _control.AddAction("Lace Scoff", new IntAdd { add = 1, intVariable = _control.GetIntVariable("Lace Pissed Off Count") });
            _control.AddAction("Lace Scoff", new IntCompare { integer1 = _control.GetIntVariable("Lace Pissed Off Count"), integer2 = 3, equal = FsmEvent.GetFsmEvent("RAGE"), greaterThan = FsmEvent.GetFsmEvent(""), lessThan = FsmEvent.GetFsmEvent(""), everyFrame = false });
            _control.AddAction("Lace Scoff", new SendEventByName
                {
                    eventTarget = new FsmEventTarget { target = FsmEventTarget.EventTarget.Self },
                    sendEvent = _control.GetStringVariable("NextState"),
                    delay = 0f,
                    everyFrame = false
                }
            );

            _control.AddActions(
                "Lace Idle",
                new FsmStateAction[]
                {
                    // Proper sfx?
                    new Tk2dPlayAnimation { gameObject = SilkenSisters.plugin.laceNPCFSMOwner, clipName = "NPC Idle Right", animLibName = "" },
                    new Wait{ time = 1f, finishEvent = FsmEvent.GetFsmEvent("FINISHED") }
                }
            );

            _control.AddActions(
                "Lace Crouch",
                new FsmStateAction[]
                {
                    //Put proper sfx
                    new Tk2dPlayAnimation { gameObject = SilkenSisters.plugin.laceNPCFSMOwner, clipName = "Crouch", animLibName = "" },
                    new AudioPlayRandomVoiceFromTableBool
                    {
                        gameObject = SilkenSisters.plugin.laceNPCFSMOwner,
                        audioClipTable = SilkenSisters.plugin.assetManager.audioClipTableCache["LaceGrunt"].Result,
                        pitchOffset = 0,
                        forcePlay = false,
                        stopPreviousSound = true,
                        activeBool = _control.GetBoolVariable("Scoff")
                    },
                    new SetBoolValue
                    {
                        boolValue = false,
                        boolVariable = _control.GetBoolVariable("Scoff")
                    },
                    new Wait{ time = 0.5f, finishEvent = FsmEvent.GetFsmEvent("FINISHED") }
                    
                }
            );

            _control.AddActions(
                "Lace Lays",
                new FsmStateAction[]
                {
                    // Proper sfx
                    new Tk2dPlayAnimationWithEvents { gameObject = SilkenSisters.plugin.laceNPCFSMOwner, clipName = "BackToSleep", animationCompleteEvent = FsmEvent.GetFsmEvent("FINISHED") },
                    new AudioPlayRandomVoiceFromTableBool
                    {
                        gameObject = SilkenSisters.plugin.laceNPCFSMOwner,
                        audioClipTable = SilkenSisters.plugin.assetManager.audioClipTableCache["LaceGrunt"].Result,
                        pitchOffset = 0,
                        forcePlay = false,
                        stopPreviousSound = true,
                        activeBool = _control.GetBoolVariable("Scoff")
                    },
                    new SetBoolValue
                    {
                        boolValue = false,
                        boolVariable = _control.GetBoolVariable("Scoff")
                    },                 
                }
            );

            _control.AddActions(
                "Needolin Ready",
                new FsmStateAction[]
                {
                    new Tk2dPlayAnimation { gameObject = SilkenSisters.plugin.laceNPCFSMOwner, clipName = "Lie", animLibName = "" },
                    new HutongGames.PlayMaker.Actions.SetPosition {
                        gameObject = SilkenSisters.plugin.laceNPCFSMOwner,
                        x = 83.5752f,
                        y = 107.0726f,
                        z = 3.4021f,
                        vector = new Vector3(83.5752f, 107.0726f, 3.4021f),
                        everyFrame = false
                    },
                }
            );

        }

        private void PunishHornet()
        {

            _control.AddState("Lock Hornet");
            _control.AddState("Lace Rage");
            _control.AddState("Lace Tele Out");
            _control.AddState("Lace Tele In");
            _control.AddState("Lace Charge Antic");
            _control.AddState("Lace Charge Break");
            _control.AddState("Lace Charge");
            _control.AddState("Hornet Dies");
            _control.AddState("Lace Laughs");

            _control.AddTransition("Lace Scoff", "RAGE", "Lock Hornet");
            _control.AddTransition("Lock Hornet", "FINISHED", "Lace Rage");
            _control.AddTransition("Lace Rage", "FINISHED", "Lace Tele Out");
            _control.AddTransition("Lace Tele Out", "FINISHED", "Lace Tele In");
            _control.AddTransition("Lace Tele In", "FINISHED", "Lace Charge Antic");
            _control.AddTransition("Lace Charge Antic", "FINISHED", "Lace Charge Break");
            _control.AddTransition("Lace Charge Break", "FINISHED", "Lace Charge");
            _control.AddTransition("Lace Charge", "FINISHED", "Hornet Dies");
            _control.AddTransition("Hornet Dies", "FINISHED", "Lace Laughs");

            _control.AddActions(
                "Lock Hornet",
                new FsmStateAction[]
                {
                    new SendMessage
                    {
                        gameObject = SilkenSisters.hornetFSMOwner,
                        delivery = 0,
                        options = SendMessageOptions.DontRequireReceiver,
                        functionCall = new FunctionCall { FunctionName = "RelinquishControl" }
                    },
                    new SendMessage
                    {
                        gameObject = SilkenSisters.hornetFSMOwner,
                        delivery = 0,
                        options = SendMessageOptions.DontRequireReceiver,
                        functionCall = new FunctionCall { FunctionName = "StopAnimationControl" }
                    }
                }
            );

            _control.AddActions(
                "Lace Rage",
                new FsmStateAction[]
                {
                    new Tk2dPlayAnimationWithEvents
                    {
                        gameObject = new FsmOwnerDefault(),
                        clipName = "Mid Battle Roar",
                        animationCompleteEvent = FsmEvent.GetFsmEvent("FINISHED"),
                        animationTriggerEvent = FsmEvent.GetFsmEvent(""),
                    },
                    new StartRoarEmitter
                    {
                        spawnPoint = new FsmOwnerDefault(),
                        delay = 0,
                        stunHero = false,
                        roarBurst = false,
                        isSmall = false,
                        noVisualEffect = false,
                        forceThroughBind = false,
                        stopOnExit = true
                    },
                    new AudioPlayerOneShotSingle
                    {
                        spawnPoint = _control.GetGameObjectVariable("Lace"),
                        delay = 0,
                        pitchMax = 1,
                        pitchMin = 1,
                        audioClip = SilkenSisters.plugin.assetManager.audioClipCache.InstantiateAsset<AudioClip>("laceStance"),
                        audioPlayer = SilkenSisters.plugin.assetManager.prefabCache["AudioPlayerActor"].Result,
                        volume = 1,
                        storePlayer = new FsmGameObject { name = "", useVariable = true },
                    },
                    new AudioPlayerOneShotSingle
                    {
                        spawnPoint = _control.GetGameObjectVariable("Lace"),
                        delay = 0,
                        pitchMax = 1,
                        pitchMin = 1,
                        audioClip = SilkenSisters.plugin.assetManager.audioClipCache.InstantiateAsset<AudioClip>("miscRumble"),
                        audioPlayer = SilkenSisters.plugin.assetManager.prefabCache["AudioPlayerActor"].Result,
                        volume = 1,
                        storePlayer = new FsmGameObject { name = "", useVariable = true },
                    },
                    new AudioPlayRandomVoiceFromTableV2
                    {
                        gameObject = SilkenSisters.plugin.laceNPCFSMOwner,
                        audioClipTable = SilkenSisters.plugin.assetManager.audioClipTableCache["LaceWail"].Result,
                        pitchOffset = 0,
                        forcePlay = false,
                        stopPreviousSound = true
                    },
                    new FaceObjectV2
                    {
                        objectA = SilkenSisters.hornetFSMOwner,
                        objectB = gameObject,
                        playNewAnimation = false,
                        newAnimationClip = "",
                        spriteFacesRight = false,
                        resetFrame = false,
                        everyFrame = false,
                        pauseBetweenTurns = 0
                    },
                    new Tk2dPlayAnimation { gameObject = SilkenSisters.hornetFSMOwner, clipName = "Taunt Back Up", animLibName = "" },
                    new Wait{ time = 2f, finishEvent = FsmEvent.GetFsmEvent("FINISHED") }
                }
            );
            _control.AddMethod("Lace Rage", makeFliesLeave);

            _control.AddActions(
                "Lace Tele Out", 
                new FsmStateAction[] {
                    new Tk2dPlayAnimationWithEvents { gameObject = SilkenSisters.plugin.laceNPCFSMOwner, clipName = "Tele Out", animationCompleteEvent = FsmEvent.GetFsmEvent("FINISHED") },
                    new AudioPlayerOneShotSingle
                    {
                        spawnPoint = _control.GetGameObjectVariable("Lace"),
                        delay = 0,
                        pitchMax = 1,
                        pitchMin = 1,
                        audioClip = SilkenSisters.plugin.assetManager.audioClipCache.InstantiateAsset<AudioClip>("laceTeleOut"),
                        audioPlayer = SilkenSisters.plugin.assetManager.prefabCache["AudioPlayerActor"].Result,
                        volume = 1,
                        storePlayer = new FsmGameObject { name = "", useVariable = true },
                    },
                    // Add the sfx
                }
            );

            _control.AddMethod("Lace Tele In", setTeleXPos);
            _control.AddActions(
                "Lace Tele In",
                new FsmStateAction[] {
                    new SetPosition
                    {
                        x = _control.GetFloatVariable("TeleX"),
                        y = 104.5677f,
                        z = 0,
                        gameObject = new FsmOwnerDefault(),
                        space = 0,
                        vector = new FsmVector3 { UseVariable = true, Name = "" },
                        everyFrame = false,
                        lateUpdate = false,
                    },
                    new FaceObjectV2
                    {
                        objectA = new FsmOwnerDefault(),
                        objectB = SilkenSisters.hornet,
                        playNewAnimation = false,
                        newAnimationClip = "",
                        spriteFacesRight = true,
                        resetFrame = false,
                        everyFrame = false,
                        pauseBetweenTurns = 0
                    },
                    new AudioPlayerOneShotSingle 
                    {
                        spawnPoint = _control.GetGameObjectVariable("Lace"),
                        delay = 0,
                        pitchMax = 1,
                        pitchMin = 1,
                        audioClip = SilkenSisters.plugin.assetManager.audioClipCache.InstantiateAsset<AudioClip>("laceTeleIn"),
                        audioPlayer = SilkenSisters.plugin.assetManager.prefabCache["AudioPlayerActor"].Result,
                        volume = 1,
                        storePlayer = new FsmGameObject { name = "", useVariable = true },
                    },
                    new Tk2dPlayAnimationWithEvents { gameObject = SilkenSisters.plugin.laceNPCFSMOwner, clipName = "Tele In", animationCompleteEvent = FsmEvent.GetFsmEvent("FINISHED") },
                }
            );

            _control.AddActions(
                "Lace Charge Antic",
                new FsmStateAction[] {
                    new Tk2dPlayAnimation { gameObject = SilkenSisters.plugin.laceNPCFSMOwner, clipName = "Charge Antic", animLibName = "" },
                    new SetVelocityByScale
                    {
                        gameObject = new FsmOwnerDefault(),
                        speed = -32,
                        ySpeed = 0,
                        everyFrame = false
                    },
                    new DecelerateXY
                    {
                        gameObject = new FsmOwnerDefault(),
                        decelerationX = 0.825f,
                        decelerationY = 0,
                        brakeOnExit = true
                    },
                    new AudioPlayerOneShotSingle
                    {
                        spawnPoint = _control.GetGameObjectVariable("Lace"),
                        delay = 0,
                        pitchMax = 1,
                        pitchMin = 1,
                        audioClip = SilkenSisters.plugin.assetManager.audioClipCache.InstantiateAsset<AudioClip>("laceBackstep"),
                        audioPlayer = SilkenSisters.plugin.assetManager.prefabCache["AudioPlayerActor"].Result,
                        volume = 1,
                        storePlayer = new FsmGameObject { name = "", useVariable = true },
                    },
                    new AudioPlayRandomVoiceFromTableV2
                    {
                        gameObject = SilkenSisters.plugin.laceNPCFSMOwner,
                        audioClipTable = SilkenSisters.plugin.assetManager.audioClipTableCache["LaceAttack"].Result,
                        pitchOffset = 0,
                        forcePlay = false,
                        stopPreviousSound = true
                    },
                    new Wait { time = 0.15f, finishEvent = FsmEvent.GetFsmEvent("FINISHED") }
                }
            );
            
            _control.AddActions(
                "Lace Charge Break",
                new FsmStateAction[] {
                    new SetVelocity2d
                    {
                        gameObject = new FsmOwnerDefault(),
                        x = 0,
                        y = 0,
                        everyFrame = false,
                        vector = new FsmVector2{UseVariable = true, Name = ""}
                    },
                    new FaceObjectV2
                    {
                        objectA = SilkenSisters.hornetFSMOwner,
                        objectB = gameObject,
                        playNewAnimation = false,
                        newAnimationClip = "",
                        spriteFacesRight = false,
                        resetFrame = false,
                        everyFrame = false,
                        pauseBetweenTurns = 0
                    },
                    new PlayRandomAudioClipTableV3
                    {
                        Table = SilkenSisters.plugin.assetManager.audioClipTableCache["HornetSpeak"].Result,
                        SpawnPoint = SilkenSisters.hornetFSMOwner,
                        SpawnPosition = new FsmVector3{ Value = new Vector3(0,0,0) },
                        ForcePlay = true,
                        AudioPlayerPrefab = new FsmObject(),
                        StoreSpawned = new FsmGameObject()
                    },
                    new ActivateGameObject
                    {
                        gameObject = new FsmOwnerDefault{ OwnerOption = OwnerDefaultOption.SpecifyGameObject, GameObject = SilkenSisters.hornet.FindChild("Special Attacks/Parry Stance Flash") },
                        activate = true,
                        everyFrame = false,
                        recursive = false,
                        resetOnExit = false,
                    },
                    new ActivateGameObject
                    {
                        gameObject = new FsmOwnerDefault{ OwnerOption = OwnerDefaultOption.SpecifyGameObject, GameObject = SilkenSisters.hornet.FindChild("Special Attacks/Parry Thread") },
                        activate = true,
                        everyFrame = false,
                        recursive = false,
                        resetOnExit = false,
                    },
                    new Tk2dPlayAnimation { gameObject = SilkenSisters.hornetFSMOwner, clipName = "Parry Stance Ground", animLibName = "" },
                    new AudioPlayerOneShotSingle
                    {
                        spawnPoint = _control.GetGameObjectVariable("Hornet"),
                        delay = 0,
                        pitchMax = 1,
                        pitchMin = 1,
                        audioClip = SilkenSisters.plugin.assetManager.audioClipCache.InstantiateAsset<AudioClip>("hornetParry"),
                        audioPlayer = SilkenSisters.plugin.assetManager.prefabCache["AudioPlayerActor"].Result,
                        volume = 1,
                        storePlayer = new FsmGameObject { name = "", useVariable = true },
                    },
                    new SendMessage
                    {
                        gameObject = SilkenSisters.hornetFSMOwner,
                        delivery = HutongGames.PlayMaker.Actions.SendMessage.MessageType.SendMessage,
                        options = SendMessageOptions.DontRequireReceiver,
                        functionCall = new FunctionCall
                        {
                            ParameterType = "None",
                            FunctionName = "flashArmoured"
                        }
                    },
                    new Wait { time = 0.2f, finishEvent = FsmEvent.GetFsmEvent("FINISHED") }
                }
            );

            _control.AddActions(
                "Lace Charge",
                new FsmStateAction[] {
                    new Tk2dPlayAnimation { gameObject = SilkenSisters.plugin.laceNPCFSMOwner, clipName = "Charge", animLibName = "" },
                    new SetVelocityByScale
                    {
                        gameObject = new FsmOwnerDefault(),
                        speed = 70,
                        ySpeed = 0,
                        everyFrame = false
                    },
                    new DecelerateXY
                    {
                        gameObject = new FsmOwnerDefault(),
                        decelerationX = 0.80f,
                        decelerationY = 0,
                        brakeOnExit = false
                    },
                    new AudioPlayerOneShotSingle
                    {
                        spawnPoint = _control.GetGameObjectVariable("Lace"),
                        delay = 0,
                        pitchMax = 1,
                        pitchMin = 1,
                        audioClip = SilkenSisters.plugin.assetManager.audioClipCache.InstantiateAsset<AudioClip>("laceBackstep"),
                        audioPlayer = SilkenSisters.plugin.assetManager.prefabCache["AudioPlayerActor"].Result,
                        volume = 1,
                        storePlayer = new FsmGameObject { name = "", useVariable = true },
                    }, 
                    new GetDistanceV2
                    {
                        gameObject = new FsmOwnerDefault(),
                        target = SilkenSisters.hornet,
                        targetOffsetX = 0,
                        targetOffsetY = 0,
                        storeResult = _control.GetFloatVariable("Distance"),
                        everyFrame = true
                    },
                    new FloatCompare
                    {
                        float1 = _control.GetFloatVariable("Distance"),
                        float2 = _control.GetFloatVariable("HitDistance"),
                        greaterThan = FsmEvent.GetFsmEvent(""),
                        equal = FsmEvent.GetFsmEvent(""),
                        lessThan = FsmEvent.GetFsmEvent("FINISHED"),
                        everyFrame = true,
                        tolerance = 0
                    }
                    
                    // Change by a hit on hornet
                }
            );

            _control.AddActions(
                "Hornet Dies", 
                new FsmStateAction[] {
                    new SetDeathRespawn
                    {
                        gameManager = GameManager.instance.gameObject,
                        respawnFacingRight = false,
                        respawnMarkerName = "Death Respawn Marker",
                        respawnType = 0,
                    },
                    new DecelerateXY
                    {
                        gameObject = new FsmOwnerDefault(),
                        decelerationX = 0.825f,
                        decelerationY = 0,
                        brakeOnExit = true
                    },
                    new Tk2dPlayAnimationWithEvents { gameObject = SilkenSisters.plugin.laceNPCFSMOwner, clipName = "Charge Recover", animationCompleteEvent = FsmEvent.GetFsmEvent("FINISHED") },
                }
            );
            _control.AddMethod("Hornet Dies", KnockOutHornet);

            _control.AddActions(
                "Lace Laughs", 
                new FsmStateAction[] {
                    new FaceObjectV2
                    {
                        objectA = new FsmOwnerDefault(),
                        objectB = SilkenSisters.hornet,
                        playNewAnimation = false,
                        newAnimationClip = "",
                        spriteFacesRight = true,
                        resetFrame = false,
                        everyFrame = false,
                        pauseBetweenTurns = 0
                    },
                    new Tk2dPlayAnimationWithEvents { gameObject = SilkenSisters.plugin.laceNPCFSMOwner, clipName = "Laugh", animationCompleteEvent = FsmEvent.GetFsmEvent("FINISHED") },
                    new AudioPlayerOneShotSingle
                    {
                        spawnPoint = _control.GetGameObjectVariable("Hornet"),
                        delay = 0,
                        pitchMax = 1,
                        pitchMin = 1,
                        audioClip = SilkenSisters.plugin.assetManager.audioClipCache.InstantiateAsset<AudioClip>("laceLaugh"),
                        audioPlayer = SilkenSisters.plugin.assetManager.prefabCache["AudioPlayerActor"].Result,
                        volume = 1,
                        storePlayer = new FsmGameObject { name = "", useVariable = true },
                    },
                }
            );

        }

        private void SetLaceLeave()
        {
            _control.AddState("Stop Singing");
            _control.AddTransition("Lace Sing", "FINISHED", "Stop Singing");

            _control.AddActions(
                "Stop Singing", 
                new FsmStateAction[]
                {
                    new Tk2dPlayAnimation { gameObject = SilkenSisters.plugin.laceNPCFSMOwner, clipName = "NPC Idle Right", animLibName = "" },
                    new SendEventToRegister { eventName = "FSM CANCEL" },
                    new SendMessage
                    {
                        gameObject = SilkenSisters.hornetFSMOwner,
                        delivery = 0,
                        options = SendMessageOptions.DontRequireReceiver,
                        functionCall = new FunctionCall { FunctionName = "RelinquishControl" }
                    },
                    new SendMessage
                    {
                        gameObject = SilkenSisters.hornetFSMOwner,
                        delivery = 0,
                        options = SendMessageOptions.DontRequireReceiver,
                        functionCall = new FunctionCall { FunctionName = "StopAnimationControl" }
                    },
                    new Tk2dPlayAnimation { gameObject = SilkenSisters.hornetFSMOwner, clipName = "Turn Back Three Quarter", animLibName = "" },
                    new Wait { time = 1f, finishEvent = FsmEvent.GetFsmEvent("FINISHED") }
                }
            );


            _control.AddState("Lace Thanks");
            _control.AddTransition("Stop Singing", "FINISHED", "Lace Thanks");
            _control.AddActions(
                "Lace Thanks", 
                new FsmStateAction[] {

                    new AudioPlayRandomVoiceFromTableV2
                    {
                        gameObject = SilkenSisters.plugin.laceNPCFSMOwner,
                        audioClipTable = SilkenSisters.plugin.assetManager.audioClipTableCache["LaceSpeak"].Result,
                        pitchOffset = 0,
                        forcePlay = false,
                        stopPreviousSound = true
                    },
                    new RunDialogue
                    {
                        Sheet = $"Mods.{SilkenSisters.Id}",
                        Key = "SILKEN_SISTERS_LACE_MOURN_2",
                        PlayerVoiceTableOverride = new FsmObject(),
                        OverrideContinue = false,
                        PreventHeroAnimation = false,
                        HideDecorators = false,
                        TextAlignment = TextAlignment.Left,
                        OffsetY = 0,
                        Target = SilkenSisters.plugin.laceNPCFSMOwner
                    }
                }
            );


            _control.AddTransition("Lace Thanks", "CONVO_END", "End Dialogue");
        }

        private void AddOutOfRange()
        {
            _control.AddState("Needolin Range Out");
            _control.AddState("Needolin range wait");
            _control.AddTransition("Needolin Range Out", "ENTER", "Needolin range wait");
            _control.AddTransition("Needolin range wait", "FINISHED", "Needolin Ready");
            _control.AddTransition("Needolin Ready", "EXIT", "Needolin Range Out");

            _control.AddAction("Needolin Range Out", new SendEventToRegister { eventName = "REMINDER NEEDOLIN END" });
            _control.AddAction("Needolin range wait", new Wait { time = 0.2f, finishEvent = FsmEvent.GetFsmEvent("FINISHED") });
        }


        private void SpawnFlies()
        {
            SilkenSisters.plugin.silkflies = SilkenSisters.plugin.assetManager.gameObjectCache.InstantiateAsset<GameObject>("silkfliesCache");
            SilkenSisters.plugin.silkflies.SetActive(false);
            SilkenSisters.plugin.silkflies.AddComponent<MourningSilkFlies>();
            SilkenSisters.plugin.silkflies.SetActive(true);

            _control.AddMethod("Jump Away", makeFliesLeave);

        }

        // -------------

        private void setTeleXPos()
        {
            _control.GetFloatVariable("TeleX").Value = SilkenSisters.hornet.transform.position.x + 3.5f;
        }

        private void DisableNpc()
        {
            gameObject.GetComponent<PlayMakerNPC>().enabled = false;
        }

        private void WakeFlies()
        {
            if (SilkenSisters.plugin.silkflies != null)
            {
                SilkenSisters.plugin.silkflies.GetComponent<MourningSilkFlies>().Wake();
            }
        }

        private void makeFliesLeave()
        {
            if (SilkenSisters.plugin.silkflies != null)
            {
                SilkenSisters.plugin.silkflies.GetComponent<MourningSilkFlies>().Leave();
            }
        }

        private void makeFliesPerch()
        {
            if (SilkenSisters.plugin.silkflies != null)
            {
                SilkenSisters.plugin.silkflies.GetComponent<MourningSilkFlies>().Perch();
            }
        }

        private void KnockOutHornet()
        {
            StartCoroutine(HeroController.instance.Die(true, false));
        }

    }

    internal class MourningSilkFlies : MonoBehaviour
    {

        int currentFly = 0;

        GameObject _baseFly = null;
        List<GameObject> _flies = new();
        List<PlayMakerFSM> _controls = new();
        Coroutine coro;
        float flyDelay = 1f;

        List<Vector3> _positions = [
            new Vector3(82.704f, 109.3679f, 3.4468f),
            new Vector3(86.4011f, 106.8389f, 3.4468f),
            new Vector3(85.9573f, 105.7356f, 3.0432f),
            new Vector3(82.2009f, 105.5666f, 2.7087f),
            new Vector3(81.7496f, 106.8397f, 3.9087f),
        ];

        List<Vector3> _flightPositions = [
            new Vector3(83.3257f, 110.2663f, 3.4468f),
            new Vector3(86.4011f, 109.0555f, 3.4468f),
            new Vector3(86.0776f, 107.0265f, 3.0432f),
            new Vector3(81.2117f, 106.7379f, 2.7087f),
            new Vector3(81.2605f, 109.0688f, 3.9087f),
        ];

        private void Awake()
        {
            Setup();
        }

        private void Setup()
        {
            GetComponents();
            SpawnNewFlies();
            SpawnNewFlies();
            SpawnNewFlies();
            SpawnNewFlies();
            SpawnNewFlies();
            SetPositions();
            ReduceBuzz();
            AddPerch();

            foreach (var f in _controls)
            {
                f.SetState("Pause");
                f.enabled = true;
            }
        }

        private void GetComponents()
        {

            _baseFly = gameObject.transform.GetChild(0).gameObject;

            for (int i = 0; i < gameObject.transform.childCount; i++)
            {
                gameObject.transform.GetChild(i).gameObject.SetActive(false);
                gameObject.transform.GetChild(i).gameObject.GetComponent<PlayFromRandomFrameMecanim>().enabled = false;
            }

        }

        private void SetPositions()
        {
            for (int i = 0; i < _flies.Count; i++)
            {
                _flies[i].transform.position = _positions[i];
                _flies[i].transform.SetScaleX(0.9f);
                _flies[i].transform.SetScaleY(0.9f);
                _controls[i].GetAction<IdleBuzzV3>("Idle", 0).manualStartPos.Value = _flightPositions[i];
                _controls[i].GetAction<IdleBuzzV3>("Idle", 0).manualStartPos.name = "Target";

                _controls[i].AddVector2Variable("Position");
                _controls[i].AddFloatVariable("DistanceToPerch");
            }
        }

        private void ReduceBuzz()
        {
            foreach (var fly in _controls)
            {
                fly.GetAction<IdleBuzzV3>("Idle", 0).roamingRangeX = 0.35f;
                fly.GetAction<IdleBuzzV3>("Idle", 0).roamingRangeY = 0.25f;
            }
        }

        private void SpawnNewFlies()
        {
            GameObject newfly = GameObject.Instantiate(_baseFly);
            newfly.transform.parent = gameObject.transform;
            _flies.Add(newfly);
            _controls.Add(newfly.GetFsmPreprocessed("Control"));
            newfly.SetActive(true);
        }


        private void AddPerch()
        {
            int i = 0;

            foreach (var fly in _controls)
            {

                fly.AddState("Perched");
                fly.AddState("Perch");
                fly.AddState("Wake");

                fly.ChangeTransition("Pause", "FINISHED", "Perched");
                fly.AddTransition("Idle", "PERCH", "Perch");
                fly.AddTransition("Idle", "FINISHED", "Perched");
                fly.AddTransition("Perch", "FINISHED", "Perched");
                fly.AddTransition("Perch", "WAKE", "Idle");
                fly.AddTransition("Perched", "WAKE", "Idle");

                fly.AddTransition("Perched", "LEAVE", "Leave");
                fly.AddTransition("Perch", "LEAVE", "Leave");

                
                fly.AddActions(
                    "Perched",
                    new FsmStateAction[]
                    {
                        new SetVelocity2d
                        {
                            gameObject = new FsmOwnerDefault(),
                            x = 0,
                            y = 0,
                            vector = new Vector2(0,0),
                            everyFrame = false,
                            
                        },
                        new AnimatorPlay
                        {
                            gameObject = new FsmOwnerDefault
                            {
                                OwnerOption = OwnerDefaultOption.UseOwner
                            },
                            stateName = "silk_fly_perched",
                            everyFrame = false,
                            normalizedTime = 1,
                            layer = new FsmInt { useVariable = true, name = "" }
                        }
                    }
                );
                
                fly.AddAction(
                    "Idle",
                    new AnimatorPlay
                    {
                        gameObject = new FsmOwnerDefault
                        {
                            OwnerOption = OwnerDefaultOption.UseOwner
                        },
                        stateName = "silk_fly",
                        everyFrame = false,
                        normalizedTime = 1,
                        layer = new FsmInt { useVariable = true, name = "" }
                    });

                fly.AddActions(
                    "Perch",
                    new FsmStateAction[] {
                        new IdleBuzzV3
                        {
                            gameObject = new FsmOwnerDefault(),
                            waitMax = 0.3f,
                            waitMin = 0f,
                            speedMax = 5,
                            accelerationMax = 30,
                            accelerationMin = 1,
                            roamingRangeX = 0,
                            roamingRangeY = 0,
                            manualStartPos = new FsmVector3{ name = "Target", Value = _positions[i] }
                        },
                        new GetPosition2D
                        {
                            gameObject = new FsmOwnerDefault(),
                            vector = fly.GetVector2Variable("Position"),
                            x = new FsmFloat(),
                            y = new FsmFloat(),
                            space = Space.World,
                            everyFrame = true,
                        },
                        new DistanceBetweenPoints2D
                        {
                            point1 = fly.GetVector2Variable("Position"),
                            point2 = new Vector2(_positions[i].x, _positions[i].y),
                            everyFrame = true,
                            distanceResult = fly.GetFloatVariable("DistanceToPerch")
                        },
                        new FloatCompare
                        {
                            float1 = fly.GetFloatVariable("DistanceToPerch"),
                            float2 = 0,
                            tolerance = 0.01f,
                            equal = FsmEvent.GetFsmEvent("FINISHED"),
                            greaterThan = FsmEvent.GetFsmEvent(""),
                            lessThan = FsmEvent.GetFsmEvent(""),
                            everyFrame = true
                        }
                        
                    }

                );

                i++;
            }
        }

        public void Perch()
        {
            if (coro != null) { 
                StopCoroutine(coro);
            }
            foreach (var fsm in _controls)
            {
                fsm.SendEvent("PERCH");
            }
        }

        public void Wake()
        {
            coro = StartCoroutine(WakeFlyCoro());
        }

        public IEnumerator WakeFlyCoro()
        {
            for (int i = 0; i < 5; i++){
                yield return new WaitForSeconds(flyDelay);
                _controls[(currentFly) % 5].SendEvent("WAKE");
                currentFly += 2;
            }
            currentFly += 1;

        }

        public void Leave()
        {
            foreach (var fsm in _controls)
            {
                fsm.SendEvent("LEAVE");
            }
        }

    }


}
