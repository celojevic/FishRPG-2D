using System;
using UnityEngine;
using System.Text.RegularExpressions;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class EnumNameArrayAttribute : PropertyAttribute
{

    public Type EnumType;

    public EnumNameArrayAttribute(Type enumType)
    {
        EnumType = enumType;
    }

}

#if UNITY_EDITOR

[CustomPropertyDrawer(typeof(EnumNameArrayAttribute))]
public class EnumNameArrayDrawer : PropertyDrawer
{

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var enumNames = Enum.GetNames(((EnumNameArrayAttribute)attribute).EnumType);
        int index = int.Parse(Regex.Match(property.propertyPath, @"\d+").Value);

        if (index < enumNames.Length)
            label = new GUIContent(enumNames[index]);

        EditorGUI.PropertyField(position, property, label, property.isExpanded);
    }

}

#endif