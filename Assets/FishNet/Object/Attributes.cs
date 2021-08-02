﻿using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using System;
using UnityEngine;

namespace FishNet.Object
{
    public class RpcAttribute : Attribute { }

    /// <summary>
    /// ServerRpc methods will send messages to the server.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class ServerRpcAttribute : RpcAttribute
    {
        /// <summary>
        /// Whether or not the ServerRpc should only be run if executed by the owner of the object
        /// </summary>
        public bool RequireOwnership = true;
    }

    /// <summary>
    /// ObserversRpc methods will send messages to all observers.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class ObserversRpcAttribute : RpcAttribute { }

    /// <summary>
    /// TargetRpc methods will send messages to a single client.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class TargetRpcAttribute : RpcAttribute { }


    /// <summary>
    /// SyncObjects are used to synchronize collections from the server to all clients automatically.
    /// <para>Value must be changed on server, not directly by clients.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public class SyncObjectAttribute : PropertyAttribute
    {
        /// <summary>
        /// How often values may update over the network.
        /// </summary>
        public float SendRate = 0.1f;
        /// <summary>
        /// Clients which may receive value updates.
        /// </summary>
        public ReadPermission ReadPermissions = ReadPermission.Observers;
    }

    /// <summary>
    /// SyncVars are used to synchronize a variable from the server to all clients automatically.
    /// <para>Value must be changed on server, not directly by clients. Hook parameter allows you to define a client-side method to be invoked when the client gets an update from the server.</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public class SyncVarAttribute : PropertyAttribute
    {
        /// <summary>
        /// How often values may update over the network.
        /// </summary>
        public float SendRate = 0.1f;
        /// <summary>
        /// Clients which may receive value updates.
        /// </summary>
        public ReadPermission ReadPermissions = ReadPermission.Observers;
        /// <summary>
        /// Channel to use. Unreliable SyncVars will use eventual consistency.
        /// </summary>
        public Channel Channel;
        ///<summary>
        ///A function that should be called on the client when the value changes.
        ///</summary>
        public string OnChange;
    }

    /// <summary>
    /// Indicates a reader and writer should be generated for the object.
    /// </summary>
    [AttributeUsage((AttributeTargets.Class | AttributeTargets.Struct), Inherited = true, AllowMultiple = false)]
    public class NetworkSerializableAttribute : Attribute { }

    /// <summary>
    /// Prevents a method from running if server is not active.
    /// <para>Can only be used inside a NetworkBehaviour</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class ServerAttribute : Attribute
    {
        /// <summary>
        /// True to throw a warning if called while the server is not active.
        /// </summary>
        public bool Warn = true;
    }

    /// <summary>
    /// Prevents this method from running if client is not active.
    /// <para>Can only be used inside a NetworkBehaviour</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class ClientAttribute : Attribute
    {
        /// <summary>
        /// True to throw a warning if called while the client is not active.
        /// </summary> 
        public bool Warn = true;
    }


}
