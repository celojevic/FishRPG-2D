using UnityEngine;
using System.Collections.Generic;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class Database : MonoBehaviour
{

    #region Singleton

    public static Database Instance;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
            Destroy(gameObject);
    }

    #endregion

    public List<ItemBase> ItemBases = new List<ItemBase>();
    public ItemBase GetItemBase(Guid guid) => ItemBases.Find(item => item.Guid == guid);

}

#region Editor
#if UNITY_EDITOR

[CustomEditor(typeof(Database))]
public class DatabaseEditor : Editor
{

    private Database _db;

    public override void OnInspectorGUI()
    {
        _db = (Database)target;

        if (GUILayout.Button("Update Database"))
        {
            UpdateDatabase();
        }

        base.OnInspectorGUI();
    }

    void UpdateDatabase()
    {
        _db.ItemBases = EditorUtils.FindScriptableObjects<ItemBase>();
        
    }



}

#endif
#endregion
