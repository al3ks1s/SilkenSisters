using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using SilkenSisters.Behaviors;
using SilkenSisters.Patches;
using Silksong.AssetHelper.Core;
using Silksong.AssetHelper.ManagedAssets;
using Silksong.AssetHelper.Plugin;
using Silksong.FsmUtil;
using Silksong.UnityHelper.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using TeamCherry.Localization;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

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
// whenever lace does the circle slash attack, smoke should rise up from around her when she does the downward thrust, similar to how it happens when Phantom does her steam slam

// ^.*SilkenSisters.*$

namespace SilkenSisters
{

    [BepInAutoPlugin(id: "io.github.al3ks1s.silkensisters")]
    [BepInDependency("org.silksong-modding.fsmutil")]
    [BepInDependency("org.silksong-modding.i18n")]
    [BepInDependency("org.silksong-modding.assethelper")]
    [BepInDependency("io.github.flibber-hk.filteredlogs", BepInDependency.DependencyFlags.SoftDependency)]

    public partial class SilkenSisters : BaseUnityPlugin
    {

        public List<ManagedAsset<GameObject>> _individualAssets = new List<ManagedAsset<GameObject>>();
        public static SilkenSisters plugin;

        public static Scene organScene;

        public ManagedAsset<GameObject> laceNPCCache = null;
        public ManagedAsset<GameObject> silkfliesCache = null;
        public ManagedAsset<GameObject> lace2BossSceneCache = null;
        public ManagedAsset<GameObject> lace1BossSceneCache = null;

        public ManagedAsset<GameObject> challengeDialogCache = null;
        public ManagedAsset<GameObject> wakeupPointCache = null;
        public ManagedAsset<GameObject> coralBossSceneCache = null;
        public ManagedAsset<GameObject> deepMemoryCache = null;

        public ManagedAsset<GameObject> infoPromptCache = null;

        public FsmState ExitMemoryCache = null;

        public GameObject laceNPCInstance = null;
        public FsmOwnerDefault laceNPCFSMOwner = null;

        public GameObject silkflies = null;

        public GameObject laceBossInstance = null;
        public GameObject laceBossSceneInstance = null;

        public FsmOwnerDefault laceBossFSMOwner = null;
        public FsmOwnerDefault phantomBossFSMOwner = null;

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

        internal static ManualLogSource Log;

        private ConfigEntry<KeyCode> modifierKey;
        private ConfigEntry<KeyCode> actionKey;
        internal ConfigEntry<float> syncWaitTime;
        internal ConfigEntry<float> syncDelay;
        internal ConfigEntry<float> syncGatherDistance;
        internal ConfigEntry<float> syncTeleDistance;
        internal ConfigEntry<float> syncRangeDistance;
        internal ConfigEntry<int> MaxHP;
        internal ConfigEntry<int> P2HP;
        internal ConfigEntry<int> P3HP;
        internal ConfigEntry<int> ParryCooldown;
        internal ConfigEntry<float> ParryBaitDistance;
        internal ConfigEntry<float> DefenseParryDistance;
        public static ConfigEntry<bool> syncedFight;

        private void Awake()
        {
            FilteredLogs.API.ApplyFilter(Name, BepInEx.Logging.LogLevel.Fatal | BepInEx.Logging.LogLevel.Error);

            SilkenSisters.Log = new ManualLogSource("SilkenSisters");
            BepInEx.Logging.Logger.Sources.Add(Log);

            SilkenSisters.plugin = this;
            bindConfig();
            requestAssets();

            SceneManager.sceneLoaded += onSceneLoaded;
            Harmony.CreateAndPatchAll(typeof(UtilityPatches));
            Harmony.CreateAndPatchAll(typeof(EncounterPatches));
            StartCoroutine(WaitAndPatch());

            Logger.LogMessage($"Plugin loaded and initialized");
        }
        
        private void bindConfig()
        {
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
                "Use the Synced patterns for the boss fights. Playtest version"
            );

