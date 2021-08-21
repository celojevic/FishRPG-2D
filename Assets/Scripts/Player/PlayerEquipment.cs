using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerEquipment : NetworkBehaviour
{

    [EnumNameArray(typeof(EquipmentSlot))]
    public readonly SyncList<NetEquipment> NetEquipment = new SyncList<NetEquipment>();
    public List<EquipmentItem> Equipment = new List<EquipmentItem>();

    public event Action<EquipmentSlot> OnEquipmentChanged;

    private Player _player;

    private void Awake()
    {
        _player = GetComponent<Player>();
    }

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
                Equipment[index] = newItem?.ToEquipItem();
                _player.Visuals.SetEquipmentSprite((EquipmentSlot)index, Equipment[index]?.Sprite);
                break;
        }

        OnEquipmentChanged?.Invoke((EquipmentSlot)index);
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
            // TODO swap with inv
            Debug.Log("isno nol");
        }

        return false;
    }

    [ServerRpc]
    public void CmdUnequip(EquipmentSlot slot)
    {
        // out of range
        if (slot >= EquipmentSlot.Count) return;

        // can't unequip slot with nothing in it
        EquipmentItem equip = Equipment[(int)slot];
        if (equip == null) return;

        if (_player.Inventory.AddItem(new NetItemValue(equip)))
        {
            NetEquipment[(int)slot] = null;
        }
    }

    #endregion

}

[System.Serializable]
public class NetEquipment
{
    public System.Guid ItemBaseGuid;

    public EquipmentItem ToEquipItem() => Database.Instance?.GetItemBase(ItemBaseGuid) as EquipmentItem;
}
