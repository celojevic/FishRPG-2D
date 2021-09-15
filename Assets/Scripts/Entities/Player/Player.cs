using FishNet.Object;
using UnityEngine;

public class Player : Entity
{

    [Header("Data")]
    public ClassBase Class;
    private byte _appearanceIndex = 0;

    [Header("Components")]
    public PlayerCombat Combat;
    public PlayerEquipment Equipment;
    public PlayerInput Input;
    public PlayerInventory Inventory;
    public PlayerMovement Movement;
    public PlayerVisuals Visuals;

    /// <summary>
    /// Returns the true center of the player sprite.
    /// TODO should be based on sprite then
    /// </summary>
    /// <returns></returns>
    internal Vector2 GetCenter() => transform.position + new Vector3(0f, 0.5f);

    protected override void Awake()
    {
        base.Awake();

        Equipment = GetComponent<PlayerEquipment>();
        Input = GetComponent<PlayerInput>();
        Inventory = GetComponent<PlayerInventory>();
        Movement = GetComponent<PlayerMovement>();
        Visuals = GetComponent<PlayerVisuals>();
    }

    #region Class

    public Appearance GetAppearance() => Class.Appearances[_appearanceIndex];

    #endregion

    // TODO cache a bool[] for if they have a tool when adding items and equipping
    public bool HasTool(ToolType toolType)
    {
        // check inv
        foreach (var item in Inventory.NetItems)
        {
            if (item == null) continue;
            var itemBase = Database.Instance.GetItemBase(item.Item.ItemBaseGuid);
            if (itemBase != null && itemBase is ToolItem tool && tool.ToolType == toolType)
                return true;
        }

        // check equipment
        foreach (var item in Equipment.NetEquipment)
        {
            if (item == null) continue;
            var equip = Database.Instance.GetItemBase(item.ItemBaseGuid);
            if (equip != null && equip is ToolItem tool && tool.ToolType == toolType)
                return true;
        }

        return false;
    }

}
