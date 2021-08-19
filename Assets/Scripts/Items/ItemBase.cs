using System;
using UnityEngine;

[CreateAssetMenu(menuName = "FishRPG/Item/Base")]
public class ItemBase : ScriptableObject, IStringBuilder
{

    [Header("Base")]
    public Guid Guid = Guid.NewGuid();
    public Sprite Sprite;
    [Range(0, 100)]
    public byte LevelReq;

    [TextArea(3, 20)]
    public string Description;

    public virtual string BuildString()
    {
        return "";
    }

    #region Editor
#if UNITY_EDITOR

    private void OnValidate()
    {
        if (Guid == Guid.Empty)
            Guid = Guid.NewGuid();
    }

#endif
    #endregion

}
