﻿using FishNet.Connection;
using FishNet.Managing;
using System;
using UnityEngine;

namespace FishNet.Authenticating
{

    public abstract class Authenticator : MonoBehaviour
    {
        #region Protected.
        /// <summary>
        /// NetworkManager for this Authenticator.
        /// </summary>
        protected NetworkManager NetworkManager { get; private set; }
        #endregion


        /// <summary>
        /// Called when authenticator has concluded a result for a connection. Boolean is true if authentication passed, false if failed.
        /// Server listens for this event automatically.
        /// </summary>
        public abstract event Action<NetworkConnection, bool> OnAuthenticationResult;

        /// <summary>
        /// Initializes this script for use.
        /// </summary>
        /// <param name="networkManager"></param>
        public virtual void FirstInitialize(NetworkManager networkManager) 
        {
            NetworkManager = networkManager;
        }

        /// <summary>
        /// Called on the server immediately after a client connects. Can be used to send data to the client for authentication.
        /// </summary>
        /// <param name="connection">Connection which is not yet authenticated.</param>
        public virtual void OnRemoteConnection(NetworkConnection connection) { }
    }


}