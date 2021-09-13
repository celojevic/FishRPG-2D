using FishNet.Object;
using UnityEngine;

public class ItemDropper : NetworkBehaviour
{

    public ItemDrop Prefab;

    [Server]
    public void SpawnItemDrop(ItemValue itemValue, Vector2 position)
    {
        ItemDrop drop = Instantiate(Prefab, position, Quaternion.identity);
        drop.Setup(itemValue);
        Spawn(drop.gameObject);
    }

}
