namespace FishRPG.Entities.Player
{
    using UnityEngine;
    using FishNet.Object;

    public class PlayerInventory : Inventory
    {

        private Player _player;

        private void Awake()
        {
            _player = GetComponent<Player>();
        }

        #region Using Items

        [ServerRpc]
        public void CmdUseItem(int slotIndex)
        {
            if (NetItems[slotIndex]?.Item != null)
            {
                ItemBase item = Database.Instance?.GetItemBase(NetItems[slotIndex].Item.ItemBaseGuid);
                if (item)
                {
                    if (item is ConsumableItem consumable)
                    {
                        if (consumable.Use(_player))
                            NetItems.RemoveAt(slotIndex);
                    }
                    else if (item is EquipmentItem equipment)
                    {
                        if (_player.Equipment.Equip(equipment))
                            NetItems.RemoveAt(slotIndex);
                    }
                }
            }
        }

        #endregion

    }
}
