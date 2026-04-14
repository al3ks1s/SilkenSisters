using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using HutongGames.PlayMaker;
using PrepatcherPlugin;
using SilkenSisters.Behaviors;
using SilkenSisters.Patches;
using SilkenSisters.Utils;
using Silksong.AssetHelper.ManagedAssets;
using Silksong.DataManager;
using Silksong.FsmUtil;
using Silksong.UnityHelper.Extensions;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using UnityEngine.SceneManagement;

using SD = System.Diagnostics;

// Idea by AidaCamelia0516

// Three levels of fight :
// Lvl 1: Lace 1 Skipped -> Fight lace 1 and phantom at the same time       -> Cutscene shows hornet and lace instead of phantom, the moment parry is hit, phantom kicks lace out and take the bullet
// Lvl 2: Deep memory in organ chamber -> Lace 2 + Phantom                  -> This time lace takes the bullet, single final phase of dragoon dash
// Lvl 3: Taunt challenge in deep memory -> Lace 2 + Phantom on steroids    -> Fakeout cutscene, goes on normally until lace repels hornet + Hard phase

// ^.*SilkenSisters.*$

namespace SilkenSisters
{

    public class SaveData
    {
        public bool laceMourned { get; set; }
    }


    [BepInAutoPlugin(id: "io.github.al3ks1s.silkensisters")]
    [BepInDependency(Silksong.FsmUtil.Plugin.Id)]
    [BepInDependency("org.silksong-modding.i18n")]
    [BepInDependency(Silksong.AssetHelper.AssetHelperPlugin.Id)]
    [BepInDependency(PrepatcherPlugin.PrepatcherPlugin.Id)]
    [BepInDependency(Silksong.DataManager.DataManagerPlugin.Id)]
    [BepInDependency("io.github.flibber-hk.filteredlogs", BepInDependency.DependencyFlags.SoftDependency)]
    public partial class SilkenSisters : BaseUnityPlugin, ISaveDataMod<SaveData>
    {
        public static SilkenSisters instance;

        private SaveData _saveData = new();

        [AllowNull]
        public SaveData SaveData { get => _saveData; set => _saveData = value ?? new(); }
        

        internal AssetManager assetManager = new AssetManager();
        internal SilkenSistersConfig configManager = new SilkenSistersConfig();
        
        
        public FsmState ExitMemoryCache = null;

        public GameObject silkflies = null;

        public GameObject laceNPCInstance = null;
        public FsmOwnerDefault laceNPCFSMOwner = null;

        public GameObject laceBossInstance = null;
        public GameObject laceBossSceneInstance = null;

        public FsmOwnerDefault laceBossFSMOwner = null;
        public FsmOwnerDefault phantomBossFSMOwner = null;

        public GameObject challengeDialogInstance = null;
        public GameObject wakeupPointInstance = null;
        public GameObject respawnPointInstance = null;
        public GameObject deepMemoryInstance = null;

        public GameObject infoPromptInstance = null;
        public GameObject syncedFightSwapLeverInstance = null;

        public GameObject phantomBossScene = null;
        public FsmOwnerDefault phantomBossSceneFSMOwner = null;


        public static GameObject hornet = null;
        public static FsmOwnerDefault hornetFSMOwner = null;
        public static ConstrainPosition hornetConstrain = null;

        
        internal static ManualLogSource Log;

        internal Harmony _utilitypatches;
        internal Harmony _langagepatches;
        internal Harmony _encounterpatches;
        internal Harmony _debugatches;

        private void Awake()
        {
            //FilteredLogs.API.ApplyFilter(Name, BepInEx.Logging.LogLevel.Fatal | BepInEx.Logging.LogLevel.Error);

            SilkenSisters.instance = this;
            SilkenSisters.Log = new ManualLogSource("SilkenSisters");
            BepInEx.Logging.Logger.Sources.Add(Log);

            configManager.BindConfig(Config);
            assetManager.RequestAssets();

            SceneManager.sceneLoaded += onSceneLoaded;

            DebugPatches.CreateDebugPatch();
            _encounterpatches = Harmony.CreateAndPatchAll(typeof(EncounterPatches));
            _utilitypatches = Harmony.CreateAndPatchAll(typeof(UtilityPatches));
            StartCoroutine(WaitAndPatch());

            Log.LogMessage($"Plugin loaded and initialized");
        }

        void OnDestroy()
        {
            clearInstances();
            assetManager.ClearCache();


            _langagepatches.UnpatchSelf();
            _encounterpatches.UnpatchSelf();
            _utilitypatches.UnpatchSelf();

            DebugPatches.RemoveDebugPatch();
        }

