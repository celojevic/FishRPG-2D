using TMPro;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UITooltip : MonoBehaviour
{

    private static Image _background;
    private static TMP_Text _text;

    private void Awake()
    {
        _background = GetComponentInChildren<Image>();
        _text = GetComponentInChildren<TMP_Text>();
        Hide();
    }

    /// <summary>
    /// Shows tooltip UI with passed string at given position.
    /// </summary>
    /// <param name="text"></param>Text to show.
    /// <param name="position"></param>Set to mouse position if default.
    public static void Show(string text, Vector2 position = default)
    {
        _background.gameObject.SetActive(true);
        _text.text = text;
        _background.transform.position = Input.mousePosition;
    }

    public static void Hide()
    {
        if (_background.gameObject.activeInHierarchy)
            _background.gameObject.SetActive(false);
    }
}
