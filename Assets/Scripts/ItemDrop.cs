using UnityEngine;

public class ItemDrop : Interactable
{

    [Header("Item Drop")]
    public ItemValue ItemValue;

    [SerializeField] private SpriteRenderer _sr = null;

    protected override void Start()
    {
        if (ItemValue.Item == null || ItemValue.Quantity <= 0)
        {
            Debug.LogWarning($"Item drop '{name}' was invalid. Destroying it...");
            Destroy(gameObject);
        }

        if (string.IsNullOrEmpty(InteractText))
            InteractText = ItemValue.Item.name;
    }

    public override void Interact(Player player)
    {
        if (player == null) return;
        if (player.Inventory == null) return;

        if (player.Inventory.AddItem(new NetItemValue(ItemValue)))
        {
            Despawn();
        }
    }

    #region Editor
#if UNITY_EDITOR

    protected override void OnValidate()
    {
        base.OnValidate();
        if (_sr == null || ItemValue == null || ItemValue.Item == null) return;

        if (_sr.sprite != ItemValue.Item.Sprite)
        {
            _sr.sprite = ItemValue.Item.Sprite;
        }
    }

#endif
    #endregion

}