            syncWaitTime = Config.Bind(
                "Sync fight",
                "Idle time", 
                1.5f,
                "Debug config for defining how long they will wait for each other to finish their actions"
            );

            syncDelay = Config.Bind(
                "Sync fight",
                "Delay time", 
                0.5f,
                "Debug config for defining how the anti-synchronous actions will be delayed"
            );

            syncGatherDistance = Config.Bind(
                "Sync fight",
                "Gather Distance",
                1.75f,
                "Debug config for defining how close lace and phantom must be for attacking"
            );

            syncTeleDistance = Config.Bind(
                "Sync fight",
                "Tele Distance",
                8f,
                "Debug Config that defines how far lace and phantom must be for teleportation move."
            );

            syncRangeDistance = Config.Bind(
                "Sync fight",
                "Range Distance",
                6f,
                "Debug Config that defines the checking distance between the siblings and hornet."
            );

            MaxHP = Config.Bind(
                "Sync fight",
                "Max HP",
                1500,
                "Debug Config that defines max pooled HP."
            );

            P2HP = Config.Bind(
                "Sync fight",
                "P2 HP",
                1100,
                "Debug Config that defines pooled hp p2 shift."
            );

            P3HP = Config.Bind(
                "Sync fight",
                "P3 HP",
                600,
                "Debug Config that defines pooled hp p3 shift."
            );

            ParryCooldown = Config.Bind(
                "Sync fight",
                "Parry CoolDown",
                3,
                "Debug Config that defines the number of attacks between each parry attempt."
            );

            ParryBaitDistance = Config.Bind(
                "Sync fight",
                "Parry Bait Distance",
                3f,
                "Debug Config that defines the distance at which they tp for parrybait."
            );

            DefenseParryDistance = Config.Bind(
                "Sync fight",
                "Defense Parry Distance",
                2f,
                "Debug Config that defines the distance at which they tp for defend parry."
            );

        }

        private void requestAssets()
        {
            laceNPCCache = ManagedAsset<GameObject>.FromSceneAsset("Coral_19", "Encounter Scene Control/Lace Meet/Lace NPC Blasted Bridge");
            lace2BossSceneCache = ManagedAsset<GameObject>.FromSceneAsset("Song_Tower_01", "Boss Scene");
            lace1BossSceneCache = ManagedAsset<GameObject>.FromSceneAsset("Bone_East_12", "Boss Scene");
            silkfliesCache = ManagedAsset<GameObject>.FromSceneAsset("Bone_East_12", "Boss Scene/Silkflies");

            challengeDialogCache = ManagedAsset<GameObject>.FromSceneAsset("Cradle_03", "Boss Scene/Intro Sequence");
            wakeupPointCache = ManagedAsset<GameObject>.FromSceneAsset("Memory_Coral_Tower", "Door Get Up");
            coralBossSceneCache = ManagedAsset<GameObject>.FromSceneAsset("Memory_Coral_Tower", "Boss Scene");
            deepMemoryCache = ManagedAsset<GameObject>.FromSceneAsset("Coral_Tower_01", "Memory Group");

            infoPromptCache = ManagedAsset<GameObject>.FromSceneAsset("Arborium_01", "Inspect Region");
        }

        private IEnumerator WaitAndPatch()
        {
            yield return new WaitForSeconds(10f); // Give game time to init Language
            Harmony.CreateAndPatchAll(typeof(Language_Get_Patch));
            Harmony.CreateAndPatchAll(typeof(FSM_Event_Patch));
        }

        public static bool canSetupLaceInteraction()
        {
            SilkenSisters.Log.LogDebug($"[CanSetup] Scene:{SceneManager.GetActiveScene().name} " +
                $"DefeatedLace2:{PlayerData._instance.defeatedLaceTower} " +
                $"DefeatedPhantom:{PlayerData._instance.defeatedPhantom} " +
                $"Act3:{PlayerData._instance.blackThreadWorld}");
            return SceneManager.GetActiveScene().name == "Organ_01" &&
                !PlayerData._instance.defeatedLaceTower && 
                PlayerData._instance.defeatedPhantom && 
                !PlayerData._instance.blackThreadWorld;
        }

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

