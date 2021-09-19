namespace FishRPG.UI
{
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

        internal void OnStart()
        {
            _panel.transform.position = PlayerPrefs.GetString(this.name, _panel.transform.position.ToString()).ToVector3();
            _panel.SetActive(false);
            UiManager.OnPlayerAssigned += UiManager_OnPlayerAssigned;
        }

        internal void OnStop()
        {
            PlayerPrefs.SetString(this.name, _panel.transform.position.ToString());
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
}
