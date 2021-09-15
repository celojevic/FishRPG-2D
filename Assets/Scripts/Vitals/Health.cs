using FishNet.Object;
using UnityEngine;

public class Health : VitalBase
{

    public override void Add(int amount)
    {
        PlayerMessageHandler.SendPlayerMsg(Owner, MessageType.Action, $"+{amount}",
            Color.green, gameObject);
        base.Add(amount);
    }

    public override void Subtract(int amount)
    {
        PlayerMessageHandler.SendPlayerMsg(Owner, MessageType.Action, $"-{amount}",
            Color.red, gameObject);
        base.Subtract(amount);
    }

    [Server]
    protected override void OnDeplete()
    {
        base.OnDeplete();

        // TODO options
        //  - log enemy KC, player death count

        Despawn();
    }

}
