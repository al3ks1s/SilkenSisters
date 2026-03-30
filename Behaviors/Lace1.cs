using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Silksong.FsmUtil;
using Silksong.UnityHelper.Extensions;
using System;
using System.Threading.Tasks;
using UnityEngine;

namespace SilkenSisters.Behaviors
{
    internal class Lace1 : MonoBehaviour
    {        // Spawn pos : 78,7832 104,5677 0,004
        // Constraints left: 72,4, right: 96,52, bot: 104
        private PlayMakerFSM _control;
        private HealthManager _healthManager;

        private void Awake()
        {
            Setup();
        }

        private async Task Setup()
        {
            try
            {
                SilkenSisters.Log.LogDebug($"[Lace1.Setup] Started setting up Lace");
                getComponents();
                buffHealth();
                editPositionConstraint();
                rerouteState();
                fixActionsPositions();
                disableTitleCard();
                setLaceFacing();
                DisableMusic();
                //addDamageDelegate();
                PrepareCorpse();
                SilkenSisters.Log.LogDebug($"[Lace1.Setup] Finished setting up Lace");
            }
            catch (Exception e)
            {
                SilkenSisters.Log.LogError($"{e} {e.Message}");
            }
        }

        private void getComponents()
        {
            gameObject.transform.position = new Vector3(78.2832f, 104.5677f, 0.004f);
            SilkenSisters.Log.LogDebug($"[Lace1.getComponents] position:{gameObject.transform.position}");
            _control = gameObject.GetFsmPreprocessed("Control");
            _healthManager = gameObject.GetComponent<HealthManager>();
        }

        private void buffHealth()
        {
            //SceneObjectManager.findChildObject(gameObject, "Pt DashPetal").SetActive(false);
            gameObject.GetComponent<HealthManager>().initHp = 600;
            gameObject.GetComponent<HealthManager>().hp = 600;

            _control.GetIntVariable("Rage HP").value = 300;
        }

        private void editPositionConstraint()
        {
            ConstrainPosition laceBossConstraint = (ConstrainPosition)gameObject.GetComponent(typeof(ConstrainPosition));
            laceBossConstraint.SetXMin(72.4f);
            laceBossConstraint.SetXMax(96.52f);
            laceBossConstraint.SetYMin(104f);
            laceBossConstraint.constrainX = true;
            laceBossConstraint.constrainY = true;

            SilkenSisters.Log.LogDebug($"[Lace1.editPositionConstraint] Constraints: " +
                $"MinX:{laceBossConstraint.xMin}" +
                $"MaxX:{laceBossConstraint.xMax}" +
                $"MinY:{laceBossConstraint.yMin}"
            );
        }

        private void rerouteState()
        {
            _control.ChangeTransition("Encountered?", "MEET", "Refight");
            _control.AddTransition("Dormant", "BATTLE START REFIGHT", "Encountered?");
            _control.AddTransition("Dormant", "BATTLE START FIRST", "Encountered?");
            _control.ChangeTransition("Start Battle", "FINISHED", "Idle");
            //_control.ChangeTransition("Start Battle Wait", "BATTLE START FIRST", "Refight Engarde");
            SilkenSisters.Log.LogDebug($"[Lace1.rerouteState] \n" +
                $"              Encountered:Meet -> {_control.GetTransition("Encountered?", "MEET").ToState}");

        }

