﻿using FishNet.Serializing;
using System;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Broadcast;
using FishNet.Object.Helping;
using FishNet.Transporting;
using FishNet.Serializing.Helping;

namespace FishNet.Managing.Client
{
    public partial class ClientManager
    {
        #region Private.
        /// <summary>
        /// Delegate to read received broadcasts.
        /// </summary>
        /// <param name="reader"></param>
        private delegate void ServerBroadcastDelegate(PooledReader reader);
        /// <summary>
        /// Delegates for each key.
        /// </summary>
        private readonly Dictionary<ushort, HashSet<ServerBroadcastDelegate>> _broadcastHandlers = new Dictionary<ushort, HashSet<ServerBroadcastDelegate>>();
        /// <summary>
        /// Delegate targets for each key.
        /// </summary>
        private Dictionary<ushort, HashSet<(int, ServerBroadcastDelegate)>> _handlerTargets = new Dictionary<ushort, HashSet<(int, ServerBroadcastDelegate)>>();
        #endregion

        /// <summary>
        /// Registers a method to call when a Broadcast arrives.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="handler">Method to call.</param>
        public void RegisterBroadcast<T>(Action<T> handler) where T : struct, IBroadcast
        {
            ushort key = typeof(T).FullName.GetStableHash16();

            /* Create delegate and add for
             * handler method. */
            HashSet<ServerBroadcastDelegate> handlers;
            if (!_broadcastHandlers.TryGetValue(key, out handlers))
            {
                handlers = new HashSet<ServerBroadcastDelegate>();
                _broadcastHandlers.Add(key, handlers);
            }
            ServerBroadcastDelegate del = CreateBroadcastDelegate(handler);
            handlers.Add(del);

            /* Add hashcode of target for handler.
             * This is so we can unregister the target later. */
            int handlerHashCode = handler.GetHashCode();
            HashSet<(int, ServerBroadcastDelegate)> targetHashCodes;
            if (!_handlerTargets.TryGetValue(key, out targetHashCodes))
            {
                targetHashCodes = new HashSet<(int, ServerBroadcastDelegate)>();
                _handlerTargets.Add(key, targetHashCodes);
            }

            targetHashCodes.Add((handlerHashCode, del));
        }

        /// <summary>
        /// Unregisters a method call from a Broadcast type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void UnregisterBroadcast<T>(Action<T> handler) where T : struct, IBroadcast
        {
            ushort key = typeof(T).FullName.GetStableHash16();

            /* If key is found for T then look for
             * the appropriate handler to remove. */
            if (_broadcastHandlers.TryGetValue(key, out HashSet<ServerBroadcastDelegate> handlers))
            {
                HashSet<(int, ServerBroadcastDelegate)> targetHashCodes;
                if (_handlerTargets.TryGetValue(key, out targetHashCodes))
                {
                    int handlerHashCode = handler.GetHashCode();
                    ServerBroadcastDelegate result = null;
                    foreach ((int targetHashCode, ServerBroadcastDelegate del) in targetHashCodes)
                    {
                        if (targetHashCode == handlerHashCode)
                        {
                            result = del;
                            break;
                        }
                    }

                    if (result != null)
                        handlers.Remove(result);
                }
            }
        }

        /// <summary>
        /// Creates a ServerBroadcastDelegate.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="handler"></param>
        /// <param name="requireAuthentication"></param>
        /// <returns></returns>
        private ServerBroadcastDelegate CreateBroadcastDelegate<T>(Action<T> handler)
        {
            void LogicContainer(PooledReader reader)
            {
                T broadcast = reader.Read<T>();
                handler?.Invoke(broadcast);
            }
            return LogicContainer;
        }

        /// <summary>
        /// Parses a received broadcast.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="connectionId"></param>
        private void ParseBroadcast(PooledReader reader)
        {
            ushort key = reader.ReadUInt16();

            // try to invoke the handler for that message
            if (_broadcastHandlers.TryGetValue(key, out HashSet<ServerBroadcastDelegate> handlers))
            {
                int readerStartPosition = reader.Position;
                /* //muchlater resetting the position could be better by instead reading once and passing in
                 * the object to invoke with. */
                foreach (ServerBroadcastDelegate handler in handlers)
                {
                    reader.Position = readerStartPosition;
                    handler.Invoke(reader);
                }
            }
            else
            {
                Debug.LogError($"Broadcast not found for key {key}.");
            }
        }


        /// <summary>
        /// Sends a Broadcast to the server.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message"></param>
        /// <param name="channel"></param>
        public void Broadcast<T>(T message, Channel channel = Channel.Reliable) where T : struct, IBroadcast
        {
            //Check local connection state.
            if (!Started)
            {
                Debug.LogError($"Cannot send broadcast to server because client is not active.");
                return;
            }

            using (PooledWriter writer = WriterPool.GetWriter())
            {
                Broadcasts.WriteBroadcast<T>(writer, message, channel);
                ArraySegment<byte> segment = writer.GetArraySegment();

                NetworkManager.TransportManager.SendToServer((byte)channel, segment);
            }
        }

    }


}
