using System;
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
        public static readonly DateTime DefaultEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        #region Private Constant

        private const int IdentifierMaxBytes = 6;

        private const int SequenceBits = 16;

        /// <summary>
        /// The sequence bit mask. 0x000000000000ffff
        /// </summary>
        private const long SequenceBitMask = -1 ^ (-1 << SequenceBits);


        #endregion Private Constant

        #region Private Fields

        /// <summary>
        /// Object used as a monitor for threads synchronization.
        /// </summary>
        private readonly object _monitor = new object();
        private readonly long _epoch;

        // store the individual bytes instead of an array
        // so we do not incur the overhead of array indexing
        // and bound checks when generating id values
        private readonly byte _identifier0;
        private readonly byte _identifier1;
        private readonly byte _identifier2;
        private readonly byte _identifier3;
        private readonly byte _identifier4;
        private readonly byte _identifier5;

        /// <summary>
        /// The last timestamp relative to <see cref="_epoch"/> in 1000s of milliseconds.
        /// </summary>
        private long _lastTimestamp;

        /// <summary>
        /// The sequence within the same 1000th of a millisecond.
        /// </summary>
        private int _sequence;

        #endregion Private Fields

        #region Public Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="IdGuidGenerator"/> class.
        /// </summary>
        public IdGuidGenerator()
            : this(0)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IdGuidGenerator"/> class.
        /// </summary>
        /// <param name="identifier">
        /// The instance identifier. Only the 6 low order bytes will be used.
        /// </param>
        public IdGuidGenerator(long identifier)
            : this(BitConverter.GetBytes(identifier))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IdGuidGenerator"/> class.
        /// </summary>
        /// <param name="identifier">
        /// The instance identifier. Only the 6 low order bytes will be used.
        /// </param>
        /// <param name="epoch">The epoch.</param>
        public IdGuidGenerator(long identifier, DateTime epoch)
            : this(BitConverter.GetBytes(identifier), epoch.Ticks)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IdGuidGenerator"/> class.
        /// The default epoch will be used.
        /// </summary>
        /// <param name="identifier">
        /// The instance identifier.  Only the first 6 bytes will be used.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="identifier"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="identifier"/> has length less than 6.
        /// </exception>
        public IdGuidGenerator(byte[] identifier)
            : this(identifier, 0)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IdGuidGenerator"/> class.
        /// </summary>
        /// <param name="identifier">
        /// The instance identifier.  Only the first 6 bytes will be used.
        /// </param>
        /// <param name="epoch">The epoch.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="identifier"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="identifier"/> has length less than 6.
        /// </exception>
        public IdGuidGenerator(byte[] identifier, DateTime epoch)
            : this(identifier, epoch.Ticks)
        {
        }

        #endregion Public Constructors

        #region Private Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="IdGuidGenerator"/> class.
        /// </summary>
        /// <param name="identifier">The identifier.</param>
        /// <param name="epoch">The epoch.</param>
        /// <exception cref="System.ArgumentNullException">identifier</exception>
        /// <exception cref="System.ArgumentException">Parameter must be an array of ;identifier</exception>
        private IdGuidGenerator(byte[] identifier, long epoch)
        {
            if (identifier == null)
            {
                throw new ArgumentNullException("identifier");
            }

            if (identifier.Length < 6)
            {
                throw new ArgumentException("Parameter must be an array of length at least 6.", "identifier");
            }

            _identifier0 = identifier[0];
            _identifier1 = identifier[1];
            _identifier2 = identifier[2];
            _identifier3 = identifier[3];
            _identifier4 = identifier[4];
            _identifier5 = identifier[5];
            _epoch = epoch == 0 ? DefaultEpoch.Ticks : epoch;
        }

        #endregion Public Constructors

        #region Public Methods

        /// <summary>
        /// Generates new identifier every time the method is called
        /// </summary>
        /// <returns>
        /// The new generated identifier.
        /// </returns>
        public Guid GenerateId()
        {
            lock (_monitor)
            {
                return NextId();
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

            if (_lastTimestamp < timestamp)
            {
                _lastTimestamp = timestamp;
                _sequence = 0;
            }
            else if (_lastTimestamp == timestamp)
            {
                // overflow 16-bits ? need to generate over 65,535,000,000 id/second?
                // release build on Intel Core i7 - 3740QM @ 2.7GHz max out at
                // about 17,000,000 id/sec
                _sequence++;
                Debug.Assert(_sequence <= 65535);
            }
            else if (_lastTimestamp > timestamp)
            {
                // If the clock was a bit fast and the clock is adjusted
                // using a time server, the new time could be a number of seconds
                // behind.
                double backwardDrift = (_lastTimestamp - timestamp) * (1.0 * TimeSpan.TicksPerMillisecond);
                throw new InvalidOperationException(string.Format("Clock moved backwards. Refusing to generate id for {0} milliseconds", (backwardDrift)));
            }
        }

        private Guid NextId()
        {
            HandleTime();

            if (BitConverter.IsLittleEndian)
            {
                var lastTimestamp = _lastTimestamp;
                return new Guid(
                    (int)(lastTimestamp >> 32 & 0xFFFFFFFF),
                    (short)(lastTimestamp >> 16 & 0xFFFF),
                    (short)(lastTimestamp & 0xFFFF),
                    _identifier5,
                    _identifier4,
                    _identifier3,
                    _identifier2,
                    _identifier1,
                    _identifier0,
                    (byte)(_sequence >> 8 & 0xff),
                    (byte)(_sequence & 0xff));
            }
            else
            {
                var lastTimestamp = _lastTimestamp;
                return new Guid(
                    (int)(lastTimestamp >> 32 & 0xFFFFFFFF),
                    (short)(lastTimestamp >> 16 & 0xFFFF),
                    (short)(lastTimestamp & 0xFFFF),
                    (byte)(_sequence & 0xff),
                    (byte)(_sequence >> 8 & 0xff),
                    _identifier0,
                    _identifier1,
                    _identifier2,
                    _identifier3,
                    _identifier4,
                    _identifier5);
            }
        }

        private long CurrentTime
        {
            get
            {
                // there are 10,000 ticks / millisecond
                return (DateTime.UtcNow.Ticks - _epoch);
            }
        }

        #endregion Private Methods
    }
}
