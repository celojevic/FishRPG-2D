using UnityEngine;

[ExecuteAlways]
public class YSort : MonoBehaviour
{

    [Tooltip("Precision of the sorting order per unit.")]
    [SerializeField] private int _multiplier = 100;

    [Tooltip("When true, the world space canvas sorting order will be modified.")]
    [SerializeField] private bool _useWorldCanvas = false;

    private SpriteRenderer _sr;
    private Canvas _canvas;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _canvas = GetComponent<Canvas>();
    }

    private void Update()
    {
        if (!_useWorldCanvas)
        {
            _sr.sortingOrder = -Mathf.RoundToInt(transform.position.y * _multiplier);
        }
        else
        {
            _canvas.sortingOrder = -Mathf.RoundToInt(transform.position.y * _multiplier);
        }
    }

    #region Editor
#if UNITY_EDITOR

    private void OnValidate()
    {
        if (_sr == null)
            _sr = GetComponent<SpriteRenderer>();
        if (_canvas == null)
            _canvas = GetComponent<Canvas>();
    }

#endif
    #endregion

}