            Stopwatch sw = Stopwatch.StartNew();

            List<ManagedAsset<GameObject>> _individualAssets = new List<ManagedAsset<GameObject>>
            {
                laceNPCCache,
                lace2BossSceneCache,
                lace1BossSceneCache,
                challengeDialogCache,
                wakeupPointCache,
                coralBossSceneCache,
                deepMemoryCache,
                infoPromptCache,
                silkfliesCache
            };

            if ( !_individualAssets.All(x => x.HasBeenLoaded)) { 
            Logger.LogMessage("[cacheGameObjects] Initializing cache");
                foreach (var asset in _individualAssets)
                {
                    asset.Load();
                }

                yield return new WaitUntil(() => _individualAssets.All(x => x.Handle.IsDone));

                GameObject bossScene = coralBossSceneCache.InstantiateAsset();
                PlayMakerFSM control = bossScene.GetFsmPreprocessed("Control");
                ExitMemoryCache = control.GetState("Exit Memory");
                GameObject.Destroy(bossScene);

                sw.Stop();
            }

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

                    FindHornet();

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
        
        private IEnumerator preloadOrgan()
        {

            yield return StartCoroutine(cacheGameObjects());
            if (!isMemory() && canSetupMemoryFight())
            {
                Logger.LogMessage($"[preloadOrgan] Is not memory and all requirements met, setting things up");
                setupDeepMemoryZone();
            }
            else if (!isMemory() && canSetupNormalFight())
            {
                Logger.LogMessage($"[preloadOrgan] Setting up normal fight");
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
                eff.transform.SetPosition2D(-100,-100);
            }
        }

        private void clearInstances()
        {
            laceNPCInstance = null;
            laceNPCFSMOwner = null;
            silkflies = null;

            laceBossInstance = null;
            laceBossSceneInstance = null;

            laceBossFSMOwner = null;

            challengeDialogInstance = null;
            deepMemoryInstance = null;

            phantomBossScene = null;
            phantomBossSceneFSMOwner = null;
            phantomBossFSMOwner = null;

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
            silkfliesCache.Unload();
            
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
            phantomBossScene = SceneManager.GetActiveScene().FindGameObject("Boss Scene");
            Logger.LogInfo($"[setupFight] {phantomBossScene}"); 

            phantomBossSceneFSMOwner = new FsmOwnerDefault { gameObject = phantomBossScene, OwnerOption = OwnerDefaultOption.SpecifyGameObject };
            phantomBossFSMOwner = new FsmOwnerDefault { gameObject = phantomBossScene.FindChild("Phantom"), OwnerOption = OwnerDefaultOption.SpecifyGameObject };

            /* ----------
            challengeDialogInstance = challengeDialogCache.InstantiateAsset();
            challengeDialogInstance.AddComponent<ChallengeRegion>();
            challengeDialogInstance.SetActive(true);
            //*/

            // ---------- 
            laceBossSceneInstance = lace1BossSceneCache.InstantiateAsset();
            foreach (DeactivateIfPlayerdataTrue deact in laceBossSceneInstance.GetComponents(typeof(DeactivateIfPlayerdataTrue))) deact.enabled = false;
            laceBossSceneInstance.AddComponent<Lace1Scene>();
            laceBossSceneInstance.SetActive(true);
            laceBossInstance = laceBossSceneInstance.FindChild("Lace Boss1");
            laceBossInstance.SetActive(false); 
            laceBossInstance.AddComponent<Lace1>();

            laceBossFSMOwner = new FsmOwnerDefault { gameObject = laceBossInstance, OwnerOption = OwnerDefaultOption.SpecifyGameObject };

            // ----------
            laceNPCInstance = laceNPCCache.InstantiateAsset();
            laceNPCInstance.AddComponent<LaceNPC>();
            laceNPCInstance.SetActive(true);


            // ----------
            Logger.LogInfo($"[setupFight] Trying to set up phantom : phantom available? {phantomBossScene != null}");
            phantomBossScene.AddComponent<PhantomScene>();
            phantomBossScene.FindChild("Phantom").AddComponent<PhantomBoss>();
        }
       
