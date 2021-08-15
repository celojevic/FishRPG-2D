using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

/// <summary>
/// Data class of custom settings for an inventory UI's grid layout.
/// </summary>
[CreateAssetMenu(menuName = "FishRPG/Inventory/UI Data")]
public class InventoryUIData : ScriptableObject
{

    [Header("Size and Spacing")]
    [Tooltip("Size of the slots when/if drawn in UI.")]
    public Vector2 SlotSize = new Vector2(100, 100);
    [Tooltip("Spacing between slots.")]
    public Vector2 Spacing = new Vector2(10, 10);
    [Tooltip("Padding around panel outside of slots.")]
    public RectOffset Padding;

    [Header("Constraints")]
    [Tooltip("If flexible, defaults to 5 columns.")]
    public Constraint Constraint = Constraint.Flexible;
    [Tooltip("Size of constraint if not flexible.")]
    public byte ConstraintCount = 5;

}

#region Editor - WIP
//#if UNITY_EDITOR

//[CustomEditor(typeof(InventoryData))]
//public class InventoryDataEditor : Editor
//{

//    private InventoryData _data;

//    public override void OnInspectorGUI()
//    {
//        _data = (InventoryData)target;
//        DrawBaseData();
//        DrawUIData();
//    }

//    void DrawBaseData()
//    {
//        GUILayout.Label("Data", EditorStyles.boldLabel);

//        _data.MaxSize = (ushort)EditorGUILayout.IntSlider("Max Size", _data.MaxSize, 0, 100);
//        _data.InvType = (InventoryType)EditorGUILayout.EnumPopup("Type", _data.InvType);

//        EditorGUILayout.Space();
//    }

//    void DrawUIData()
//    {
//        GUILayout.Label("UI Data", EditorStyles.boldLabel);

//        _data.AutoSetUI = EditorGUILayout.Toggle("Auto Set UI", _data.AutoSetUI);
//        if (_data.AutoSetUI)
//        {
//            _data.SlotSize = EditorGUILayout.Vector2Field("Slot Size", _data.SlotSize);
//            _data.Spacing = EditorGUILayout.Vector2Field("Spacing", _data.Spacing);
//            _data.Padding = EditorGUILayout.RectIntField("Padding", _data.Padding);
//        }

//        // draw ui preview?

//        EditorGUILayout.Space();
//    }

//}

//#endif
#endregion
