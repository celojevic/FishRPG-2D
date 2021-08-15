using FishNet.Object;
using FishNet.Object.Synchronizing;
using System;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : NetworkBehaviour
{

    [Tooltip("Data object for this inventory.")]
    [SerializeField] private InventoryData _data = null;

    public SyncList<NetItemValue> NetItems = new SyncList<NetItemValue>();
    public List<ItemValue> Items = new List<ItemValue>();

    /// <summary>
    /// Invokes the index of the changed item.
    /// </summary>
    public event Action OnItemChanged;

    #region Client Items Callback

    public override void OnStartClient(bool isOwner)
    {
        if (!isOwner) return;
        NetItems.OnChange += Items_OnChange;
    }

    private void Items_OnChange(SyncListOperation op, int index, 
        NetItemValue oldItem, NetItemValue newItem, bool asServer)
    {
        if (asServer) return;

        switch (op)
        {
            case SyncListOperation.Add:
                Items.Add(newItem.ToItemValue());
                break;

            case SyncListOperation.Insert:
                Items.Insert(index, newItem.ToItemValue());
                break;

            case SyncListOperation.Set:
                Items[index] = newItem.ToItemValue();
                break;

            case SyncListOperation.RemoveAt:
                Items.RemoveAt(index);
                break;

            case SyncListOperation.Clear:
                Items.Clear();
                break;

            case SyncListOperation.Complete:
                // Prevent invoking twice
                return;
        }

        OnItemChanged?.Invoke();
    }

    private void OnDestroy()
    {
        NetItems.OnChange -= Items_OnChange;
    }

    #endregion

    #region Adding Items

    [Server]
    public bool AddItem(NetItemValue item)
    {
        if (!TryAddItem(item)) return false;

        NetItems.Add(item);
        return true;
    }

    [Server]
    bool TryAddItem(NetItemValue item)
    {
        if (HasItem(item.Item))
        {
            // stackables
        }
        else
        {
            if (NetItems.Count >= _data.MaxSize)
            {
                // notify inv full
                return false;
            }
        }

        return true;
    }

    #endregion

    #region Helper Functions

    bool HasItem(NetItem item)
    {
        foreach (var itemVal in NetItems)
        {
            if (itemVal.Item == item)
                return true;
        }

        return false;
    }

    #endregion

}
