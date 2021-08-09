using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIPlayerVitalBar : MonoBehaviour
{

    [SerializeField] private VitalType _vitalType;
    [SerializeField] private Image _fillImage = null;

    [Header("Numbers")]
    [SerializeField] private bool _showNumbers = true;
    [SerializeField] private TMP_Text _numberText = null;

    private VitalBase _vital;

    private void Awake()
    {
        UiManager.OnPlayerAssigned += UiManager_OnPlayerAssigned;
    }

    private void OnDestroy()
    {
        UiManager.OnPlayerAssigned -= UiManager_OnPlayerAssigned;
        _vital.OnVitalChanged -= Vital_OnVitalChanged;
    }

    private void UiManager_OnPlayerAssigned()
    {
        if (UiManager.Player != null)
        {
            _vital = UiManager.Player.GetVital(_vitalType);
            _vital.OnVitalChanged += Vital_OnVitalChanged;

            UpdateBar();
        }
        else
        {
            UiManager.OnPlayerAssigned -= UiManager_OnPlayerAssigned;
            _vital.OnVitalChanged -= Vital_OnVitalChanged;
        }
    }

    private void Vital_OnVitalChanged()
    {
        UpdateBar();
    }

    void UpdateBar()
    {
        _fillImage.fillAmount = _vital.Percent;
        if (_showNumbers && _numberText)
            _numberText.text = $"{_vital.CurrentVital} / {_vital.MaxVital}";
    }

}