        private IEnumerator WaitAndPatch()
        {
            yield return new WaitForSeconds(10f); // Give game time to init Language
            _langagepatches = Harmony.CreateAndPatchAll(typeof(Language_Get_Patch));
        }

        public static bool canSetupLaceInteraction()
        {
            SilkenSisters.Log.LogDebug($"[CanSetupLaceInteraction] Scene:{SceneManager.GetActiveScene().name} " +
                $"DefeatedLace2:{PlayerDataAccess.defeatedLaceTower} " +
                $"DefeatedPhantom:{PlayerDataAccess.defeatedPhantom} " +
                $"Act3:{PlayerDataAccess.blackThreadWorld}");
            return SceneManager.GetActiveScene().name == "Organ_01" &&
                    !PlayerDataAccess.defeatedLaceTower && 
                     PlayerDataAccess.defeatedPhantom && 
                    !PlayerDataAccess.blackThreadWorld && 
                    //!PlayerDataAccess.encounteredLaceTower && 
                    !SilkenSisters.instance.SaveData.laceMourned;
        }

        public static bool canSetupMemoryFight()
        {
            SilkenSisters.Log.LogDebug($"[CanSetup] Scene:{SceneManager.GetActiveScene().name} " +
                $"DefeatedLace2:{PlayerDataAccess.defeatedLaceTower} " +
                $"DefeatedPhantom:{PlayerDataAccess.defeatedPhantom} " +
                $"Act3:{PlayerDataAccess.blackThreadWorld} " +
                $"Needolin:{PlayerDataAccess.hasNeedolinMemoryPowerup}");
            return SceneManager.GetActiveScene().name == "Organ_01" && 
                    PlayerDataAccess.defeatedLaceTower &&
                    PlayerDataAccess.defeatedPhantom && 
                    PlayerDataAccess.blackThreadWorld && 
                    PlayerDataAccess.hasNeedolinMemoryPowerup;
        }

        public static bool canSetupNormalFight()
        {
            SilkenSisters.Log.LogDebug($"[CanSetup] Scene:{SceneManager.GetActiveScene().name} " +
                $"DefeatedLace1:{PlayerDataAccess.defeatedLace1} " +
                $"DefeatedLace2:{PlayerDataAccess.defeatedLaceTower} " +
                $"DefeatedPhantom:{PlayerDataAccess.defeatedPhantom} " +
                $"Act3:{PlayerDataAccess.blackThreadWorld}");
            return SceneManager.GetActiveScene().name == "Organ_01" &&
                    !PlayerDataAccess.defeatedLace1 &&
                    !PlayerDataAccess.defeatedLaceTower &&
                    !PlayerDataAccess.defeatedPhantom && 
                    !PlayerDataAccess.blackThreadWorld;
        }

        public static bool isMemory()
        {
            SilkenSisters.Log.LogDebug($"[isMemory] Scene:{SceneManager.GetActiveScene().name} " +
                $"DefeatedPhantom:{PlayerDataAccess.defeatedPhantom} " +
                $"Act3:{PlayerDataAccess.blackThreadWorld} " +
                $"Needolin:{PlayerDataAccess.hasNeedolinMemoryPowerup}");
            return SceneManager.GetActiveScene().name == "Organ_01" && !PlayerDataAccess.defeatedPhantom && !PlayerDataAccess.blackThreadWorld && PlayerDataAccess.hasNeedolinMemoryPowerup;
        }
        
        private void onSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Log.LogDebug($"[onSceneLoaded] Scene loaded : {scene.name}, active scene : {SceneManager.GetActiveScene()}, Path:{scene.path}");

            if (scene.name == "Organ_01") {
                Log.LogDebug($"[onSceneLoaded] Organ Detected, preloading");

                FindHornet();
                StartCoroutine(preloadOrgan());
            } else {
                Log.LogDebug($"[onSceneLoaded] Scene is not organ, clearing instances");
                clearInstances();
            }

            if (scene.name == "Quit_To_Menu") {
                PlayerDataVariableEvents.OnGetBool -= PrepatcherUtils.SilkenSisterMonitor;
                clearInstances();
                assetManager.ClearCache();
            }
        }
        
