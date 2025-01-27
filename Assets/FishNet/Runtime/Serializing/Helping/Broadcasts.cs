﻿using FishNet.Object.Helping;
using FishNet.Transporting;

namespace FishNet.Serializing.Helping
{

    internal static class Broadcasts
    {
        /// <summary>
        /// Writes a broadcast to writer.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="writer"></param>
        /// <param name="message"></param>
        /// <param name="channel"></param>
        /// <returns></returns>
        internal static PooledWriter WriteBroadcast<T>(PooledWriter writer, T message, Channel channel)
        {
            writer.WriteByte((byte)PacketId.Broadcast);
            writer.WriteUInt16(typeof(T).FullName.GetStableHash16()); //muchlater codegen this to pass in hash. use technique similar to rpcs to limit byte/shorts.
            writer.Write<T>(message);
            return writer;
        }
    }

}