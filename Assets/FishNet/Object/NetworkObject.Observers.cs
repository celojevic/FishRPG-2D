using FishNet.Connection;
using FishNet.Observering;
using System.Collections.Generic;
using UnityEngine;

namespace FishNet.Object
{
    public partial class NetworkObject : MonoBehaviour
    {
        #region Public.
        /// <summary>
        /// True if this NetworkObject uses the observer system.
        /// </summary>
        public bool UsingObservers => (_networkObserver != null);
        /// <summary>
        /// Clients which can get messages from this NetworkObject.
        /// </summary>
        public HashSet<NetworkConnection> Observers = new HashSet<NetworkConnection>();
        #endregion

        #region Private.
        /// <summary>
        /// NetworkObserver on this object. May be null if not using observers.
        /// </summary>
        private NetworkObserver _networkObserver = null;
        #endregion

        /// <summary>
        /// Initializes this script for use.
        /// </summary>
        private void PreInitializeObservers()
        {
            _networkObserver = GetComponent<NetworkObserver>();
            if (_networkObserver != null)
                _networkObserver.PreInitialize(this);
        }

        /// <summary>
        /// Removes a connection from observers for this object.
        /// </summary>
        /// <param name="connection"></param>
        internal bool RemoveObserver(NetworkConnection connection)
        {
            return Observers.Remove(connection);
        }

        /// <summary>
        /// Adds the connection to observers if conditions are met.
        /// </summary>
        /// <param name="connection"></param>
        /// <returns>True if added to Observers.</returns>
        internal ObserverStateChange RebuildObservers(NetworkConnection connection)
        {
            //Not using observer system, this object is seen by everything.
            if (!UsingObservers)
                return ObserverStateChange.Added;

            ObserverStateChange osc = _networkObserver.RebuildObservers(connection);
            if (osc == ObserverStateChange.Added)
                Observers.Add(connection);
            else if (osc == ObserverStateChange.Removed)
                Observers.Remove(connection);

            return osc;
        }

    }

}

