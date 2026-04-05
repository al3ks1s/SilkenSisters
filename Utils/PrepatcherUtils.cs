using System;
using System.Collections.Generic;
using System.Text;
using PrepatcherPlugin;

namespace SilkenSisters.Utils
{
    internal class PrepatcherUtils
    {

        internal static bool SilkenSisterMonitor(PlayerData pd, string fieldName, bool current)
        {
            if (fieldName == "defeatedPhantom" || fieldName == "blackThreadWorld")
            {
                //SilkenSisters.Log.LogMessage($"Setting bool {fieldName} from {current} to {false}");
                return false;
            }

            return current;
        }

    }
}
