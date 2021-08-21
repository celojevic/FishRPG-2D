using UnityEngine;
using UnityEngine.UI;

public class UIEquipment : UiPanel
{

    [Header("UI Equipment")]
    [SerializeField, EnumNameArray(typeof(EquipmentSlot))] 
    private Image[] _equipSlotImages = new Image[(int)EquipmentSlot.Count];

    #region Equipment Changed Callback

    protected override void UiManager_OnPlayerAssigned()
    {
        base.UiManager_OnPlayerAssigned();
        UiManager.Player.Equipment.OnEquipmentChanged += Equipment_OnEquipmentChanged;
    }

    private void OnDestroy()
    {
        if (UiManager.Player)
            UiManager.Player.Equipment.OnEquipmentChanged -= Equipment_OnEquipmentChanged;
    }

    private void Equipment_OnEquipmentChanged(EquipmentSlot slot)
    {
        RefreshSlot(slot);
    }

    #endregion

    protected override void OnPanelActivation(bool isActive)
    {
        base.OnPanelActivation(isActive);
        if (isActive)
            RefreshSlots();
    }

    void RefreshSlots()
    {
        for (EquipmentSlot i = 0; i < EquipmentSlot.Count; i++)
        {
            RefreshSlot(i);
        }
    }

    void RefreshSlot(EquipmentSlot slot)
    {
        // out of range
        int index = (int)slot;
        if (index >= _equipSlotImages.Length) return;

        var equip = UiManager.Player.Equipment.Equipment[index];
        if (equip != null)
        {
            _equipSlotImages[index].enabled = true;
            _equipSlotImages[index].sprite = equip.Sprite;
        }
        else
        {
            _equipSlotImages[index].enabled = false;
        }
    }

}
