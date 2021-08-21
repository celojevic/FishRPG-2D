using UnityEngine;

public class RandomRotation : MonoBehaviour
{

    [SerializeField] private bool _zOnly = true;

    private void Start()
    {
        if (_zOnly)
        {
            transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
        }
        else
        {
            transform.rotation = Random.rotation;
        }
    }

}
