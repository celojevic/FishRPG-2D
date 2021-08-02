using FishNet.Object;
using UnityEngine;

public class Player : NetworkBehaviour
{

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

}
