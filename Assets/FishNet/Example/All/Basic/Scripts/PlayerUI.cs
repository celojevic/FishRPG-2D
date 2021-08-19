using UnityEngine;
using UnityEngine.UI;

namespace FishNet.Example.Basic
{
    public class PlayerUI : MonoBehaviour
    {
        [Header("Player Components")]
        public Image image;

        [Header("Child Text Objects")]
        public Text PlayerNameText;
        public Text PlayerDataText;

        Player _player;

        /// <summary>
        /// Caches the controlling Player object, subscribes to its events
        /// </summary>
        /// <param name="player">Player object that controls this UI</param>
        public void SetPlayer(Player player)
        {
            // cache reference to the player that controls this UI object
            this._player = player;

            // subscribe to the events raised by SyncVar Hooks on the Player object
            player.OnPlayerNumberChanged += OnPlayerNumberChanged;
            player.OnPlayerColorChanged += OnPlayerColorChanged;
            player.OnPlayerDataChanged += OnPlayerDataChanged;

            // add a visual background for the local player in the UI
            if (player.IsOwner)
                image.color = new Color(1f, 1f, 1f, 0.1f);
        }

        private void OnDisable()
        {
            _player.OnPlayerNumberChanged -= OnPlayerNumberChanged;
            _player.OnPlayerColorChanged -= OnPlayerColorChanged;
            _player.OnPlayerDataChanged -= OnPlayerDataChanged;
        }

        // This value can change as clients leave and join
        private void OnPlayerNumberChanged(int newPlayerNumber)
        {
            PlayerNameText.text = string.Format("Player {0:00}", newPlayerNumber);
        }

        // Random color set by Player::OnStartServer
        private void OnPlayerColorChanged(Color32 newPlayerColor)
        {
            PlayerNameText.color = newPlayerColor;
        }

        // This updates from Player::UpdateData via InvokeRepeating on server
        private void OnPlayerDataChanged(int newPlayerData)
        {
            // Show the data in the UI
            PlayerDataText.text = string.Format("Data: {0:000}", newPlayerData);
        }

    }
}
