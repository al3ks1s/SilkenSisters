using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using GenericVariableExtension;
using HarmonyLib;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Silksong.FsmUtil;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using TeamCherry.Localization;
using UnityEngine;
using UnityEngine.SceneManagement;
using SilkenSisters.SceneManagement;
using InControl.NativeDeviceProfiles;


// Idea by AidaCamelia0516

// Challenge : Cradle_03 - Boss Scene/Intro Sequence

// Common health pool -> Sync fight phases
// 

// Three levels of fight :
// Lvl 1: Lace 1 Skipped -> Fight lace 1 and phantom at the same time       -> Cutscene shows hornet and lace instead of phantom, the moment parry is hit, phantom kicks lace out and take the bullet
// Lvl 2: Deep memory in organ chamber -> Lace 2 + Phantom                  -> This time lace takes the bullet, single final phase of dragoon dash
// Lvl 3: Taunt challenge in deep memory -> Lace 2 + Phantom on steroids    -> Fakeout cutscene, goes on normally until lace repels hornet + Hard phase

// Hidden interaction if phantom beaten alone                               -> Lace mourning over Phantom's remains, TeleOut after quick dialogue

// How to make Phantom stronger :
//      - Less idle time
//      - Faster Throws
//      - Less timeout between dives
//      - Multi dives - With patterns?
//      - Fog clone

// How to make Lace stronger:
//      - Less idle time
//      - Multiple cross slash like lost lace
//      - 

// __instance.GetComponent<tk2dSpriteAnimator>().Library.GetClipByName("Jump Antic").fps = 40;

namespace SilkenSisters
{

    public class FSMEditor
    {

        public Dictionary<string, FsmState> states = new Dictionary<string, FsmState>();
        public Dictionary<string, Dictionary<string, FsmTransition>> transitions = new Dictionary<string, Dictionary<string, FsmTransition>>();
        public Dictionary<string, FsmEvent> events = new Dictionary<string, FsmEvent>();

        public static bool fsmEditorLog = false;

        public void compileFSM(ref PlayMakerFSM fsm)
        {
            foreach (FsmEvent ev in fsm.FsmEvents)
            {
                this.events[ev.Name] = ev;
            }

            int i = 0;
            foreach (FsmState state in fsm.FsmStates)
            {

                states[state.Name] = state;
                transitions[state.Name] = new Dictionary<string, FsmTransition>();

                if (fsmEditorLog) SilkenSisters.Log.LogInfo($"Index: {i} State: {states[state.Name].Name}. {state.Transitions.Length} Transitions");

                foreach (FsmTransition transition in state.Transitions)
                {
                    transitions[state.Name][transition.EventName] = transition;
                    if (fsmEditorLog) SilkenSisters.Log.LogInfo($"   Transition : {transitions[state.Name][transition.EventName].EventName}, {transitions[state.Name][transition.EventName].ToState}");
                }
                foreach (FsmStateAction action in state.Actions)
                {
                    if (fsmEditorLog) SilkenSisters.Log.LogInfo($"       Action : {action.GetType()}");
                }
                if (fsmEditorLog) SilkenSisters.Log.LogInfo($"");
                i++;
            }
        }
    }

    public class InvokeMethod : FsmStateAction
    {
        private readonly Action _action;

        public InvokeMethod(Action action)
        {
            _action = action;
        }

        public override void OnEnter()
        {
            _action.Invoke();

            Finish();
        }
    }


    // TODO - adjust the plugin guid as needed
    [BepInAutoPlugin(id: "io.github.al3ks1s.silkensisters")]
    [BepInDependency("org.silksong-modding.fsmutil")]
    public partial class SilkenSisters : BaseUnityPlugin
    {

        public static Scene organScene;

        private GameObject laceNPCCache;
        private GameObject lace2BossSceneCache;
        private GameObject lace1BossSceneCache;

        private GameObject challengeDialogCache;
        private GameObject wakeupPointCache;
        private GameObject deepMemoryCache;

        private FsmState ExitMemoryCache;

        private GameObject laceNPCInstance;
        private FsmOwnerDefault laceNPCFSMOwner;

        private GameObject lace2BossInstance;
        private GameObject lace2BossSceneInstance;

        private FsmOwnerDefault laceBossFSMOwner;

        private GameObject lace1BossInstance;
        private GameObject lace1BossSceneInstance;

        private GameObject challengeDialogInstance;
        private GameObject wakeupPointInstance;
        private GameObject respawnPointInstance;
        private GameObject deepMemoryInstance;

        
        private GameObject phantomBossScene;
        private FsmOwnerDefault phantomBossSceneFSMOwner;


        private static bool laceBoss2Active = false;
        private static bool phantomSpeedToggle = false;
        private static bool phantomDragoonToggle = false;

        public static bool persistentInMemory = false;

        private static GameObject hornet;
        private static FsmOwnerDefault hornetFSMOwner;

        private ConfigEntry<KeyCode> modifierKey;
        private ConfigEntry<KeyCode> actionKey;
        internal static ManualLogSource Log;

        private void Awake()
        {
            Logger.LogInfo($"Plugin loaded and initialized {Application.streamingAssetsPath}");
            Log = base.Logger;

            modifierKey = Config.Bind(
                "Keybinds",
                "Modifier",
                KeyCode.LeftAlt,
                "Modifier"
            );

            StartCoroutine(WaitAndPatch());

            SceneManager.sceneLoaded += onSceneLoaded;

            Harmony.CreateAndPatchAll(typeof(SilkenSisters));
        }
       
