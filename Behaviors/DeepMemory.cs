using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Silksong.FsmUtil;
using Silksong.UnityHelper.Extensions;
using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

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
            try
            {
                SilkenSisters.Log.LogMessage($"[DeepMemory.Setup] Started setting deep memory up");
                getComponents();
                setPosition();
                disableCrustKingObjects();
                editFSMTransition();
                editPlayerData();
                bypassToolPickup();
                manageTransitionGates();
                SilkenSisters.Log.LogMessage($"[DeepMemory.Setup] Done");
            }
            catch (Exception e)
            {
                SilkenSisters.Log.LogError($"{e} {e.Message}");
            }
        }

        private void getComponents()
        {
            _control = gameObject.GetFsmPreprocessed("To Memory");
            _before = gameObject.FindChild("before");
        }

        private void setPosition()
        {
            gameObject.transform.position = new Vector3(59.249f, 56.7457f, -3.1141f);
            _before.FindChild("Deep_Memory_appear/threads").SetActive(false);
            _before.FindChild("thread_memory").transform.SetLocalPosition2D(1.768f, -3.143f);
            SilkenSisters.Log.LogInfo($"[DeepMemory.setPosition] position:{gameObject.transform.position}");
        }

        private void disableCrustKingObjects()
        {
            SilkenSisters.Log.LogMessage($"[DeepMemory.disableCrustKingObjects] Finding and deleting coral king sprite");
            GameObject.Destroy(_before.FindChild("CK_ground_hit0004").gameObject);
        }

        private void editFSMTransition()
        {
            _control.GetAction<BeginSceneTransition>("Transition Scene", 4).sceneName = "Organ_01";
            _control.GetAction<BeginSceneTransition>("Transition Scene", 4).entryGateName = "door_wakeInMemory_phantom";

            SilkenSisters.Log.LogInfo($"[DeepMemory.editFSMTransition] " +
                $"Scene:{_control.GetAction<BeginSceneTransition>("Transition Scene", 4).sceneName}, " +
                $"Gate:{_control.GetAction<BeginSceneTransition>("Transition Scene", 4).entryGateName}");
        }

        private void editPlayerData()
        {
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
            PlayMakerFSM pickupFSM = _before.GetFsmPreprocessed("activate memory on tool pickup");
            pickupFSM.GetTransition("State 1", "PICKED UP").fsmEvent = FsmEvent.GetFsmEvent("FINISHED");
        }

        private void manageTransitionGates()
        {

            InvokeMethod resp = new InvokeMethod(disableRespawn);
            _control.AddAction("Transition Scene", resp);

            InvokeMethod door = new InvokeMethod(enableDoor);
            _control.InsertAction("Transition Scene", door, 0);

        }

        private void enableDoor()
        {
            SilkenSisters.plugin.wakeupPointInstance.SetActive(true);
            SilkenSisters.Log.LogInfo($"[DeepMemory.enableDoor] Door active?:{SilkenSisters.plugin.wakeupPointInstance.activeSelf}");
        }
        private void disableRespawn()
        {
            SilkenSisters.plugin.respawnPointInstance.SetActive(false);
            SilkenSisters.plugin.respawnPointInstance.GetComponent<PlayMakerFSM>().fsm.SetState("Pause");
            SilkenSisters.Log.LogInfo($"[DeepMemory.disableRespawn] Respawn active?:{SilkenSisters.plugin.respawnPointInstance.activeSelf}, Current state:{SilkenSisters.plugin.respawnPointInstance.GetComponent<PlayMakerFSM>().ActiveStateName}");
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
            try
            {
                SilkenSisters.Log.LogMessage($"[WakeUpMemory.Setup] Started setting wakeup transition up");
                getComponents();
                setName();
                setPosition();
                editFSM();
                SilkenSisters.Log.LogMessage($"[WakeUpMemory.Setup] Finished");
            }
            catch (Exception e)
            {
                SilkenSisters.Log.LogError($"{e} {e.Message}");
            }
        }

        private void getComponents()
        {
            _wakeTransitionGate = gameObject.FindChild("door_wakeInMemory");
            _control = _wakeTransitionGate.GetFsmPreprocessed("Wake Up");
        }

        private void setName()
        {
            _wakeTransitionGate.name = "door_wakeInMemory_phantom";
            SilkenSisters.Log.LogInfo($"[WakeUpMemory.setName] gateName:{_wakeTransitionGate.name}");
        }

        private void setPosition()
        {
            //gameObject.transform.position = new Vector3(59.249f, 56.7457f, 0f);
            gameObject.transform.position = new Vector3(115.4518f, 104.5621f, 0f);
            SilkenSisters.Log.LogInfo($"[WakeUpMemory.setPosition] gatePosition:{gameObject.transform.position}");
        }

        private void editFSM()
        {
            SilkenSisters.Log.LogInfo("[WakeUpMemory.editFSM] Editing the door FSM");

            InvokeMethod inv2 = new InvokeMethod(enableRespawn);
            _control.AddAction("Take Control", inv2);

            InvokeMethod inv3 = new InvokeMethod(enableIsMemory);
            _control.AddAction("Take Control", inv3);

            InvokeMethod inv = new InvokeMethod(SilkenSisters.plugin.setupMemoryFight);
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
            GameManager._instance.ForceCurrentSceneIsMemory(true);
            SilkenSisters.Log.LogInfo($"[WakeUpMemory.enableIsMemory] Is Memory? {GameManager._instance.IsMemoryScene()} {GameManager._instance.forceCurrentSceneMemory}");
        }

        private void enableRespawn()
        {
            SilkenSisters.plugin.respawnPointInstance.SetActive(true);
            SilkenSisters.Log.LogInfo($"[WakeUpMemory.enableRespawn] respawnObject active:{SilkenSisters.plugin.respawnPointInstance.activeSelf}");
        }

        private void disableSelf()
        {
            gameObject.SetActive(false);
        }

        private void closeOffOrgan()
        {
            GameObject gildedDoor = SceneManager.GetActiveScene().FindGameObject("Boss Scene/Gates/Battle Gate (1)");
            
            GameObject.Instantiate(gildedDoor).transform.SetPosition3D(45.4916f, 71.6012f, 0.003f);
            GameObject.Instantiate(gildedDoor).transform.SetPosition3D(67.0862f, 8.5155f, 0.003f);
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
           try
           {
                SilkenSisters.Log.LogMessage($"[WakeUpRespawn.Setup] Started setting WakeUpRespawn up");
                getComponents();
                setName();
                setPosition();
                editFSM();
                SilkenSisters.Log.LogMessage($"[WakeUpRespawn.Setup] Finished");
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

        private void setName()
        {
            gameObject.name = "door_wakeOnGround_phantom";
            SilkenSisters.Log.LogInfo($"[WakeUpRespawn.setName] gateName:{gameObject.name}");
        }
        private void setPosition()
        {
            gameObject.transform.position = new Vector3(59.249f, 56.7457f, 0f);
            SilkenSisters.Log.LogInfo($"[WakeUpRespawn.setPosition] gatePosition:{gameObject.transform.position}");
        }

        private void editFSM()
        {
            PlayMakerFSM respawnFSM = gameObject.GetFsmPreprocessed("Wake Up");
            respawnFSM.DisableAction("Save?", 1);

            InvokeMethod replenishTool = new InvokeMethod(replenishTools);
            // respawnFSM.AddAction("End", replenishTool);

            InvokeMethod inv3 = new InvokeMethod(disableDoor);
            respawnFSM.AddAction("End", inv3);

            InvokeMethod disSelf = new InvokeMethod(disableSelf);
            respawnFSM.AddAction("End", disSelf);
        }

        private void disableDoor()
        {
            SilkenSisters.Log.LogInfo("[WakeUpRespawn.disableDoor] Trying to disable door");
            SilkenSisters.plugin.wakeupPointInstance.SetActive(false);
            SilkenSisters.plugin.wakeupPointInstance.GetComponent<PlayMakerFSM>().fsm.Reinitialize();
            SilkenSisters.plugin.wakeupPointInstance.FindChild("door_wakeInMemory_phantom").GetComponent<PlayMakerFSM>().fsm.SetState("Pause");
            SilkenSisters.Log.LogInfo($"[WakeUpRespawn.disableDoor] Door {SilkenSisters.plugin.wakeupPointInstance.name} enabled?:{SilkenSisters.plugin.wakeupPointInstance.activeSelf}");
        }

        private void disableSelf()
        {
            gameObject.SetActive(false);
            SilkenSisters.Log.LogInfo($"[WakeUpRespawn.disableSelf] {gameObject.activeSelf}");
        }

        private void replenishTools()
        {
            SilkenSisters.Log.LogInfo("Replenishing tools");
            foreach (ToolItem tool in ToolItemManager.GetCurrentEquippedTools())
            {
                if (tool.IsAttackType())
                {
                    SilkenSisters.Log.LogWarning($"{tool.GetName()}");
                    float outCost;
                    int outReserve;
                    tool.TryReplenishSingle(true, 0, out outCost, out outReserve);
                    SilkenSisters.Log.LogWarning($"{outCost} {outReserve}");
                }
            }
        }
    }
}