using BepInEx;

namespace SilkenSisters
{
    // TODO - adjust the plugin guid as needed
    [BepInAutoPlugin(id: "io.github.silkensisters")]
    public partial class SilkenSistersPlugin : BaseUnityPlugin
    {
        private void Awake()
        {
            // Put your initialization logic here
            Logger.LogInfo($"Plugin {Name} ({Id}) has loaded!");
        }
    }
}