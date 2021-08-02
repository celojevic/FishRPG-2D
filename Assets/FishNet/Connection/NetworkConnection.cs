using FishNet.Managing;
using FishNet.Object;
using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace FishNet.Connection
{

    public partial class NetworkConnection : IEquatable<NetworkConnection>
    {

        #region Public.
        /// <summary>
        /// NetworkManager managing this class.
        /// </summary>
        public NetworkManager NetworkManager { get; private set; } = null;
        /// <summary>
        /// True if this connection is authenticated.
        /// </summary>
        public bool Authenticated { get; private set; } = false;
        /// <summary>
        /// True if this connection is valid.
        /// </summary>
        public bool IsValid => (ClientId >= 0);
        /// <summary>
        /// Unique Id for this connection.
        /// </summary>
        public int ClientId = -1;
        /// <summary>
        /// Objects owned by this connection.
        /// </summary>
        public HashSet<NetworkObject> Objects = new HashSet<NetworkObject>();
        /// <summary>
        /// Scenes this connection is in.
        /// </summary>
        public HashSet<Scene> Scenes = new HashSet<Scene>();
        /// <summary>
        /// True if this connection has loaded default networked scenes.
        /// </summary>
        internal bool LoadedNetworkedScenes = false;
        #endregion

        #region Comparers.
        public override bool Equals(object obj)
        {
            return this.Equals(obj as NetworkConnection);
        }
        public bool Equals(NetworkConnection nc)
        {
            if (nc is null)
                return false;
            if (System.Object.ReferenceEquals(this, nc))
                return true;
            if (this.GetType() != nc.GetType())
                return false;
            
            return (this.ClientId == nc.ClientId);
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        public static bool operator ==(NetworkConnection a, NetworkConnection b)
        {
            if (a is null && b is null)
                return true;
            if (a is null && !(b is null))
                return false;

            return (b == null) ? a.Equals(b) : b.Equals(a);
        }
        public static bool operator !=(NetworkConnection a, NetworkConnection b)
        {
            return !(a == b);
        }
        #endregion

        public NetworkConnection() { }
        public NetworkConnection(NetworkManager manager, int clientId)
        {
            Initialize(manager, clientId);
        }

        /// <summary>
        /// Initializes this for use.
        /// </summary>
        /// <param name="nm"></param>
        /// <param name="clientId"></param>
        private void Initialize(NetworkManager nm, int clientId)
        {
            NetworkManager = nm;
            ClientId = clientId;
            InitializeBuffer();
        }

        /// <summary>
        /// Resets this instance.
        /// </summary>
        internal void Reset()
        {
            ClientId = -1;
            Objects.Clear();
            Authenticated = false;
            NetworkManager = null;
        }

        /// <summary>
        /// Sets connection as authenticated.
        /// </summary>
        internal void ConnectionAuthenticated()
        {
            Authenticated = true;
        }

        /// <summary>
        /// Adds to Objects owned by this connection.
        /// </summary>
        /// <param name="nob"></param>
        internal void AddObject(NetworkObject nob)
        {
            Objects.Add(nob);
        }

        /// <summary>
        /// Removes from Objects owned by this connection.
        /// </summary>
        /// <param name="nob"></param>
        internal void RemoveObject(NetworkObject nob)
        {
            Objects.Remove(nob);
        }

    }


}