        private IEnumerator WaitAndPatch()
        {
            yield return new WaitForSeconds(2f); // Give game time to init Language
            Harmony.CreateAndPatchAll(typeof(Language_Get_Patch));
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(HeroController), "Die")]
        private static void setDieTester(HeroController __instance, ref bool nonLethal, ref bool frostDeath)
        {
            SilkenSisters.Log.LogInfo($"Hornet died {nonLethal} {frostDeath} / isMemory? {SilkenSisters.isMemory()}");
            if (SilkenSisters.isMemory())
            {
                SilkenSisters.Log.LogInfo($"Hornet died in memory, nonLethal enabled");

                PlayerData._instance.defeatedPhantom = true;
                PlayerData._instance.blackThreadWorld = true;

                nonLethal = true;
            }
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(ClampAngle), "DoClamp")]
        private static void setClampAngleListener(ClampAngle __instance)
        {
            SilkenSisters.Log.LogInfo($"DoClamp     Clamped to angle {__instance.fsm.FindFloatVariable("Angle")} between {__instance.minValue}/{__instance.maxValue} : {__instance.angleVariable}");
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(ClampAngleByScale), "DoClamp")]
        private static void setClampAngleScaleListener(ClampAngleByScale __instance)
        {
            SilkenSisters.Log.LogInfo($"DoClampScl  Clamped angle {__instance.fsm.FindFloatVariable("Angle")} between {__instance.positiveMinValue}/{__instance.positiveMaxValue} or {__instance.negativeMinValue}/{__instance.negativeMaxValue} : {__instance.angleVariable}");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(SetVelocityAsAngle), "DoSetVelocity")]
        private static void setVelocitySetListener(SetVelocityAsAngle __instance)
        {
            SilkenSisters.Log.LogInfo($"DoVelocity  Set to angle {__instance.fsm.FindFloatVariable("Angle")}");
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(FsmState), "OnEnter")]
        private static void setEventListener(FsmState __instance)
        {
            // Enable Corpse Lace Hooking
            if (__instance.Fsm.GameObject.name == "Corpse Lace2(Clone)" && __instance.Name == "Start" && SilkenSisters.laceBoss2Active)
            {
                SilkenSisters.Log.LogInfo("Started setting corpse handler");
                GameObject laceCorpse = __instance.Fsm.GameObject;
                GameObject laceCorpseNPC = GameObject.Find($"{__instance.Fsm.GameObject.name}/NPC");
                SilkenSisters.Log.LogInfo($"{laceCorpseNPC}");

                FSMEditor e1 = new FSMEditor();
                PlayMakerFSM fsm1 = laceCorpse.GetComponent<PlayMakerFSM>();
                e1.compileFSM(ref fsm1);

                FSMEditor e2 = new FSMEditor();
                PlayMakerFSM fsm2 = laceCorpseNPC.GetComponent<PlayMakerFSM>();
                e1.compileFSM(ref fsm2);

                SilkenSisters.Log.LogInfo("Fixing facing");
                PlayMakerFSM laceCorpseFSM = FsmUtil.GetFsmPreprocessed(laceCorpse, "Control");
                laceCorpseFSM.GetAction<CheckXPosition>("Set Facing", 0).compareTo = 72;
                laceCorpseFSM.GetAction<CheckXPosition>("Set Facing", 1).compareTo = 96;

                SilkenSisters.Log.LogInfo("Disabling interact action");
                laceCorpseFSM.DisableAction("NPC Ready", 0);
                SendEventByName lace_death_event = new SendEventByName();
                lace_death_event.sendEvent = "INTERACT";
                lace_death_event.delay = 0f;
                FsmOwnerDefault owner = new FsmOwnerDefault();
                owner.gameObject = laceCorpseNPC;
                owner.ownerOption = OwnerDefaultOption.SpecifyGameObject;

                FsmEventTarget target = new FsmEventTarget();
                target.gameObject = owner;
                target.target = FsmEventTarget.EventTarget.GameObject;
                lace_death_event.eventTarget = target;
                laceCorpseFSM.AddAction("NPC Ready", lace_death_event);

                SilkenSisters.Log.LogInfo("Editing NPC routes to skip dialogue");
                PlayMakerFSM laceCorpseNPCFSM = FsmUtil.GetFsmPreprocessed(laceCorpseNPC, "Control");
                SilkenSisters.Log.LogInfo("Editing Idle");
                laceCorpseNPCFSM.ChangeTransition("Idle", "INTERACT", "Drop Pause");
                SilkenSisters.Log.LogInfo("Drop Pause");
                laceCorpseNPCFSM.DisableAction("Drop Pause", 0);
                laceCorpseNPCFSM.ChangeTransition("Drop Down", "FINISHED", "End Pause");
                laceCorpseNPCFSM.ChangeTransition("Drop Down", "IS_HURT", "End Pause");
                SilkenSisters.Log.LogInfo("End Pause");
                laceCorpseNPCFSM.DisableAction("End Pause", 0);
                laceCorpseNPCFSM.GetAction<Wait>("End Pause", 1).time = 0.5f;

                SilkenSisters.Log.LogInfo("Disabling end actions");
                laceCorpseNPCFSM.DisableAction("End", 5);
                laceCorpseNPCFSM.DisableAction("End", 6);
                laceCorpseNPCFSM.DisableAction("End", 7);
                laceCorpseNPCFSM.DisableAction("End", 8);
                laceCorpseNPCFSM.DisableAction("End", 9);
                laceCorpseNPCFSM.DisableAction("End", 10);
                laceCorpseNPCFSM.DisableAction("End", 11);
                laceCorpseNPCFSM.DisableAction("End", 12);
                laceCorpseNPCFSM.DisableAction("End", 13);

                SilkenSisters.Log.LogInfo("Disabling audio cutting");
                laceCorpseFSM.DisableAction("Start", 0);
                laceCorpseNPCFSM.DisableAction("Talk 1 Start", 3);
                laceCorpseNPCFSM.DisableAction("End", 0);

                SilkenSisters.Log.LogInfo("Finished setting up corpse handler");
            }

            if (__instance.Fsm.GameObject.name == "Lace Boss2 New")
            {
                if (__instance.Name == "Dstab Angle" || __instance.Name == "Tele In" || __instance.Name == "Downstab")
                {
                    SilkenSisters.Log.LogInfo($"{__instance.Fsm.GameObject.name}, Entering state {__instance.Name} {__instance.Transitions.FirstOrDefault().EventName}");
                    if (__instance.Actions.Length > 0)
                    {
                        foreach (FsmStateAction action in __instance.Actions)
                        {
                            // SilkenSisters.Log.LogInfo($"    Action for state {__instance.Name}: {action.GetType()}");

                            if (action.GetType() == typeof(SetPosition2d))
                            {
                                SilkenSisters.Log.LogInfo($"        Teleport to heigth {((SetPosition2d)action).y}");
                            }
                        }
                    }
                }
            }

            if (__instance.Fsm.GameObject.name == "Phantom")
            {
                // SilkenSisters.Log.LogInfo($"{__instance.Fsm.GameObject.name}, Entering state {__instance.Name}");
                if (__instance.Actions.Length > 0)
                {
                    foreach (FsmTransition transi in __instance.transitions)
                    { 
                        // SilkenSisters.Log.LogInfo($"    transitions for state {__instance.Name}: {transi.EventName} to {transi.toState}");
                    }

                    foreach (FsmStateAction action in __instance.Actions)
                    {
                        // SilkenSisters.Log.LogInfo($"        Action for state {__instance.Name}: {action.GetType()}");
                    }
                }
            }
            
        }

        private void onSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Logger.LogInfo($"Scene loaded : {scene.name}, active scene : {SceneManager.GetActiveScene()}");

            string[] excludedScenes = new string[]{ "Menu_Title", "Pre_Menu_Loader", "Pre_Menu_Intro", "Quit_To_Menu" };

            if (scene.name == "Organ_01")
            {
                Logger.LogInfo($"Organ Detected, preloading");
                organScene = scene;
                preloadOrgan();
            } 
        }

        private async Task preloadOrgan()
        {

            await cacheGameObjects();

            if (!isMemory())
            {
                Logger.LogInfo($"Is not memory, setting things up");
                setupDeepMemoryZone();
                setupWakeupPoint();
                setupRespawnPoint();
            }

            GameObject eff = GameObject.Find("Deep Memory Enter Black(Clone)");
            if (eff != null)
            {
                Logger.LogInfo("Deleting leftover memory effect");
                GameObject.Destroy(eff);
            }

        }

        public static bool isMemory()
        {
            return SceneManager.GetActiveScene().name == "Organ_01" && !PlayerData._instance.defeatedPhantom && !PlayerData._instance.blackThreadWorld && PlayerData._instance.hasNeedolinMemoryPowerup;
        }

        private async Task cacheGameObjects()
        {
            if (laceNPCCache == null || lace2BossSceneCache == null || challengeDialogCache == null || wakeupPointCache == null || deepMemoryCache == null)
            {

                Logger.LogWarning("Initializing cache");
                laceNPCCache = await SceneObjectManager.loadObjectFromScene("Coral_19", "Encounter Scene Control/Lace Meet/Lace NPC Blasted Bridge");
                lace2BossSceneCache = await SceneObjectManager.loadObjectFromScene("Song_Tower_01", "Boss Scene");

                AssetBundle laceboss_bundle = AssetBundle.LoadFromFile(Path.Combine(Application.streamingAssetsPath, "aa", "StandaloneWindows64", "localpoolprefabs_assets_laceboss.bundle"));

                //lace1BossSceneCache;

                challengeDialogCache = await SceneObjectManager.loadObjectFromScene("Cradle_03", "Boss Scene/Intro Sequence");
                wakeupPointCache = await SceneObjectManager.loadObjectFromScene("Memory_Coral_Tower", "Door Get Up");

                GameObject bossScene = await SceneObjectManager.loadObjectFromScene("Memory_Coral_Tower", "Boss Scene");
                PlayMakerFSM control = FsmUtil.GetFsmPreprocessed(bossScene, "Control");
                ExitMemoryCache = control.GetState("Exit Memory");
                Logger.LogInfo($"{ExitMemoryCache.name}, {ExitMemoryCache.actions.Length}");

                deepMemoryCache = await SceneObjectManager.loadObjectFromScene("Coral_Tower_01", "Memory Group");


                if (laceNPCCache == null || lace2BossSceneCache == null || challengeDialogCache == null || wakeupPointCache == null || deepMemoryCache == null)
                {
                    Logger.LogWarning("One of the item requested could not be found");
                }
            } else
            {
                await Task.Delay(300);
            }
        }

        private void setupFight()
        {
            registerPhantom();
            spawnChallengeSequence();
            spawnLaceBoss2();
            spawnLaceNpc();
            setupPhantom();
        }

        private void registerPhantom()
        {
            Logger.LogInfo($"Trying to register phantom");
            phantomBossScene = SceneObjectManager.findObjectInCurrentScene("Boss Scene");
            Logger.LogInfo($"{phantomBossScene}");

            if (phantomBossScene != null)
            {
                Logger.LogInfo($"Registering FSMOwner");
                phantomBossSceneFSMOwner = new FsmOwnerDefault();
                phantomBossSceneFSMOwner.OwnerOption = OwnerDefaultOption.SpecifyGameObject;
                phantomBossSceneFSMOwner.GameObject = phantomBossScene;
            }
        }

        private void setupPhantom()
        {
            Logger.LogInfo($"Trying to set up phantom : phantom available? {phantomBossScene != null}");
            Logger.LogInfo($"{phantomBossScene}");
            if (phantomBossScene != null)
            {
                GameObject phantomBoss = SceneObjectManager.findChildObject(phantomBossScene, "Phantom");

                Logger.LogInfo($"Phantom setup begin");

                PlayMakerFSM phantomSceneFSM = FsmUtil.GetFsmPreprocessed(phantomBossScene, "Control");
                PlayMakerFSM phantomBossFSM = FsmUtil.GetFsmPreprocessed(phantomBoss, "Control");

                // Disable phantom's arena detectors
                ((PlayMakerUnity2DProxy)phantomBossScene.GetComponent(typeof(PlayMakerUnity2DProxy))).enabled = false;
                ((BoxCollider2D)phantomBossScene.GetComponent(typeof(BoxCollider2D))).enabled = false;

                // Trigger lace jump
                Logger.LogInfo($"Trigger lace jump");
                SendEventByName lace_jump_event = new SendEventByName();
                lace_jump_event.sendEvent = "ENTER";
                lace_jump_event.delay = 0;
                FsmEventTarget target = new FsmEventTarget();
                target.gameObject = laceNPCFSMOwner;
                target.target = FsmEventTarget.EventTarget.GameObject;
                lace_jump_event.eventTarget = target;

                phantomSceneFSM.AddAction("Organ Hit", lace_jump_event);

                // FG Column - enable LaceBoss Object
                Logger.LogInfo($"Enable laceBoss {laceBossFSMOwner} {laceBossFSMOwner.gameObject}");
                ActivateGameObject activate_lace_boss = new ActivateGameObject();
                activate_lace_boss.activate = true;
                activate_lace_boss.gameObject = laceBossFSMOwner;
                activate_lace_boss.recursive = false;

                phantomBossFSM.AddAction("Appear", activate_lace_boss);

                // Trigger lace 
                Logger.LogInfo($"Trigger lace boss");
                SendEventByName lace_boss_start = new SendEventByName();
                lace_boss_start.sendEvent = "BATTLE START FIRST";
                lace_boss_start.delay = 0;
                FsmEventTarget target_boss = new FsmEventTarget();
                target_boss.gameObject = laceBossFSMOwner;
                target_boss.target = FsmEventTarget.EventTarget.GameObject;
                lace_boss_start.eventTarget = target_boss;

                phantomBossFSM.AddAction("To Idle", lace_boss_start);

                Logger.LogInfo($"Setup lace toggles");
                InvokeMethod meth = new InvokeMethod(toggleLaceFSM);
                phantomBossFSM.AddAction("Final Parry", meth);

                InvokeMethod meth2 = new InvokeMethod(toggleLaceFSM);
                phantomBossFSM.AddAction("End Recover", meth2);

                Logger.LogInfo($"Change boss title");
                phantomSceneFSM.GetAction<DisplayBossTitle>("Start Battle", 3).bossTitle = "SILKEN_SISTERS";

                // Skip 
                Logger.LogInfo($"Skip cutscene interaction");
                phantomBossFSM.GetAction<Wait>("Time Freeze", 4).time = 0.001f;
                phantomBossFSM.GetAction<ScaleTime>("Time Freeze", 5).timeScale = 1f;

                phantomBossFSM.DisableAction("Parry Ready", 0);
                phantomBossFSM.DisableAction("Parry Ready", 1);
                phantomBossFSM.GetAction<Wait>("Parry Ready", 4).time = 0.001f;
                phantomBossFSM.GetAction<Wait>("Parry Ready", 4).finishEvent = FsmEvent.GetFsmEvent("PARRY");

                phantomBossFSM.ChangeTransition("Death Explode", "FINISHED", "End Recover");
                phantomBossFSM.AddAction("End Recover", phantomBossFSM.GetAction<SetPositionToObject2D>("Get Control", 2));
                phantomBossFSM.AddAction("End Recover", phantomBossFSM.GetAction<SetPositionToObject2D>("Get Control", 4));

                phantomBossFSM.DisableAction("Set Data", 0);
                phantomBossFSM.DisableAction("Set Data", 1);
                phantomBossFSM.DisableAction("Set Data", 2);

                Logger.LogInfo($"Reset playerdata on death and end");

                InvokeMethod inv = new InvokeMethod(reset);
                phantomBossFSM.InsertAction("Hornet Dead", inv, 0);
                phantomBossFSM.InsertAction("End", inv, 0);

                phantomBoss.transform.SetPositionX(77.1797f);

                FsmGameObject laceBossVar = phantomBossFSM.AddGameObjectVariable("LaceBoss2");
                laceBossVar.SetName("LaceBoss2");

                FindGameObject laceObject = new FindGameObject();
                laceObject.objectName = $"{lace2BossInstance.name}";
                laceObject.store = laceBossVar;
                laceObject.withTag = "Untagged";

                GameObjectIsNull laceIsNull = new GameObjectIsNull();
                laceIsNull.gameObject = laceBossVar;
                laceIsNull.isNotNull = FsmEvent.GetFsmEvent("BLOCKED HIT");

                phantomBossFSM.AddTransition("Final Parry", "BLOCKED HIT", "Counter Stance");
                phantomBossFSM.InsertAction("Final Parry", laceIsNull, 0);
                phantomBossFSM.InsertAction("Final Parry", laceObject, 0);

                Logger.LogInfo($"{respawnPointInstance}");

                PlayMakerFSM sourceFSM = FsmUtil.GetFsmPreprocessed(SceneObjectManager.findChildObject(deepMemoryCache, "before/thread_memory"), "FSM");

                FsmGameObject deepMemVar = phantomBossFSM.AddGameObjectVariable("Deep Memory Enter");

                phantomBossFSM.AddState("Deep Memory Enter");
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

                phantomBossFSM.AddAction("Deep Memory Enter", deepMemoryEffect);
                phantomBossFSM.AddAction("Deep Memory Enter", camOff);
                phantomBossFSM.AddAction("Deep Memory Enter", deepEnterWait);

                phantomBossFSM.AddState("Deep Memory Enter Fall");
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

                phantomBossFSM.AddAction("Deep Memory Enter Fall", sourceFSM.GetAction<HeroControllerMethods>("Deep Memory Enter Fall", 1));
                phantomBossFSM.AddAction("Deep Memory Enter Fall", sourceFSM.GetAction<HeroControllerMethods>("Deep Memory Enter Fall", 2));
                phantomBossFSM.AddAction("Deep Memory Enter Fall", audio1);
                phantomBossFSM.AddAction("Deep Memory Enter Fall", audio2);
                phantomBossFSM.AddAction("Deep Memory Enter Fall", audio3);
                phantomBossFSM.AddAction("Deep Memory Enter Fall", camOff2);
                phantomBossFSM.AddAction("Deep Memory Enter Fall", audio4);
                phantomBossFSM.AddAction("Deep Memory Enter Fall", fallAnim);

                phantomBossFSM.AddState("Collapse");

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

                phantomBossFSM.AddAction("Collapse", audio5);
                phantomBossFSM.AddAction("Collapse", audio6);
                phantomBossFSM.AddAction("Collapse", waitForAnim);

                FsmState exitMemory = new FsmState(ExitMemoryCache);
                exitMemory.GetAction<ScreenFader>(1).startColour = new Color(0, 0, 0, 0);
                exitMemory.GetAction<ScreenFader>(1).endColour = new Color(0, 0, 0, 1);

                exitMemory.GetAction<StartPreloadingScene>(0).SceneName = "Organ_01";
                exitMemory.GetAction<BeginSceneTransition>(4).sceneName = "Organ_01";
                exitMemory.GetAction<BeginSceneTransition>(4).entryGateName = $"{respawnPointInstance.name}";

                exitMemory.GetAction<Wait>(2).time = 0.5f;

                phantomBossFSM.AddState(exitMemory);

                phantomBossFSM.AddTransition("Set Data", "FINISHED", "Deep Memory Enter");
                phantomBossFSM.AddTransition("Deep Memory Enter", "FINISHED", "Deep Memory Enter Fall");
                phantomBossFSM.AddTransition("Deep Memory Enter Fall", "FINISHED", "Collapse");
                phantomBossFSM.AddTransition("Collapse", "FINISHED", "Exit Memory");

                HutongGames.PlayMaker.Actions.SetPlayerDataBool disablePhantom = new HutongGames.PlayMaker.Actions.SetPlayerDataBool();
                disablePhantom.boolName = "defeatedPhantom";
                disablePhantom.value = true;

                HutongGames.PlayMaker.Actions.SetPlayerDataBool world_black_thread = new HutongGames.PlayMaker.Actions.SetPlayerDataBool();
                world_black_thread.boolName = "blackThreadWorld";
                world_black_thread.value = true;

                phantomBossFSM.InsertAction("Collapse", disablePhantom, 0);
                phantomBossFSM.InsertAction("Collapse", world_black_thread, 0);

                Logger.LogInfo($"Finished setting up phantom");
            }
        }

        private void spawnChallengeSequence()
        {

            challengeDialogInstance = GameObject.Instantiate(challengeDialogCache);
            // Challenge region 84.375 106.8835 3.64 - 84,2341 112,4307 4,9999
            // Challenge dialog 83,9299 105,8935 2,504

            GameObject challengeRegion = SceneObjectManager.findChildObject(challengeDialogInstance, "Challenge Region"); // GameObject.Find($"{challengeDialog.name}/Challenge Region");

            challengeDialogInstance.transform.position = new Vector3(84.45f, 105.8935f, 2.504f);
            challengeRegion.transform.localPosition = new Vector3(-0.2145f, 1.1139f, 2.4959f);
            //challengeRegion.transform.SetPosition3D(84.2341f, 112.4307f, 4.9999f);
            Logger.LogInfo($"Setting dialog position at {challengeDialogInstance.transform.position}");
            
            Logger.LogInfo($"Disabling Cradle specific things");
            SceneObjectManager.findChildObject(challengeDialogInstance, "Challenge Glows/Cradle__0013_loom_strut_based (2)").SetActive(false);
            SceneObjectManager.findChildObject(challengeDialogInstance, "Challenge Glows/Cradle__0013_loom_strut_based (3)").SetActive(false);

            PlayMakerFSM challengeDialogFSM = challengeDialogInstance.GetFsmPreprocessed("First Challenge");
            PlayMakerFSM challengeDialogRegionFSM = challengeRegion.GetFsmPreprocessed("Challenge");

            Logger.LogInfo("Disabling Silk's intro");
            challengeDialogFSM.GetTransition("Idle", "CHALLENGE START").FsmEvent = FsmEvent.GetFsmEvent("QUICK START");

            // Trigger phantom boss scene
            Logger.LogInfo($"Setting battle trigger");
            SendEventByName battle_begin_event = new SendEventByName();
            battle_begin_event.sendEvent = "ENTER";
            battle_begin_event.delay = 0;
            FsmEventTarget target = new FsmEventTarget();
            target.gameObject = phantomBossSceneFSMOwner;
            target.target = FsmEventTarget.EventTarget.GameObject;
            battle_begin_event.eventTarget = target;

            challengeDialogRegionFSM.AddAction("Challenge Complete", battle_begin_event);
            challengeDialogRegionFSM.GetAction<GetXDistance>("Straight Back?", 1).gameObject.ownerOption = OwnerDefaultOption.UseOwner;

            PlayMakerFSM HornetSpecialSFM = SilkenSisters.hornet.GetComponents<PlayMakerFSM>().First(f => f.FsmName == "Silk Specials");
            Logger.LogInfo($"{HornetSpecialSFM.FsmName}");
            challengeDialogRegionFSM.DisableAction("Hornet Voice", 0);
            challengeDialogRegionFSM.AddAction("Hornet Voice", HornetSpecialSFM.GetStateAction("Standard", 0));

            challengeDialogInstance.SetActive(true);
        }

        private void toggleLaceFSM()
        {
            if (lace2BossInstance != null)
            {
                Logger.LogInfo("Pausing Lace");
                PlayMakerFSM pfsm = SceneObjectManager.findChildObject(lace2BossInstance, "Lace Boss2 New") .GetComponents<PlayMakerFSM>().First(pfsm => pfsm.FsmName == "Control");
                pfsm.fsm.manualUpdate = !pfsm.fsm.manualUpdate;
            }
        }

        private void enableDoor()
        {
            wakeupPointInstance.SetActive(true);
            Logger.LogInfo($"Set door to {wakeupPointInstance.activeSelf}");
        }
        private void disableDoor()
        {
            wakeupPointInstance.SetActive(false);
            wakeupPointInstance.GetComponent<PlayMakerFSM>().fsm.Reinitialize();
            SceneObjectManager.findChildObject(wakeupPointInstance, "door_wakeInMemory_phantom").GetComponent<PlayMakerFSM>().fsm.SetState("Pause");
            Logger.LogInfo($"Set door to {wakeupPointInstance.activeSelf}");
        }

        private void enableRespawn()
        {
            respawnPointInstance.SetActive(true);
            Logger.LogInfo($"Set respawn to {respawnPointInstance.activeSelf}");
        }
        private void disableRespawn()
        {
            respawnPointInstance.SetActive(false);
            respawnPointInstance.GetComponent<PlayMakerFSM>().fsm.SetState("Pause");
            Logger.LogInfo($"Set respawn to {respawnPointInstance.activeSelf}");
        }

        private void toggleChallenge()
        {
            challengeDialogInstance.SetActive(!challengeDialogInstance.activeSelf);
            Logger.LogInfo($"Set challengeDialog to {challengeDialogInstance.activeSelf}");
        }

        private void setupWakeupPoint()
        {
            if (wakeupPointInstance == null) {
                Logger.LogInfo("Setting up memory wake point");
                wakeupPointInstance = GameObject.Instantiate(wakeupPointCache);
                wakeupPointInstance.SetActive(false);
                DontDestroyOnLoad(wakeupPointInstance);
                SceneObjectManager.findChildObject(wakeupPointInstance, "door_wakeInMemory").name = "door_wakeInMemory_phantom";
                SceneObjectManager.findChildObject(wakeupPointInstance, "Fade").SetActive(false);
                //wakeupPointInstance.transform.position = new Vector3(59.249f, 56.7457f, 0f);
                wakeupPointInstance.transform.position = new Vector3(115.4518f, 104.5621f, 0f);
                //SceneObjectManager.findChildObject(wakeupPointInstance, "door_wakeInMemory_phantom/Death Respawn Marker").GetComponent<RespawnMarker>().respawnFacingRight = false;

                PlayMakerFSM doorFsm = FsmUtil.GetFsmPreprocessed(SceneObjectManager.findChildObject(wakeupPointInstance, "door_wakeInMemory_phantom"), "Wake Up");
                InvokeMethod inv = new InvokeMethod(setupFight);
                doorFsm.AddAction("Take Control", inv);
                InvokeMethod inv2 = new InvokeMethod(enableRespawn);
                doorFsm.AddAction("Get Up", inv2);
                
                doorFsm.GetAction<ConvertBoolToFloat>("Fade Up", 1).falseValue = 3f;
                doorFsm.GetAction<ConvertBoolToFloat>("Fade Up", 1).trueValue = 3f;

                Logger.LogInfo("Finished setting up wake up point");

            } else
            {
                Logger.LogInfo("Memory wake point already set up, ignoring");
            }
        }
        
        private void setupDeepMemoryZone()
        {
            deepMemoryCache.GetComponent<TestGameObjectActivator>().playerDataTest.TestGroups[0].Tests[0].FieldName = "defeatedPhantom";
            deepMemoryCache.GetComponent<TestGameObjectActivator>().playerDataTest.TestGroups[0].Tests[0].BoolValue = false;
            deepMemoryInstance = Instantiate(deepMemoryCache);

            deepMemoryInstance.transform.position = new Vector3(59.249f, 56.7457f, -3.1141f);

            GameObject before = SceneObjectManager.findChildObject(deepMemoryInstance, "before"); // deep_memory.transform.GetComponentsInChildren<Transform>(true).First(tf => tf.name == "before").gameObject;
            GameObject.Destroy(SceneObjectManager.findChildObject(before, "CK_ground_hit0004").gameObject);

            PlayMakerFSM memoryFSM = deepMemoryInstance.GetFsmPreprocessed("To Memory");

            memoryFSM.GetAction<BeginSceneTransition>("Transition Scene", 4).sceneName = "Organ_01";
            memoryFSM.GetAction<BeginSceneTransition>("Transition Scene", 4).entryGateName = "door_wakeInMemory_phantom";

            InvokeMethod door = new InvokeMethod(enableDoor);
            memoryFSM.InsertAction("Transition Scene", door, 4);

            InvokeMethod inv3 = new InvokeMethod(disableRespawn);
            memoryFSM.AddAction("Transition Scene", inv3);

            HutongGames.PlayMaker.Actions.SetPlayerDataBool enablePhantom = new HutongGames.PlayMaker.Actions.SetPlayerDataBool();
            enablePhantom.boolName = "defeatedPhantom";
            enablePhantom.value = false;

            HutongGames.PlayMaker.Actions.SetPlayerDataBool world_normal = new HutongGames.PlayMaker.Actions.SetPlayerDataBool();
            world_normal.boolName = "blackThreadWorld";
            world_normal.value = false;

            memoryFSM.InsertAction("Transition Scene", enablePhantom, 0);
            memoryFSM.InsertAction("Transition Scene", world_normal, 0);

            PlayMakerFSM pickupFSM = FsmUtil.GetFsmPreprocessed(before, "activate memory on tool pickup");
            pickupFSM.GetTransition("State 1", "PICKED UP").fsmEvent = FsmEvent.GetFsmEvent("FINISHED");

            deepMemoryInstance.SetActive(true);
            Logger.LogInfo($"Finished setting up deep memory");
        }

        private void setupRespawnPoint()
        {
            if (respawnPointInstance == null)
            {
                respawnPointInstance = GameObject.Instantiate(SceneObjectManager.findChildObject(deepMemoryCache, "door_wakeOnGround"));
                GameObject.DontDestroyOnLoad(respawnPointInstance);
                respawnPointInstance.name = "door_wakeOnGround_phantom";
                respawnPointInstance.transform.position = new Vector3(59.249f, 56.7457f, 0f);

                PlayMakerFSM respawnFSM = FsmUtil.GetFsmPreprocessed(respawnPointInstance, "Wake Up");
                InvokeMethod inv3 = new InvokeMethod(disableDoor);
                respawnFSM.AddAction("End", inv3);

                respawnPointInstance.SetActive(false);
                Logger.LogInfo($"Respawn point: {respawnPointInstance}");
            }
        }

        private void spawnLaceNpc()
        {
            Logger.LogInfo($"Spawning lace on the organ bench");

            laceNPCInstance = GameObject.Instantiate(laceNPCCache);

            Logger.LogInfo($"Disabling lace npc range detection");
            SceneObjectManager.findChildObject(laceNPCInstance, "Start Range").SetActive(false);

            laceNPCFSMOwner = new FsmOwnerDefault();
            laceNPCFSMOwner.OwnerOption = OwnerDefaultOption.SpecifyGameObject;
            laceNPCFSMOwner.GameObject = laceNPCInstance;

            GameObject hornet = GameObject.Find("Hero_Hornet(Clone)");
            Logger.LogInfo($"Hornet positon at {hornet.transform.position}");

            laceNPCInstance.transform.position = new Vector3(81.9569f, 106.1943f, 2.7021f);
            laceNPCInstance.transform.SetScaleX(-0.9f);
            laceNPCInstance.transform.SetScaleY(0.9f);
            laceNPCInstance.transform.SetScaleZ(0.9f);
            Logger.LogInfo($"Setting lace position at {laceNPCInstance.transform.position}");

            PlayMakerFSM laceFSM = (PlayMakerFSM)laceNPCInstance.GetComponent(typeof(PlayMakerFSM));

            laceFSM.ChangeTransition("Take Control", "LAND", "Sit Up");
            laceFSM.ChangeTransition("Take Control", "LAND", "Sit Up");
            laceFSM.GetTransition("Take Control", "LAND").fsmEvent = FsmEvent.GetFsmEvent("FINISHED");
            laceFSM.DisableAction("Take Control", 3);
            
            Wait w2 = new Wait();
            w2.time = 2f;
            laceFSM.DisableAction("Sit Up", 4);
            laceFSM.AddAction("Sit Up", w2);

            SetPosition laceTargetPos = laceFSM.GetAction<SetPosition>("Sit Up", 3);

            laceTargetPos.vector = new Vector3(81.9569f, 106.6942f, 2.7021f);
            laceTargetPos.x = 81.9569f;
            laceTargetPos.y = 106.6942f;
            laceTargetPos.z = 2.7021f;

            laceFSM.ChangeTransition("Sit Up", "FINISHED", "Jump Antic");
                
            Logger.LogInfo("Setting actions to give back hornet control");
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

            laceFSM.AddAction("Jump Away", message_control_regain);
            laceFSM.AddAction("Jump Away", message_control_idle);

            InvokeMethod inv2 = new InvokeMethod(toggleChallenge);
            laceFSM.AddAction("Jump Away", inv2);

            laceNPCInstance.SetActive(true);
        }

        private void spawnLaceBoss2()
        {
            // Spawn pos : 78,7832 104,5677 0,004
            // Constraints left: 72,4, right: 96,52, bot: 104

            // Needed to get her loaded in the scene
            bool has_beaten_lace = PlayerData.instance.defeatedLaceTower;
            Logger.LogInfo($"Has beaten lace? {has_beaten_lace}, saving for later");
            PlayerData.instance.defeatedLaceTower = false;

            lace2BossSceneInstance = Instantiate(lace2BossSceneCache);
            lace2BossSceneInstance.SetActive(true);

            Logger.LogInfo($"Trying to find Lace Boss from scene {lace2BossSceneInstance.gameObject.name}");
            lace2BossInstance = SceneObjectManager.findChildObject(lace2BossSceneInstance, "Lace Boss2 New"); //GameObject.Find($"{laceBossScene.gameObject.name}/Lace Boss2 New");
            Logger.LogInfo($"Lace object: {lace2BossInstance}");

            Logger.LogInfo($"Disabling unwanted LaceBossScene items");
            SceneObjectManager.findChildObject(lace2BossSceneInstance, "Flower Effect Hornet").SetActive(false);
            SceneObjectManager.findChildObject(lace2BossSceneInstance, "Slam Particles").SetActive(false);
            SceneObjectManager.findChildObject(lace2BossSceneInstance, "steam hazard").SetActive(false);
            SceneObjectManager.findChildObject(lace2BossSceneInstance, "Silk Heart Memory Return").SetActive(false);

            Logger.LogInfo($"Moving lace arena objects");
            SceneObjectManager.findChildObject(lace2BossSceneInstance, "Arena L").transform.position = new Vector3(72f, 104f, 0f);
            SceneObjectManager.findChildObject(lace2BossSceneInstance, "Arena R").transform.position = new Vector3(97f, 104f, 0f);
            SceneObjectManager.findChildObject(lace2BossSceneInstance, "Centre").transform.position = new Vector3(84.5f, 104f, 0f);

            SceneObjectManager.findChildObject(lace2BossInstance, "Pt DashPetal").SetActive(false);
            SceneObjectManager.findChildObject(lace2BossInstance, "Pt SkidPetal").SetActive(false);
            SceneObjectManager.findChildObject(lace2BossInstance, "Pt RisingPetal").SetActive(false);
            SceneObjectManager.findChildObject(lace2BossInstance, "Pt MovePetal").SetActive(false);

            laceBossFSMOwner = new FsmOwnerDefault();
            laceBossFSMOwner.OwnerOption = OwnerDefaultOption.SpecifyGameObject;
            laceBossFSMOwner.GameObject = lace2BossInstance;

            // Disabling the check so that we don't need to track it further
            Logger.LogInfo($"Disabling defeated check");
            DeactivateIfPlayerdataTrue comp = (DeactivateIfPlayerdataTrue)lace2BossInstance.GetComponent(typeof(DeactivateIfPlayerdataTrue));
            comp.enabled = false;
            PlayerData.instance.defeatedLaceTower = has_beaten_lace; // Putting back the value

            ConstrainPosition laceBossConstraint = (ConstrainPosition)lace2BossInstance.GetComponent(typeof(ConstrainPosition));
            laceBossConstraint.SetXMin(72.4f);
            laceBossConstraint.SetXMax(96.52f);
            laceBossConstraint.SetYMin(104f);
            laceBossConstraint.constrainX = true;
            laceBossConstraint.constrainY = true;

            // Finite state machine edition
            PlayMakerFSM laceBossFSM = FsmUtil.GetFsmPreprocessed(lace2BossInstance, "Control");
            Logger.LogInfo(laceBossFSM.FsmName);

            // Reroute states
            Logger.LogInfo("Rerouting states");
            laceBossFSM.ChangeTransition("Init", "REFIGHT", "Start Battle Wait");
            laceBossFSM.ChangeTransition("Start Battle Wait", "BATTLE START REFIGHT", "Refight Engarde");
            laceBossFSM.ChangeTransition("Start Battle Wait", "BATTLE START FIRST", "Refight Engarde");

            // Lengthen the engarde state
            Wait wait_engarde = new Wait();
            wait_engarde.time = 1f;
            Logger.LogInfo("Increase engarde time");
            laceBossFSM.AddAction("Refight Engarde", wait_engarde);
                
            // Change floor height
            Logger.LogInfo("Fix floor heights");
            laceBossFSM.GetAction<SetPosition2d>("ComboSlash 1", 0).y = 104.5677f;
            laceBossFSM.GetAction<SetPosition2d>("Charge Antic", 2).y = 104.5677f;
            laceBossFSM.GetAction<SetPosition2d>("Counter Antic", 1).y = 104.5677f;

            Logger.LogInfo("Fixing Counter Teleport");
            laceBossFSM.GetAction<SetPosition>("Counter TeleIn", 4).y = 110f;
            ClampPosition clamp_pos = new ClampPosition();
            clamp_pos.maxX = 94f;
            clamp_pos.minX = 74f;
            laceBossFSM.InsertAction("Counter TeleIn", clamp_pos, 4);

            // Disable lace's title card
            Logger.LogInfo("Disabling title card");
            laceBossFSM.DisableAction("Start Battle Refight", 4);
            laceBossFSM.DisableAction("Start Battle", 4);

            laceBossFSM.GetAction<FloatClamp>("Set CrossSlash Pos", 1).minValue = 73f;
            laceBossFSM.GetAction<FloatClamp>("Set CrossSlash Pos", 1).maxValue = 96f;

            laceBossFSM.FindFloatVariable("Land Y").Value = 104.5677f;
            laceBossFSM.FindFloatVariable("Arena Plat Bot Y").Value = 102f;

            //lace.transform.position = hornet.transform.position + new Vector3(3, 0, 0);
            lace2BossInstance.transform.position = new Vector3(78.2832f, 104.5677f, 0.004f);
            lace2BossInstance.transform.SetScaleX(1);
            Logger.LogInfo($"Setting lace position at {lace2BossInstance.transform.position}");

            

            SilkenSisters.laceBoss2Active = true;
            lace2BossInstance.SetActive(false);

        }

        private void reset()
        {
            Logger.LogInfo("Reset variables");
            SilkenSisters.laceBoss2Active = false;
        }

        private void Update()
        {

            if (SilkenSisters.hornet == null)
            {
                SilkenSisters.hornet = GameObject.Find("Hero_Hornet(Clone)");
                if (SilkenSisters.hornet != null)
                {
                    SilkenSisters.hornetFSMOwner = new FsmOwnerDefault();
                    SilkenSisters.hornetFSMOwner.OwnerOption = OwnerDefaultOption.SpecifyGameObject;
                    SilkenSisters.hornetFSMOwner.GameObject = SilkenSisters.hornet;
                }
            }

            // ------------------------------------------------------------------

            if (Input.GetKey(modifierKey.Value) && Input.GetKeyDown(KeyCode.O))
            {
                spawnLaceBoss2();
            }

            if (Input.GetKey(modifierKey.Value) && Input.GetKeyDown(KeyCode.H))
            {
                SilkenSisters.hornet.transform.position = new Vector3(114f, 105f, 0.004f);
            }

            if (Input.GetKey(modifierKey.Value) && Input.GetKeyDown(KeyCode.L))
            {
                Logger.LogInfo($"{SceneManager.GetSceneByName("Organ_01")}");
                Logger.LogInfo($"{SceneManager.GetAllScenes().Length}");
            }

            if (Input.GetKey(modifierKey.Value) && Input.GetKeyDown(KeyCode.Keypad0))
            {
                lace2BossInstance.SetActive(true);
                ((PlayMakerFSM)lace2BossInstance.GetComponent(typeof(PlayMakerFSM))).SendEvent("BATTLE START FIRST");
                lace2BossInstance.GetComponent<HealthManager>().hp = 1;
            }

            if (Input.GetKey(modifierKey.Value) && Input.GetKeyDown(KeyCode.Keypad1))
            {
                SceneObjectManager.findChildObject(phantomBossScene, "Phantom").GetComponent<HealthManager>().hp = 1;
            }

            if (Input.GetKey(modifierKey.Value) && Input.GetKeyDown(KeyCode.Keypad2))
            {
                toggleLaceFSM();
            }

            if (Input.GetKey(modifierKey.Value) && Input.GetKeyDown(KeyCode.K))
            {
                PlayMakerFSM.BroadcastEvent("ENTER");
                ((PlayMakerFSM)phantomBossScene.GetComponent(typeof(PlayMakerFSM))).SendEvent("ENTER");
            }

            if (Input.GetKey(modifierKey.Value) && Input.GetKeyDown(KeyCode.G))
            {

                PlayMakerFSM phantomBossFSM = FsmUtil.GetFsmPreprocessed(SceneObjectManager.findChildObject(phantomBossScene, "Phantom"), "Control");
                Logger.LogInfo("Switching dragoon time");
                if (phantomDragoonToggle)
                {
                    Logger.LogInfo("Slow dragoon");
                    phantomBossFSM.GetAction<Wait>("Dragoon Away", 3).time = 0.25f;
                    phantomBossFSM.FindFloatVariable("Dragoon Drop Time").Value = 0.9f;

                }
                else
                {
                    Logger.LogInfo("Fast dragoon");
                    phantomBossFSM.GetAction<Wait>("Dragoon Away", 3).time = 0.15f;
                    phantomBossFSM.FindFloatVariable("Dragoon Drop Time").Value = 0.4f;
                }
                phantomDragoonToggle = !phantomDragoonToggle;
            }
        }
    }

    // Title cards
    [HarmonyPatch(typeof(Language), "Get")]
    [HarmonyPatch(new[] { typeof(string), typeof(string) })]
    public static class Language_Get_Patch
    {
        private static void Postfix(string key, string sheetTitle, ref string __result)
        {
            if (key == "SILKEN_SISTERS_SUPER") __result = "Silken Sisters";
            if (key == "SILKEN_SISTERS_MAIN") __result = "Phantom & Lace";
            if (key == "SILKEN_SISTERS_SUB") __result = "";
        }
    }

}
