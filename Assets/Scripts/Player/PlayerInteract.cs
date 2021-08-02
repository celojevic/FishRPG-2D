using UnityEngine;

public class PlayerInteract : MonoBehaviour
{

    [SerializeField] private KeyCode _interactKey = KeyCode.F;

    private void Update()
    {
        if (Input.GetKeyDown(_interactKey))
        {
            TryInteract();
        }
    }

    void TryInteract()
    {

    }

}
