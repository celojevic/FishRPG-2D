namespace FishRPG.Interactables
{
    using UnityEngine;
    using FishRPG.Entities.Player;
    using FishRPG.Dialogue.Runtime;

    public class Npc : Interactable
    {

        [SerializeField] private DialogueData _dialogue = null;

        public override void Interact(Player player)
        {
            Debug.Log(_dialogue.Nodes[0].Text);
        }

    }
}
