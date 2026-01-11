using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Silksong.FsmUtil;
using Silksong.UnityHelper.Extensions;
using System;
using System.Threading.Tasks;
using UnityEngine;

namespace SilkenSisters.Behaviors
{

    internal class Lace2 : MonoBehaviour
    {

        // Spawn pos : 78,7832 104,5677 0,004
        // Constraints left: 72,4, right: 96,52, bot: 104
        private PlayMakerFSM _control;

        private void Awake()
        {
            Setup();
        }

        private async Task Setup()
        {
            try { 
                SilkenSisters.Log.LogMessage($"[Lace2.Setup] Started setting up Lace");
                getComponents();
                disableParticleEffects();
                editPositionConstraint();
                rerouteState();
                fixActionsPositions();
                disableTitleCard();
                fixWallRangeAlert(); 
                disableLaceMusic();
                setLaceFacing();
                prepareSync();
                SilkenSisters.Log.LogMessage($"[Lace2.Setup] Finished setting up Lace");
            } catch (Exception e) {
                SilkenSisters.Log.LogError($"{e} {e.Message}");
            }
        }

        private void getComponents()
        {
            gameObject.transform.position = new Vector3(78.2832f, 104.5677f, 0.004f);
            SilkenSisters.Log.LogInfo($"[Lace2.getComponents] position:{gameObject.transform.position}");
            _control = gameObject.GetFsmPreprocessed("Control");
        }

        private void disableParticleEffects()
        {
            gameObject.FindChild("Pt DashPetal").SetActive(false);
            gameObject.FindChild("Pt SkidPetal").SetActive(false);
            gameObject.FindChild("Pt RisingPetal").SetActive(false);
            gameObject.FindChild("Pt MovePetal").SetActive(false);

        }

        private void editPositionConstraint()
        {
            ConstrainPosition laceBossConstraint = (ConstrainPosition)gameObject.GetComponent(typeof(ConstrainPosition));
            laceBossConstraint.SetXMin(72.4f);
            laceBossConstraint.SetXMax(96.52f);
            laceBossConstraint.SetYMin(104f);
            laceBossConstraint.constrainX = true;
            laceBossConstraint.constrainY = true;

            SilkenSisters.Log.LogInfo($"[Lace2.editPositionConstraint] Constraints: " +
                $"MinX:{laceBossConstraint.xMin}" +
                $"MaxX:{laceBossConstraint.xMax}" +
                $"MinY:{laceBossConstraint.yMin}"
            );
        }

        private void rerouteState()
        {
            _control.ChangeTransition("Init", "REFIGHT", "Start Battle Wait");
            _control.ChangeTransition("Start Battle Wait", "BATTLE START REFIGHT", "Refight Engarde");
            _control.ChangeTransition("Start Battle Wait", "BATTLE START FIRST", "Refight Engarde");

            // Lengthen the engarde state
            Wait wait_engarde = new Wait();
            wait_engarde.time = 2f;
            //_control.AddAction("Refight Engarde", wait_engarde);

            SilkenSisters.Log.LogInfo($"[Lace2.rerouteState] \n" +
                $"              Init:REFIGHT -> {_control.GetTransition("Init", "REFIGHT").ToState} \n" +
                $"              Start Battle Wait:BATTLE START REFIGHT -> {_control.GetTransition("Start Battle Wait", "BATTLE START REFIGHT").ToState}\n" +
                $"              Start Battle Wait:BATTLE START FIRST -> {_control.GetTransition("Start Battle Wait", "BATTLE START FIRST").ToState}");

        }

