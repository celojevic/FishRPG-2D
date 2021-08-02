using FishNet.Connection;
using UnityEngine;

namespace FishNet.Object
{

    public abstract partial class NetworkBehaviour : MonoBehaviour
    {
        #region Private.
        /// <summary>
        /// True if using OnStartServer callback.
        /// </summary>
        private bool _usingOnStartServer = false;
        /// <summary>
        /// True if using OnStopServer callback.
        /// </summary>
        private bool _usingOnStopServer = false;
        /// <summary>
        /// True if using OnOwnershipServer callback.
        /// </summary>
        private bool _usingOnOwnershipServer = false;
        /// <summary>
        /// True if using OnSpawnSent callback.
        /// </summary>
        private bool _usingOnSpawnServer = false;
        /// <summary>
        /// True if using OnDespawnServer callback.
        /// </summary>
        private bool _usingOnDespawnServer = false;
        /// <summary>
        /// True if using OnStartClient callback.
        /// </summary>
        private bool _usingOnStartClient = false;
        /// <summary>
        /// True if using OnStopClient callback.
        /// </summary>
        private bool _usingOnStopClient = false;
        /// <summary>
        /// True if using OnOwnershipClient callback.
        /// </summary>
        private bool _usingOnOwnershipClient = false;
        #endregion

        /// <summary>
        /// Called on the server after initializing this object.
        /// SyncTypes modified before or during this method will be sent to clients in the spawn message.
        /// </summary>
        public virtual void OnStartServer() { }
        private void NetworkObject_OnStartServer() { OnStartServer(); }
        protected void UsingOnStartServerInternal() { _usingOnStartServer = true; }
        /// <summary>
        /// Called on the server before deinitializing this object.
        /// </summary>
        public virtual void OnStopServer() { }
        private void NetworkObject_OnStopServer() { OnStopServer(); }
        protected void UsingOnStopServerInternal() { _usingOnStopServer = true; }
        /// <summary>
        /// Called on the server after ownership has changed.
        /// </summary>
        /// <param name="currentOwner">Current owner of this object.</param>
        public virtual void OnOwnershipServer(NetworkConnection currentOwner) { }
        private void NetworkObject_OnOwnershipServer(NetworkConnection currentOwner) { OnOwnershipServer(currentOwner); }
        protected void UsingOnOwnershipServerInternal() { _usingOnOwnershipServer = true; }
        /// <summary>
        /// Called on the server after a spawn message for this object has been sent to clients.
        /// Useful for sending remote calls or actions to clients .
        /// </summary>
        public virtual void OnSpawnServer(NetworkConnection connection) { }
        private void NetworkObject_OnSpawnServer(NetworkConnection connection) { OnSpawnServer(connection); }
        protected void UsingOnSpawnServerInternal() { _usingOnSpawnServer = true; }
        /// <summary>
        /// Called on the server before a despawn message for this object has been sent to clients.
        /// Useful for sending remote calls or actions to clients.
        /// </summary>
        public virtual void OnDespawnServer(NetworkConnection connection) { }
        private void NetworkObject_OnDespawnServer(NetworkConnection connection) { OnDespawnServer(connection); }
        protected void UsingOnDespawnServerInternal() { _usingOnDespawnServer = true; }
        /// <summary>
        /// Called on the client after initializing this object.
        /// </summary>
        /// <param name="isOwner">True if the owner of this object.</param>
        public virtual void OnStartClient(bool isOwner) { }
        private void NetworkObject_OnStartClient(bool isOwner) { OnStartClient(isOwner); }
        protected void UsingOnStartClientInternal() { _usingOnStartClient = true; }
        /// <summary>
        /// Called on the client before deinitializing this object.
        /// </summary>
        /// <param name="isOwner">True if the owner of this object.</param>
        public virtual void OnStopClient(bool isOwner) { }
        private void NetworkObject_OnStopClient(bool isOwner) { OnStopClient(isOwner); }
        protected void UsingOnStopClientInternal() { _usingOnStopClient = true; }
        /// <summary>
        /// Called on the client after gaining or losing ownership.
        /// </summary>
        /// <param name="newOwner">Current owner of this object.</param>
        public virtual void OnOwnershipClient(NetworkConnection newOwner) { }
        private void NetworkObject_OnOwnershipClient(NetworkConnection newOwner) { OnOwnershipClient(newOwner); }
        protected void UsingOnOwnershipClientInternal() { _usingOnOwnershipClient = true; }


        /// <summary>
        /// PreInitializes this script.
        /// </summary>
        /// <param name="networkManager"></param>
        private void PreInitializeCallbacks(NetworkObject networkObject)
        {
            if (_usingOnStartServer)
                networkObject.OnStartServer += NetworkObject_OnStartServer;

            if (_usingOnStopServer)
                networkObject.OnStopServer += NetworkObject_OnStopServer;

            if (_usingOnOwnershipServer)
                networkObject.OnOwnershipServer += NetworkObject_OnOwnershipServer;

            if (_usingOnSpawnServer)
                networkObject.OnSpawnServer += NetworkObject_OnSpawnServer;
            if (_usingOnDespawnServer)
                networkObject.OnDespawnServer += NetworkObject_OnDespawnServer;

            if (_usingOnStartClient)
                networkObject.OnStartClient += NetworkObject_OnStartClient;

            if (_usingOnStopClient)
                networkObject.OnStopClient += NetworkObject_OnStopClient;

            if (_usingOnOwnershipClient)
                networkObject.OnOwnershipClient += NetworkObject_OnOwnershipClient;
        }

    }


}