using FishNet.Object;
using UnityEngine;

public class Entity : NetworkBehaviour
{

    [Header("Entity Components")]
    public VitalBase[] Vitals;

    protected virtual void Awake()
    {
        Vitals = GetComponents<VitalBase>();
    }

    #region Vitals

    public VitalBase GetVital(VitalType type)
    {
        switch (type)
        {
            case VitalType.Health:
                foreach (var item in Vitals)
                    if (item is Health)
                        return item;
                break;

            case VitalType.Mana:
                foreach (var item in Vitals)
                    if (item is Mana)
                        return item;
                break;
        }

        return null;
    }

    #endregion

}
