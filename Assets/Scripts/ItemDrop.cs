using FishNet.Object;
using UnityEngine;

public class ItemDrop : Interactable
{

    [Header("Item Drop")]
    public ItemValue ItemValue;

    [SerializeField] private SpriteRenderer _sr = null;

    private void Awake()
    {
        if (ItemValue.Item == null || ItemValue.Quantity <= 0)
        {
            Debug.LogWarning($"Item drop '{name}' was invalid. Destroying it...");
            Destroy(gameObject);
        }
    }

    protected override void Start()
    {
        if (string.IsNullOrEmpty(InteractText))
            InteractText = ItemValue.Item.name;
    }

    protected override void OnTriggerEnter2D(Collider2D other)
    {
        base.OnTriggerEnter2D(other);

        if (!IsServer) return;
        if (other.CompareTag("Player") && !other.isTrigger)
        {
            //PlayerInventory inv = other.GetComponent<PlayerInventory>();
            //if (inv && inv.AddItem(new NetItemValue(ItemValue)))
            //{
            //    Despawn();
            //}
        }
    }

    #region Editor
#if UNITY_EDITOR

    private void OnValidate()
    {
        if (_sr == null || ItemValue == null || ItemValue.Item == null) return;

        if (_sr.sprite != ItemValue.Item.Sprite)
        {
            _sr.sprite = ItemValue.Item.Sprite;
        }
    }

#endif
    #endregion

}
