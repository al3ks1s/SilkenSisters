using Silksong.AssetHelper;
using Silksong.FsmUtil;
using Silksong.AssetHelper.ManagedAssets;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using UnityEngine;

namespace SilkenSisters.Utils
{

    
    internal class AssetManager
    {

        internal ManagedAssetGroup<GameObject> sceneCache;
        internal ManagedAssetGroup<AudioClip> audioClipCache;
        internal ManagedAssetGroup<GameObject> prefabCache;
        internal ManagedAssetGroup<RandomAudioClipTable> audioClipTableCache;
    
        internal void RequestAssets()
        {

            sceneCache = ManagedAssetGroup<GameObject>.RequestAndCreate(
                new Dictionary<string, ManagedAssetGroup<GameObject>.SceneAssetInfo>()
                {
                    { "laceNPCCache",               new ManagedAssetGroup<GameObject>.SceneAssetInfo("Coral_19", "Encounter Scene Control/Lace Meet/Lace NPC Blasted Bridge" ) },

                    { "lace2BossSceneCache",        new ManagedAssetGroup<GameObject>.SceneAssetInfo("Song_Tower_01", "Boss Scene") },
                    
                    { "lace1BossSceneCache",        new ManagedAssetGroup<GameObject>.SceneAssetInfo("Bone_East_12", "Boss Scene")},
                    { "silkfliesCache",             new ManagedAssetGroup<GameObject>.SceneAssetInfo("Bone_East_12", "Boss Scene/Silkflies")},
                    { "syncedFightSwapLeverCache",  new ManagedAssetGroup<GameObject>.SceneAssetInfo("Bone_East_12", "dock_pipe_trapdoor")},
                    
                    { "challengeDialogCache",       new ManagedAssetGroup<GameObject>.SceneAssetInfo("Cradle_03", "Boss Scene/Intro Sequence")},
                    
                    { "wakeupPointCache",           new ManagedAssetGroup<GameObject>.SceneAssetInfo("Memory_Coral_Tower", "Door Get Up")},
                    { "coralBossSceneCache",        new ManagedAssetGroup<GameObject>.SceneAssetInfo("Memory_Coral_Tower", "Boss Scene")},
                    
                    { "deepMemoryCache",            new ManagedAssetGroup<GameObject>.SceneAssetInfo("Coral_Tower_01", "Memory Group")},
                    
                    { "infoPromptCache",            new ManagedAssetGroup<GameObject>.SceneAssetInfo("Arborium_01", "Inspect Region")},
                },
                null
            );


            audioClipCache = ManagedAssetGroup<AudioClip>.RequestAndCreate(null,
                new Dictionary<string, ManagedAssetGroup<AudioClip>.NonSceneAssetInfo>()
                {
                    {"phantomSing", new ManagedAssetGroup<AudioClip>.NonSceneAssetInfo("sfxstatic_assets_areaorgan", "Assets/Audio/HornetVoices/Phantom/NewTake/Phantom-2-Sing.wav" )},

                    {"laceSpeak",   new ManagedAssetGroup<AudioClip>.NonSceneAssetInfo("sfxstatic_assets_laceboss", "Assets/Audio/Voices/Lace_Silksong/Lace_weak_talk_01.wav")},
                    {"laceSpeak2",  new ManagedAssetGroup<AudioClip>.NonSceneAssetInfo("sfxstatic_assets_laceboss", "Assets/Audio/Voices/Lace_Silksong/Lace_talk_haughty_04.wav")},
                    {"laceScoff",   new ManagedAssetGroup<AudioClip>.NonSceneAssetInfo("sfxstatic_assets_laceboss", "Assets/Audio/Voices/Lace_Silksong/Lace_short_attack_grunt_03.wav")},
                    {"laceWail",    new ManagedAssetGroup<AudioClip>.NonSceneAssetInfo("sfxstatic_assets_laceboss", "Assets/Audio/Voices/Lace_Silksong/Lace_wail_short_02.wav")},
                    {"laceLaugh",   new ManagedAssetGroup<AudioClip>.NonSceneAssetInfo("sfxstatic_assets_laceboss", "Assets/Audio/Voices/Lace_Silksong/Lace_long_laugh_04.wav")},
                    {"laceStance",  new ManagedAssetGroup<AudioClip>.NonSceneAssetInfo("sfxstatic_assets_laceboss", "Assets/Audio/SFX/HornetEnemy/Lace/lace_prepare_stance.wav")},
                    {"laceBackstep",new ManagedAssetGroup<AudioClip>.NonSceneAssetInfo("sfxstatic_assets_laceboss", "Assets/Audio/SFX/HornetEnemy/Lace/lace_back_or_forward_step_2.wav")},
                    {"laceCharge",  new ManagedAssetGroup<AudioClip>.NonSceneAssetInfo("sfxstatic_assets_laceboss", "Assets/Audio/SFX/HornetEnemy/Lace/lace_horizontal_dash.wav")},
                    
                    {"laceWake",    new ManagedAssetGroup<AudioClip>.NonSceneAssetInfo("sfxstatic_assets_shared", "Assets/Audio/SFX/HornetEnemy/Trobbio/trobbio_jump.wav")},
                    {"laceTeleOut", new ManagedAssetGroup<AudioClip>.NonSceneAssetInfo("sfxstatic_assets_areasong", "Assets/Audio/SFX/Enemy/Bosses/Mantis Lords/mantis_lord_misc_jump_2.wav")},
                    {"laceTeleIn",  new ManagedAssetGroup<AudioClip>.NonSceneAssetInfo("sfxstatic_assets_areaclover", "Assets/Audio/SFX/HornetEnemy/grasshopper_slasher_dash_1.wav")},

                    {"hornetSword", new ManagedAssetGroup<AudioClip>.NonSceneAssetInfo("sfxstatic_assets_shared", "Assets/Audio/SFX/sword_5.wav")},
                    {"hornetParry", new ManagedAssetGroup<AudioClip>.NonSceneAssetInfo("sfxstatic_assets_shared", "Assets/Audio/SFX/Enemy/Bosses/Hornet/hornet_parry_prepare.wav")},

                    {"miscRumble",  new ManagedAssetGroup<AudioClip>.NonSceneAssetInfo("sfxstatic_assets_shared", "Assets/Audio/SFX/Props/misc_rumble_impact.wav")},
                }
            );
            
            prefabCache = ManagedAssetGroup<GameObject>.RequestAndCreate(null,
                new Dictionary<string, ManagedAssetGroup<GameObject>.NonSceneAssetInfo>()
                {
                    {"AudioPlayerActor", new ManagedAssetGroup<GameObject>.NonSceneAssetInfo("globalpoolprefabs_assets_all", "Assets/Audio/Audio Player Actor.prefab" )},

                }
            );

            audioClipTableCache = ManagedAssetGroup<RandomAudioClipTable>.RequestAndCreate(null,
                new Dictionary<string, ManagedAssetGroup<RandomAudioClipTable>.NonSceneAssetInfo>()
                {
                    {"LaceWail",        new ManagedAssetGroup<RandomAudioClipTable>.NonSceneAssetInfo("sfxdynamic_assets_laceboss", "Assets/Audio/Voices/Lace_Silksong/Lace_wail_short.asset" )},
                    {"LaceGrunt",       new ManagedAssetGroup<RandomAudioClipTable>.NonSceneAssetInfo("sfxdynamic_assets_laceboss", "Assets/Audio/Voices/Lace_Silksong/lace_grunt.asset" )},
                    {"LaceWeakTalk",    new ManagedAssetGroup<RandomAudioClipTable>.NonSceneAssetInfo("sfxdynamic_assets_laceboss", "Assets/Audio/Voices/Lace_Silksong/Lace_defeated_talk_final.asset" )},
                    {"LaceSpeak",       new ManagedAssetGroup<RandomAudioClipTable>.NonSceneAssetInfo("sfxdynamic_assets_laceboss", "Assets/Audio/Voices/Lace_Silksong/lace_battle_admonish.asset" )},
                    {"LaceAttack",      new ManagedAssetGroup<RandomAudioClipTable>.NonSceneAssetInfo("sfxdynamic_assets_laceboss", "Assets/Audio/Voices/Lace_Silksong/lace_attack_call.asset" )},
                    {"LaceCollapse",    new ManagedAssetGroup<RandomAudioClipTable>.NonSceneAssetInfo("sfxdynamic_assets_laceboss", "Assets/Audio/Voices/Lace_Silksong/Lace_after_battle_collapse.asset" )},
                    
                    {"HornetSpeak",     new ManagedAssetGroup<RandomAudioClipTable>.NonSceneAssetInfo("herodynamic_assets_all", "Assets/Audio/Voices/Hornet_Silksong/Hornet Voice Action Grunt.asset" )},

                }
            );
        }

        internal IEnumerator CacheObjects()
        {

            yield return sceneCache.Load();
            yield return audioClipCache.Load();
            yield return prefabCache.Load();
            yield return audioClipTableCache.Load();

            //assetManager.gameObjectCache.InstantiateAsset<GameObject>("

            GameObject bossScene = sceneCache.InstantiateAsset<GameObject>("coralBossSceneCache");
            PlayMakerFSM control = bossScene.GetFsmPreprocessed("Control");
            SilkenSisters.instance.ExitMemoryCache = control.GetState("Exit Memory");
            GameObject.Destroy(bossScene);
        }

        internal void ClearCache()
        {
            sceneCache.Unload();
            audioClipCache.Unload();
            prefabCache.Unload();
        }


    }
}
