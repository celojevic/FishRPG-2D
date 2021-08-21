using FishNet.Object;
using UnityEngine;

public class Player : NetworkBehaviour
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
    public VitalBase[] Vitals;

    /// <summary>
    /// Returns the true center of the player sprite.
    /// TODO should be based on sprite then
    /// </summary>
    /// <returns></returns>
    internal Vector2 GetCenter() => transform.position + new Vector3(0f, 0.5f);

    private void Awake()
    {
        Equipment = GetComponent<PlayerEquipment>();
        Input = GetComponent<PlayerInput>();
        Inventory = GetComponent<PlayerInventory>();
        Movement = GetComponent<PlayerMovement>();
        Visuals = GetComponent<PlayerVisuals>();
        Vitals = GetComponents<VitalBase>();
    }

    #region Class

    public Appearance GetAppearance() => Class.Appearances[_appearanceIndex];

    #endregion

    #region Vitals

    public VitalBase GetVital(VitalType type)
    {
        switch (type)
        {
            case VitalType.Health:
                foreach (var item in Vitals)
                {
                    if (item is Health h)
                        return h;
                }
                break;

            case VitalType.Mana:
                foreach (var item in Vitals)
                {
                    if (item is Mana m)
                        return m;
                }
                break;
        }

        return null;
    }

    public VitalType GetVitalType(int index)
    {
        if (Vitals[index] is Health) return VitalType.Health;
        if (Vitals[index] is Mana) return VitalType.Mana;

        return VitalType.Count;
    }

    #endregion

}
