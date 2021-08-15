using System.Collections.Generic;
using UnityEngine;

public class UiInventory : UiPanel
{

    [Tooltip("Player inv data. Should match the one in player prefab.")]
    [SerializeField] private InventoryData _invData = null;

    [Header("Slots")]
    [SerializeField] private UIInventorySlot _slotPrefab = null;
    [SerializeField] private Transform _slotLayoutGroup = null;

    private List<UIInventorySlot> _slots = new List<UIInventorySlot>();

    void Start()
    {
        if (_invData == null)
        {
            Debug.LogError("Need to assign InventoryData object to the Inventory UI!");
            return;
        }

        SetupSlots();
    }

    protected override void UiManager_OnPlayerAssigned()
    {
        base.UiManager_OnPlayerAssigned();

        // TODO check if datas match
        //if (_invData != UiManager.Player.Inventory.data)

        UiManager.Player.Inventory.OnItemChanged += Inventory_OnItemChanged;
    }
    private void OnDestroy()
    {
        UiManager.Player.Inventory.OnItemChanged -= Inventory_OnItemChanged;
    }

    private void Inventory_OnItemChanged()
    {
        RefreshSlots();
    }

    protected override void OnPanelActivation(bool isActive)
    {
        base.OnPanelActivation(isActive);
        if (isActive)
            RefreshSlots();
    }

    void RefreshSlots()
    {
        // Disable unused icon images
        for (int i = UiManager.Player.Inventory.Items.Count; i < _slots.Count; i++)
            _slots[i].SetIconActive(false);

        for (int i = 0; i < UiManager.Player.Inventory.Items.Count; i++)
            _slots[i].Setup(UiManager.Player.Inventory.Items[i]);
    }

    void SetupSlots()
    {
        for (int i = 0; i < _invData.MaxSize; i++)
        {
            UIInventorySlot slot = Instantiate(_slotPrefab, _slotLayoutGroup);
            slot.Setup(i);
            _slots.Add(slot);
        }
    }


}
