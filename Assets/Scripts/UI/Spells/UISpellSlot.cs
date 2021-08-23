using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UISpellSlot : MonoBehaviour
{

    [SerializeField] private Image _iconImage = null;
    [SerializeField] private TMP_Text _nameText = null;

    public void Setup(SpellBase spell)
    {
        _iconImage.sprite = spell.Sprite;
        _nameText.text = spell.name;
    }

}
