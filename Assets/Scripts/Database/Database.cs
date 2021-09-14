using System;
using System.Collections.Generic;

using UnityEngine;

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

    #region Items

    public List<ItemBase> ItemBases = new List<ItemBase>();
    public ItemBase GetItemBase(Guid guid) => ItemBases.Find(item => item.Guid == guid);

    #endregion

    #region Classes

    public List<ClassBase> Classes = new List<ClassBase>();

    #endregion

    #region Stats

    public List<StatBase> Stats = new List<StatBase>();

    #endregion

    #region Skills

    public List<SkillBase> Skills = new List<SkillBase>();

    #endregion

    #region Resources

    public List<ResourceData> Resources = new List<ResourceData>();

    #endregion

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
        _db.Classes = EditorUtils.FindScriptableObjects<ClassBase>();
        _db.Stats = EditorUtils.FindScriptableObjects<StatBase>();
        _db.Skills = EditorUtils.FindScriptableObjects<SkillBase>();
        _db.Resources = EditorUtils.FindScriptableObjects<ResourceData>();
    }

}

#endif
#endregion
