namespace FishRPG.Vitals
{
    using UnityEngine;

    public class Mana : VitalBase
    {

        public override void Add(int amount)
        {
            base.Add(amount);

            PlayerMessageHandler.SendPlayerMsg(Owner, MessageType.Action, $"+{amount}",
                Color.cyan, gameObject);
        }

        public override void Subtract(int amount)
        {
            base.Subtract(amount);

            PlayerMessageHandler.SendPlayerMsg(Owner, MessageType.Action, $"-{amount}",
                Color.magenta, gameObject);
        }

    }
}
