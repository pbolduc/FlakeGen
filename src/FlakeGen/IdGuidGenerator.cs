﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace FlakeGen
{
    using System.Diagnostics;

    /// <summary>
    /// A decentralized, k-ordered id generator
    /// Generated ids are Guid (128-bit wide)
    /// <list>
    /// <item><description>64-bit timestamp - milliseconds since the epoch (Jan 1 1970)</description></item>
    /// <item><description>48-bit worker id; it can be MAC address or other identifier</description></item>
    /// <item><description>16-bit sequence # - usually 0 incremented when more than one id is requested in the same millisecond and reset to 0 when the clock ticks forward</description></item>
    /// </list>
    /// </summary>
    public class IdGuidGenerator : IIdGenerator<Guid>
    {
        #region Private Constant

        private const int IdentifierMaxBytes = 6;

        private const int SequenceBits = 16;

        private const long SequenceBitMask = -1 ^ (-1 << SequenceBits);

        #endregion Private Constant

        #region Private Fields

        /// <summary>
        /// Object used as a monitor for threads synchronization.
        /// </summary>
        private readonly object monitor = new object();

        private readonly ulong epoch;

        private readonly byte[] identifier;

        private ulong lastTimestamp;

        private int sequence;

        private readonly Func<Guid> nextId;

        #endregion Private Fields

        #region Public Constructors

        public IdGuidGenerator()
            : this(0)
        {
        }

        public IdGuidGenerator(long identifier)
            : this(BitConverter.GetBytes(identifier).Take(6).ToArray())
        {
        }

        public IdGuidGenerator(long identifier, DateTime epoch)
            : this(BitConverter.GetBytes(identifier).Take(6).ToArray(), (ulong)epoch.Ticks / 10)
        {
        }

        public IdGuidGenerator(byte[] identifier)
            : this(identifier, 0)
        {
        }

        public IdGuidGenerator(byte[] identifier, DateTime epoch)
            : this(identifier, (ulong)epoch.Ticks / 10)
        {
        }

        #endregion Public Constructors

        #region Private Constructors

        private IdGuidGenerator(byte[] identifier, ulong epoch)
        {
            if (identifier.Length > 6)
                throw new ApplicationException("Identifier too long");

            this.identifier = identifier;
            this.epoch = epoch == 0 ? (ulong)new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks / 10 : epoch;

            this.nextId = BitConverter.IsLittleEndian ? (Func<Guid>)LittleEndianNextId : BigEndianNextId;
            //this.nextId = OriginalNextId;
        }

        #endregion Public Constructors

        #region Public Methods

        public Guid GenerateId()
        {
            lock (monitor)
            {
                return nextId();
            }
        }

        public IEnumerator<Guid> GetEnumerator()
        {
            while (true)
            {
                yield return GenerateId();
            }
        }

        #endregion Public Methods

        #region Private Methods

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private void HandleTime()
        {
            var timestamp = CurrentTime;

            if (lastTimestamp < timestamp)
            {
                lastTimestamp = timestamp;
                sequence = 0;
            }
            else if (lastTimestamp > timestamp)
            {
                // If the clock was a bit fast and the clock is adjusted
                // using a time server, the new time could be a number of seconds
                // behind.
                throw new ApplicationException("Clock is running backwards");
            }
            else
            {
                // overflow 16-bits ? need to generate over 65,535,000 id/second?
                // release build on Intel Core i7 - 3740QM @ 2.7GHz max out at
                // about 16,000,000 id/sec
                sequence++;
                Debug.Assert(sequence <= 65535);
            }
        }

        private Guid LittleEndianNextId()
        {
            HandleTime();

            ulong timestamp = lastTimestamp;

            Guid id = new Guid(
                (int)(timestamp >> 32 & 0xFFFFFFFF),
                (short)(timestamp >> 16 & 0xFFFF),
                (short)(timestamp & 0xFFFF),
                identifier[5],
                identifier[4],
                identifier[3],
                identifier[2],
                identifier[1],
                identifier[0],
                (byte)(sequence >> 8 & 0xff),
                (byte)(sequence & 0xff));

            return id;
        }

        private Guid BigEndianNextId()
        {
            HandleTime();

            ulong timestamp = lastTimestamp;

            Guid id = new Guid(
                (int)(timestamp >> 32 & 0xFFFFFFFF),
                (short)(timestamp >> 16 & 0xFFFF),
                (short)(timestamp & 0xFFFF),
                (byte)(sequence & 0xff),
                (byte)(sequence >> 8 & 0xff),
                identifier[0],
                identifier[1],
                identifier[2],
                identifier[3],
                identifier[4],
                identifier[5]);

            return id;
        }

        ////private Guid OriginalNextId()
        ////{
        ////    HandleTime();

        ////    byte[] id = new byte[8];
        ////    byte[] sequenceBytes = BitConverter.GetBytes(sequence & SequenceBitMask).Take(2).ToArray();

        ////    Array.Copy(identifier, 0, id, 2, identifier.Length); // identifier - 48 bits
        ////    Array.Copy(sequenceBytes, 0, id, 0, 2); // sequence - 16 bits

        ////    if (BitConverter.IsLittleEndian)
        ////        Array.Reverse(id);

        ////    return new Guid((int)(lastTimestamp >> 32 & 0xFFFFFFFF),
        ////        (short)(lastTimestamp >> 16 & 0xFFFF),
        ////        (short)(lastTimestamp & 0xFFFF),
        ////        id);
        ////}


        private ulong CurrentTime
        {
            get { return ((ulong)DateTime.UtcNow.Ticks / 10) - epoch; }
        }

        #endregion Private Methods
    }
}
