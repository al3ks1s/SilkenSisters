using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using GenericVariableExtension;
using HarmonyLib;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using InControl.NativeDeviceProfiles;
using SilkenSisters.Behaviors;
using SilkenSisters.SceneManagement;
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
using static Unity.Burst.Intrinsics.X86.Avx;


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

// __instance.GetComponent<tk2dSpriteAnimator>().Library.GetClipByName("Jump Antic").fps = 40;

// Todo
// Fix Lace downstab -> ClampByScale gets the wrong values for some reason

// Fix Lace Throwing you outside arena on upstabs
// Make phantom invincible until fight start
// Disable respawn point on wake up / Scene change if not organ
// whenever lace does the circle slash attack, smoke should rise up from around her when she does the downward thrust, similar to how it happens when Phantom does her steam slam

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
    [BepInDependency("org.silksong-modding.i18n")]
    public partial class SilkenSisters : BaseUnityPlugin
    {

        public static SilkenSisters plugin;

        public static Scene organScene;

        public GameObject laceNPCCache = null;
        public GameObject lace2BossSceneCache = null;
        public GameObject lace1BossSceneCache = null;

        public GameObject challengeDialogCache = null;
        public GameObject wakeupPointCache = null;
        public GameObject deepMemoryCache = null;

        public FsmState ExitMemoryCache = null;

        public GameObject laceNPCInstance = null;
        public FsmOwnerDefault laceNPCFSMOwner = null;

        public GameObject lace2BossInstance = null;
        public GameObject lace2BossSceneInstance = null;

        public FsmOwnerDefault laceBossFSMOwner = null;

        public GameObject lace1BossInstance = null;
        public GameObject lace1BossSceneInstance = null;

        public GameObject challengeDialogInstance = null;
        public GameObject wakeupPointInstance = null;
        public GameObject respawnPointInstance = null;
        public GameObject deepMemoryInstance = null;

        public GameObject phantomBossScene = null;
        public FsmOwnerDefault phantomBossSceneFSMOwner = null;


        private bool cachingSceneObjects = false;

        public static GameObject hornet = null;
        public static FsmOwnerDefault hornetFSMOwner = null;
        public static ConstrainPosition hornetConstrain = null;


        private string laceBossPrefabName = null;
        private ConfigEntry<KeyCode> modifierKey;
        private ConfigEntry<KeyCode> actionKey;
        internal static ManualLogSource Log;

        private void Awake()
        {
            Logger.LogInfo($"Plugin loaded and initialized {Application.streamingAssetsPath}");
            Log = base.Logger;

            SilkenSisters.plugin = this;

            modifierKey = Config.Bind(
                "Keybinds",
                "Modifier",
                KeyCode.LeftAlt,
                "Modifier"
            );
            
            StartCoroutine(WaitAndPatch());

            SceneManager.sceneLoaded += onSceneLoaded;

            AssetBundle laceBossPrefab = AssetBundle.LoadFromFile(Path.Combine(
                    Application.streamingAssetsPath,
                    "aa",
                    Application.platform switch
                    {
                        RuntimePlatform.WindowsPlayer => "StandaloneWindows64",
                        RuntimePlatform.OSXPlayer => "StandaloneOSX",
                        RuntimePlatform.LinuxPlayer => "StandaloneLinux64",
                        _ => ""
                    },
                    "localpoolprefabs_assets_laceboss.bundle"
                ));
            laceBossPrefabName = laceBossPrefab.GetName();
            laceBossPrefab.Unload(true);

            Harmony.CreateAndPatchAll(typeof(SilkenSisters));
        }
        
        private IEnumerator WaitAndPatch()
        {
            yield return new WaitForSeconds(2f); // Give game time to init Language
            Harmony.CreateAndPatchAll(typeof(Language_Get_Patch));
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(HeroController), "Die")]
        private static void setDeathListener(HeroController __instance, ref bool nonLethal, ref bool frostDeath)
        {
            SilkenSisters.Log.LogInfo($"Hornet died {nonLethal} {frostDeath} / isMemory? Mod:{SilkenSisters.isMemory()} Scene:{GameManager._instance.IsMemoryScene()}");
            if (SilkenSisters.isMemory() || GameManager._instance.IsMemoryScene())
            {
                SilkenSisters.Log.LogInfo($"Hornet died in memory, nonLethal enabled");

                PlayerData._instance.defeatedPhantom = true;
                PlayerData._instance.blackThreadWorld = true;
                if (SilkenSisters.hornetConstrain != null)
                {
                    SilkenSisters.hornetConstrain.enabled = false;
                }
                //nonLethal = true;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameManager), "SaveGame", new Type[] { typeof(int), typeof(Action<bool>), typeof(bool), typeof(AutoSaveName) })]
        private static bool setSaveListener(GameManager __instance, ref int saveSlot, ref Action<bool> ogCallback, ref bool withAutoSave, ref AutoSaveName autoSaveName)
        {

            ogCallback?.Invoke(true);
            SilkenSisters.Log.LogInfo($"Trying to save game. isMemory? Mod:{SilkenSisters.isMemory()} Scene:{GameManager._instance.IsMemoryScene()}");
            if (SilkenSisters.isMemory() || GameManager._instance.IsMemoryScene())
            {
                SilkenSisters.Log.LogInfo($"Currently in the memory fight, skipping save process");
                return false;
            }
            else
            {
                SilkenSisters.Log.LogInfo($"Currently not in the memory fight, continuing save process");
                return true;
            }
        }
        
        [HarmonyPrefix]
        [HarmonyPatch(typeof(FsmState), "OnEnter")]
        private static void setEventListener(FsmState __instance)
        {
            // Enable Corpse Lace Hooking
            if (__instance.Fsm.GameObject.name == "Corpse Lace2(Clone)" && __instance.Name == "Start" && isMemory())
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
    
            bool logDeepMemory = false;
            if (logDeepMemory && (__instance.Fsm.GameObject.name == $"{SilkenSisters.plugin.deepMemoryInstance}" || __instance.Fsm.GameObject.name == $"before" || __instance.Fsm.GameObject.name == $"thread_memory"))
            {
                SilkenSisters.Log.LogInfo($"{__instance.Fsm.GameObject.name}, {__instance.fsm.name}, Entering state {__instance.Name}");
                if (__instance.Actions.Length > 0)
                {
                    foreach (FsmTransition transi in __instance.transitions)
                    {
                        SilkenSisters.Log.LogInfo($"    transitions for state {__instance.Name}: {transi.EventName} to {transi.toState}");
                    }

                    foreach (FsmStateAction action in __instance.Actions)
                    {
                        SilkenSisters.Log.LogInfo($"        Action for state {__instance.Name}: {action.GetType()}");
                    }
                }
            }
        }



        public static bool canSetup()
        {
            return SceneManager.GetActiveScene().name == "Organ_01" && PlayerData._instance.defeatedPhantom && PlayerData._instance.blackThreadWorld && PlayerData._instance.hasNeedolinMemoryPowerup;
        }

        public static bool isMemory()
        {
            return SceneManager.GetActiveScene().name == "Organ_01" && !PlayerData._instance.defeatedPhantom && !PlayerData._instance.blackThreadWorld && PlayerData._instance.hasNeedolinMemoryPowerup;
        }

        private async Task cacheGameObjects()
        {
            if (laceNPCCache == null || lace2BossSceneCache == null || challengeDialogCache == null || wakeupPointCache == null || deepMemoryCache == null)
            {

                cachingSceneObjects = true;

                AssetBundle laceBossPrefab = null;
                if (AssetBundle.GetAllLoadedAssetBundles().FirstOrDefault(b => b.GetName() == laceBossPrefabName) != null)
                {
                    AssetBundle.GetAllLoadedAssetBundles().FirstOrDefault(b => b.GetName() == laceBossPrefabName).Unload(true);
                }
                
                laceBossPrefab = AssetBundle.LoadFromFile(Path.Combine(
                    Application.streamingAssetsPath,
                    "aa",
                    Application.platform switch
                    {
                        RuntimePlatform.WindowsPlayer => "StandaloneWindows64",
                        RuntimePlatform.OSXPlayer => "StandaloneOSX",
                        RuntimePlatform.LinuxPlayer => "StandaloneLinux64",
                        _ => ""
                    },
                    "localpoolprefabs_assets_laceboss.bundle"
                ));

                Logger.LogWarning("Initializing cache");
                laceNPCCache = await SceneObjectManager.loadObjectFromScene("Coral_19", "Encounter Scene Control/Lace Meet/Lace NPC Blasted Bridge");
                laceNPCCache.AddComponent<LaceNPC>();


                lace2BossSceneCache = await SceneObjectManager.loadObjectFromScene("Song_Tower_01", "Boss Scene");
                lace2BossSceneCache.AddComponent<Lace2Scene>();
                GameObject lace2BossTempCache = SceneObjectManager.findChildObject(lace2BossSceneCache, "Lace Boss2 New");
                lace2BossTempCache.AddComponent<Lace2>();
                ((DeactivateIfPlayerdataTrue)lace2BossTempCache.GetComponent(typeof(DeactivateIfPlayerdataTrue))).enabled = false;

                challengeDialogCache = await SceneObjectManager.loadObjectFromScene("Cradle_03", "Boss Scene/Intro Sequence");
                wakeupPointCache = await SceneObjectManager.loadObjectFromScene("Memory_Coral_Tower", "Door Get Up");
                wakeupPointCache.AddComponent<WakeUpMemory>();
                

                GameObject bossScene = await SceneObjectManager.loadObjectFromScene("Memory_Coral_Tower", "Boss Scene");
                PlayMakerFSM control = FsmUtil.GetFsmPreprocessed(bossScene, "Control");
                ExitMemoryCache = control.GetState("Exit Memory");
                GameObject.Destroy(bossScene);
                Logger.LogInfo($"{ExitMemoryCache.name}, {ExitMemoryCache.actions.Length}");

                // Deep memory stuff
                deepMemoryCache = await SceneObjectManager.loadObjectFromScene("Coral_Tower_01", "Memory Group");
                deepMemoryCache.AddComponent<DeepMemory>();
                deepMemoryCache.GetComponent<TestGameObjectActivator>().playerDataTest.TestGroups[0].Tests[0].FieldName = "defeatedPhantom";
                deepMemoryCache.GetComponent<TestGameObjectActivator>().playerDataTest.TestGroups[0].Tests[0].BoolValue = false;

                Logger.LogWarning("Caching done");
                cachingSceneObjects = false;
                if (laceBossPrefab != null)
                { 
                    Logger.LogWarning("Unload lace prefab");
                    laceBossPrefab.Unload(false);
                    laceBossPrefab = null;
                }


                if (laceNPCCache == null || lace2BossSceneCache == null || challengeDialogCache == null || wakeupPointCache == null || deepMemoryCache == null)
                {
                    Logger.LogWarning("One of the item requested could not be found");
                }

            }
            else
            {
                await Task.Delay(300);
            }
        }

        private void onSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Logger.LogInfo($"Scene loaded : {scene.name}, active scene : {SceneManager.GetActiveScene()}");

            string[] excludedScenes = new string[]{ "Menu_Title", "Pre_Menu_Loader", "Pre_Menu_Intro", "Quit_To_Menu" };

            if (!cachingSceneObjects) { 
                if (scene.name == "Organ_01")
                {
                    Logger.LogInfo($"Organ Detected, preloading");
                    organScene = scene;

                    preloadOrgan();

                }
                else
                {
                    Logger.LogInfo($"Scene is not organ, clearing instances and cache");
                    clearInstances();
                    clearCache();
                }
            }
        }

        private async Task preloadOrgan()
        {

            await cacheGameObjects();

            if (!isMemory() && canSetup())
            {
                Logger.LogInfo($"Is not memory and all requirements met, setting things up");
                setupDeepMemoryZone();
            }
            else
            {
                if (!canSetup())
                {
                    Logger.LogInfo("Requirements are not met, skipping setting up the memory entrance");
                }

                if (isMemory())
                {
                    Logger.LogInfo("Scene is a memory, skipping deep memory stuff");
                }
            }

            GameObject eff = GameObject.Find("Deep Memory Enter Black(Clone)");
            if (eff != null)
            {
                Logger.LogInfo("Deleting leftover memory effect");
                GameObject.Destroy(eff);
            }
        }

        private void clearInstances()
        {
            laceNPCInstance = null;
            laceNPCFSMOwner = null;

            lace2BossInstance = null;
            lace2BossSceneInstance = null;

            laceBossFSMOwner = null;

            lace1BossInstance = null;
            lace1BossSceneInstance = null;

            challengeDialogInstance = null;
            deepMemoryInstance = null;

            phantomBossScene = null;
            phantomBossSceneFSMOwner = null;

            if (wakeupPointInstance != null)
            {
                GameObject.Destroy(wakeupPointInstance);
                wakeupPointInstance = null;
            }

            if (respawnPointInstance != null)
            {
                GameObject.Destroy(respawnPointInstance);
                respawnPointInstance = null;
            }

        }

        private void clearCache()
        {
            hornet = null;
            hornetFSMOwner = null;

            if (hornetConstrain != null)
            {
                GameObject.Destroy(hornetConstrain);
                hornetConstrain = null;
            }

            GameObject.Destroy(laceNPCCache);
            GameObject.Destroy(lace2BossSceneCache);
            GameObject.Destroy(lace1BossSceneCache);

            GameObject.Destroy(challengeDialogCache);
            GameObject.Destroy(wakeupPointCache);
            GameObject.Destroy(deepMemoryCache);

            laceNPCCache = null;
            lace2BossSceneCache = null;
            lace1BossSceneCache = null;

            challengeDialogCache = null;
            wakeupPointCache = null;
            deepMemoryCache = null;

            ExitMemoryCache = null;
        }


        
        public void setupFight()
        {
            Logger.LogInfo($"Trying to register phantom");
            phantomBossScene = SceneObjectManager.findObjectInCurrentScene("Boss Scene");
            Logger.LogInfo($"{phantomBossScene}");

            Logger.LogInfo($"Registering FSMOwner");
            phantomBossSceneFSMOwner = new FsmOwnerDefault();
            phantomBossSceneFSMOwner.OwnerOption = OwnerDefaultOption.SpecifyGameObject;
            phantomBossSceneFSMOwner.GameObject = phantomBossScene;
            
            // ----------
            challengeDialogInstance = GameObject.Instantiate(challengeDialogCache);
            challengeDialogInstance.AddComponent<ChallengeRegion>();
            challengeDialogInstance.SetActive(true);

            // ----------
            lace2BossSceneInstance = Instantiate(lace2BossSceneCache);
            lace2BossSceneInstance.SetActive(true);

            Logger.LogInfo($"Trying to find Lace Boss from scene {lace2BossSceneInstance.gameObject.name}");
            lace2BossInstance = SceneObjectManager.findChildObject(lace2BossSceneInstance, "Lace Boss2 New");
            Logger.LogInfo($"Lace object: {lace2BossInstance}");
            lace2BossInstance.SetActive(false);

            laceBossFSMOwner = new FsmOwnerDefault();
            laceBossFSMOwner.OwnerOption = OwnerDefaultOption.SpecifyGameObject;
            laceBossFSMOwner.GameObject = lace2BossInstance;

            // ----------
            laceNPCInstance = GameObject.Instantiate(laceNPCCache);
            laceNPCInstance.SetActive(true);

            // ----------
            Logger.LogInfo($"Trying to set up phantom : phantom available? {phantomBossScene != null}");
            Logger.LogInfo($"{phantomBossScene}");
            phantomBossScene.AddComponent<PhantomScene>();
            SceneObjectManager.findChildObject(phantomBossScene, "Phantom").AddComponent<PhantomBoss>();

        }
        private void setupDeepMemoryZone()
        {
            deepMemoryInstance = Instantiate(deepMemoryCache);
            deepMemoryInstance.SetActive(true);

            if (wakeupPointInstance == null)
            {
                Logger.LogInfo("Setting up memory wake point");
                wakeupPointInstance = GameObject.Instantiate(wakeupPointCache);
                wakeupPointInstance.SetActive(false);
                DontDestroyOnLoad(wakeupPointInstance);
            }

            if (respawnPointInstance == null)
            {
                Logger.LogInfo("Setting respawn point");
                respawnPointInstance = GameObject.Instantiate(SceneObjectManager.findChildObject(deepMemoryCache, "door_wakeOnGround"));
                respawnPointInstance.SetActive(false);
                respawnPointInstance.AddComponent<WakeUpRespawn>();
                GameObject.DontDestroyOnLoad(respawnPointInstance);
            }
        }


        // Temporary for debug
        private void toggleLaceFSM()
        {
            if (lace2BossInstance != null)
            {
                Logger.LogInfo("Pausing Lace");
                PlayMakerFSM pfsm = SceneObjectManager.findChildObject(lace2BossInstance, "Lace Boss2 New").GetComponents<PlayMakerFSM>().First(pfsm => pfsm.FsmName == "Control");
                pfsm.fsm.manualUpdate = !pfsm.fsm.manualUpdate;
            }
        }

        private void spawnLaceBoss2()
        {

            lace2BossSceneInstance = Instantiate(lace2BossSceneCache);
            lace2BossSceneInstance.SetActive(true);

            Logger.LogInfo($"Trying to find Lace Boss from scene {lace2BossSceneInstance.gameObject.name}");
            lace2BossInstance = SceneObjectManager.findChildObject(lace2BossSceneInstance, "Lace Boss2 New");
            Logger.LogInfo($"Lace object: {lace2BossInstance}");
            lace2BossInstance.SetActive(false);

            laceBossFSMOwner = new FsmOwnerDefault();
            laceBossFSMOwner.OwnerOption = OwnerDefaultOption.SpecifyGameObject;
            laceBossFSMOwner.GameObject = lace2BossInstance;
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

                    if (SilkenSisters.hornet.GetComponent<ConstrainPosition>() == null) { 
                        SilkenSisters.hornetConstrain = SilkenSisters.hornet.AddComponent<ConstrainPosition>();

                        Logger.LogInfo($"{hornetConstrain.GetName()}");

                        hornetConstrain.SetXMax(96.727f);
                        hornetConstrain.SetXMin(72.323f);

                        hornetConstrain.constrainX = true;
                        hornetConstrain.constrainY = false;

                        hornetConstrain.enabled = false;
                    }
                }
            }

            // ------------------------------------------------------------------
            
            if (Input.GetKey(modifierKey.Value) && Input.GetKeyDown(KeyCode.O))
            {
                spawnLaceBoss2();
            }

            if (Input.GetKey(modifierKey.Value) && Input.GetKeyDown(KeyCode.H))
            {
                SilkenSisters.hornet.transform.position = new Vector3(84.45f, 105f, 0.004f);
            }

            if (Input.GetKey(modifierKey.Value) && Input.GetKeyDown(KeyCode.Keypad0))
            {
                lace2BossInstance.SetActive(true);
                ((PlayMakerFSM)lace2BossInstance.GetComponent(typeof(PlayMakerFSM))).SendEvent("BATTLE START FIRST");
            }

            if (Input.GetKey(modifierKey.Value) && Input.GetKeyDown(KeyCode.Keypad3))
            {
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


            if (Input.GetKey(modifierKey.Value) && Input.GetKeyDown(KeyCode.Keypad7))
            {
                PlayerData._instance.defeatedPhantom = true;
                PlayerData._instance.blackThreadWorld = true;
                PlayerData._instance.HasStoredMemoryState = true;
            }
        }
    }

    
    // Title cards
    [HarmonyPatch(typeof(Language), "Get")]
    [HarmonyPatch(new[] { typeof(string), typeof(string) })]
    public static class Language_Get_Patch
    {
        private static void Prefix(ref string key, ref string sheetTitle)
        {
            if (key.Contains("SILKEN_SISTERS")) sheetTitle = $"Mods.io.github.al3ks1s.silkensisters";
        }
    }
}