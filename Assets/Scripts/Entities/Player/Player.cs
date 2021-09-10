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

}
