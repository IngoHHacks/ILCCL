using System.Globalization;

namespace ILCCL.Patches;

[HarmonyPatch]
public class LogPatch
{
    /*
     * Patch:
     * Enables the game's unity logger if the user has enabled it in the config.
     */
    [HarmonyPatch(typeof(UnmappedGlobals), nameof(UnmappedGlobals.nv))]
    [HarmonyPostfix]
    public static void Globals_nv()
    {
        // Enable the game's unity logger if the user has enabled it in the config.
        Debug.unityLogger.logEnabled = Plugin.EnableGameUnityLog.Value;
        Debug.unityLogger.filterLogType = Plugin.GameUnityLogLevel.Value.ToLower() == "error" ? LogType.Error :
            Plugin.GameUnityLogLevel.Value.ToLower() == "warning" ? LogType.Warning : LogType.Log;

        // Enable printing stack traces.
        Application.SetStackTraceLogType(LogType.Exception, StackTraceLogType.Full);

        // Set the culture to en-US to enforce decimal points instead of commas.
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("en-US");
    }
}