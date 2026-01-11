using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Newtonsoft.Json;
using SilkenSisters.Behaviors;
using SilkenSisters.SceneManagement;
using Silksong.AssetHelper;
using Silksong.AssetHelper.Core;
using Silksong.AssetHelper.ManagedAssets;
using Silksong.AssetHelper.Plugin;
using Silksong.FsmUtil;
using Silksong.UnityHelper.Extensions;
using System;
using System;
using System.Collections;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO;
using System.Linq;
using System.Linq;
using System.Threading.Tasks;
using TeamCherry.Localization;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;
using Silksong.AssetHelper.Core;
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

// Sync Lace and Phantom

// __instance.GetComponent<tk2dSpriteAnimator>().Library.GetClipByName("Jump Antic").fps = 40;

// Todo
// Fix Lace downstab -> ClampByScale gets the wrong values for some reason

// Fix Lace Throwing you outside arena on upstabs
// Make phantom invincible until fight start
// Disable respawn point on wake up / Scene change if not organ
// whenever lace does the circle slash attack, smoke should rise up from around her when she does the downward thrust, similar to how it happens when Phantom does her steam slam

// ^.*SilkenSisters.*$

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

    [BepInAutoPlugin(id: "io.github.al3ks1s.silkensisters")]
    [BepInDependency("org.silksong-modding.fsmutil")]
    [BepInDependency("org.silksong-modding.i18n")]
    [BepInDependency("io.github.flibber-hk.assethelper")]
    [BepInDependency("io.github.flibber-hk.filteredlogs", BepInDependency.DependencyFlags.SoftDependency)]

    public partial class SilkenSisters : BaseUnityPlugin
    {

        public List<AddressableAsset<GameObject>> _individualAssets = new List<AddressableAsset<GameObject>>();
        public static SilkenSisters plugin;

        public static Scene organScene;

        public AddressableAsset<GameObject> laceNPCCache = null;
        public AddressableAsset<GameObject> lace2BossSceneCache = null;
        public AddressableAsset<GameObject> lace1BossSceneCache = null;

        public AddressableAsset<GameObject> challengeDialogCache = null;
        public AddressableAsset<GameObject> wakeupPointCache = null;
        public AddressableAsset<GameObject> coralBossSceneCache = null;
        public AddressableAsset<GameObject> deepMemoryCache = null;

        public AddressableAsset<GameObject> infoPromptCache = null;

        public FsmState ExitMemoryCache = null;

        public GameObject laceNPCInstance = null;
        public FsmOwnerDefault laceNPCFSMOwner = null;

        public GameObject laceBossInstance = null;
        public GameObject laceBossSceneInstance = null;

        public FsmOwnerDefault laceBossFSMOwner = null;

        public GameObject challengeDialogInstance = null;
        public GameObject wakeupPointInstance = null;
        public GameObject respawnPointInstance = null;
        public GameObject deepMemoryInstance = null;

        public GameObject infoPromptInstance = null;

        public GameObject phantomBossScene = null;
        public FsmOwnerDefault phantomBossSceneFSMOwner = null;


        private bool cachingSceneObjects = false;

        public static GameObject hornet = null;
        public static FsmOwnerDefault hornetFSMOwner = null;
        public static ConstrainPosition hornetConstrain = null;


        private string laceBossPrefabName = null;
        internal static ManualLogSource Log;

        private ConfigEntry<KeyCode> modifierKey;
        private ConfigEntry<KeyCode> actionKey;
        public static ConfigEntry<bool> syncedFight;

        public static bool debugBuild;

        private void Awake()
        {
            //FilteredLogs.API.ApplyFilter(Name);

            SilkenSisters.Log = new ManualLogSource("SilkenSisters");
            BepInEx.Logging.Logger.Sources.Add(Log);

            debugBuild = true;

            SilkenSisters.plugin = this;

            modifierKey = Config.Bind(
                "Keybinds",
                "Modifier",
                KeyCode.LeftAlt,
                "Modifier"
            );

            syncedFight = Config.Bind(
                "General",
                "SyncedFight",
                false,
                "Use the Synced patterns for the boss fights. Unavailable as of yet."
            );

            StartCoroutine(WaitAndPatch());

            requestAssets();

            SceneManager.sceneLoaded += onSceneLoaded;
            Harmony.CreateAndPatchAll(typeof(SilkenSisters));

            Logger.LogMessage($"Plugin loaded and initialized");
        }

        private void requestAssets()
        {
            laceNPCCache = AddressableAsset<GameObject>.FromSceneAsset("Coral_19", "Encounter Scene Control/Lace Meet/Lace NPC Blasted Bridge");
            lace2BossSceneCache = AddressableAsset<GameObject>.FromSceneAsset("Song_Tower_01", "Boss Scene");
            lace1BossSceneCache = AddressableAsset<GameObject>.FromSceneAsset("Bone_East_12", "Boss Scene");

            challengeDialogCache = AddressableAsset<GameObject>.FromSceneAsset("Cradle_03", "Boss Scene/Intro Sequence");
            wakeupPointCache = AddressableAsset<GameObject>.FromSceneAsset("Memory_Coral_Tower", "Door Get Up");
            coralBossSceneCache = AddressableAsset<GameObject>.FromSceneAsset("Memory_Coral_Tower", "Boss Scene");
            deepMemoryCache = AddressableAsset<GameObject>.FromSceneAsset("Coral_Tower_01", "Memory Group");

            infoPromptCache = AddressableAsset<GameObject>.FromSceneAsset("Arborium_01", "Inspect Region");
        }

        private IEnumerator WaitAndPatch()
        {
            yield return new WaitForSeconds(2f); // Give game time to init Language
            Harmony.CreateAndPatchAll(typeof(Language_Get_Patch));
        }

        /*
        [HarmonyPostfix]
        [HarmonyPatch(typeof(tk2dSpriteAnimator), "Start")]
        private static void setClipListener(tk2dSpriteAnimator __instance)
        {
            if (__instance.Library.name == "Lace Anim") { 
                SilkenSisters.Log.LogMessage("[tk2dSpriteAnimator.ClipListen] A SpriteAnimator was started, dumping");
                SilkenSisters.Log.LogMessage($"[tk2dSpriteAnimator.ClipListen] {__instance.gameObject.name} {__instance.Library.name}");

                foreach (tk2dSpriteAnimationClip clip in __instance.library.clips)
                {
                    SilkenSisters.Log.LogMessage($"[tk2dSpriteAnimator.ClipListen] {clip.name}");
                }

            }
        }
        */

        [HarmonyPrefix]
        [HarmonyPatch(typeof(HeroController), "Die")]
        private static void setDeathListener(HeroController __instance, ref bool nonLethal, ref bool frostDeath)
        {
            SilkenSisters.Log.LogInfo($"[DeathListener] Hornet died nonLethal:{nonLethal} frost:{frostDeath} / isMemory? Mod:{SilkenSisters.isMemory()} Scene:{GameManager._instance.IsMemoryScene()}");
            if (SilkenSisters.isMemory() || GameManager._instance.IsMemoryScene())
            {
                
                PlayerData._instance.defeatedPhantom = true;
                PlayerData._instance.blackThreadWorld = true;
                if (SilkenSisters.hornetConstrain != null)
                {
                    SilkenSisters.hornetConstrain.enabled = false;
                }

                SilkenSisters.Log.LogInfo($"[DeathListener] Hornet died in memory, variable reset: defeatedPhantom:{PlayerData._instance.defeatedPhantom}, blackThreadWorld:{PlayerData._instance.blackThreadWorld}");

                //nonLethal = true;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameManager), "SaveGame", new Type[] { typeof(int), typeof(Action<bool>), typeof(bool), typeof(AutoSaveName) })]
        private static bool setSaveListener(GameManager __instance, ref int saveSlot, ref Action<bool> ogCallback, ref bool withAutoSave, ref AutoSaveName autoSaveName)
        {
            ogCallback?.Invoke(true);
            SilkenSisters.Log.LogInfo($"[SaveListener] Trying to save game. isMemory? Mod:{SilkenSisters.isMemory()} Scene:{GameManager._instance.IsMemoryScene()}. Skipping?:{SilkenSisters.isMemory() || GameManager._instance.IsMemoryScene()}");
            return !(SilkenSisters.isMemory() || GameManager._instance.IsMemoryScene());
        }
        
        [HarmonyPrefix]
        [HarmonyPatch(typeof(FsmState), "OnEnter")]
        private static void setStateListener(FsmState __instance)
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

            if (__instance.Fsm.GameObject.name == "Lace Boss2 New" && __instance.Fsm.Name == "Control")
            {
                SilkenSisters.Log.LogInfo($"[StateListen] {__instance.Name}");
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

        /*
        [HarmonyPostfix]
        [HarmonyPatch(typeof(AssetBundle), "UnloadAsync")]
        private static void setBundleListener2(AssetBundle __instance)
        {
            SilkenSisters.Log.LogInfo($"[AssetBundle.UnloadAsync] {__instance.GetName()} {__instance.name}");
            foreach (string scenePath in __instance.GetAllScenePaths())
            {
                SilkenSisters.Log.LogInfo($"[AssetBundle.UnloadAsync]        {scenePath}");
            }
        }*/

        /*
        [HarmonyPostfix]
        [HarmonyPatch(typeof(FsmState), "OnEnter")]
        private static void setPostEventListener(FsmState __instance)
        {

            bool logDeepMemory = true;
            if (logDeepMemory && (__instance.Fsm.GameObject.name == $"Lace Boss2 New" && (__instance.name == "Bounce Back")))
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
        }*/


        public static bool canSetupMemoryFight()
        {
            SilkenSisters.Log.LogDebug($"[CanSetup] Scene:{SceneManager.GetActiveScene().name} " +
                $"DefeatedLace2:{PlayerData._instance.defeatedLaceTower} " +
                $"DefeatedPhantom:{PlayerData._instance.defeatedPhantom} " +
                $"Act3:{PlayerData._instance.blackThreadWorld} " +
                $"Needolin:{PlayerData._instance.hasNeedolinMemoryPowerup}");
            return SceneManager.GetActiveScene().name == "Organ_01" && PlayerData._instance.defeatedLaceTower && PlayerData._instance.defeatedPhantom && PlayerData._instance.blackThreadWorld && PlayerData._instance.hasNeedolinMemoryPowerup;
        }

        public static bool canSetupNormalFight()
        {
            SilkenSisters.Log.LogDebug($"[CanSetup] Scene:{SceneManager.GetActiveScene().name} " +
                $"DefeatedLace1:{PlayerData._instance.defeatedLace1} " +
                $"DefeatedLace2:{PlayerData._instance.defeatedLaceTower} " +
                $"DefeatedPhantom:{PlayerData._instance.defeatedPhantom} " +
                $"Act3:{PlayerData._instance.blackThreadWorld}");
            return SceneManager.GetActiveScene().name == "Organ_01" &&
                !PlayerData._instance.defeatedLace1 &&
                !PlayerData._instance.defeatedLaceTower &&
                !PlayerData._instance.defeatedPhantom && 
                !PlayerData._instance.blackThreadWorld;
        }

        public static bool isMemory()
        {
            SilkenSisters.Log.LogDebug($"[isMemory] Scene:{SceneManager.GetActiveScene().name} " +
                $"DefeatedPhantom:{PlayerData._instance.defeatedPhantom} " +
                $"Act3:{PlayerData._instance.blackThreadWorld} " +
                $"Needolin:{PlayerData._instance.hasNeedolinMemoryPowerup}");
            return SceneManager.GetActiveScene().name == "Organ_01" && !PlayerData._instance.defeatedPhantom && !PlayerData._instance.blackThreadWorld && PlayerData._instance.hasNeedolinMemoryPowerup;
        }

        private IEnumerator cacheGameObjects()
        {


            List<AddressableAsset<GameObject>> _individualAssets = new List<AddressableAsset<GameObject>>
            {
                laceNPCCache,
                lace2BossSceneCache,
                lace1BossSceneCache,
                challengeDialogCache,
                wakeupPointCache,
                coralBossSceneCache,
                deepMemoryCache,
                infoPromptCache
            };

            Logger.LogMessage("[cacheGameObjects] Initializing cache");
            Stopwatch sw = Stopwatch.StartNew();
            foreach (var asset in _individualAssets)
            {
                asset.Load();
            }

            yield return new WaitUntil(() => _individualAssets.All(x => x.Handle.IsDone));

            GameObject bossScene = coralBossSceneCache.InstantiateAsset();
            PlayMakerFSM control = bossScene.GetFsmPreprocessed("Control");
            ExitMemoryCache = control.GetState("Exit Memory");
            GameObject.Destroy(bossScene);
            Logger.LogInfo($"[cacheGameObjects] {ExitMemoryCache.name}, {ExitMemoryCache.actions.Length}");

            sw.Stop();
            Log.LogInfo($"It took {sw.ElapsedMilliseconds}ms to load the assets");

            Logger.LogMessage("[cacheGameObjects] Caching done");
            
        }
       
        private void onSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Logger.LogInfo($"[onSceneLoaded] Scene loaded : {scene.name}, active scene : {SceneManager.GetActiveScene()}, Path:{scene.path}");

            string[] excludedScenes = new string[]{ "Menu_Title", "Pre_Menu_Loader", "Pre_Menu_Intro", "Quit_To_Menu" };

            if (!cachingSceneObjects) { 
                if (scene.name == "Organ_01")
                {
                    Logger.LogMessage($"[onSceneLoaded] Organ Detected, preloading");
                    organScene = scene;

                    StartCoroutine(preloadOrgan());
                }
                else
                {
                    Logger.LogMessage($"[onSceneLoaded] Scene is not organ, clearing instances and cache");
                    clearInstances();
                }

            
            }

            if (scene.name == "Quit_To_Menu")
            {
                clearInstances();
                clearCache();
            }
        }

        private IEnumerator testLoading()
        {

            laceNPCCache = AddressableAsset<GameObject>.FromSceneAsset("Coral_19", "Encounter Scene Control/Lace Meet/Lace NPC Blasted Bridge");
            lace2BossSceneCache = AddressableAsset<GameObject>.FromSceneAsset("Song_Tower_01", "Boss Scene");
            lace1BossSceneCache = AddressableAsset<GameObject>.FromSceneAsset("Bone_East_12", "Boss Scene");

            challengeDialogCache = AddressableAsset<GameObject>.FromSceneAsset("Cradle_03", "Boss Scene/Intro Sequence");
            wakeupPointCache = AddressableAsset<GameObject>.FromSceneAsset("Memory_Coral_Tower", "Door Get Up");
            coralBossSceneCache = AddressableAsset<GameObject>.FromSceneAsset("Memory_Coral_Tower", "Boss Scene");
            deepMemoryCache = AddressableAsset<GameObject>.FromSceneAsset("Coral_Tower_01", "Memory Group");

            infoPromptCache = AddressableAsset<GameObject>.FromSceneAsset("Arborium_01", "Inspect Region");

            IList<string> keys =
            new List<string>{
                CatalogKeys.GetKeyForSceneAsset("Coral_19", "Encounter Scene Control/Lace Meet/Lace NPC Blasted Bridge"),
                CatalogKeys.GetKeyForSceneAsset("Song_Tower_01", "Boss Scene"),
                CatalogKeys.GetKeyForSceneAsset("Bone_East_12", "Boss Scene"),
                CatalogKeys.GetKeyForSceneAsset("Cradle_03", "Boss Scene/Intro Sequence"),
                CatalogKeys.GetKeyForSceneAsset("Memory_Coral_Tower", "Door Get Up"),
                CatalogKeys.GetKeyForSceneAsset("Memory_Coral_Tower", "Boss Scene"),
                CatalogKeys.GetKeyForSceneAsset("Coral_Tower_01", "Memory Group"),
                CatalogKeys.GetKeyForSceneAsset("Arborium_01", "Inspect Region")
            };
            Stopwatch sw = Stopwatch.StartNew();
            yield return Addressables.LoadAssetsAsync<GameObject>(keys, obj => {}, Addressables.MergeMode.Union);
            sw.Stop();
            Log.LogInfo($"Loading as a group took {sw.ElapsedMilliseconds}ms");
        }

        private IEnumerator preloadOrgan()
        {

            yield return StartCoroutine(cacheGameObjects());
            if (!isMemory() && canSetupMemoryFight())
            {
                Logger.LogMessage($"[preloadOrgan] Is not memory and all requirements met, setting things up");
                setupDeepMemoryZone();
            }
            else if (!isMemory() && canSetupNormalFight() && SilkenSisters.debugBuild)
            {
                Logger.LogMessage($"[preloadOrgan] Setting up normalFight (not available as of yet)");
                setupNormalFight();
            }
            else
            {
                Logger.LogInfo($"[preloadOrgan] Scene info: canSetup?:{canSetupMemoryFight()}, isMemory?:{isMemory()}");
                if (!isMemory() && !canSetupMemoryFight() && !canSetupNormalFight())
                {
                    Logger.LogMessage($"[preloadOrgan] Displaying the info prompt");
                    infoPromptInstance = infoPromptCache.InstantiateAsset();
                    infoPromptInstance.AddComponent<InfoPrompt>();
                    infoPromptInstance.SetActive(true);
                }
            }
            yield return new WaitForSeconds(0.2f);
            GameObject eff = GameObject.Find("Deep Memory Enter Black(Clone)");
            if (eff != null)
            {
                Logger.LogMessage("[preloadOrgan] Deleting leftover memory effect");
                GameObject.Destroy(eff);
            }

            eff = GameObject.Find("Deep Memory Pre Enter Effect(Clone)");
            if (eff != null)
            {
                Logger.LogMessage("[preloadOrgan] Deleting leftover memory effect");
                GameObject.Destroy(eff);
            }
        }

        private void clearInstances()
        {
            laceNPCInstance = null;
            laceNPCFSMOwner = null;

            laceBossInstance = null;
            laceBossSceneInstance = null;

            laceBossFSMOwner = null;

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

            laceNPCCache.Unload();
            lace2BossSceneCache.Unload();
            lace1BossSceneCache.Unload();

            challengeDialogCache.Unload();
            wakeupPointCache.Unload();
            deepMemoryCache.Unload();

            ExitMemoryCache = null;

        }

        public void setupNormalFight()
        {
            Logger.LogMessage($"[setupFight] Trying to register phantom");
            phantomBossScene = SceneObjectManager.findObjectInCurrentScene("Boss Scene");
            Logger.LogInfo($"[setupFight] {phantomBossScene}");

            Logger.LogMessage($"[setupFight] Registering FSMOwner");
            phantomBossSceneFSMOwner = new FsmOwnerDefault();
            phantomBossSceneFSMOwner.OwnerOption = OwnerDefaultOption.SpecifyGameObject;
            phantomBossSceneFSMOwner.GameObject = phantomBossScene;

            // ----------
            challengeDialogInstance = challengeDialogCache.InstantiateAsset();
            challengeDialogInstance.AddComponent<ChallengeRegion>();
            challengeDialogInstance.SetActive(true);

            // ---------- 
            laceBossSceneInstance = lace1BossSceneCache.InstantiateAsset();
            laceBossSceneInstance.AddComponent<Lace1>();
            laceBossSceneInstance.SetActive(true);
            Logger.LogInfo($"[setupFight] Trying to find Lace Boss from scene {laceBossSceneInstance.gameObject.name}");
            laceBossInstance = SceneObjectManager.findChildObject(laceBossSceneInstance, "Lace Boss1");
            Logger.LogInfo($"[setupFight] Lace object: {laceBossInstance}");
            laceBossInstance.SetActive(false); 
            laceBossInstance.AddComponent<Lace1>();
            foreach (DeactivateIfPlayerdataTrue deact in laceBossInstance.GetComponents(typeof(DeactivateIfPlayerdataTrue)))
            {
                deact.enabled = false;
            }

            laceBossFSMOwner = new FsmOwnerDefault();
            laceBossFSMOwner.OwnerOption = OwnerDefaultOption.SpecifyGameObject;
            laceBossFSMOwner.GameObject = laceBossInstance;

            // ----------
            laceNPCInstance = laceNPCCache.InstantiateAsset();
            laceNPCInstance.AddComponent<LaceNPC>();
            laceNPCInstance.SetActive(true);

            // ----------
            Logger.LogInfo($"[setupFight] Trying to set up phantom : phantom available? {phantomBossScene != null}");
            Logger.LogInfo($"[setupFight] {phantomBossScene}");
            phantomBossScene.AddComponent<PhantomScene>();
            SceneObjectManager.findChildObject(phantomBossScene, "Phantom").AddComponent<PhantomBoss>();
        }
       
        public void setupMemoryFight()
        {
            Logger.LogMessage($"[setupFight] Trying to register phantom");
            phantomBossScene = SceneObjectManager.findObjectInCurrentScene("Boss Scene");
            Logger.LogInfo($"[setupFight] {phantomBossScene}");

            Logger.LogMessage($"[setupFight] Registering FSMOwner");
            phantomBossSceneFSMOwner = new FsmOwnerDefault();
            phantomBossSceneFSMOwner.OwnerOption = OwnerDefaultOption.SpecifyGameObject;
            phantomBossSceneFSMOwner.GameObject = phantomBossScene;
            
            // ----------
            challengeDialogInstance = challengeDialogCache.InstantiateAsset();
            challengeDialogInstance.AddComponent<ChallengeRegion>();
            challengeDialogInstance.SetActive(true);

            // ----------
            laceBossSceneInstance = lace2BossSceneCache.InstantiateAsset();
            laceBossSceneInstance.AddComponent<Lace2Scene>();
            laceBossSceneInstance.SetActive(true);

            Logger.LogInfo($"[setupFight] Trying to find Lace Boss from scene {laceBossSceneInstance.gameObject.name}");
            laceBossInstance = SceneObjectManager.findChildObject(laceBossSceneInstance, "Lace Boss2 New");
            Logger.LogInfo($"[setupFight] Lace object: {laceBossInstance}");
            laceBossInstance.SetActive(false);
            laceBossInstance.AddComponent<Lace2>();
            ((DeactivateIfPlayerdataTrue)laceBossInstance.GetComponent(typeof(DeactivateIfPlayerdataTrue))).enabled = false;
            
            laceBossFSMOwner = new FsmOwnerDefault();
            laceBossFSMOwner.OwnerOption = OwnerDefaultOption.SpecifyGameObject;
            laceBossFSMOwner.GameObject = laceBossInstance;

            // ----------
            laceNPCInstance = laceNPCCache.InstantiateAsset();
            laceNPCInstance.AddComponent<LaceNPC>();
            laceNPCInstance.SetActive(true);

            // ----------
            Logger.LogInfo($"[setupFight] Trying to set up phantom : phantom available? {phantomBossScene != null}");
            Logger.LogInfo($"[setupFight] {phantomBossScene}");
            phantomBossScene.AddComponent<PhantomScene>();
            SceneObjectManager.findChildObject(phantomBossScene, "Phantom").AddComponent<PhantomBoss>();

        }

        private void setupDeepMemoryZone()
        {
            //PlayerData.instance.defeatedCoralKing = false;
            //PlayerData.instance.encounteredCoralKing = false;

            SilkenSisters.Log.LogWarning($"{PlayerData.instance.defeatedCoralKing}, {PlayerData.instance.defeatedCoralKing}");

            deepMemoryInstance = deepMemoryCache.InstantiateAsset();
            deepMemoryInstance.AddComponent<DeepMemory>();
            deepMemoryInstance.GetComponent<TestGameObjectActivator>().playerDataTest.TestGroups[0].Tests[0].FieldName = "defeatedPhantom";
            deepMemoryInstance.GetComponent<TestGameObjectActivator>().playerDataTest.TestGroups[0].Tests[0].BoolValue = false;
            deepMemoryInstance.SetActive(true);

            if (wakeupPointInstance == null)
            {
                Logger.LogMessage("[setupDeepMemoryZone] Setting up memory wake point");
                wakeupPointInstance = wakeupPointCache.InstantiateAsset();
                wakeupPointInstance.SetActive(false);
                wakeupPointInstance.AddComponent<WakeUpMemory>();
                DontDestroyOnLoad(wakeupPointInstance);
            }

            if (respawnPointInstance == null)
            {
                Logger.LogMessage("[setupDeepMemoryZone] Setting respawn point");
                deepMemoryInstance.findChildObject("door_wakeOnGround");
                respawnPointInstance = GameObject.Instantiate(deepMemoryInstance.FindChild("door_wakeOnGround"));
                respawnPointInstance.SetActive(false);
                respawnPointInstance.AddComponent<WakeUpRespawn>();
                DontDestroyOnLoad(respawnPointInstance);
            }
        }


        // Temporary for debug
        private void toggleLaceFSM()
        {
            if (laceBossInstance != null)
            {
                Logger.LogMessage("Pausing Lace");
                PlayMakerFSM pfsm = SceneObjectManager.findChildObject(laceBossInstance, "Lace Boss2 New").GetComponents<PlayMakerFSM>().First(pfsm => pfsm.FsmName == "Control");
                pfsm.fsm.manualUpdate = !pfsm.fsm.manualUpdate;
            }
        }

        private void spawnLaceBoss2()
        {

            laceBossSceneInstance = lace2BossSceneCache.InstantiateAsset();
            laceBossSceneInstance.AddComponent<Lace2Scene>();
            laceBossSceneInstance.SetActive(true);

            Logger.LogInfo($"[spawnLaceBoss2] Trying to find Lace Boss from scene {laceBossSceneInstance.gameObject.name}");
            laceBossInstance = laceBossSceneInstance.FindChild("Lace Boss2 New");
            Logger.LogInfo($"[spawnLaceBoss2] Lace object: {laceBossInstance}");
            laceBossInstance.SetActive(false);
            laceBossInstance.AddComponent<Lace2>();

            laceBossFSMOwner = new FsmOwnerDefault();
            laceBossFSMOwner.OwnerOption = OwnerDefaultOption.SpecifyGameObject;
            laceBossFSMOwner.GameObject = laceBossInstance;
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

                    if (SilkenSisters.hornet.GetComponent<ConstrainPosition>() == null)
                    {
                        SilkenSisters.hornetConstrain = SilkenSisters.hornet.AddComponent<ConstrainPosition>();

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
                laceBossInstance.SetActive(true);
                ((PlayMakerFSM)laceBossInstance.GetComponent(typeof(PlayMakerFSM))).SendEvent("BATTLE START FIRST");
            }

            if (Input.GetKey(modifierKey.Value) && Input.GetKeyDown(KeyCode.Keypad3))
            {
                laceBossInstance.GetComponent<HealthManager>().hp = 1;
            }

            if (Input.GetKey(modifierKey.Value) && Input.GetKeyDown(KeyCode.Keypad1))
            {
                SceneObjectManager.findChildObject(phantomBossScene, "Phantom").GetComponent<HealthManager>().hp = 1;
            }

            if (Input.GetKey(modifierKey.Value) && Input.GetKeyDown(KeyCode.Keypad2))
            {
                StartCoroutine(testLoading());
                //toggleLaceFSM();
            }

            if (Input.GetKey(modifierKey.Value) && Input.GetKeyDown(KeyCode.Keypad8))
            {
                SceneObjectManager.findChildObject(phantomBossScene, "Phantom").SetActive(false);
            }

            if (Input.GetKey(modifierKey.Value) && Input.GetKeyDown(KeyCode.Keypad6))
            {
                ((PlayMakerFSM)laceBossInstance.GetComponent(typeof(PlayMakerFSM))).SetState("Multihit Slash End");
            }

            if (Input.GetKey(modifierKey.Value) && Input.GetKeyDown(KeyCode.Keypad4))
            {
                PlayMakerFSM.BroadcastEvent("PHANTOM_SYNC");
                PlayMakerFSM.BroadcastEvent("LACE_SYNC");
            }

            if (Input.GetKey(modifierKey.Value) && Input.GetKeyDown(KeyCode.P))
            {
                PlayerData._instance.defeatedPhantom = true;
                PlayerData._instance.defeatedLaceTower = true;
                PlayerData._instance.blackThreadWorld = true;
                PlayerData._instance.hasNeedolinMemoryPowerup = true;
                SilkenSisters.Log.LogWarning($"[CanSetup] Scene:{SceneManager.GetActiveScene().name} " +
                    $"DefeatedLace2:{PlayerData._instance.defeatedLaceTower} " +
                    $"DefeatedPhantom:{PlayerData._instance.defeatedPhantom} " +
                    $"Act3:{PlayerData._instance.blackThreadWorld} " +
                    $"Needolin:{PlayerData._instance.hasNeedolinMemoryPowerup}");
            }

            if (Input.GetKey(modifierKey.Value) && Input.GetKeyDown(KeyCode.L))
            {
                PlayerData._instance.defeatedPhantom = false;
                PlayerData._instance.defeatedLace1 = false;
                PlayerData._instance.defeatedLaceTower = false;
                PlayerData._instance.blackThreadWorld = false;
                PlayerData._instance.hasNeedolinMemoryPowerup = false;
                SilkenSisters.Log.LogWarning($"[CanSetup] Scene:{SceneManager.GetActiveScene().name} " +
                    $"DefeatedLace2:{PlayerData._instance.defeatedLaceTower} " +
                    $"DefeatedPhantom:{PlayerData._instance.defeatedPhantom} " +
                    $"Act3:{PlayerData._instance.blackThreadWorld}");
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
            if (key.Contains("SILKEN_SISTERS")) sheetTitle = $"Mods.{SilkenSisters.Id}";
        }
    }

}