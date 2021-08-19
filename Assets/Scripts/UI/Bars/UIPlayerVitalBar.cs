using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIPlayerVitalBar : MonoBehaviour
{

    [SerializeField] private Image _fillImage = null;

    [Header("Numbers")]
    [SerializeField] private bool _showNumbers = true;
    [SerializeField] private TMP_Text _numberText = null;

    private VitalBase _vital;

    private void OnDestroy()
    {
        if (_vital)
            _vital.OnVitalChanged -= Vital_OnVitalChanged;
    }

    public void Setup(VitalBase vital)
    {
        if (UiManager.Player != null)
        {
            _vital = vital;
            _vital.OnVitalChanged += Vital_OnVitalChanged;

            if (_vital is Health)
                _fillImage.color = Color.red;
            else if (_vital is Mana)
                _fillImage.color = Color.cyan;

            UpdateBar();
        }
        else
        {
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
