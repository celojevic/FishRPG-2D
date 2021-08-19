using UnityEngine;

public class DestroyAfterTime : MonoBehaviour
{

    [Tooltip("How long to wait until destroying the object.")]
    [SerializeField] private float _delay = 1f;
    [Tooltip("Useful when object pooling.")]
    [SerializeField] private bool _deactivateInstead = false;

    private float _timer;

    private void Start()
    {
        _timer = Time.time + _delay;
    }

    private void Update()
    {
        if (Time.time < _timer) return;
        
        if (_deactivateInstead)
        {
            gameObject.SetActive(false);
        }
        else
        {
            Destroy(gameObject);
        }
    }


}