        private void fixActionsPositions()
        {
            // Change floor height
            SilkenSisters.Log.LogDebug("Fix floor heights");

            //_control.GetAction<SetPosition>("Counter TeleIn", 4).y = 110f;
            //SilkenSisters.Log.LogDebug($"[Lace1.fixActionsPositions] TeleHeight: {_control.GetAction<SetPosition>("Counter TeleIn", 4).y}");

            _control.GetAction<FloatInRange>("Downstab Land", 1).lowerValue = 73f;
            _control.GetAction<FloatInRange>("Downstab Land", 1).upperValue = 96f;
            SilkenSisters.Log.LogDebug($"[Lace1.fixActionsPositions] Downstab Land Pos: min:{_control.GetAction<FloatInRange>("Downstab Land", 1).lowerValue}, max:{_control.GetAction<FloatInRange>("Downstab Land", 1).upperValue}");

            _control.GetAction<FloatInRange>("Dstab Constrain?", 1).lowerValue = 73f;
            _control.GetAction<FloatInRange>("Dstab Constrain?", 1).upperValue = 96f;
            SilkenSisters.Log.LogDebug($"[Lace1.fixActionsPositions] CrossSlash Pos: min:{_control.GetAction<FloatInRange>("Dstab Constrain?", 1).lowerValue}, max:{_control.GetAction<FloatInRange>("Dstab Constrain?", 1).upperValue}");


            _control.FindFloatVariable("Land Y").Value = 104.5677f;
            _control.FindFloatVariable("Centre X").Value = 84f;
            SilkenSisters.Log.LogDebug($"[Lace1.fixActionsPositions] Float vars: " +
                $"Land Y: {_control.FindFloatVariable("Land Y").Value} " +
                $"Centre X: {_control.FindFloatVariable("Centre X").Value}"
            );
        }

        private void disableTitleCard()
        {
            SilkenSisters.Log.LogDebug("[Lace1.disableTitleCard] Disabling title card");
            _control.DisableAction("Start Battle", 3);

            SilkenSisters.Log.LogDebug($"[Lace1.disableTitleCard] " +
                $"(Start Battle):{_control.GetStateAction("Start Battle", 3).active}");

        }

        private void DisableMusic()
        {
            _control.DisableAction("Start Battle", 1);
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
            SilkenSisters.Log.LogDebug($"[Lace1.setLaceFacing] Facing Action:{_control.GetStateAction("Init", 4).GetType()}");

            _control.DisableAction("Refight", 1);
            
            Tk2dPlayAnimation laceIdle = new Tk2dPlayAnimation();
            laceIdle.animLibName = "";
            laceIdle.clipName = "Idle";
            laceIdle.gameObject = SilkenSisters.plugin.laceBossFSMOwner;
            _control.InsertAction("Dormant", 1, laceIdle);
            SilkenSisters.Log.LogDebug($"[Lace1.setLaceFacing] fsmowner:{laceIdle.gameObject}");

            

        }

        private void addDamageDelegate()
        {
            _healthManager.TookDamage += TransferDamage;
        }

        private void TransferDamage()
        {
            SilkenSisters.Log.LogDebug($"Lace: {_healthManager.hp}");
            SilkenSisters.Log.LogDebug($"Lace: {_healthManager.lastHitInstance.DamageDealt}");
            //SilkenSisters.plugin.phantomBossScene.FindChild("Phantom").GetComponent<HealthManager>().hp -= _healthManager.lastHitInstance.DamageDealt;

        }

