using System.Collections.Generic;
using UnityEngine;

public class UiInventory : UiPanel
{

    [Tooltip("Player inv data. Should match the one in player prefab.")]
    [SerializeField] private InventoryData _invData = null;

    [Header("Slots")]
    [SerializeField] private UIInventorySlot _slotPrefab = null;
    [SerializeField] private Transform _slotLayoutGroup = null;

    protected override void Start()
    {
        base.Start();

        if (_invData == null)
        {
            Debug.LogError("Need to assign InventoryData object to the Inventory UI!");
            return;
        }
    }

    protected override void UiManager_OnPlayerAssigned()
    {
        base.UiManager_OnPlayerAssigned();

        // TODO check if datas match
        //if (_invData != UiManager.Player.Inventory.data)

        SetupSlots();
    }

    void SetupSlots()
    {
        if (UiManager.Player == null) return;

        for (int i = 0; i < _invData.MaxSize; i++)
        {
            UIInventorySlot slot = Instantiate(_slotPrefab, _slotLayoutGroup);
            //slot.Setup(UiManager.Player.Inventory.Items)
        }
    }


}