        public void setupMemoryFight()
        {
            Logger.LogMessage($"[setupFight] Trying to register phantom");
            phantomBossScene = SceneManager.GetActiveScene().FindGameObject("Boss Scene");
            Logger.LogInfo($"[setupFight] {phantomBossScene}");

            phantomBossSceneFSMOwner = new FsmOwnerDefault { gameObject = phantomBossScene, OwnerOption = OwnerDefaultOption.SpecifyGameObject };
            phantomBossFSMOwner = new FsmOwnerDefault { gameObject = phantomBossScene.FindChild("Phantom"), OwnerOption = OwnerDefaultOption.SpecifyGameObject };

            // ----------
            challengeDialogInstance = challengeDialogCache.InstantiateAsset();
            challengeDialogInstance.AddComponent<ChallengeRegion>();
            challengeDialogInstance.SetActive(true);

            // ----------
            laceBossSceneInstance = lace2BossSceneCache.InstantiateAsset();
            laceBossSceneInstance.AddComponent<Lace2Scene>();
            laceBossSceneInstance.SetActive(true);

            laceBossInstance = laceBossSceneInstance.FindChild("Lace Boss2 New");
            laceBossInstance.SetActive(false);
            laceBossInstance.AddComponent<Lace2>();
            ((DeactivateIfPlayerdataTrue)laceBossInstance.GetComponent(typeof(DeactivateIfPlayerdataTrue))).enabled = false;

            laceBossFSMOwner = new FsmOwnerDefault { gameObject = laceBossInstance, OwnerOption = OwnerDefaultOption.SpecifyGameObject };
            laceBossInstance.SetActive(true);

            // ----------
            laceNPCInstance = laceNPCCache.InstantiateAsset();
            laceNPCInstance.AddComponent<LaceNPC>();
            laceNPCInstance.SetActive(true);

            // ----------
            Logger.LogInfo($"[setupFight] Trying to set up phantom : phantom available? {phantomBossScene != null}");
            Logger.LogInfo($"[setupFight] {phantomBossScene}");
            phantomBossScene.AddComponent<PhantomScene>();
            phantomBossScene.FindChild("Phantom").AddComponent<PhantomBoss>();

            phantomBossScene.AddComponent<SyncControl>();


        }

