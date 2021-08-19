using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections.Generic;
using UnityEngine;

namespace FishNet.Example.Basic
{
    public class Player : NetworkBehaviour
    {
        // Events that the UI will subscribe to
        public event System.Action<int> OnPlayerNumberChanged;
        public event System.Action<Color32> OnPlayerColorChanged;
        public event System.Action<int> OnPlayerDataChanged;

        // Players List to manage playerNumber
        internal static readonly List<Player> playersList = new List<Player>();

        internal static void ResetPlayerNumbers()
        {
            int playerNumber = 0;
            foreach (Player player in playersList)
            {
                player.PlayerNumber = playerNumber++;
            }
        }

        [Header("Player UI")]
        public GameObject PlayerUIPrefab;
        private GameObject _playerUI;

        [Header("SyncVars")]

        /// <summary>
        /// This is appended to the player name text, e.g. "Player 01"
        /// </summary>
        [SyncVar(OnChange = nameof(PlayerNumberChanged))]
        public int PlayerNumber = 0;

        /// <summary>
        /// This is updated by UpdateData which is called from OnStartServer via InvokeRepeating
        /// </summary>
        [SyncVar(OnChange = nameof(PlayerDataChanged))]
        public int PlayerData = 0;

        /// <summary>
        /// Random color for the playerData text, assigned in OnStartServer
        /// </summary>
        [SyncVar(OnChange = nameof(PlayerColorChanged))]
        public Color32 playerColor = Color.white;

        // This is called by the hook of playerNumber SyncVar above
        private void PlayerNumberChanged(int _, int newPlayerNumber, bool asServer)
        {
            OnPlayerNumberChanged?.Invoke(newPlayerNumber);
        }

        // This is called by the hook of playerData SyncVar above
        private void PlayerDataChanged(int _, int newPlayerData, bool asServer) 
        {
            OnPlayerDataChanged?.Invoke(newPlayerData);
        }

        // This is called by the hook of playerColor SyncVar above
        private void PlayerColorChanged(Color32 _, Color32 newPlayerColor, bool asServer) //todo these hook methods arent throwing when bool is missing.
        {
            OnPlayerColorChanged?.Invoke(newPlayerColor);
        }

        /// <summary>
        /// Called on the server after initializing this object.
        /// SyncTypes modified before or during this method will be sent to clients in the spawn message.
        /// </summary>
        public override void OnStartServer()
        {
            base.OnStartServer();

            // Add this to the static Players List
            playersList.Add(this);

            // set the Player Color SyncVar
            playerColor = Random.ColorHSV(0f, 1f, 0.9f, 0.9f, 1f, 1f);

            // Start generating updates
            InvokeRepeating(nameof(UpdateData), 1, 1);
        }

        /// <summary>
        /// Called on the server before deinitializing this object.
        /// </summary>
        public override void OnStopServer()
        {
            CancelInvoke();
            playersList.Remove(this);
        }

        // This only runs on the server, called from OnStartServer via InvokeRepeating
        [Server]
        void UpdateData()
        {
            PlayerData = Random.Range(100, 1000);
        }

        /// <summary>
        /// Called on every NetworkBehaviour when it is activated on a client.
        /// <para>Objects on the host have this function called, as there is a local client on the host. The values of SyncVars on object are guaranteed to be initialized correctly with the latest state from the server when this function is called on the client.</para>
        /// </summary>
        public override void OnStartClient(bool isOwner)
        {
            // Activate the main panel
            ((BasicNetworkManager)InstanceFinder.NetworkManager).MainPanel.gameObject.SetActive(true);
            // Instantiate the player UI as child of the Players Panel
            _playerUI = Instantiate(PlayerUIPrefab, ((BasicNetworkManager)InstanceFinder.NetworkManager).PlayersPanel);

            // Set this player object in PlayerUI to wire up event handlers
            _playerUI.GetComponent<PlayerUI>().SetPlayer(this);

            // Invoke all event handlers with the current data
            OnPlayerNumberChanged.Invoke(PlayerNumber);
            OnPlayerColorChanged.Invoke(playerColor);
            OnPlayerDataChanged.Invoke(PlayerData);
        }

        /// <summary>
        /// This is invoked on clients when the server has caused this object to be destroyed.
        /// <para>This can be used as a hook to invoke effects or do client specific cleanup.</para>
        /// </summary>
        public override void OnStopClient(bool isOwner)
        {
            // Remove this player's UI object
            Destroy(_playerUI);

            // Disable the main panel for local player
            if (isOwner)
              ((BasicNetworkManager)InstanceFinder.NetworkManager).MainPanel.gameObject.SetActive(false);
        }
    }
}
