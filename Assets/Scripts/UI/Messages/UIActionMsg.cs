using TMPro;
using UnityEngine;

public class UIActionMsg : MonoBehaviour
{

    [SerializeField] private TMP_Text _text = null;

    public void Setup(string text, Color color = new Color())
    {
        _text.text = text;
        _text.color = color;
    }

}