        private void setupDeepMemoryZone()
        {

            SilkenSisters.Log.LogDebug($"{PlayerData.instance.defeatedCoralKing}, {PlayerData.instance.defeatedCoralKing}");

            deepMemoryInstance = deepMemoryCache.InstantiateAsset();
            deepMemoryInstance.SetActive(false);
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
                deepMemoryInstance.FindChild("door_wakeOnGround");
                respawnPointInstance = GameObject.Instantiate(deepMemoryInstance.FindChild("door_wakeOnGround"));
                respawnPointInstance.SetActive(false);
                respawnPointInstance.AddComponent<WakeUpRespawn>();
                DontDestroyOnLoad(respawnPointInstance);
            }
        }

        public void FindHornet()
        {
            if (SilkenSisters.hornet == null)
            {
                if (HeroController.instance != null)
                {
                    SilkenSisters.hornet = HeroController.instance.gameObject;
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
            }
        }


        private void Update()
        {

            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Keypad0))
            {
                ((PlayMakerFSM)phantomBossScene.FindChild("Phantom").GetComponent(typeof(PlayMakerFSM))).SetState("Phase Parry Bait");
            }
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Keypad1))
            {
                ((PlayMakerFSM)phantomBossScene.FindChild("Phantom").GetComponent(typeof(PlayMakerFSM))).SetState("Phase Defense Parry");
            }
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Keypad2))
            {
                ((PlayMakerFSM)phantomBossScene.FindChild("Phantom").GetComponent(typeof(PlayMakerFSM))).SetState("Evade Antic");
            }
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Keypad3))
            {
                ((PlayMakerFSM)phantomBossScene.FindChild("Phantom").GetComponent(typeof(PlayMakerFSM))).SetState("Run To Lace");
            }
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Keypad4))
            {
                ((PlayMakerFSM)phantomBossScene.FindChild("Phantom").GetComponent(typeof(PlayMakerFSM))).SetState("Parry Antic");
            }
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Keypad5))
            {
                ((PlayMakerFSM)phantomBossScene.FindChild("Phantom").GetComponent(typeof(PlayMakerFSM))).SetState("G Throw Antic");
            }
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Keypad6))
            {
                ((PlayMakerFSM)phantomBossScene.FindChild("Phantom").GetComponent(typeof(PlayMakerFSM))).SetState("Set A Throw");
            }
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Keypad7))
            {
                ((PlayMakerFSM)phantomBossScene.FindChild("Phantom").GetComponent(typeof(PlayMakerFSM))).SetState("A Throw Aim");
            }
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Keypad8))
            {
                ((PlayMakerFSM)phantomBossScene.FindChild("Phantom").GetComponent(typeof(PlayMakerFSM))).SetState("Normal Dragoon");
            }
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Keypad9))
            {
                ((PlayMakerFSM)phantomBossScene.FindChild("Phantom").GetComponent(typeof(PlayMakerFSM))).SetState("Dragoon Rage");
            }
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.KeypadPlus))
            {
                ((PlayMakerFSM)phantomBossScene.FindChild("Phantom").GetComponent(typeof(PlayMakerFSM))).SetState("Phase Antic");
            }

            if (Input.GetKey(KeyCode.RightControl) && Input.GetKeyDown(KeyCode.Keypad0))
            {
                ((PlayMakerFSM)laceBossInstance.GetComponent(typeof(PlayMakerFSM))).SetState("Charge Antic");
            }
            if (Input.GetKey(KeyCode.RightControl) && Input.GetKeyDown(KeyCode.Keypad1))
            {
                ((PlayMakerFSM)laceBossInstance.GetComponent(typeof(PlayMakerFSM))).SetState("J Slash Antic");
            }
            if (Input.GetKey(KeyCode.RightControl) && Input.GetKeyDown(KeyCode.Keypad2))
            {
                ((PlayMakerFSM)laceBossInstance.GetComponent(typeof(PlayMakerFSM))).SetState("Evade");
            }
            if (Input.GetKey(KeyCode.RightControl) && Input.GetKeyDown(KeyCode.Keypad3))
            {
                ((PlayMakerFSM)laceBossInstance.GetComponent(typeof(PlayMakerFSM))).SetState("Counter Antic");
            }
            if (Input.GetKey(KeyCode.RightControl) && Input.GetKeyDown(KeyCode.Keypad4))
            {
                ((PlayMakerFSM)laceBossInstance.GetComponent(typeof(PlayMakerFSM))).SetState("ComboSlash 1");
            }
            if (Input.GetKey(KeyCode.RightControl) && Input.GetKeyDown(KeyCode.Keypad5))
            {
                ((PlayMakerFSM)laceBossInstance.GetComponent(typeof(PlayMakerFSM))).SetState("CrossSlash Aim");
            }
            if (Input.GetKey(KeyCode.RightControl) && Input.GetKeyDown(KeyCode.Keypad6))
            {
                ((PlayMakerFSM)laceBossInstance.GetComponent(typeof(PlayMakerFSM))).SetState("Tele Out");
            }
            if (Input.GetKey(KeyCode.RightControl) && Input.GetKeyDown(KeyCode.Keypad7))
            {
                ((PlayMakerFSM)laceBossInstance.GetComponent(typeof(PlayMakerFSM))).SetState("Hop To Phantom");
            }
            if (Input.GetKey(KeyCode.RightControl) && Input.GetKeyDown(KeyCode.Keypad8))
            {
                ((PlayMakerFSM)laceBossInstance.GetComponent(typeof(PlayMakerFSM))).SetState("Tele Out Defense");
            }
            if (Input.GetKey(KeyCode.RightControl) && Input.GetKeyDown(KeyCode.Keypad9))
            {
                ((PlayMakerFSM)laceBossInstance.GetComponent(typeof(PlayMakerFSM))).SetState("Tele Out Bait");
            }

            if (Input.GetKey(KeyCode.RightControl) && Input.GetKeyDown(KeyCode.P))
            {
                phantomBossScene.GetFsm("Silken Sisters Sync Control").SendEvent("FINISHE");
            }


            if (Input.GetKey(modifierKey.Value) && Input.GetKeyDown(KeyCode.H))
            {
                SilkenSisters.hornet.transform.position = new Vector3(90.45f, 105f, 0.004f);
            }

            if (Input.GetKey(modifierKey.Value) && Input.GetKeyDown(KeyCode.Keypad3))
            {
                laceBossInstance.GetComponent<HealthManager>().hp = 1;
            }

            if (Input.GetKey(modifierKey.Value) && Input.GetKeyDown(KeyCode.Keypad1))
            {
                phantomBossScene.FindChild("Phantom").GetComponent<HealthManager>().hp = 1;
            }


            if (Input.GetKey(modifierKey.Value) && Input.GetKeyDown(KeyCode.O))
            {
                PlayerData.instance.defeatedPhantom = false;
                PlayerData.instance.blackThreadWorld = false;
                var op = SceneManager.LoadSceneAsync("Organ_01", LoadSceneMode.Single);
                op.completed += (AsyncOperation op) =>
                {
                    GameManager._instance.ForceCurrentSceneIsMemory(true);
                    setupMemoryFight();
                    SilkenSisters.hornet.transform.position = new Vector3(90.45f, 105f, 0.004f);
                };
            }

            if (Input.GetKey(modifierKey.Value) && Input.GetKeyDown(KeyCode.U))
            {
                PlayerData.instance.defeatedPhantom = true;
                PlayerData.instance.blackThreadWorld = true;
                HeroController.instance.RefillSilkToMaxSilent();
                var op = SceneManager.LoadSceneAsync("Organ_01", LoadSceneMode.Single);
                op.completed += (AsyncOperation op) =>
                {
                    
                };
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
                var op = SceneManager.LoadSceneAsync("Organ_01", LoadSceneMode.Single);
                op.completed += (AsyncOperation op) =>
                {

                };
            }

            if (Input.GetKey(modifierKey.Value) && Input.GetKeyDown(KeyCode.L))
            {
                PlayerData._instance.defeatedPhantom = false;
                PlayerData._instance.defeatedLace1 = false;
                PlayerData._instance.defeatedLaceTower = false;
                PlayerData._instance.blackThreadWorld = false;
                PlayerData._instance.hasNeedolinMemoryPowerup = false;
                PlayerData._instance.encounteredLace1 = false;
                SilkenSisters.Log.LogWarning($"[CanSetup] Scene:{SceneManager.GetActiveScene().name} " +
                    $"DefeatedLace2:{PlayerData._instance.defeatedLaceTower} " +
                    $"DefeatedPhantom:{PlayerData._instance.defeatedPhantom} " +
                    $"Act3:{PlayerData._instance.blackThreadWorld}");
                var op = SceneManager.LoadSceneAsync("Organ_01", LoadSceneMode.Single);
                op.completed += (AsyncOperation op) =>
                {

                };
            }
        }
    
    }
}