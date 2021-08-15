using UnityEngine;

public class UiPanel : MonoBehaviour
{

    [Header("UI Panel")]
    [SerializeField] private GameObject _panel = null;
    [SerializeField] private KeyCode _key = KeyCode.None;

    /// <summary>
    /// Sets the panel inactive and subscribes to events. 
    /// Must call base method in overrides.
    /// </summary>
    protected virtual void Start()
    {
        _panel.SetActive(false);
        UiManager.OnPlayerAssigned += UiManager_OnPlayerAssigned;
    }

    private void OnDestroy()
    {
        UiManager.OnPlayerAssigned -= UiManager_OnPlayerAssigned;
    }

    /// <summary>
    /// Unsubscribes from event. 
    /// Must call base method in overrides.
    /// </summary>
    protected virtual void UiManager_OnPlayerAssigned()
    {
        UiManager.OnPlayerAssigned -= UiManager_OnPlayerAssigned;
    }

    /// <summary>
    /// Checks for input key to toggle the panel.
    /// </summary>
    private void Update()
    {
        if (Input.GetKeyDown(_key))
        {
            _panel.SetActive(!_panel.activeInHierarchy);
        }
    }

}
