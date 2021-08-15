using UnityEngine;

/// <summary>
/// Base class for all UI panels that are opened with the press of a key.
/// Must be childed to a UI Manager canvas object.
/// </summary>
public class UiPanel : MonoBehaviour
{

    [Header("UI Panel")]
    [SerializeField] private GameObject _panel = null;
    [SerializeField] private KeyCode _key = KeyCode.None;

    /// <summary>
    /// Sets the panel inactive and subscribes to events. 
    /// Must call base method in overrides.
    /// </summary>
    internal void OnStart()
    {
        _panel.SetActive(false);
        UiManager.OnPlayerAssigned += UiManager_OnPlayerAssigned;
    }

    internal void OnStop()
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
    internal void CheckKeyDown()
    {
        if (Input.GetKeyDown(_key))
        {
            _panel.SetActive(!_panel.activeInHierarchy);
            OnPanelActivation(_panel.activeInHierarchy);
        }
    }

    protected virtual void OnPanelActivation(bool isActive) { }

}
