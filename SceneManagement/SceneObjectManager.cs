using UnityEngine;
using UnityEngine.SceneManagement;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Silksong.FsmUtil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SilkenSisters.SceneManagement
{
    internal static class SceneObjectManager
    {
        public static string sceneFolder = Path.Combine(Application.streamingAssetsPath, "aa", "StandaloneWindows64", "scenes_scenes_scenes");

        public static async Task<GameObject> loadObjectFromScene(string sceneName, string objectToRetrieve)
        {
            GameObject go_copy = null;

            SilkenSisters.Log.LogDebug($"Current scene {SceneManager.GetActiveScene().name}");
            SilkenSisters.Log.LogDebug($"Loading {sceneName} scene");

            AssetBundle bundle = AssetBundle.LoadFromFile(Path.Combine(sceneFolder, $"{sceneName}.bundle".ToLower()));
            await SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

            Scene scene = SceneManager.GetSceneByName(sceneName);
            SilkenSisters.Log.LogDebug($"Scene {scene.name} successfully loaded");

            GameObject go = SceneObjectManager.findObjectInScene(scene, objectToRetrieve);
            go_copy = GameObject.Instantiate(go);
            GameObject.DontDestroyOnLoad(go_copy);
            
            SilkenSisters.Log.LogDebug($"Unloading '{scene.name}' scene");
            await SceneManager.UnloadSceneAsync(scene.name);
            SilkenSisters.Log.LogDebug($"Unloading bundle '{bundle.name}'");
            await bundle.UnloadAsync(false);

            go_copy.SetActive(false);

            return go_copy;
        }

        public static GameObject findObjectInScene(Scene scene, string objectToRetrieve)
        {
            int objectIndex = 0;
            string[] objectHierarchy = objectToRetrieve.Split("/");

            SilkenSisters.Log.LogDebug($"Searching scene {scene.name} for object '{objectToRetrieve}'");
            SilkenSisters.Log.LogDebug($"Scene {scene.name} has {scene.GetRootGameObjects().Length} objects");

            GameObject cur_obj = scene.GetRootGameObjects().First<GameObject>(obj => obj.name == objectHierarchy[objectIndex]);
            objectIndex += 1;

            while (objectIndex < objectHierarchy.Length)
            {
                SilkenSisters.Log.LogDebug($"Current child object searched for: '{objectHierarchy[objectIndex]}'");
                cur_obj = cur_obj.transform.GetComponentsInChildren<Transform>(true).First(tf => tf.name == objectHierarchy[objectIndex]).gameObject;
                objectIndex += 1;
            }

            SilkenSisters.Log.LogDebug($"Found object {cur_obj}");

            return cur_obj;  
        }

        public static GameObject findObjectInCurrentScene(string objectToRetrieve)
        {
            return findObjectInScene(SceneManager.GetActiveScene(), objectToRetrieve);
        }

        public static GameObject findChildObject(GameObject obj, string childObj)
        {
            int objectIndex = 0;
            string[] objectHierarchy = childObj.Split("/");

            GameObject cur_obj = obj;

            while (objectIndex < objectHierarchy.Length)
            {
                SilkenSisters.Log.LogDebug($"Current child object searched for: '{objectHierarchy[objectIndex]}'");
                cur_obj = cur_obj.transform.GetComponentsInChildren<Transform>(true).First(tf => tf.name == objectHierarchy[objectIndex]).gameObject;
                objectIndex += 1;
            }

            SilkenSisters.Log.LogDebug($"Found object {cur_obj}");

            return cur_obj;
        }
    }
}
