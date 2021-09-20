namespace FishRPG.Interactables
{
    using FishNet.Object;
    using UnityEngine;
    using FishRPG.Entities.Player;

    public class Fountain : Interactable
    {

        [Header("Fountain")]
        [SerializeField, Min(0)] private int _replenishAmount = 100;
        [SerializeField] private VitalType _vital;

        [Server]
        public override void Interact(Player player)
        {
            player.GetVital(_vital).Add(_replenishAmount);
        }

    }

}