        private void fixActionsPositions()
        {
            // Change floor height
            SilkenSisters.Log.LogMessage("Fix floor heights");
            _control.GetAction<SetPosition2d>("ComboSlash 1", 0).y = 104.5677f;
            _control.GetAction<SetPosition2d>("Charge Antic", 2).y = 104.5677f;
            _control.GetAction<SetPosition2d>("Counter Antic", 1).y = 104.5677f;

            SilkenSisters.Log.LogInfo($"[Lace2.fixActionsPositions] Floor heights:\n" +
                $"              ComboSlash 1: {_control.GetAction<SetPosition2d>("ComboSlash 1", 0).y}\n" +
                $"              Charge Antic: {_control.GetAction<SetPosition2d>("Charge Antic", 2).y}\n" +
                $"              Counter Antic {_control.GetAction<SetPosition2d>("Counter Antic", 1).y}"
            );

            _control.GetAction<SetPosition>("Counter TeleIn", 4).y = 110f;
            SilkenSisters.Log.LogInfo($"[Lace2.fixActionsPositions] TeleHeight: {_control.GetAction<SetPosition>("Counter TeleIn", 4).y}");

            FloatClamp clamp_pos = new FloatClamp();
            clamp_pos.floatVariable = _control.FindFloatVariable("Tele X");
            clamp_pos.maxValue = 96f;
            clamp_pos.minValue = 73f;

            _control.InsertAction("Counter TeleIn", clamp_pos, 4);
            SilkenSisters.Log.LogInfo($"[Lace2.fixActionsPositions] TeleXClamp: min:{_control.GetAction<FloatClamp>("Counter TeleIn", 4).minValue}, max:{_control.GetAction<FloatClamp>("Counter TeleIn", 4).maxValue}");


            _control.FindFloatVariable("Tele Out Floor").Value = 103f;
            _control.GetAction<FloatClamp>("Tele In", 6).minValue = 73f;
            _control.GetAction<FloatClamp>("Tele In", 6).maxValue = 96f;
            _control.GetAction<SetPosition2d>("Tele In", 7).y = 104.5677f;

            // -----
            _control.GetAction<FloatClamp>("Set CrossSlash Pos", 1).minValue = 73f;
            _control.GetAction<FloatClamp>("Set CrossSlash Pos", 1).maxValue = 96f;
            SilkenSisters.Log.LogInfo($"[Lace2.fixActionsPositions] CrossSlash Pos: min:{_control.GetAction<FloatClamp>("Set CrossSlash Pos", 1).minValue}, max:{_control.GetAction<FloatClamp>("Set CrossSlash Pos", 1).maxValue}");

            _control.FindFloatVariable("Land Y").Value = 104.5677f;
            _control.FindFloatVariable("Arena Plat Bot Y").Value = 102f;
            _control.FindFloatVariable("Centre X").Value = 84f;
            SilkenSisters.Log.LogInfo($"[Lace2.fixActionsPositions] Float vars: " +
                $"Land Y: {_control.FindFloatVariable("Land Y").Value} " +
                $"Arena Plat Bot Y: {_control.FindFloatVariable("Arena Plat Bot Y").Value} " +
                $"Centre X: {_control.FindFloatVariable("Centre X").Value}"
            );
            

            // -----
            _control.GetAction<CheckXPosition>("Force R?", 2).compareTo = 73f;
            _control.GetAction<CheckXPosition>("Force L?", 1).compareTo = 96f;
            SilkenSisters.Log.LogInfo($"[Lace2.fixActionsPositions] Lace Dstab bounds: Left:{_control.GetAction<CheckXPosition>("Force R?", 2).compareTo}, Right:{_control.GetAction<CheckXPosition>("Force L?", 1).compareTo}");


            // ----- Bombs, unused attack for now
            _control.FindFloatVariable("Bomb Max X").Value = 96f;
            _control.FindFloatVariable("Bomb Min X").Value = 72f;
            _control.FindFloatVariable("Bomb Max Y").Value = 115f;
            _control.FindFloatVariable("Bomb Min Y").Value = 105f;

        }

        private void disableTitleCard()
        {
            SilkenSisters.Log.LogMessage("[Lace2.disableTitleCard] Disabling title card");
            _control.DisableAction("Start Battle Refight", 4);
            _control.DisableAction("Start Battle", 4);

            SilkenSisters.Log.LogInfo($"[Lace2.disableTitleCard] " +
                $"(Start Battle Refight):{_control.GetStateAction("Start Battle Refight", 4).active}, " +
                $"(Start Battle):{_control.GetStateAction("Start Battle", 4).active}");

        }

        private void disableLaceMusic()
        {
            _control.DisableAction("Start Battle Refight", 1);
            _control.DisableAction("Start Battle Refight", 2);
        }

