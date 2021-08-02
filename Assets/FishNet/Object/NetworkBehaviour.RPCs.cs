using FishNet.Connection;
using FishNet.Serializing;
using FishNet.Transporting;
using System.Collections.Generic;
using UnityEngine;

namespace FishNet.Object
{


    public abstract partial class NetworkBehaviour : MonoBehaviour
    {
        #region Private.
        /// <summary>
        /// Registered ServerRpc methods.
        /// </summary>
        private readonly Dictionary<int, ServerRpcDelegate> _serverRpcDelegates = new Dictionary<int, ServerRpcDelegate>();
        /// <summary>
        /// Registered ObserversRpc methods.
        /// </summary>
        private readonly Dictionary<int, ClientRpcDelegate> _observersRpcDelegates = new Dictionary<int, ClientRpcDelegate>();
        /// <summary>
        /// Registered TargetRpc methods.
        /// </summary>
        private readonly Dictionary<int, ClientRpcDelegate> _targetRpcDelegates = new Dictionary<int, ClientRpcDelegate>();
        /// <summary>
        /// Number of total RPC methods for scripts in the same inheritance tree for this instance.
        /// </summary>
        private ushort _rpcMethodCount = 0;
        #endregion

        /// <summary>
        /// Registers a RPC method.
        /// </summary>
        /// <param name="rpcHash"></param>
        /// <param name="del"></param>
        public void CreateServerRpcDelegateInternal(int rpcHash, ServerRpcDelegate del)
        {
            _serverRpcDelegates[rpcHash] = del;
        }
        /// <summary>
        /// Registers a RPC method.
        /// </summary>
        /// <param name="rpcHash"></param>
        /// <param name="del"></param>
        public void CreateObserversRpcDelegateInternal(int rpcHash, ClientRpcDelegate del)
        {
            _observersRpcDelegates[rpcHash] = del;
        }
        /// <summary>
        /// Registers a RPC method.
        /// </summary>
        /// <param name="rpcHash"></param>
        /// <param name="del"></param>
        public void CreateTargetRpcDelegateInternal(int rpcHash, ClientRpcDelegate del)
        {
            _targetRpcDelegates[rpcHash] = del;
        }

        /// <summary>
        /// Sets number of RPCs for scripts in the same inheritance tree as this NetworkBehaviour.
        /// </summary>
        /// <param name="count"></param>
        public void SetRpcMethodCountInternal(ushort count)
        {
            _rpcMethodCount = count;
        }

        /// <summary>
        /// Called when a ServerRpc is received.
        /// </summary>
        internal void OnServerRpc(PooledReader reader, NetworkConnection senderClient)
        {
            /* If more than 255 rpc methods then read a ushort,
            * otherwise read a byte. */
            ushort methodHash = (_rpcMethodCount > byte.MaxValue) ?
                reader.ReadUInt16() : reader.ReadByte();

            if (senderClient == null)
            {
                Debug.LogError($"NetworkConnection is null. ServerRpc {methodHash} will not complete.");
                return;
            }

            if (_serverRpcDelegates.TryGetValue(methodHash, out ServerRpcDelegate del))
                del.Invoke(this, reader, senderClient);
            else
                Debug.LogWarning($"ServerRpc not found for hash {methodHash}.");
        }

        /// <summary>
        /// Called when an ObserversRpc is received.
        /// </summary>
        internal void OnObserversRpc(PooledReader reader)
        {
            /* If more than 255 rpc methods then read a ushort,
            * otherwise read a byte. */
            ushort methodHash = (_rpcMethodCount > byte.MaxValue) ?
                reader.ReadUInt16() : reader.ReadByte();

            if (_observersRpcDelegates.TryGetValue(methodHash, out ClientRpcDelegate del))
                del.Invoke(this, reader);
            else
                Debug.LogWarning($"ObserversRpc not found for hash {methodHash}.");
        }

        /// <summary>
        /// Called when an TargetRpc is received.
        /// </summary>
        internal void OnTargetRpc(PooledReader reader)
        {
            /* If more than 255 rpc methods then read a ushort,
            * otherwise read a byte. */
            ushort methodHash = (_rpcMethodCount > byte.MaxValue) ?
                reader.ReadUInt16() : reader.ReadByte();

            if (_targetRpcDelegates.TryGetValue(methodHash, out ClientRpcDelegate del))
                del.Invoke(this, reader);
            else
                Debug.LogWarning($"TargetRpc not found for hash {methodHash}.");
        }

