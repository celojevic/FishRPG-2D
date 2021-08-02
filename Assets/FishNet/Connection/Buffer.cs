using System;
using System.Buffers;
using System.Collections.Generic;

namespace FishNet.Connection
{

    public class PacketBundle
    {
        /// <summary>
        /// Next index to write data to for the current bufferIndex.
        /// </summary>
        private int _writeIndex = 0;
        /// <summary>
        /// All buffers written. Collection is not cleared when reset but rather the index in which to write is.
        /// </summary>
        private List<SharedBuffer> _buffers = new List<SharedBuffer>();
        /// <summary>
        /// Buffer which is being written to.
        /// </summary>
        private int _bufferIndex = 0;
        /// <summary>
        /// Maximum size packet the transport can handle for this channel.
        /// </summary>
        private int _maximumTransportUnit = 1200;
        /// <summary>
        /// Number of buffers written to. Will return 0 if nothing has been written.
        /// </summary>
        public int WrittenBuffers =>(_bufferIndex == 0 && _writeIndex == 0) ? 0 : (_bufferIndex + 1);
        /// <summary>
        /// Number of bytes to reserve at the beginning of each buffer.
        /// </summary>
        private int _reserve = 0;


        internal PacketBundle(int mtu, int reserve = 0)
        {
            Reset(mtu, reserve);
        }

        /// <summary>
        /// Resets using current settings.
        /// </summary>
        internal void Reset()
        {
            Reset(_maximumTransportUnit, _reserve);
        }
        /// <summary>
        /// Resets this PacketBundle.
        /// </summary>
        /// <param name="mtu">MaximumTransmissionUnit allowed per buffer.</param>
        /// <param name="reserve">Number of bytes at the beginning of each buffer to reserve. Reserving puts the WriteIndex at the specified value.</param>
        internal void Reset(int mtu, int reserve = 0)
        {
            _maximumTransportUnit = mtu;
            _reserve = reserve;
            _writeIndex = reserve;
            _bufferIndex = 0;

            //Make sure there is at least one buffer present.
            if (_buffers.Count == 0)
            {
                SharedBuffer sb = new SharedBuffer(_maximumTransportUnit, _reserve);
                _buffers.Add(sb);
            }
            else
            {
                for (int i = 0; i < _buffers.Count; i++)
                    _buffers[i].Reset(_maximumTransportUnit, _reserve);
            }

        }

        /// <summary>
        /// Writes a segment to this packet bundle using the current WriteIndex.
        /// </summary>
        /// <param name="segment"></param>
        internal void Write(ArraySegment<byte> segment)
        {
            //Nothing to be written.
            if (segment.Count == 0)
                return;
            /* If the segment count is larger than the mtu then
             * something went wrong. Nothing should call this method
             * directly except the TransportManager, which will automatically
             * split packets that exceed MTU into reliable ordered. */
            if (segment.Count > _maximumTransportUnit)
            {
                UnityEngine.Debug.LogError($"Segment is length of {segment.Count} while MTU is {_maximumTransportUnit}. Packet was not split properly and will not be sent.");
                return;
            }

            int remaining = _maximumTransportUnit - _writeIndex;
            int segmentCount = segment.Count;
            /* If not enough remaining in buffer to append segment
             * then increase bufferIndex. This will cause a new buffer
             * to be created if needed. If the index already exist then
             * overwrite it with a new sharedbuffer. The old sharedbuffer
             * will be returned to the pool. This is done because the mtu
             * size may vary betwen uses. */
            if (segmentCount > remaining)
            {
                _writeIndex = _reserve;
                _bufferIndex++;
                //If need to make a new shared buffer then do so.
                if (_buffers.Count <= _bufferIndex)
                {
                    _buffers.Add(new SharedBuffer(_maximumTransportUnit));
                }
                //Reset length on buffer being used.
                _buffers[_buffers.Count - 1].Length = _reserve;
            }

            //Buffer to write into.
            SharedBuffer buffer = _buffers[_bufferIndex];
            Buffer.BlockCopy(segment.Array, segment.Offset, buffer.Data, _writeIndex, segmentCount);
            _writeIndex += segmentCount;
            buffer.Length += segmentCount;
        }

        /// <summary>
        /// Gets a buffer on the specified index.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        internal SharedBuffer GetBuffer(int index)
        {
            if (index >= _buffers.Count || index < 0)
            {
                UnityEngine.Debug.LogError($"Index of {index} is out of bounds. There are {_buffers.Count} available.");
                return null;
            }
            if (index > _bufferIndex)
            {
                UnityEngine.Debug.LogError($"Index of {index} exceeds the number of written buffers. There are {WrittenBuffers} written buffers.");
                return null;
            }

            return _buffers[index];
        }

        /// <summary>
        /// Returns a PacketBundle for a channel. ResetPackets must be called afterwards.
        /// </summary>
        /// <param name="channel"></param>
        /// <returns>True if PacketBundle is valid on the index and contains data.</returns>
        internal static bool GetPacketBundle(int channel, List<PacketBundle> bundles, out PacketBundle packetBundle)
        {
            //Out of bounds.
            if (channel >= bundles.Count)
            {
                packetBundle = null;
                return false;
            }

            packetBundle = bundles[channel];
            //No packets to send.
            if (packetBundle.WrittenBuffers == 0)
                return false;

            return true;
        }
    }

    /// <summary>
    /// A buffer using a shared ArrayPool.
    /// </summary>
    internal class SharedBuffer
    {
        /// <summary>
        /// Buffer data.
        /// </summary>
        internal byte[] Data;
        /// <summary>
        /// Amount written to the buffer.
        /// </summary>
        internal int Length = 0;
        /// <summary>
        /// Shared ArrayPool. Made static because I'm unable to validate if instanced versions all point to the same object. Without being sure I don't want to risk creating a bunch of ArrayPool references.
        /// </summary>
        private static ArrayPool<byte> _shared = null;

        internal SharedBuffer(int size, int reserve = 0)
        {
            Reset(size, reserve);
        }

        /// <summary>
        /// Resets using values.
        /// </summary>
        /// <param name="size"></param>
        /// <param name="reserve"></param>
        internal void Reset(int size, int reserve = 0)
        {
            if (_shared == null)
                _shared = ArrayPool<byte>.Shared;

            ReturnRented();
            Data = _shared.Rent(size);
            Length = reserve;
        }

        ~SharedBuffer()
        {
            ReturnRented();
        }

        /// <summary>
        /// Returns rented data to shared.
        /// </summary>
        private void ReturnRented()
        {
            if (Data != null && _shared != null)
                _shared.Return(Data, false);
        }
    }


}