        private IEnumerator preloadOrgan()
        {

            yield return StartCoroutine(assetManager.CacheObjects());

            if (!isMemory() &&  canSetupMemoryFight())
            {
                Log.LogDebug($"[preloadOrgan] Is not memory and all requirements met, setting things up");
                setupDeepMemoryZone();
            }
            else if (!isMemory() && canSetupNormalFight())
            {
                Log.LogDebug($"[preloadOrgan] Setting up normal fight");
                setupNormalFight();
            }
            else
            {
                Log.LogDebug($"[preloadOrgan] Scene info: canSetup?:{canSetupMemoryFight()}, isMemory?:{isMemory()}");
                if (!isMemory() && !canSetupMemoryFight() && !canSetupNormalFight())
                {
                    Log.LogDebug($"[preloadOrgan] Displaying the info prompt");
                    infoPromptInstance = assetManager.sceneCache.InstantiateAsset<GameObject>("infoPromptCache");
                    infoPromptInstance.AddComponent<InfoPrompt>();
                    infoPromptInstance.SetActive(true);
                }

            }

            if (PlayerDataAccess.defeatedPhantom)
            {
                phantomBossScene = SceneManager.GetActiveScene().FindGameObject("Boss Scene");
                GameObject mask = SilkenSisters.instance.phantomBossScene.FindChild("Return Mask");
                GameObject pin = SilkenSisters.instance.phantomBossScene.FindChild("Return Mask/Death Pin");

                mask.transform.SetPosition3D(84.4431f, 107.719f, 3.5096f);
                mask.transform.SetRotationZ(2.1319f);
                pin.transform.SetPosition3D(83.7081f, 107.4613f, 3.5106f);
                pin.transform.SetRotationZ(239.2857f);
            }

            if (canSetupLaceInteraction())
            {
                phantomBossScene = SceneManager.GetActiveScene().FindGameObject("Boss Scene");

                laceNPCInstance = assetManager.sceneCache.InstantiateAsset<GameObject>("laceNPCCache");
                laceNPCInstance.AddComponent<LaceMourning>();
                laceNPCInstance.SetActive(true);
            }

            yield return new WaitForSeconds(0.2f);
            GameObject eff = GameObject.Find("Deep Memory Enter Black(Clone)");
            if (eff != null)
            {
                Log.LogDebug("[preloadOrgan] Deleting leftover memory effect");
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

            infoPromptInstance = null;
            syncedFightSwapLeverInstance = null;

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

        public void setupNormalFight()
        {
            Log.LogDebug($"[setupFight] Trying to register phantom");
            phantomBossScene = SceneManager.GetActiveScene().FindGameObject("Boss Scene");
            Log.LogDebug($"[setupFight] {phantomBossScene}"); 

            phantomBossSceneFSMOwner = new FsmOwnerDefault { gameObject = phantomBossScene, OwnerOption = OwnerDefaultOption.SpecifyGameObject };
            phantomBossFSMOwner = new FsmOwnerDefault { gameObject = phantomBossScene.FindChild("Phantom"), OwnerOption = OwnerDefaultOption.SpecifyGameObject };

            // ---------- 
            laceBossSceneInstance = assetManager.sceneCache.InstantiateAsset<GameObject>("lace1BossSceneCache");
            foreach (DeactivateIfPlayerdataTrue deact in laceBossSceneInstance.GetComponents(typeof(DeactivateIfPlayerdataTrue))) deact.enabled = false;
            laceBossSceneInstance.AddComponent<Lace1Scene>();
            laceBossSceneInstance.SetActive(true);
            laceBossInstance = laceBossSceneInstance.FindChild("Lace Boss1");
            laceBossInstance.SetActive(false); 
            laceBossInstance.AddComponent<Lace1>();

            laceBossFSMOwner = new FsmOwnerDefault { gameObject = laceBossInstance, OwnerOption = OwnerDefaultOption.SpecifyGameObject };

            // ----------
            laceNPCInstance = assetManager.sceneCache.InstantiateAsset<GameObject>("laceNPCCache");
            laceNPCInstance.AddComponent<LaceNPC>();
            laceNPCInstance.SetActive(true);

            // ----------
            Log.LogDebug($"[setupFight] Trying to set up phantom : phantom available? {phantomBossScene != null}");
            phantomBossScene.AddComponent<PhantomScene>();
            phantomBossScene.FindChild("Phantom").AddComponent<PhantomBoss>();
        }
       
        public void setupMemoryFight()
        {
            Log.LogDebug($"[setupFight] Trying to register phantom");
            phantomBossScene = SceneManager.GetActiveScene().FindGameObject("Boss Scene");
            Log.LogDebug($"[setupFight] {phantomBossScene}");

            phantomBossSceneFSMOwner = new FsmOwnerDefault { gameObject = phantomBossScene, OwnerOption = OwnerDefaultOption.SpecifyGameObject };
            phantomBossFSMOwner = new FsmOwnerDefault { gameObject = phantomBossScene.FindChild("Phantom"), OwnerOption = OwnerDefaultOption.SpecifyGameObject };

            // ----------
            challengeDialogInstance = assetManager.sceneCache.InstantiateAsset<GameObject>("challengeDialogCache");
            challengeDialogInstance.AddComponent<ChallengeRegion>();
            challengeDialogInstance.SetActive(true);

            // ----------
            laceBossSceneInstance = assetManager.sceneCache.InstantiateAsset<GameObject>("lace2BossSceneCache");
            laceBossSceneInstance.AddComponent<Lace2Scene>();
            laceBossSceneInstance.SetActive(true);

            laceBossInstance = laceBossSceneInstance.FindChild("Lace Boss2 New");
            laceBossInstance.SetActive(false);
            laceBossInstance.AddComponent<Lace2>();
            ((DeactivateIfPlayerdataTrue)laceBossInstance.GetComponent(typeof(DeactivateIfPlayerdataTrue))).enabled = false;

            laceBossFSMOwner = new FsmOwnerDefault { gameObject = laceBossInstance, OwnerOption = OwnerDefaultOption.SpecifyGameObject };
            laceBossInstance.SetActive(true);

            // ----------
            laceNPCInstance = assetManager.sceneCache.InstantiateAsset<GameObject>("laceNPCCache");
            laceNPCInstance.AddComponent<LaceNPC>();
            laceNPCInstance.SetActive(true);

            // ----------
            Log.LogDebug($"[setupFight] Trying to set up phantom : phantom available? {phantomBossScene != null}");
            Log.LogDebug($"[setupFight] {phantomBossScene}");
            phantomBossScene.AddComponent<PhantomScene>();
            phantomBossScene.FindChild("Phantom").AddComponent<PhantomBoss>();

            phantomBossScene.AddComponent<SyncControl>();


        }

        private void setupDeepMemoryZone()
        {

            SilkenSisters.Log.LogDebug($"{PlayerDataAccess.defeatedCoralKing}, {PlayerDataAccess.defeatedCoralKing}");

            deepMemoryInstance = assetManager.sceneCache.InstantiateAsset<GameObject>("deepMemoryCache");
            deepMemoryInstance.SetActive(false);
            deepMemoryInstance.AddComponent<DeepMemory>();
            deepMemoryInstance.GetComponent<TestGameObjectActivator>().playerDataTest.TestGroups[0].Tests[0].FieldName = "defeatedPhantom";
            deepMemoryInstance.GetComponent<TestGameObjectActivator>().playerDataTest.TestGroups[0].Tests[0].BoolValue = false;
            deepMemoryInstance.SetActive(true);

            if (wakeupPointInstance == null)
            {
                Log.LogDebug("[setupDeepMemoryZone] Setting up memory wake point");
                wakeupPointInstance = assetManager.sceneCache.InstantiateAsset<GameObject>("wakeupPointCache");
                wakeupPointInstance.SetActive(false);
                wakeupPointInstance.AddComponent<WakeUpMemory>();
                DontDestroyOnLoad(wakeupPointInstance);
            }

            if (respawnPointInstance == null)
            {
                Log.LogDebug("[setupDeepMemoryZone] Setting respawn point");
                deepMemoryInstance.FindChild("door_wakeOnGround");
                respawnPointInstance = GameObject.Instantiate(deepMemoryInstance.FindChild("door_wakeOnGround"));
                respawnPointInstance.SetActive(false);
                respawnPointInstance.AddComponent<WakeUpRespawn>();
                DontDestroyOnLoad(respawnPointInstance);
            }

            syncedFightSwapLeverInstance = assetManager.sceneCache.InstantiateAsset<GameObject>("syncedFightSwapLeverCache");
            syncedFightSwapLeverInstance.AddComponent<FightSelectionLever>();

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

            CheckDebugKeybinds();

            if (Input.GetKey(configManager.modifierKey.Value) && Input.GetKeyDown(KeyCode.Y))
            {
                PlayerDataAccess.defeatedPhantom = true;
                PlayerDataAccess.defeatedLaceTower = true;
                PlayerDataAccess.blackThreadWorld = true;
                PlayerDataAccess.hasNeedolinMemoryPowerup = true;
                SilkenSisters.Log.LogWarning($"[CanSetup] Scene:{SceneManager.GetActiveScene().name} " +
                                                        $"DefeatedLace2:{PlayerDataAccess.defeatedLaceTower} " +
                                                        $"DefeatedPhantom:{PlayerDataAccess.defeatedPhantom} " +
                                                        $"Act3:{PlayerDataAccess.blackThreadWorld} " +
                                                        $"Needolin:{PlayerDataAccess.hasNeedolinMemoryPowerup}");
            }

        }

        [SD.Conditional("DEBUG")]
        private void CheckDebugKeybinds()
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
                laceNPCInstance.GetFsm("Control").SetState("Lock Hornet");
            }


            if (Input.GetKey(configManager.modifierKey.Value) && Input.GetKeyDown(KeyCode.H))
            {
                SilkenSisters.hornet.transform.position = new Vector3(90.45f, 105f, 0.004f);
            }

            if (Input.GetKey(configManager.modifierKey.Value) && Input.GetKeyDown(KeyCode.Keypad3))
            {
                laceBossInstance.GetComponent<HealthManager>().hp = 1;
            }

            if (Input.GetKey(configManager.modifierKey.Value) && Input.GetKeyDown(KeyCode.Keypad1))
            {
                phantomBossScene.FindChild("Phantom").GetComponent<HealthManager>().hp = 1;
            }

            if (Input.GetKey(configManager.modifierKey.Value) && Input.GetKeyDown(KeyCode.O))
            {

                PlayerDataVariableEvents.OnGetBool += PrepatcherUtils.SilkenSisterMonitor;

                var op = SceneManager.LoadSceneAsync("Organ_01", LoadSceneMode.Single);
                op.completed += (AsyncOperation op) =>
                {
                    GameManager._instance.ForceCurrentSceneIsMemory(true);
                    setupMemoryFight();
                    SilkenSisters.hornet.transform.position = new Vector3(90.45f, 105f, 0.004f);
                };
            }

            if (Input.GetKey(configManager.modifierKey.Value) && Input.GetKeyDown(KeyCode.U))
            {
                PlayerDataAccess.defeatedPhantom = true;
                PlayerDataAccess.blackThreadWorld = true;
                HeroController.instance.RefillSilkToMaxSilent();
                var op = SceneManager.LoadSceneAsync("Organ_01", LoadSceneMode.Single);
                op.completed += (AsyncOperation op) =>
                {

                };
            }

            if (Input.GetKey(configManager.modifierKey.Value) && Input.GetKeyDown(KeyCode.K))
            {
                PlayerDataAccess.defeatedPhantom = true;
                PlayerDataAccess.blackThreadWorld = false;
                PlayerDataAccess.defeatedLaceTower = false;
                HeroController.instance.RefillSilkToMaxSilent();
                var op = SceneManager.LoadSceneAsync("Organ_01", LoadSceneMode.Single);
                op.completed += (AsyncOperation op) =>
                {

                };
            }

            if (Input.GetKey(configManager.modifierKey.Value) && Input.GetKeyDown(KeyCode.P))
            {
                PlayerDataAccess.defeatedPhantom = true;
                PlayerDataAccess.defeatedLaceTower = true;
                PlayerDataAccess.blackThreadWorld = true;
                PlayerDataAccess.hasNeedolinMemoryPowerup = true;
                SilkenSisters.Log.LogWarning($"[CanSetup] Scene:{SceneManager.GetActiveScene().name} " +
                    $"DefeatedLace2:{PlayerDataAccess.defeatedLaceTower} " +
                    $"DefeatedPhantom:{PlayerDataAccess.defeatedPhantom} " +
                    $"Act3:{PlayerDataAccess.blackThreadWorld} " +
                    $"Needolin:{PlayerDataAccess.hasNeedolinMemoryPowerup}");
                var op = SceneManager.LoadSceneAsync("Organ_01", LoadSceneMode.Single);
                op.completed += (AsyncOperation op) =>
                {

                };
            }

            if (Input.GetKey(configManager.modifierKey.Value) && Input.GetKeyDown(KeyCode.L))
            {
                PlayerDataAccess.defeatedPhantom = false;
                PlayerDataAccess.defeatedLace1 = false;
                PlayerDataAccess.defeatedLaceTower = false;
                PlayerDataAccess.blackThreadWorld = false;
                PlayerDataAccess.hasNeedolinMemoryPowerup = false;
                PlayerDataAccess.encounteredLace1 = false;
                SilkenSisters.Log.LogWarning($"[CanSetup] Scene:{SceneManager.GetActiveScene().name} " +
                    $"DefeatedLace2:{PlayerDataAccess.defeatedLaceTower} " +
                    $"DefeatedPhantom:{PlayerDataAccess.defeatedPhantom} " +
                    $"Act3:{PlayerDataAccess.blackThreadWorld}");
                var op = SceneManager.LoadSceneAsync("Organ_01", LoadSceneMode.Single);
                op.completed += (AsyncOperation op) =>
                {

                };
            }

        }

    }
}