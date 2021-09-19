using UnityEngine;
using FishRPG.Entities.Player;

public abstract class ConsumableItem : ItemBase
{

    public abstract bool Use(Player user);

}
