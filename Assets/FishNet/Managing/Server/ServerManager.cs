using FishNet.Managing.Server.Object;
using FishNet.Connection;
using FishNet.Serializing;
using FishNet.Transporting;
using System;
using System.Collections.Generic;
using UnityEngine;


namespace FishNet.Managing.Server
{
    [DisallowMultipleComponent]
    public partial class ServerManager : MonoBehaviour
    {
        #region Public.
        /// <summary>
        /// Called after a client completes authentication. This invokes before OnClientAuthenticated so FishNet may run operations on authenticated clients before user code does.
        /// </summary>
        internal event Action<NetworkConnection> OnClientAuthenticatedInternal;
        /// <summary>
        /// Called after a client completes authentication.
        /// </summary>
        public event Action<NetworkConnection> OnClientAuthenticated;
        /// <summary>
        /// True if the server is running.
        /// </summary>
        public bool Active { get; private set; } = false;
        /// <summary>
        /// Connected clients which have authenticated.
        /// </summary>
        [HideInInspector]
        public HashSet<int> AuthenticatedClients = new HashSet<int>();
        /// <summary>
        /// ObjectHandler for server objects.
        /// </summary>
        public ServerObjects Objects { get; private set; } = null;
        /// <summary>
        /// Connected clients.
        /// </summary>
        [HideInInspector]
        public Dictionary<int, NetworkConnection> Clients = new Dictionary<int, NetworkConnection>();
        /// <summary>
        /// NetworkManager for server.
        /// </summary>
        [HideInInspector]
        public NetworkManager NetworkManager = null;
        #endregion

        #region Serialized.
        /// <summary>
        /// 
        /// </summary>
        [Tooltip("True to share current owner of objects with all clients. False to hide owner of objects from everyone but owner.")]
        [SerializeField]
        private bool _shareOwners = true;
        /// <summary>
        /// True to share current owner of objects with all clients. False to hide owner of objects from everyone but owner.
        /// </summary>
        internal bool ShareOwners => _shareOwners;

        #endregion

        /// <summary>
        /// Initializes this script for use.
        /// </summary>
        /// <param name="manager"></param>
        internal void Initialize(NetworkManager manager)
        {
            NetworkManager = manager;
            Objects = new ServerObjects(manager);
            //Unsubscrive first incase already subscribed.
            SubscribeToTransport(false);
            SubscribeToTransport(true);
        }


        /// <summary>
        /// Changes subscription status to transport.
        /// </summary>
        /// <param name="subscribe"></param>
        private void SubscribeToTransport(bool subscribe)
        {
            if (NetworkManager == null || NetworkManager.TransportManager == null || NetworkManager.TransportManager.Transport == null)
                return;

            if (!subscribe)
            {
                NetworkManager.TransportManager.Transport.OnServerReceivedData -= Transport_OnServerReceivedData;
                NetworkManager.TransportManager.Transport.OnServerConnectionState -= Transport_OnServerConnectionState;
                NetworkManager.TransportManager.Transport.OnRemoteConnectionState -= Transport_OnRemoteConnectionState;
            }
            else
            {
                NetworkManager.TransportManager.Transport.OnServerReceivedData += Transport_OnServerReceivedData;
                NetworkManager.TransportManager.Transport.OnServerConnectionState += Transport_OnServerConnectionState;
                NetworkManager.TransportManager.Transport.OnRemoteConnectionState += Transport_OnRemoteConnectionState;
            }
        }

        /// <summary>
        /// Called when a connection state changes local server.
        /// </summary>
        /// <param name="args"></param>
        private void Transport_OnServerConnectionState(ServerConnectionStateArgs args)
        {
            Active = (args.ConnectionState == LocalConnectionStates.Started);
            Objects.OnServerConnectionState(args);
            //If not connected then clear clients.
            if (args.ConnectionState != LocalConnectionStates.Started)
                Clients.Clear();
            
            if (args.ConnectionState == LocalConnectionStates.Started)
                Debug.Log("Server started."); //tmp.
            else if (args.ConnectionState == LocalConnectionStates.Stopped)
                Debug.Log("Server stopped."); //tmp.
        }

