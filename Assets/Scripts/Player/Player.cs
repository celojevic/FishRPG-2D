using FishNet.Object;
using UnityEngine;

public class Player : NetworkBehaviour
{

    [Header("Data")]
    public ClassBase Class;
    private byte _appearanceIndex = 0;

    [Header("Components")]
    public PlayerInput Input;
    public PlayerInventory Inventory;
    public PlayerMovement Movement;
    public PlayerVisuals Visuals;

    public VitalBase[] Vitals;

    private void Awake()
    {
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
