namespace FishRPG.UI
{
    using UnityEngine;

    public class UISpells : UiPanel
    {

        [Header("UI Spells")]
        [SerializeField] private UISpellSlot _slotPrefab = null;
        [SerializeField] private Transform _scrollViewContent = null;

        protected override void UiManager_OnPlayerAssigned()
        {
            base.UiManager_OnPlayerAssigned();
            foreach (var item in UiManager.Player.Class.StartingSpells)
            {
                var slot = Instantiate(_slotPrefab, _scrollViewContent);
                slot.Setup(item);
            }
        }

    }
}
