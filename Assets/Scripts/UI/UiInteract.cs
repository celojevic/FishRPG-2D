using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UiInteract : MonoBehaviour
{

    private static TMP_Text _nameText;
    private static TMP_Text _keyText;
    private static Image _keyImage;

    private void Awake()
    {
        var texts = GetComponentsInChildren<TMP_Text>(true);
        _nameText = texts[0];
        _keyText = texts[1];

        _keyImage = GetComponentInChildren<Image>(true);
    }

    private void Start()
    {
        Hide();
    }

    public static void Show(string text)
    {
        _nameText.gameObject.SetActive(true);
        _nameText.text = text;
        _keyText.text = System.Enum.GetName(
            typeof(KeyCode), 
            PlayerPrefs.GetInt(Constants.Keys.INTERACT, (int)KeyCode.F)
        );
        _keyImage.gameObject.SetActive(true);
    }

    public static void Hide()
    {
        _nameText.gameObject.SetActive(false);
        _keyImage.gameObject.SetActive(false);
    }

}
