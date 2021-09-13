using FishNet.Object;
using UnityEngine;

public class ItemDropper : NetworkBehaviour
{

    public ItemDrop Prefab;

    protected void SpawnItemDrop(ItemValue itemValue, Vector2 position)
    {
        Instantiate(Prefab, position, Quaternion.identity).Setup(itemValue);

    }

}
