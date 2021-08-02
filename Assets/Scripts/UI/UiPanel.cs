using UnityEngine;

public class UiPanel : MonoBehaviour
{

    [Header("UI Panel")]
    [SerializeField] private GameObject _panel = null;
    [SerializeField] private KeyCode _key = KeyCode.None;

    private void Start()
    {
        _panel.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(_key))
        {
            _panel.SetActive(!_panel.activeInHierarchy);
        }
    }

}
