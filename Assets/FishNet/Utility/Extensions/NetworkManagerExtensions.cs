using FishNet.Managing;
using FishNet.Managing.Client;
using FishNet.Managing.Server;
using FishNet.Managing.Timing;
using FishNet.Managing.Transporting;
using UnityEngine;

namespace FishNet
{

    public static class InstanceFinder
    {

        #region Public.
        /// <summary>
        /// Returns the first found NetworkManager instance.
        /// </summary>
        public static NetworkManager NetworkManager
        {
            get
            {
                if (_networkManager == null)
                {
                    NetworkManager[] managers = GameObject.FindObjectsOfType<NetworkManager>();
                    if (managers.Length > 0)
                    {
                        if (managers.Length > 1)
                            Debug.LogWarning($"Multiple NetworkManagers found, the first result will be returned. If you only wish to have one NetworkManager then uncheck 'Allow Multiple' within your NetworkManagers.");

                        _networkManager = managers[0];
                    }
                    else
                    {
                        Debug.LogError($"NetworkManager not found in any open scenes.");
                    }
                }

                return _networkManager;
            }
        }

        /// <summary>
        /// Returns the first instance of ServerManager.
        /// </summary>
        public static ServerManager ServerManager
        {
            get
            {
                NetworkManager nm = NetworkManager;
                return (nm == null) ? null : nm.ServerManager;
            }
        }

        /// <summary>
        /// Returns the first instance of ClientManager.
        /// </summary>
        public static ClientManager ClientManager
        {
            get
            {
                NetworkManager nm = NetworkManager;
                return (nm == null) ? null : nm.ClientManager;
            }
        }

        /// <summary>
        /// Returns the first instance of TransportManager.
        /// </summary>
        public static TransportManager TransportManager
        {
            get
            {
                NetworkManager nm = NetworkManager;
                return (nm == null) ? null : nm.TransportManager;
            }
        }

        /// <summary>
        /// Returns the first instance of TransportManager.
        /// </summary>
        public static TimeManager TimeManager
        {
            get
            {
                NetworkManager nm = NetworkManager;
                return (nm == null) ? null : nm.TimeManager;
            }
        }
        #endregion

        #region Private.
        /// <summary>
        /// NetworkManager instance.
        /// </summary>
        private static NetworkManager _networkManager;
        #endregion


    }


}