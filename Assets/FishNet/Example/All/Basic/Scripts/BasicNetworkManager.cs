using FishNet.Component.Spawning;
using FishNet.Managing;
using FishNet.Object;
using FishNet.Transporting;
using UnityEngine;

namespace FishNet.Example.Basic
{
    [AddComponentMenu("")]
    public class BasicNetworkManager : NetworkManager
    {
        [Header("Canvas UI")]

        [Tooltip("Assign Main Panel so it can be turned on from Player:OnStartClient")]
        public RectTransform MainPanel;

        [Tooltip("Assign Players Panel for instantiating PlayerUI as child")]
        public RectTransform PlayersPanel;


        protected override void Awake()
        {
            base.Awake();
            base.TransportManager.Transport.OnRemoteConnectionState += Transport_OnRemoteConnectionState;

            PlayerSpawner ps = gameObject.GetComponent<PlayerSpawner>();
            ps.OnSpawned += playerSpawner_OnSpawned;
        }

        /// <summary>
        /// Called when server spawns a player.
        /// </summary>
        private void playerSpawner_OnSpawned(NetworkObject obj)
        {
            Player.ResetPlayerNumbers();
        }

        /// <summary>
        /// Called when a connection state changes for a remote client.
        /// </summary>
        private void Transport_OnRemoteConnectionState(RemoteConnectionStateArgs obj)
        {
            if (obj.ConnectionState == RemoteConnectionStates.Stopped)
                Player.ResetPlayerNumbers();
        }

    }
}
