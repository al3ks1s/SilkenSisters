using HarmonyLib;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using SilkenSisters.SceneManagement;
using Silksong.FsmUtil;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SilkenSisters.Behaviors
{
    internal class DeepMemory : MonoBehaviour
    {

        private PlayMakerFSM _control;
        private GameObject _before;

        private void Awake()
        {
            Setup();
        }

        private async Task Setup()
        {
            getComponents();
            setPosition();
            disableCrustKingObjects();
            editFSMTransition();
            editPlayerData();
            bypassToolPickup();
            manageTransitionGates();
            SilkenSisters.Log.LogInfo($"Finished setting up deep memory");
        }

        private void getComponents()
        {
            _control = gameObject.GetFsmPreprocessed("To Memory");
            _before = SceneObjectManager.findChildObject(gameObject, "before");
        }

        private void setPosition()
        {
            gameObject.transform.position = new Vector3(59.249f, 56.7457f, -3.1141f);
            SilkenSisters.Log.LogInfo($"Set deep memory zone position at {gameObject.transform.position}");
        }

        private void disableCrustKingObjects()
        {
            SilkenSisters.Log.LogInfo($"Finding and deleting coral king sprite");
            GameObject.Destroy(SceneObjectManager.findChildObject(_before, "CK_ground_hit0004").gameObject);
        }

        private void editFSMTransition()
        {
            SilkenSisters.Log.LogInfo($"Editing scene transition state actions");
            _control.GetAction<BeginSceneTransition>("Transition Scene", 4).sceneName = "Organ_01";
            _control.GetAction<BeginSceneTransition>("Transition Scene", 4).entryGateName = "door_wakeInMemory_phantom";
        }

        private void editPlayerData()
        {
            SilkenSisters.Log.LogInfo($"Setting playerdata to enable phantom fight");
            HutongGames.PlayMaker.Actions.SetPlayerDataBool enablePhantom = new HutongGames.PlayMaker.Actions.SetPlayerDataBool();
            enablePhantom.boolName = "defeatedPhantom";
            enablePhantom.value = false;

            HutongGames.PlayMaker.Actions.SetPlayerDataBool world_normal = new HutongGames.PlayMaker.Actions.SetPlayerDataBool();
            world_normal.boolName = "blackThreadWorld";
            world_normal.value = false;

            _control.InsertAction("Transition Scene", enablePhantom, 0);
            _control.InsertAction("Transition Scene", world_normal, 0);
        }

        private void bypassToolPickup()
        {
            SilkenSisters.Log.LogInfo($"Bypassing tool pick up for deep memory zone activation");
            PlayMakerFSM pickupFSM = _before.GetFsmPreprocessed("activate memory on tool pickup");
            pickupFSM.GetTransition("State 1", "PICKED UP").fsmEvent = FsmEvent.GetFsmEvent("FINISHED");
        }

        private void manageTransitionGates()
        {
            SilkenSisters.Log.LogInfo($"Adding action to enable memory door");
            InvokeMethod door = new InvokeMethod(enableDoor);
            _control.InsertAction("Transition Scene", door, 4);

            SilkenSisters.Log.LogInfo($"Adding action to disable tank respawn");
            InvokeMethod resp = new InvokeMethod(disableRespawn);
            _control.AddAction("Transition Scene", resp);
            
        }

        private void enableDoor()
        {
            SilkenSisters.plugin.wakeupPointInstance.SetActive(true);
            SilkenSisters.Log.LogInfo($"Set door to {SilkenSisters.plugin.wakeupPointInstance.activeSelf}");
        }
        private void disableRespawn()
        {
            SilkenSisters.plugin.respawnPointInstance.SetActive(false);
            SilkenSisters.plugin.respawnPointInstance.GetComponent<PlayMakerFSM>().fsm.SetState("Pause");
            SilkenSisters.Log.LogInfo($"Set respawn to {SilkenSisters.plugin.respawnPointInstance.activeSelf}");
        }

    }

    internal class WakeUpMemory : MonoBehaviour
    {

        private GameObject _wakeTransitionGate;
        private PlayMakerFSM _control;

        private void Awake()
        {
            Setup();
        }

        private async Task Setup()
        {
            getComponents();
            setName();
            setPosition();
            editFSM();
            SilkenSisters.Log.LogInfo("Finished setting up wake up point");
        }

        private void getComponents()
        {
            _wakeTransitionGate = SceneObjectManager.findChildObject(gameObject, "door_wakeInMemory");
            _control = _wakeTransitionGate.GetFsmPreprocessed("Wake Up");
        }

        private void setName()
        {
            SilkenSisters.Log.LogInfo("Editing wakeup point name");
            _wakeTransitionGate.name = "door_wakeInMemory_phantom";
        }

        private void setPosition()
        {
            //gameObject.transform.position = new Vector3(59.249f, 56.7457f, 0f);
            gameObject.transform.position = new Vector3(115.4518f, 104.5621f, 0f);
        }

        private void editFSM()
        {
            SilkenSisters.Log.LogInfo("Editing the door FSM");

            InvokeMethod inv2 = new InvokeMethod(enableRespawn);
            _control.AddAction("Take Control", inv2);

            InvokeMethod inv3 = new InvokeMethod(enableIsMemory);
            _control.AddAction("Take Control", inv3);

            InvokeMethod inv = new InvokeMethod(SilkenSisters.plugin.setupFight);
            _control.AddAction("Take Control", inv);

            _control.GetAction<ConvertBoolToFloat>("Fade Up", 1).falseValue = 3f;
            _control.GetAction<ConvertBoolToFloat>("Fade Up", 1).trueValue = 3f;

            InvokeMethod disSelf = new InvokeMethod(disableSelf);
            _control.AddAction("End", disSelf);


            InvokeMethod closeOrgan = new InvokeMethod(closeOffOrgan);
            _control.AddAction("End", closeOrgan);




        }

        private void enableIsMemory()
        {
            SilkenSisters.Log.LogInfo("Enabling current scene to be memory");
            GameManager._instance.ForceCurrentSceneIsMemory(true);
            SilkenSisters.Log.LogInfo($"Is Memory? {GameManager._instance.IsMemoryScene()} {GameManager._instance.forceCurrentSceneMemory}");
        }


        private void enableRespawn()
        {
            SilkenSisters.plugin.respawnPointInstance.SetActive(true);
            SilkenSisters.Log.LogInfo($"Set respawn to {SilkenSisters.plugin.respawnPointInstance.activeSelf}");
        }

        private void disableSelf()
        {
            gameObject.SetActive(false);
        }

        private void closeOffOrgan()
        {
            SilkenSisters.Log.LogInfo("Closing off organ exits");
            GameObject gildedDoor = SceneObjectManager.findObjectInCurrentScene("Boss Scene/Gates/Battle Gate (1)");

            GameObject.Instantiate(gildedDoor).transform.SetPosition3D(45.4916f, 71.6012f, 0.003f);
            GameObject.Instantiate(gildedDoor).transform.SetPosition3D(11.5445f, 8.5155f, 0.003f);
        }

    }

    internal class WakeUpRespawn : MonoBehaviour
    {
        private PlayMakerFSM _control;
        private void Awake()
        {
            Setup();
        }

        private async Task Setup()
        {
            getComponents();
            setName();
            setPosition();
            editFSM();
        }

        private void getComponents()
        {
            _control = gameObject.GetFsmPreprocessed("Control");
        }

        private void setName()
        {
            gameObject.name = "door_wakeOnGround_phantom";
        }
        private void setPosition()
        {
            gameObject.transform.position = new Vector3(59.249f, 56.7457f, 0f);
        }

        private void editFSM()
        {
            SilkenSisters.Log.LogInfo($"Editing FSM to disable the door");
            PlayMakerFSM respawnFSM = gameObject.GetFsmPreprocessed("Wake Up");

            TryReplenishTools replenishTools = new TryReplenishTools();
            replenishTools.Method = ToolItemManager.ReplenishMethod.BenchSilent;
            replenishTools.DoReplenish = true;
            respawnFSM.AddAction("End", replenishTools);

            InvokeMethod inv3 = new InvokeMethod(disableDoor);
            respawnFSM.AddAction("End", inv3);

            InvokeMethod disSelf = new InvokeMethod(disableSelf);
            respawnFSM.AddAction("End", disSelf);
        }

        private void disableDoor()
        {
            SilkenSisters.plugin.wakeupPointInstance.SetActive(false);
            SilkenSisters.plugin.wakeupPointInstance.GetComponent<PlayMakerFSM>().fsm.Reinitialize();
            SceneObjectManager.findChildObject(SilkenSisters.plugin.wakeupPointInstance, "door_wakeInMemory_phantom").GetComponent<PlayMakerFSM>().fsm.SetState("Pause");
            SilkenSisters.Log.LogInfo($"Set door to {SilkenSisters.plugin.wakeupPointInstance.activeSelf}");
        }

        private void disableSelf()
        {
            SilkenSisters.Log.LogInfo("respawn point disabling self");
            gameObject.SetActive(false);
        }

    }
}