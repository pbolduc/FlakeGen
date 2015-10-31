﻿using System;
using System.Collections.Generic;

namespace FlakeGen
{
    public class IdStringGeneratorWrapper : IIdGenerator<string>
    {
        #region Predefined Converters

        /// <summary>
        /// Default converter calling <code>ToString()</code> on generated id
        /// </summary>
        public static readonly Func<long, string> Default =
            (id) => id.ToString();

        /// <summary>
        /// Converter calling <code>ToString()</code> with formating "X" on generated id
        /// </summary>
        public static readonly Func<long, string> UpperHex =
            (id) => id.ToString("X");

        /// <summary>
        /// Converter calling <code>ToString()</code> with formating "x" on generated id
        /// </summary>
        public static readonly Func<long, string> LowerHex =
            (id) => id.ToString("x");

        public static readonly Func<long, string> Base32 =
            (id) => Encoder.Encode32(id);

        public static readonly Func<long, string> Base32LeadingZero =
            (id) => Encoder.Encode32(id, true);

        #endregion Predefined Converters

        #region Private Fields

        private readonly IIdGenerator<long> baseGenerator;

        private readonly Func<long, string> converter;

        private readonly string prefix;

        #endregion Private Fields

        #region Public Constructors

        public IdStringGeneratorWrapper(IIdGenerator<long> baseGenerator, string prefix = null)
            : this(baseGenerator, Default, prefix)
        {
        }

        public IdStringGeneratorWrapper(
            IIdGenerator<long> baseGenerator, Func<long, string> converter, string prefix = null)
        {
            if (baseGenerator == null)
                throw new ArgumentNullException("baseGenerator", "base generator must be provided");

            if (converter == null)
                throw new ArgumentNullException("converter", "converter must be provided");

            this.baseGenerator = baseGenerator;
            this.converter = converter;
            this.prefix = string.IsNullOrEmpty(prefix) ? string.Empty : prefix;
        }

        #endregion Public Constructors

        public string GenerateId()
        {
            return prefix + converter(baseGenerator.GenerateId());
        }

        public IEnumerator<string> GetEnumerator()
        {
            while (true)
            {
                yield return GenerateId();
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
