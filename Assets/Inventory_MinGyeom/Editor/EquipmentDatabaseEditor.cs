using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EquipmentDatabase))]
public class EquipmentDatabaseEditor : Editor
{
    private readonly Dictionary<EquipmentCategory, bool> categoryFoldouts = new();

    private SerializedProperty equipmentDefinitionsProperty;

    private void OnEnable()
    {
        equipmentDefinitionsProperty = serializedObject.FindProperty("equipmentDefinitions");

        Array categories = Enum.GetValues(typeof(EquipmentCategory));
        for (int i = 0; i < categories.Length; i++)
        {
            EquipmentCategory category = (EquipmentCategory)categories.GetValue(i);
            if (!categoryFoldouts.ContainsKey(category))
            {
                categoryFoldouts.Add(category, true);
            }
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawScriptReference();
        EditorGUILayout.Space(4f);

        Array categories = Enum.GetValues(typeof(EquipmentCategory));
        for (int i = 0; i < categories.Length; i++)
        {
            DrawCategorySection((EquipmentCategory)categories.GetValue(i));
            EditorGUILayout.Space(6f);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawScriptReference()
    {
        using (new EditorGUI.DisabledScope(true))
        {
            MonoScript script = MonoScript.FromScriptableObject((EquipmentDatabase)target);
            EditorGUILayout.ObjectField("Script", script, typeof(MonoScript), false);
        }
    }

    private void DrawCategorySection(EquipmentCategory category)
    {
        List<int> indices = GetIndicesForCategory(category);
        string headerLabel = $"{category} ({indices.Count})";

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();

        categoryFoldouts[category] = EditorGUILayout.Foldout(categoryFoldouts[category], headerLabel, true);
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Add", GUILayout.Width(60f)))
        {
            AddEquipmentDefinition(category);
        }

        EditorGUILayout.EndHorizontal();

        if (categoryFoldouts[category])
        {
            EditorGUI.indentLevel++;

            if (indices.Count == 0)
            {
                EditorGUILayout.HelpBox("No equipment registered in this category.", MessageType.Info);
            }
            else
            {
                for (int i = 0; i < indices.Count; i++)
                {
                    int arrayIndex = indices[i];
                    SerializedProperty elementProperty = equipmentDefinitionsProperty.GetArrayElementAtIndex(arrayIndex);
                    DrawEquipmentDefinition(category, arrayIndex, elementProperty);
                }
            }

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
    }

    private List<int> GetIndicesForCategory(EquipmentCategory category)
    {
        List<int> indices = new();

        for (int i = 0; i < equipmentDefinitionsProperty.arraySize; i++)
        {
            SerializedProperty elementProperty = equipmentDefinitionsProperty.GetArrayElementAtIndex(i);
            SerializedProperty categoryProperty = elementProperty.FindPropertyRelative("category");
            if (categoryProperty != null && categoryProperty.enumValueIndex == (int)category)
            {
                indices.Add(i);
            }
        }

        return indices;
    }

    private void DrawEquipmentDefinition(
        EquipmentCategory category,
        int arrayIndex,
        SerializedProperty elementProperty)
    {
        SerializedProperty equipmentIdProperty = elementProperty.FindPropertyRelative("equipmentId");
        SerializedProperty displayNameProperty = elementProperty.FindPropertyRelative("displayName");
        SerializedProperty categoryProperty = elementProperty.FindPropertyRelative("category");
        SerializedProperty rarityProperty = elementProperty.FindPropertyRelative("rarity");
        SerializedProperty isGachaEnabledProperty = elementProperty.FindPropertyRelative("isGachaEnabled");
        SerializedProperty uiIconProperty = elementProperty.FindPropertyRelative("uiIcon");
        SerializedProperty descriptionProperty = elementProperty.FindPropertyRelative("description");
        SerializedProperty maxLevelProperty = elementProperty.FindPropertyRelative("maxLevel");
        SerializedProperty requiredItemCountProperty = elementProperty.FindPropertyRelative("requiredItemCountForNextLevel");
        SerializedProperty attackProperty = elementProperty.FindPropertyRelative("attack");
        SerializedProperty hpProperty = elementProperty.FindPropertyRelative("hp");
        SerializedProperty healPerSecProperty = elementProperty.FindPropertyRelative("healPerSec");
        SerializedProperty critChanceProperty = elementProperty.FindPropertyRelative("critChance");
        SerializedProperty critDamageProperty = elementProperty.FindPropertyRelative("critDamage");

        string title = GetEntryTitle(category, equipmentIdProperty.stringValue, displayNameProperty.stringValue, arrayIndex);
        elementProperty.isExpanded = EditorGUILayout.Foldout(elementProperty.isExpanded, title, true);

        if (!elementProperty.isExpanded)
        {
            return;
        }

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.PropertyField(equipmentIdProperty, new GUIContent("ID"));
        EditorGUILayout.PropertyField(displayNameProperty, new GUIContent("Display Name"));

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.PropertyField(categoryProperty);
        }

        EditorGUILayout.PropertyField(rarityProperty);
        EditorGUILayout.PropertyField(isGachaEnabledProperty, new GUIContent("Gacha Enabled"));
        EditorGUILayout.PropertyField(uiIconProperty, new GUIContent("UI Icon"));
        EditorGUILayout.PropertyField(descriptionProperty, new GUIContent("Description"));

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Stats", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(attackProperty, new GUIContent("Attack"));
        EditorGUILayout.PropertyField(hpProperty, new GUIContent("HP"));
        EditorGUILayout.PropertyField(healPerSecProperty, new GUIContent("Heal Per Sec"));
        EditorGUILayout.PropertyField(critChanceProperty, new GUIContent("Crit Chance"));
        EditorGUILayout.PropertyField(critDamageProperty, new GUIContent("Crit Damage"));

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Progression (temporary)", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(maxLevelProperty, new GUIContent("Max Level"));
        EditorGUILayout.PropertyField(requiredItemCountProperty, new GUIContent("Required Equipment Count"));

        EditorGUILayout.Space(4f);
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Delete", GUILayout.Width(70f)))
        {
            RemoveEquipmentDefinition(arrayIndex);
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    private void AddEquipmentDefinition(EquipmentCategory category)
    {
        int newIndex = equipmentDefinitionsProperty.arraySize;
        equipmentDefinitionsProperty.arraySize++;

        SerializedProperty newElement = equipmentDefinitionsProperty.GetArrayElementAtIndex(newIndex);
        InitializeEquipmentDefinition(newElement, category);
        newElement.isExpanded = true;
    }

    private void RemoveEquipmentDefinition(int arrayIndex)
    {
        equipmentDefinitionsProperty.DeleteArrayElementAtIndex(arrayIndex);
        serializedObject.ApplyModifiedProperties();
        GUIUtility.ExitGUI();
    }

    private static void InitializeEquipmentDefinition(SerializedProperty elementProperty, EquipmentCategory category)
    {
        elementProperty.FindPropertyRelative("equipmentId").stringValue = string.Empty;
        elementProperty.FindPropertyRelative("displayName").stringValue = string.Empty;
        elementProperty.FindPropertyRelative("category").enumValueIndex = (int)category;
        elementProperty.FindPropertyRelative("rarity").enumValueIndex = (int)EquipmentRarity.Common;
        elementProperty.FindPropertyRelative("isGachaEnabled").boolValue = true;
        elementProperty.FindPropertyRelative("uiIcon").objectReferenceValue = null;
        elementProperty.FindPropertyRelative("description").stringValue = string.Empty;
        elementProperty.FindPropertyRelative("maxLevel").intValue = 10;
        elementProperty.FindPropertyRelative("requiredItemCountForNextLevel").intValue = 10;
        elementProperty.FindPropertyRelative("attack").intValue = 0;
        elementProperty.FindPropertyRelative("hp").intValue = 0;
        elementProperty.FindPropertyRelative("healPerSec").floatValue = 0f;
        elementProperty.FindPropertyRelative("critChance").floatValue = 0f;
        elementProperty.FindPropertyRelative("critDamage").floatValue = 0f;
    }

    private static string GetEntryTitle(
        EquipmentCategory category,
        string equipmentId,
        string displayName,
        int arrayIndex)
    {
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            return displayName;
        }

        if (!string.IsNullOrWhiteSpace(equipmentId))
        {
            return equipmentId;
        }

        return $"{category} Equipment {arrayIndex + 1}";
    }
}
