using UnityEngine;

public class PlayerInput : MonoBehaviour
{

    /// <summary>
    /// Cached input vector each frame.
    /// </summary>
    internal Vector2 InputVector;

    [Header("Input Axes")]
    [Tooltip("When true, input axes will use GetAxisRaw, i.e. no acceleration.")]
    [SerializeField] private bool _getAxisRaw = false;

    private void Update()
    {
        GetAxes();
    }

    void GetAxes()
    {
        if (!_getAxisRaw)
        {
            InputVector = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        }
        else
        {
            InputVector = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        }
    }

}
