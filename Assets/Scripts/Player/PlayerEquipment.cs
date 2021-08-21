using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerEquipment : NetworkBehaviour
{

    [EnumNameArray(typeof(EquipmentSlot))]
    public SyncList<NetEquipment> NetEquipment = new SyncList<NetEquipment>();
    public List<EquipmentItem> Equipment = new List<EquipmentItem>();

    private void Start()
    {
        NetEquipment.Clear();
        for (EquipmentSlot i = 0; i < EquipmentSlot.Count; i++)
            NetEquipment.Add(null);
    }

    #region Client Synclist Callbacks

    public override void OnOwnershipClient(NetworkConnection newOwner)
    {
        base.OnOwnershipClient(newOwner);
        if (!newOwner.IsLocalClient) return;

        Equipment = new List<EquipmentItem>();
        for (EquipmentSlot i = 0; i < EquipmentSlot.Count; i++)
            Equipment.Add(null);

        NetEquipment.OnChange += NetEquipment_OnChange;
    }

    private void NetEquipment_OnChange(SyncListOperation op, int index, 
        NetEquipment oldItem, NetEquipment newItem, bool asServer)
    {
        if (asServer) return;

        switch (op)
        {
            case SyncListOperation.Set:
                Equipment[index] = newItem.ToEquipItem();
                break;
        }
    }

    private void OnDestroy()
    {
        NetEquipment.OnChange -= NetEquipment_OnChange;
    }

    #endregion

    #region Server

    [Server]
    public bool Equip(EquipmentItem equipment)
    {
        int index = (int)equipment.Slot;
        if (NetEquipment[index] == null)
        {
            NetEquipment[index] = equipment.ToNetEquip();
            return true;
        }
        else
        {
            // TODO return item so can swap with inv
            Debug.Log("isno nol");
        }

        return false;
    }

    #endregion

}

[System.Serializable]
public class NetEquipment
{
    public System.Guid ItemBaseGuid;

    public EquipmentItem ToEquipItem() => Database.Instance?.GetItemBase(ItemBaseGuid) as EquipmentItem;
}