        /// <summary>
        /// Sends a RPC to server.
        /// Internal use.
        /// </summary>
        /// <param name="rpcHash"></param>
        /// <param name="methodWriter"></param>
        /// <param name="channel"></param>
        public void SendServerRpc(ushort rpcHash, PooledWriter methodWriter, Channel channel)
        {
            PooledWriter writer = CreateRpcHeader(rpcHash, methodWriter, PacketId.ServerRpc);
            NetworkObject.NetworkManager.TransportManager.SendToServer((byte)channel, writer.GetArraySegment());
            writer.Dispose();
        }
        /// <summary>
        /// Sends a RPC to observers.
        /// Internal use.
        /// </summary>
        /// <param name="rpcHash"></param>
        /// <param name="methodWriter"></param>
        /// <param name="channel"></param>
        public void SendObserversRpc(ushort rpcHash, PooledWriter methodWriter, Channel channel)
        {
            PooledWriter writer = CreateRpcHeader(rpcHash, methodWriter, PacketId.ObserversRpc);

            //If not using observers then send to all.
            if (!NetworkObject.UsingObservers)
                NetworkObject.NetworkManager.TransportManager.SendToClients((byte)channel, writer.GetArraySegment());
            //Otherwise send to observers.
            else
                NetworkObject.NetworkManager.TransportManager.SendToClients((byte)channel, writer.GetArraySegment(), NetworkObject.Observers);

            writer.Dispose();
        }

        /// <summary>
        /// Sends a RPC to target.
        /// Internal use.
        /// </summary>
        /// <param name="rpcHash"></param>
        /// <param name="methodWriter"></param>
        /// <param name="channel"></param>
        /// <param name="target"></param>
        public void SendTargetRpc(ushort rpcHash, PooledWriter methodWriter, Channel channel, NetworkConnection target)
        {
            /* These checks could be codegened in to save a very very small amount of performance
             * by performing them before the serializer is written, but the odds of these failing
             * are very low and I'd rather keep the complexity out of codegen. */
            if (target == null)
            {
                Debug.LogWarning($"Action cannot be completed as no Target is specified.");
                return;
            }
            else
            {
                /* If not using observers, sending to owner,
                 * or observers contains target. */
                bool canSendTotarget = (!NetworkObject.UsingObservers ||
                    NetworkObject.OwnerId == target.ClientId ||
                    NetworkObject.Observers.Contains(target));

                if (!canSendTotarget)
                {
                    Debug.LogWarning($"Action cannot be completed as Target is not an observer for object {gameObject.name}");
                    return;
                }
            }

            PooledWriter writer = CreateRpcHeader(rpcHash, methodWriter, PacketId.TargetRpc);
            NetworkObject.NetworkManager.TransportManager.SendToClient((byte)channel, writer.GetArraySegment(), target);
            writer.Dispose();
        }

        /// <summary>
        /// Creates a PooledWriter and writes the header for a rpc.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="rpcHash"></param>
        /// <param name="packetId"></param>
        private PooledWriter CreateRpcHeader(ushort rpcHash, PooledWriter methodWriter, PacketId packetId)
        {
            PooledWriter writer = WriterPool.GetWriter();
            writer.WriteByte((byte)packetId);
            writer.WriteNetworkBehaviour(this);
            /* If more than 255 rpc methods then write a ushort,
             * otherwise write a byte. */
            if (_rpcMethodCount > byte.MaxValue)
                writer.WriteUInt16(rpcHash);
            else
                writer.WriteByte((byte)rpcHash);
            writer.WriteArraySegment(methodWriter.GetArraySegment());
            return writer;
        }


        #region Editor.
#if UNITY_EDITOR
        protected virtual void Reset()
        {
            /* Manually iterate up the chain because GetComponentInParent doesn't
             * work when modifying prefabs in the inspector. Unity, you're starting
             * to suck a lot right now. */
            NetworkObject result = null;
            Transform climb = transform;
            while (climb != null)
            {
                if (climb.TryGetComponent<NetworkObject>(out result))
                    break;
                else
                    climb = transform.parent;
            }

            if (result == null)
                transform.root.gameObject.AddComponent<NetworkObject>();
        }
#endif
        #endregion
    }


}