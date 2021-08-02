using FishNet.Object;
using FishNet.Object.Synchronizing;
using System;
using UnityEngine;

public class Inventory : NetworkBehaviour
{

    [Tooltip("Data object for this inventory.")]
    [SerializeField] private InventoryData _data = null;

    public SyncList<NetItemValue> Items = new SyncList<NetItemValue>();

    #region Adding Items

    [Server]
    public bool AddItem(NetItemValue item)
    {
        if (!TryAddItem(item)) return false;

        Items.Add(item);
        return true;
    }

    [Server]
    bool TryAddItem(NetItemValue item)
    {
        if (HasItem(item.Item))
        {

        }
        else
        {
            if (Items.Count >= _data.MaxSize)
            {
                // notify inv full
                return false;
            }

            Items.Add(item);
        }

        return true;
    }

    #endregion

    #region Helper Functions

    bool HasItem(NetItem item)
    {
        foreach (var itemVal in Items)
        {
            if (itemVal.Item == item)
                return true;
        }

        return false;
    }

    #endregion

}
