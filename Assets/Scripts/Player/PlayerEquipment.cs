using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections.Generic;
using UnityEngine;

public class PlayerEquipment : NetworkBehaviour
{

    [EnumNameArray(typeof(EquipmentSlot))]
    public SyncList<NetEquipment> NetEquipment = new SyncList<NetEquipment>();
    public List<EquipmentItem> Equipment = new List<EquipmentItem>();

    public override void OnStartServer()
    {
        base.OnStartServer();
        NetEquipment = new SyncList<NetEquipment>();
        for (int i = 0; i < (int)EquipmentSlot.Count; i++)
        {
            NetEquipment.Add(new NetEquipment());
        }
    }

    [Server]
    public void Equip()
    {

    }

}

public class NetEquipment
{
    public System.Guid ItemBaseGuid; 
}
