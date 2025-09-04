using System;
using UnityEditor;
using UnityEditor.TerrainTools;
using UnityEditorInternal;
using UnityEngine;


[CustomEditor(typeof(Setting))]
public class SettingEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        var rarityPercentArray = serializedObject.FindProperty("m_rarity_percent");
        foreach (Rarity rarity in System.Enum.GetValues(typeof(Rarity)))
        {
            int index = (int)rarity;
            SerializedProperty element = rarityPercentArray.GetArrayElementAtIndex(index);
            Color rarityColor = UtilManager.GetColorByRarity(rarity);
            GUIStyle elementStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = rarityColor }
            };

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(rarity.ToString(), elementStyle, GUILayout.Width(100));
            EditorGUILayout.PropertyField(element, GUIContent.none);
            EditorGUILayout.EndHorizontal();
        }

        var setting = (Setting)target;

        float total = 0;
        foreach (var item in setting.m_rarity_percent)
        {
            total += item;
        }

        Color textColor = (Mathf.Approximately(total, 100f)) ? Color.green : Color.red;
        GUIStyle style = new GUIStyle(EditorStyles.label)
        {
            normal = {textColor = textColor}
        };
        EditorGUILayout.LabelField($"Total: {total}", style);

        serializedObject.ApplyModifiedProperties();
    }
}
