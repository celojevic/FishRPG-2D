using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UiInventory : UiPanel
{

    [Tooltip("Player inv data. Should match the one in player prefab.")]
    [SerializeField] private InventoryData _invData = null;

    [Header("Slots")]
    [SerializeField] private UIInventorySlot _slotPrefab = null;
    [SerializeField] private GridLayoutGroup _slotLayoutGroup = null;

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
        if (UiManager.Player)
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

    /// <summary>
    /// Sets up child UI inventory slots once on Start.
    /// </summary>
    void SetupSlots()
    {
        Resize();

        for (int i = 0; i < _invData.MaxSize; i++)
        {
            UIInventorySlot slot = Instantiate(_slotPrefab, _slotLayoutGroup.transform);
            slot.Setup(i);
            _slots.Add(slot);
        }
    }

    void Resize()
    {
        if (_invData.UIData == null) return;

        _slotLayoutGroup.cellSize = _invData.UIData.SlotSize;
        _slotLayoutGroup.spacing = _invData.UIData.Spacing;
        _slotLayoutGroup.padding = _invData.UIData.Padding;
        _slotLayoutGroup.constraint = _invData.UIData.Constraint;
        _slotLayoutGroup.constraintCount = _invData.UIData.ConstraintCount;
    }

    public void UseItemInSlot(int slotIndex)
    {
        UiManager.Player.Inventory.CmdUseItem(slotIndex);
    }

}
