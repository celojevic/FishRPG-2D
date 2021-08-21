using UnityEngine;

public class UIEquipmentSlot : UISlot
{

    [SerializeField] private EquipmentSlot _mySlot;

    protected override void HandleRightClick()
    {
        if (UiManager.Player.Equipment.Equipment[(int)_mySlot] != null)
            UiManager.Player.Equipment.CmdUnequip(_mySlot);
    }

    private void OnValidate()
    {
        if (_mySlot == EquipmentSlot.Count)
        {
            Debug.LogWarning($"Slot cannot be the Count. Assign a valid slot to {name}.");
            _mySlot = EquipmentSlot.Weapon;
        }
    }

}
