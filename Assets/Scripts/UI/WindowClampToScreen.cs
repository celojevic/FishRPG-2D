using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindowClampToScreen : MonoBehaviour
{

    private RectTransform _rectTransform;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
    }

    private void Update()
    {
        float x = Mathf.Clamp(_rectTransform.anchoredPosition.x,
            _rectTransform.anchoredPosition.x - _rectTransform.rect.width / 2f,
            Screen.width);
        float y = Mathf.Clamp(_rectTransform.anchoredPosition.y,
            _rectTransform.anchoredPosition.y - _rectTransform.sizeDelta.y / 2f,
            Screen.width);

        Debug.Log(_rectTransform.sizeDelta);
        _rectTransform.anchoredPosition = new Vector2(x, y);
    }

}
