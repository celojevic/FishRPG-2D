using FishNet.Object;
using UnityEngine;

public abstract class Interactable : NetworkBehaviour
{

    [Header("Interactable")]
    [Tooltip("Tag of object that can interact with this.")]
    [SerializeField] private string _otherTag = "Player";
    [Tooltip("When true, only local player will trigger events.")]
    [SerializeField] private bool _localPlayerOnly = true;
    [Tooltip("If empty, name will be used.")]
    [SerializeField] protected string InteractText = "";

    protected virtual void Start()
    {
        if (string.IsNullOrEmpty(InteractText))
            InteractText = name;
    }

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(_otherTag)) return;
        if (_localPlayerOnly && other.GetComponent<Player>() != UiManager.Player) return;

        UiInteract.Show(InteractText);
    }

    protected virtual void OnTriggerExit2D(Collider2D other)
    {
        UiInteract.Hide();
    }

    public abstract void Interact(Player player);

}