        private void fixWallRangeAlert()
        {
            GameObject wallRange = gameObject.transform.parent.gameObject.FindChild("Wall Range");
            wallRange.transform.SetPosition3D(84.0349f, 103.67f, 0f);
            SilkenSisters.Log.LogInfo($"[Lace2.fixWallRangeAlert] position:{wallRange.transform.position}");

            BoxCollider2D[] boxes = wallRange.GetComponents<BoxCollider2D>();
            boxes[0].size = new Vector2(5f, 30f);
            boxes[0].offset = new Vector2(-9f, 0.4726f);

            boxes[1].size = new Vector2(5f, 35.1782f);
            boxes[1].offset = new Vector2(10f, 7.1234f);


            SilkenSisters.Log.LogInfo($"[Lace2.fixWallRangeAlert] alertLeft: Size:{boxes[0].size}, Size:{boxes[0].offset}");
            SilkenSisters.Log.LogInfo($"[Lace2.fixWallRangeAlert] alertRight: Size:{boxes[1].size}, Size:{boxes[1].offset}");
        }

        private void setLaceFacing()
        {
            FaceObjectV2 faceRight = new FaceObjectV2();
            faceRight.spriteFacesRight = true;
            faceRight.playNewAnimation = false;
            faceRight.newAnimationClip = "";
            faceRight.resetFrame = false;
            faceRight.everyFrame = false;
            faceRight.pauseBetweenTurns = 0.5f;
            faceRight.objectA = SilkenSisters.plugin.laceBossFSMOwner;
            faceRight.objectB = SilkenSisters.hornet;

            _control.InsertAction("Init", faceRight, 4);
            _control.DisableAction("Refight Engarde", 0);

            SilkenSisters.Log.LogInfo($"[Lace2.setLaceFacing] Facing Action:{_control.GetStateAction("Init", 4).GetType()}");
            SilkenSisters.Log.LogInfo($"[Lace2.Refight Engarde] Base facing active?:{_control.GetStateAction("Refight Engarde", 0).active}");
        }

        private void prepareSync()
        {
            if (SilkenSisters.syncedFight.Value && SilkenSisters.debugBuild) { 

                SilkenSisters.Log.LogMessage($"[Lace.prepareSync] Adding a Sync state");
                _control.AddState("SilkenSync");

                if (FsmEvent.GetFsmEvent("LACE_SYNC") == null)
                {
                    FsmEvent laceSync = new FsmEvent("LACE_SYNC");
                    FsmEvent.AddFsmEvent(laceSync);
                }

                SilkenSisters.Log.LogMessage($"[Lace.prepareSync] Added a sync event");

                _control.ChangeTransition("Evade Move", "FINISHED", "Idle");
                _control.ChangeTransition("CrossSlash?", "FINISHED", "SilkenSync");

                _control.AddTransition("SilkenSync", "LACE_SYNC", "Distance Check");
            }
        }
    }

    internal class Lace2Scene : MonoBehaviour
    {

        private PlayMakerFSM _control;

        private void Awake()
        {
            Setup();
        }

        private async Task Setup()
        {
            SilkenSisters.Log.LogMessage($"[Lace2Scene.Setup] Started setting Lace scene up");
            getComponents();
            disableSceneObjects();
            moveSceneBounds();
            SilkenSisters.Log.LogMessage($"[Lace2Scene.Setup] Finished setting Lace scene up");
        }

        private void getComponents()
        {
            _control = gameObject.GetFsmPreprocessed("Control");
        }

        private void disableSceneObjects()
        {
            SilkenSisters.Log.LogMessage($"[Lace2Scene.disableSceneObjects] Disabling unwanted LaceBossScene items");
            gameObject.FindChild("Flower Effect Hornet").SetActive(false);
            gameObject.FindChild("steam hazard").SetActive(false);
            gameObject.FindChild("Silk Heart Memory Return").SetActive(false);
        }

        private void moveSceneBounds()
        {
            SilkenSisters.Log.LogMessage($"[Lace2Scene.moveSceneBounds] Moving lace arena objects");
            gameObject.FindChild("Arena L").transform.position = new Vector3(72f, 104f, 0f);
            gameObject.FindChild("Arena R").transform.position = new Vector3(97f, 104f, 0f);
            gameObject.FindChild("Centre").transform.position = new Vector3(84.5f, 104f, 0f);
        }
    }
}
