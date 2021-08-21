using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Clamps window to the screen when dragging.
/// Requires AspectRatioFitter to respect layout group positions.
/// </summary>
[RequireComponent(typeof(AspectRatioFitter))]
public class WindowClampToScreen : MonoBehaviour
{

    private RectTransform _rectTransform;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        GetComponent<AspectRatioFitter>().aspectRatio = (float)Screen.width / (float)Screen.height;
    }

    private void Update()
    {
        float x = Mathf.Clamp(_rectTransform.position.x,
            _rectTransform.rect.width / 2f, 
            Screen.width - _rectTransform.rect.width / 2f);

        float y = Mathf.Clamp(_rectTransform.position.y, 
            _rectTransform.rect.height / 2f, 
            Screen.height - _rectTransform.rect.height / 2f);

        _rectTransform.position = new Vector2(x, y);
    }

}
