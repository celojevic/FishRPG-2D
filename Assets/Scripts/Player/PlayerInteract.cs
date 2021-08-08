using FishNet.Object;
using UnityEngine;

public class PlayerInteract : NetworkBehaviour
{

    [SerializeField] private KeyCode _interactKey = KeyCode.F;

    private Player _player;
    private Interactable _interactableInRange;

    private void Awake()
    {
        _player = GetComponent<Player>(); 
    }

    private void Update()
    {
        if (Input.GetKeyDown(_interactKey))
        {
            TryInteract();
        }
    }

    void TryInteract()
    {
        if (_interactableInRange == null) return;

        _interactableInRange.Interact(_player);
        UiInteract.Hide();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Interactable") && other.isTrigger)
        {
            _interactableInRange = other.GetComponent<Interactable>();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Interactable") && other.isTrigger)
        {
            _interactableInRange = null;
        }
    }

}