        private void PrepareCorpse()
        {

            SilkenSisters.Log.LogDebug("Started setting corpse handler");
            GameObject laceCorpse = gameObject.FindChild("Corpse Lace1(Clone)");
            PlayMakerFSM laceCorpseFSM = FsmUtil.GetFsmPreprocessed(laceCorpse, "Control");
            laceCorpse.GetComponent<ConstrainPosition>().SetXMin(70);

            laceCorpseFSM.DisableAction("Start", 1);
            laceCorpseFSM.DisableAction("Start", 2);
            laceCorpseFSM.DisableAction("Jump Away", 8);

            laceCorpseFSM.AddState("Tele Out");
            laceCorpseFSM.AddAction("Tele Out", new SetIsKinematic2d { isKinematic = true, gameObject = new FsmOwnerDefault { OwnerOption = OwnerDefaultOption.UseOwner } });
            laceCorpseFSM.AddAction(
                "Tele Out", 
                new Tk2dPlayAnimationWithEvents { 
                    clipName = "Tele Out", 
                    gameObject = new FsmOwnerDefault { 
                        OwnerOption = OwnerDefaultOption.UseOwner 
                    }, 
                    animationCompleteEvent = FsmEvent.GetFsmEvent("FINISHED") 
                } 
            );

            laceCorpseFSM.AddState("Tele In");
            laceCorpseFSM.AddAction(
                "Tele In", 
                new FaceObjectV2 { 
                    objectA = new FsmOwnerDefault { OwnerOption = OwnerDefaultOption.UseOwner },
                    objectB = new FsmGameObject { Value = GameObject.Find("Boss Scene/Gates/Battle Gate") },
                    newAnimationClip = "",
                    playNewAnimation = false,
                    spriteFacesRight = true,
                    resetFrame = false,
                    everyFrame = false,
                    pauseBetweenTurns = 0
                } 
            );

            laceCorpseFSM.AddAction(
                "Tele In",
                new PlayRandomAudioClipTable
                {
                    Table = _control.GetAction<PlayRandomAudioClipTable>("Lava Tele Out", 0).Table,
                    SpawnPosition = new FsmVector3 { Value = new Vector3(0, 0, 0) },
                    SpawnPoint = new FsmOwnerDefault { OwnerOption = OwnerDefaultOption.UseOwner },
                    AudioPlayerPrefab = new FsmObject { Value = null },
                }
            );
            laceCorpseFSM.AddAction(
                "Tele In",
                new SetPosition
                {
                    gameObject = new FsmOwnerDefault
                    {
                        OwnerOption = OwnerDefaultOption.UseOwner
                    },
                    vector = new FsmVector3
                    {
                        Value = new Vector3(81.9569f, 106.2943f, 2.7723f)
                    },
                    x = 81.9569f,
                    y = 107.0221f,
                    z = 2.7723f,
                }
            ); 

            /*
            laceCorpseFSM.AddAction(
                "Tele In",
                new SetScale
                {
                    gameObject = new FsmOwnerDefault
                    {
                        OwnerOption = OwnerDefaultOption.UseOwner
                    },
                    vector = new FsmVector3
                    {
                        Value = new Vector3(0.9f,0.9f,0.9f)
                    },
                    x = 0.9f,
                    y = 0.9f,
                    z = 0.9f,
                }
            );
            */

            laceCorpseFSM.AddAction(
                "Tele In",
                new Tk2dPlayAnimationWithEvents
                {
                    clipName = "Tele In",
                    gameObject = new FsmOwnerDefault { OwnerOption = OwnerDefaultOption.UseOwner },
                    animationCompleteEvent = FsmEvent.GetFsmEvent("FINISHED")
                }
            );

            laceCorpseFSM.AddState("Ouchie");
            laceCorpseFSM.AddAction(
                "Ouchie", 
                new Tk2dPlayAnimation
                {
                    clipName = "Death Land Stun",
                    gameObject = new FsmOwnerDefault { OwnerOption = OwnerDefaultOption.UseOwner },
                    animLibName = ""
                }
            );
            
            laceCorpseFSM.AddAction(
                "Ouchie", 
                new Tk2dPlayFrame
                {
                    frame = 5,
                    gameObject = new FsmOwnerDefault { OwnerOption = OwnerDefaultOption.UseOwner },
                }
            );
            
            laceCorpseFSM.AddAction(
                "Ouchie", 
                new Wait
                {
                    finishEvent = FsmEvent.GetFsmEvent("FINISHED"),
                    time = 1f,
                }
            );
            
            laceCorpseFSM.AddState("Man that hurts");
            laceCorpseFSM.AddAction(
                "Man that hurts", 
                new Tk2dPlayAnimation
                {
                    clipName = "Death 1",
                    gameObject = new FsmOwnerDefault { OwnerOption = OwnerDefaultOption.UseOwner },
                    animLibName = ""
                }
            );


            laceCorpseFSM.ChangeTransition("Land", "FINISHED", "Tele Out");
            laceCorpseFSM.AddTransition("Tele Out", "FINISHED", "Tele In");
            laceCorpseFSM.AddTransition("Tele In", "FINISHED", "Ouchie");
            laceCorpseFSM.AddTransition("Ouchie", "FINISHED", "Man that hurts");
            laceCorpseFSM.AddTransition("Man that hurts", "LACE JUMP", "Jump Antic");


            SilkenSisters.Log.LogDebug("Finished setting up corpse handler");
        }

