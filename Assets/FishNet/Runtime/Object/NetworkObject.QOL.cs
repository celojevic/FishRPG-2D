﻿using FishNet.Connection;
using UnityEngine;

namespace FishNet.Object
{
    public partial class NetworkObject : MonoBehaviour
    {
        #region Public.
        /// <summary>
        /// True if the client is running and authenticated.
        /// </summary>
        public bool IsClient => NetworkManager.IsClient;
        /// <summary>
        /// True if client only.
        /// </summary>
        public bool IsClientOnly => (!IsServer && IsClient);
        /// <summary>
        /// True if the server is running.
        /// </summary>
        public bool IsServer => NetworkManager.IsServer;
        /// <summary>
        /// True if server only.
        /// </summary>
        public bool IsServerOnly => (IsServer && !IsClient);
        /// <summary>
        /// True if client and server are active.
        /// </summary>
        public bool IsHost => (IsServer && IsClient);
        /// <summary>
        /// True if the owner of this object. Only contains value on clients.
        /// </summary>
        public bool IsOwner => (NetworkManager == null || !IsClient) ? false : (NetworkManager.ClientManager.Connection == Owner);
        /// <summary> 
        /// True if the owner is a valid connection.
        /// </summary>
        public bool OwnerIsValid => (Owner == null) ? false : (Owner.IsValid);
        /// <summary>
        /// ClientId for this NetworkObject owner. Only visible to server.
        /// </summary>
        public int OwnerId => (!OwnerIsValid) ? -1 : Owner.ClientId;
        /// <summary>
        /// Returns if this object is spawned.
        /// </summary>
        public bool IsSpawned => (!Deinitializing && ObjectId >= 0);
        #endregion

        /// <summary>
        /// Despawns this NetworkObject. Only call from the server.
        /// </summary>
        public void Despawn()
        {
            if (!CanSpawnOrDespawn(true))
                return;

            NetworkManager.ServerManager.Despawn(this);
        }
        /// <summary>
        /// Spawns an object over the network. Only call from the server.
        /// </summary>
        /// <param name="networkObject"></param>
        public void Spawn(NetworkObject networkObject, NetworkConnection ownerConnection = null)
        {
            if (!CanSpawnOrDespawn(true))
                return;
            NetworkManager.ServerManager.Spawn(networkObject, ownerConnection);
        }
        /// <summary>
        /// Spawns an object over the network. Only call from the server.
        /// </summary>
        public void Spawn(GameObject go, NetworkConnection ownerConnection = null)
        {
            if (!CanSpawnOrDespawn(true))
                return;
            NetworkManager.ServerManager.Spawn(go, ownerConnection);
        }

        /// <summary>
        /// Returns if Spawn or Despawn can be called.
        /// </summary>
        /// <param name="warn">True to warn if not able to execute spawn or despawn.</param>
        /// <returns></returns>
        internal bool CanSpawnOrDespawn(bool warn)
        {
            bool canExecute = true;

            if (NetworkManager == null)
            {
                canExecute = false;
                if (warn)
                    Debug.LogWarning($"Cannot despawn {gameObject.name}, NetworkManager reference is null. This may occur if the object is not spawned or initialized.");
            }
            else if (!IsServer)
            {
                canExecute = false;
                if (warn)
                    Debug.LogWarning($"Cannot spawn or despawn {gameObject.name}, server is not active.");
            }
            else if (Deinitializing)
            {
                canExecute = false;
                if (warn)
                    Debug.LogWarning($"Cannot despawn {gameObject.name}, it is already deinitializing.");
            }

            return canExecute;
        }

    }

}

