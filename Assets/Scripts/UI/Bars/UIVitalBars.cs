using UnityEngine;

public class UIVitalBars : MonoBehaviour
{

    [SerializeField] private UIPlayerVitalBar _prefab = null;

    private void Start()
    {
        UiManager.OnPlayerAssigned += UiManager_OnPlayerAssigned;
    }

    private void OnDestroy()
    {
        UiManager.OnPlayerAssigned -= UiManager_OnPlayerAssigned;
    }

    private void UiManager_OnPlayerAssigned()
    {
        UiManager.OnPlayerAssigned -= UiManager_OnPlayerAssigned;
        for (int i = 0; i < UiManager.Player.Vitals.Length; i++)
        {
            Instantiate(_prefab, transform).Setup(UiManager.Player.Vitals[i]);
        }
    }

}
