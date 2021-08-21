using UnityEngine;
using UnityEngine.UI;

public class UIInventorySlot : UISlot
{

    [SerializeField] private Image _itemIconImage = null;

    private UiInventory _uiInventory;
    private int _myIndex = -1;

    private void Awake()
    {
        _uiInventory = GetComponentInParent<UiInventory>();
    }

    public void SetIconActive(bool active) => _itemIconImage.enabled = active;

    /// <summary>
    /// Sets the slot's index.
    /// </summary>
    /// <param name="index"></param>
    public void Setup(int index)
    {
        _myIndex = index;
    }

    /// <summary>
    /// Sets the slot's item and shows item sprite if it exists.
    /// </summary>
    /// <param name="itemVal"></param>
    public void Setup(ItemValue itemVal)
    {
        // no item
        if (itemVal?.Item == null)
        {
            _itemIconImage.enabled = false;
            return;
        }

        _itemIconImage.enabled = true;
        _itemIconImage.sprite = itemVal.Item.Sprite;

        // TODO quantity text
    }

    bool HasItem() => _itemIconImage.enabled;

    protected override void HandleRightClick()
    {
        base.HandleRightClick();
        if (!HasItem()) return;

        UiManager.Player.Inventory.CmdUseItem(_myIndex);
    }

}
