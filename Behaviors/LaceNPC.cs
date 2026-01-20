using GenericVariableExtension;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Silksong.FsmUtil;
using Silksong.UnityHelper.Extensions;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

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
                SilkenSisters.Log.LogMessage($"[LaceNPC.Setup] Spawning lace on the organ bench");
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
                resumePhantom();
                SilkenSisters.Log.LogMessage($"[LaceNPC.Setup] Finished setting up LaceNPC");
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
            SilkenSisters.Log.LogInfo($"[LaceNPC.disableRangeDetection] LaceNPCDetection?:{gameObject.FindChild("Start Range").activeSelf}");
        }

        private void setPosition()
        {
            _npcTransform.SetPosition3D(81.9569f, 106.2943f, 2.7723f);
            SilkenSisters.Log.LogInfo($"[LaceNPC.setPosition] position:{_npcTransform.position}");
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
            SilkenSisters.Log.LogMessage("[LaceNPC.editFSMAnimations] Editing Lace NPC FSM");

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

        }

        private void SkipDialogue()
        {

            _control.AddTransition("Take Control", "SKIP", "Sit Up");
            _control.AddAction("Take Control", new BoolTestDelay { boolVariable = _control.GetBoolVariable("IsMemory"), isTrue = FsmEvent.GetFsmEvent("SKIP"), delay = 0.5f });

            _control.AddTransition("Sit Up", "SKIP", "Lace Ready");
            _control.AddAction("Sit Up", new BoolTest { boolVariable = _control.GetBoolVariable("IsMemory"), isTrue = FsmEvent.GetFsmEvent("SKIP") });
            
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

        private void toggleChallenge()
        {
            SilkenSisters.plugin.challengeDialogInstance.SetActive(!SilkenSisters.plugin.challengeDialogInstance.activeSelf);
            SilkenSisters.Log.LogInfo($"[LaceNPC.toggleChallenge] challenge?:{SilkenSisters.plugin.challengeDialogInstance.activeSelf}");
        }

        private void startConstrainHornet()
        {
            SilkenSisters.hornetConstrain.enabled = true;
            SilkenSisters.Log.LogInfo($"[LaceNPC.startConstrainHornet] constrainHornet?:{SilkenSisters.hornetConstrain.enabled}");
        }

        private void makeFliesLeave()
        {
            SilkenSisters.plugin.silkflies.GetComponent<SilkFlies>().Leave();
            SilkenSisters.Log.LogInfo($"[LaceNPC.startConstrainHornet] constrainHornet?:{SilkenSisters.hornetConstrain.enabled}");
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


}
