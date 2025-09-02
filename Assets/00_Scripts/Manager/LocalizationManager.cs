using UnityEditor.Localization.Editor;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.U2D;

public class LocalizationManager
{
    public static string GetUIText(string stringKey)
    {        
        return GetText("UI", stringKey);
    }

    public static string GetHeroText(string stringKey)
    {
        return GetText("Hero", stringKey);
    }

    public static string GetText(string tableKey, string stringKey)
    {
        var curLocale = LocalizationSettings.SelectedLocale;
        return LocalizationSettings.StringDatabase.GetLocalizedString(tableKey, stringKey, curLocale);
    }

    public static LocalizedString GetUILocalString(string stringKey)
    {
        return new LocalizedString { TableReference = "UI", TableEntryReference = stringKey };
    }

    public static LocalizedString GetHeroLocalString(string stringKey)
    {
        return new LocalizedString { TableReference = "Hero", TableEntryReference = stringKey }; 
    }


}
