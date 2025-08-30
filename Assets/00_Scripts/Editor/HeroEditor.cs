using System;
using UnityEditor;
using UnityEditor.TerrainTools;
using UnityEditorInternal;
using UnityEngine;


[CustomEditor(typeof(HeroStat))]
public class HeroEditor : Editor
{
    private ReorderableList reorderableDebuffList;
    private void OnEnable()
    {
        SerializedProperty debuffTypeProperty = serializedObject.FindProperty("debuffs");
        reorderableDebuffList = new ReorderableList(serializedObject, debuffTypeProperty, true, true, true, true);

        reorderableDebuffList.drawHeaderCallback = (rect) =>
        {
            EditorGUI.LabelField(rect, "Hero Debuffs");
        };

        reorderableDebuffList.elementHeightCallback = (index) =>
        {
            SerializedProperty element = debuffTypeProperty.GetArrayElementAtIndex(index);
            SerializedProperty paramsProp = element.FindPropertyRelative("values");

            var baseHeight = EditorGUIUtility.singleLineHeight + 6.0f;
            var paramHeight = paramsProp.arraySize * (EditorGUIUtility.singleLineHeight + 4.0f);

            return baseHeight + paramHeight + 10.0f;
        };

        reorderableDebuffList.drawElementCallback = (rect, index, inActive, isFocused) =>
        {
            SerializedProperty element = debuffTypeProperty.GetArrayElementAtIndex(index);
            rect.y += 2;

            SerializedProperty debuffTypeProp = element.FindPropertyRelative("type");
            EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), debuffTypeProp, new GUIContent("Debuff Type"));

            Debuff type = (Debuff)debuffTypeProp.enumValueIndex;
            SerializedProperty paramProp = element.FindPropertyRelative("values");

            rect.y += EditorGUIUtility.singleLineHeight + 4.0f;
            switch(type)
            {
                case Debuff.Slow:
                    {
                        paramProp.arraySize = 3;

                        DrawValuesField(paramProp, rect, 0, "Slow Chance(0.0~1.0)"); rect.y += EditorGUIUtility.singleLineHeight + 4;
                        DrawValuesField(paramProp, rect, 1, "Slow Duration(sec)"); rect.y += EditorGUIUtility.singleLineHeight + 4;
                        DrawValuesField(paramProp, rect, 2, "Slow Amount"); 

                        break;
                    }
                case Debuff.Stun:
                    {
                        paramProp.arraySize = 2;

                        DrawValuesField(paramProp, rect, 0, "Stun Chance(0.0~1.0)"); rect.y += EditorGUIUtility.singleLineHeight + 4;
                        DrawValuesField(paramProp, rect, 1, "Stun Duration(sec)");

                        break;
                    }
            }
        };

        reorderableDebuffList.onAddCallback = (list) =>
        {
            int index = list.serializedProperty.arraySize;
            list.serializedProperty.InsertArrayElementAtIndex(index); // add new element

            // set default values
            SerializedProperty newElement = list.serializedProperty.GetArrayElementAtIndex(index);
            newElement.FindPropertyRelative("type").enumValueIndex = (int)Debuff.Slow;
            newElement.FindPropertyRelative("values").arraySize = 0;

        };

        reorderableDebuffList.onRemoveCallback = (list) =>
        {
            if(EditorUtility.DisplayDialog("Remove debuff", "Are you sure to remove this element?", "YES", "NO"))
            {
                ReorderableList.defaultBehaviours.DoRemoveButton(list);
            }
        };
    }

    private void DrawValuesField(SerializedProperty prop, Rect rect, int index, string name)
    {
        EditorGUI.LabelField(new Rect(rect.x, rect.y, 150, EditorGUIUtility.singleLineHeight), name);

        prop.GetArrayElementAtIndex(index).floatValue =
        EditorGUI.FloatField(
            new Rect(rect.x + 130.0f, rect.y, 150, EditorGUIUtility.singleLineHeight),
            prop.GetArrayElementAtIndex(index).floatValue);
    }

    public override void OnInspectorGUI()
    {
        // base.OnInspectorGUI();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("Name"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("ATK"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("ATK_Speed"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("Range"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("animatorController"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("rarity"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("prfBullet"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("activeSkill"));

        EditorGUILayout.Space(20);

        reorderableDebuffList.DoLayoutList();

        serializedObject.ApplyModifiedProperties();
    }
}