        /// <summary>
        /// Called when a connection state changes for a remote client.
        /// </summary>
        private void Transport_OnRemoteConnectionState(RemoteConnectionStateArgs args)
        {
            //If connection state is for a remote client.
            if (args.ConnectionId >= 0)
            {
                //If started then add to authenticated clients.
                if (args.ConnectionState == RemoteConnectionStates.Started)
                {
                    Debug.Log($"Remote connection started for Id {args.ConnectionId}."); //tmp.

                    NetworkConnection conn = new NetworkConnection(NetworkManager, args.ConnectionId);
                    /* Immediately send connectionId to client. Some transports
                     * don't give clients their remoteId, therefor it has to be sent
                     * by the ServerManager. This packet is very simple and can be built
                     * on the spot. */
                    SendConnectionId(conn);
                    AuthenticatedClients.Add(args.ConnectionId);
                    //Clients[args.ConnectionId] = conn;
                    Clients.Add(args.ConnectionId, conn);
                    ClientAuthenticated(conn);
                }
                //If stopping.
                else if (args.ConnectionState == RemoteConnectionStates.Stopped)
                {
                    /* If client's connection is found then clean
                     * them up from server. */
                    if (Clients.TryGetValue(args.ConnectionId, out NetworkConnection conn))
                    {
                        AuthenticatedClients.Remove(args.ConnectionId);
                        Clients.Remove(args.ConnectionId);
                        Objects.ClientDisconnected(conn);
                        conn.Reset();

                        Debug.Log($"Remote connection stopped for Id {args.ConnectionId}."); //tmp.
                    }
                }
            }
        }

        /// <summary>
        /// Sends client their connectionId.
        /// </summary>
        /// <param name="connectionid"></param>
        private void SendConnectionId(NetworkConnection conn)
        {
            using (PooledWriter writer = WriterPool.GetWriter())
            {
                writer.WriteByte((byte)PacketId.ConnectionId);
                writer.WriteInt32(conn.ClientId);
                NetworkManager.TransportManager.SendToClient((byte)Channel.Reliable, writer.GetArraySegment(), conn);
            }
        }
        /// <summary>
        /// Called when the server socket receives data.
        /// </summary>
        private void Transport_OnServerReceivedData(ServerReceivedDataArgs args)
        {
            DataReceived(args);
        }

        /// <summary>
        /// Called when the server receives data.
        /// </summary>
        /// <param name="args"></param>
        private void DataReceived(ServerReceivedDataArgs args)
        {
            //Not from a valid connection.
            if (args.ConnectionId < 0)
                return;
            //User isn't authenticated. //muchlater check if authentication packet before exiting.
            if (!AuthenticatedClients.Contains(args.ConnectionId))
                return;
            ArraySegment<byte> segment = args.Data;
            if (segment.Count == 0)
                return;

            PacketId packetId = PacketId.Unset;
            try
            {
                //PooledReader reader = ReaderPool.GetReader(segment, NetworkManager);
                using (PooledReader reader = ReaderPool.GetReader(segment, NetworkManager))
                {
                    while (reader.Remaining > 0)
                    {
                        packetId = (PacketId)reader.ReadByte();
                        if (packetId == PacketId.ServerRpc)
                        {
                            Objects.ParseServerRpc(reader, args.ConnectionId);
                        }
                        else if (packetId == PacketId.Broadcast)
                        {
                            ParseBroadcast(reader, args.ConnectionId);
                        }
                        else
                        {
                            Debug.LogError($"Unhandled PacketId of {(byte)packetId}. Remaining data has been purged.");
                            return;
                        }
                    }
                }
            }
            //catch (Exception e)
            //{
            //    Debug.LogError($"Error parsing data. PacketId {packetId}. {e.Message}.");
            //}
            finally { }

        }

        /// <summary>
        /// Called when a remote client authenticates with the server.
        /// </summary>
        /// <param name="connectionId"></param>
        private void ClientAuthenticated(NetworkConnection connection)
        {
            connection.ConnectionAuthenticated();
            Objects.ClientAuthenticated(connection);
            OnClientAuthenticatedInternal?.Invoke(connection);
            OnClientAuthenticated?.Invoke(connection);
        }

    }


}
