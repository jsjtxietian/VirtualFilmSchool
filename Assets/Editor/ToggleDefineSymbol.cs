using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ToggleDefineSymbol : Editor
{
    public static readonly string development = "DEVELOPMENT";


    [MenuItem("Symbol/Set DEV")]
    public static void SetDEV()
    {
        string symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(
            BuildTargetGroup.Standalone);

        if (!symbols.Contains(development))
        {
            PlayerSettings.SetScriptingDefineSymbolsForGroup(
                BuildTargetGroup.Standalone, symbols + ";" + development);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(
                BuildTargetGroup.Android, symbols + ";" + development);
        }
    }


    [MenuItem("Symbol/UnSet DEV")]
    public static void UnsetDEV()
    {
        string symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(
            BuildTargetGroup.Standalone);

        if (symbols.Contains(development))
        {
            symbols = symbols.Replace(";" + development, "");
            PlayerSettings.SetScriptingDefineSymbolsForGroup(
                BuildTargetGroup.Standalone, symbols);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(
                BuildTargetGroup.Android, symbols);
        }
    }
}