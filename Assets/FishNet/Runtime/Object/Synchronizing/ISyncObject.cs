﻿using FishNet.Managing;
using FishNet.Serializing;

namespace FishNet.Object.Synchronizing.Internal
{
    /// <summary>
    /// A sync object is an object that can synchronize it's state
    /// between server and client, such as a SyncList
    /// </summary>
    public interface ISyncType
    {
        /// <summary>
        /// true if there are changes since the last flush
        /// </summary>
        bool IsDirty { get; }
        /// <summary>
        /// Sets index for the SyncType.
        /// </summary>
        /// <param name="index"></param>
        void SetSyncIndex(NetworkBehaviour networkBehaviour, uint index);
        /// <summary>
        /// PreInitializes this for use with the network.
        /// </summary>
        void PreInitialize(NetworkManager networkManager);
        /// <summary>
        /// Writes all changed values.
        /// </summary>
        /// <param name="writer"></param>
        ///<param name="resetSyncTick">True to set the next time data may sync.</param>
        void Write(PooledWriter writer, bool resetSyncTick = true);
        /// <summary>
        /// Writers all values if not initial values.
        /// </summary>
        /// <param name="writer"></param>
        void WriteIfChanged(PooledWriter writer);
        /// <summary>
        /// Sets current values.
        /// </summary>
        /// <param name="reader"></param>
        void Read(PooledReader reader);
        /// <summary>
        /// Resets the SyncObject so that it can be re-used
        /// </summary>
        void Reset();
    }


}