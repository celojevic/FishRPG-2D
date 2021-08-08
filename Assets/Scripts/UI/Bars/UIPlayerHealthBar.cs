using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIPlayerHealthBar : MonoBehaviour
{

    [SerializeField] private Image _healthFill = null;
    private Health _health;

    private void Awake()
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
        _health = (Health)UiManager.Player.GetVital(VitalType.Health);
        UpdateHealthBar();
    }

    void UpdateHealthBar()
    {
        _healthFill.fillAmount = _health.Percent;
    }

}