        private void prepareSync()
        {
            if (false)
            {

                SilkenSisters.Log.LogDebug($"[Lace.prepareSync] Adding a Sync state");
                _control.AddState("SilkenSync");

                if (FsmEvent.GetFsmEvent("LACE_SYNC") == null)
                {
                    FsmEvent laceSync = new FsmEvent("LACE_SYNC");
                    FsmEvent.AddFsmEvent(laceSync);
                }

                SilkenSisters.Log.LogDebug($"[Lace.prepareSync] Added a sync event");

                _control.ChangeTransition("Evade Move", "FINISHED", "Idle");
                _control.ChangeTransition("CrossSlash?", "FINISHED", "SilkenSync");

                _control.AddTransition("SilkenSync", "LACE_SYNC", "Distance Check");
            }
        }
    }

    internal class Lace1Scene : MonoBehaviour
    {

        private PlayMakerFSM _control;

        private void Awake()
        {
            Setup();
        }

        private async Task Setup()
        {
            SilkenSisters.Log.LogDebug($"[Lace1Scene.Setup] Started setting Lace scene up");
            getComponents();
            disableSceneObjects();
            moveSceneBounds();
            fixWallRangeAlert();
            SilkenSisters.Log.LogDebug($"[Lace1Scene.Setup] Finished setting Lace scene up");
        }

        private void getComponents()
        {
            _control = gameObject.GetFsmPreprocessed("Control");
        }

        private void disableSceneObjects()
        {
            SilkenSisters.Log.LogDebug($"[Lace1Scene.disableSceneObjects] Disabling unwanted LaceBossScene items");
            gameObject.FindChild("Slam Particles").SetActive(false);
            gameObject.FindChild("Battle Gates").SetActive(false);
            gameObject.FindChild("Silkflies").SetActive(false);
            gameObject.FindChild("Silkflies w/o Sprint").SetActive(false);
        }

        private void moveSceneBounds()
        {
            SilkenSisters.Log.LogDebug($"[Lace1Scene.moveSceneBounds] Moving lace arena objects");
            //SceneObjectManager.findChildObject(gameObject, "Arena L").transform.position = new Vector3(72f, 104f, 0f);
            //SceneObjectManager.findChildObject(gameObject, "Arena R").transform.position = new Vector3(97f, 104f, 0f);
            gameObject.FindChild("Arena Centre").transform.position = new Vector3(84.5f, 104f, 0f);
        }

        private void fixWallRangeAlert()
        {
            GameObject wallRange = gameObject.transform.parent.gameObject.FindChild("Wall Range");
            wallRange.transform.SetPosition3D(84.0349f, 103.67f, 0f);
            SilkenSisters.Log.LogDebug($"[Lace1.fixWallRangeAlert] position:{wallRange.transform.position}");

            BoxCollider2D[] boxes = wallRange.GetComponents<BoxCollider2D>();
            boxes[0].size = new Vector2(5f, 30f);
            boxes[0].offset = new Vector2(-9f, 0.4726f);

            boxes[1].size = new Vector2(5f, 35.1782f);
            boxes[1].offset = new Vector2(10f, 7.1234f);

            SilkenSisters.Log.LogDebug($"[Lace1.fixWallRangeAlert] alertLeft: Size:{boxes[0].size}, Size:{boxes[0].offset}");
            SilkenSisters.Log.LogDebug($"[Lace1.fixWallRangeAlert] alertRight: Size:{boxes[1].size}, Size:{boxes[1].offset}");
        }
    }
}
