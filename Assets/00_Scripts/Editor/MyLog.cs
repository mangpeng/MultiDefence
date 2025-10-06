using UnityEngine;

public static class LogToggle
{
    public static bool EnableLog = true;

    [UnityEditor.MenuItem("Tools/Toggle Logs")]
    static void ToggleLogs()
    {
        EnableLog = !EnableLog;
        Debug.Log("Logs " + (EnableLog ? "Enabled" : "Disabled"));
    }
}
