using UnityEngine;

namespace FishNet.Object
{


    public abstract partial class NetworkBehaviour : MonoBehaviour
    {
        #region Debug //debug
        //[HideInInspector]
        //public string GivenName;
        //public void SetGivenName(string s) => GivenName = s;
        #endregion

        /// <summary>
        /// Returns if this NetworkBehaviour is spawned.
        /// </summary>
        public bool IsSpawned => (NetworkObject != null && NetworkObject.IsSpawned);

        /// <summary>
        /// ComponentIndex for this NetworkBehaviour.
        /// </summary>
        public byte ComponentIndex { get; private set; } = 0;
        /// <summary>
        /// NetworkObject this behaviour is for.
        /// </summary>        
        public NetworkObject NetworkObject { get; private set; } = null;


        /// <summary>
        /// Prepares this script for initialization.
        /// </summary>
        /// <param name="networkObject"></param>
        /// <param name="componentIndex"></param>
        public void PreInitialize(NetworkObject networkObject, byte componentIndex)
        {
            NetworkObject = networkObject;
            ComponentIndex = componentIndex;
            PreInitializeSyncTypes(networkObject);
            PreInitializeCallbacks(networkObject);
        }

    }


}