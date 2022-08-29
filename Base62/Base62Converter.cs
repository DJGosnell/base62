﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Base62
{
    /// <summary>
    /// Encodes and decodes text to and from base62 encoding.
    /// </summary>
    public class Base62Converter
    {
        private const string DEFAULT_CHARACTER_SET = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        private const string INVERTED_CHARACTER_SET = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private readonly string characterSet;

        /// <summary>
        /// Initializes a new instance of <see cref="Base62Converter"/>.
        /// </summary>
        public Base62Converter()
            : this(CharacterSet.DEFAULT)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="Base62Converter"/> with the provided <see cref="CharacterSet"/>.
        /// </summary>
        /// <param name="charset"></param>
        public Base62Converter(CharacterSet charset)
        {
            if (charset == CharacterSet.DEFAULT)
                characterSet = DEFAULT_CHARACTER_SET;
            else
                characterSet = INVERTED_CHARACTER_SET;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="Base62Converter"/> with a randomized
        /// character set order based upon the passed seed.
        /// </summary>
        /// <param name="randomSeed">Seed used to randomize the characters.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "SCS0005:Weak random number generator.", Justification = "<Pending>")]
        public Base62Converter(int randomSeed)
        {
            char[] array = DEFAULT_CHARACTER_SET.ToCharArray();
            Random rng = new Random(randomSeed);
            int n = array.Length;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                var value = array[k];
                array[k] = array[n];
                array[n] = value;
            }
            characterSet = new string(array);
        }

        /// <summary>
        /// Encodes the input text to Base62 format.
        /// </summary>
        /// <param name="value">The input value.</param>
        /// <returns>Encoded base62 value.</returns>
        public string EncodeBytesToString(byte[] value)
        {
            var converted = Encode(value);
            var output = new char[converted.Length];

            for (int i = 0; i < output.Length; i++)
            {
                output[i] = characterSet[converted[i]];
            }

            return new string(output);
        }
        /// <summary>
        /// Decodes the input text from Base62 format.
        /// </summary>
        /// <param name="value">The input value.</param>
        /// <returns>The decoded value.</returns>
        public byte[] DecodeStringToBytes(string value)
        {
            var arr = new byte[value.Length];
            for (var i = 0; i < arr.Length; i++)
            {
                arr[i] = (byte)characterSet.IndexOf(value[i]);
            }

            return Decode(arr);
        }

        /// <summary>
        /// Encodes the input text to Base62 format.
        /// </summary>
        /// <param name="value">The input value.</param>
        /// <returns>Encoded base62 value.</returns>
        public string Encode(string value)
        {
            var arr = Encoding.UTF8.GetBytes(value);
            var converted = Encode(arr);
            var output = new char[converted.Length];
            for (var i = 0; i < converted.Length; i++)
            {
                output[i] = characterSet[converted[i]];
            }

            return new string(output);
        }

        /// <summary>
        /// Decodes the input text from Base62 format.
        /// </summary>
        /// <param name="value">The input value.</param>
        /// <returns>The decoded value.</returns>
        public string Decode(string value)
        {
            var arr = new byte[value.Length];
            for (var i = 0; i < arr.Length; i++)
            {
                arr[i] = (byte)characterSet.IndexOf(value[i]);
            }

            var converted = Decode(arr);
            return Encoding.UTF8.GetString(converted, 0, converted.Length);
        }

        /// <summary>
        /// Encodes the input bytes to Base62 format.
        /// </summary>
        /// <param name="value">The input value.</param>
        /// <returns>Encoded base62 value.</returns>
        public byte[] Encode(byte[] value)
        {
            return BaseConvert(value, 256, 62);
        }

        /// <summary>
        /// Decodes the input bytes from Base62 format.
        /// </summary>
        /// <param name="value">The inpnut value.</param>
        /// <returns>The decoded value.</returns>
        public byte[] Decode(byte[] value)
        {
            return BaseConvert(value, 62, 256);
        }

        /// <summary>
        /// Converts source byte array from the source base to the destination base.
        /// </summary>
        /// <param name="source">Byte array to convert.</param>
        /// <param name="sourceBase">Source base to convert from.</param>
        /// <param name="targetBase">Target base to convert to.</param>
        /// <returns>Converted byte array.</returns>
        public static byte[] BaseConvert(byte[] source, int sourceBase, int targetBase)
        {
            if (targetBase < 0 || targetBase > 256)
                throw new ArgumentOutOfRangeException(nameof(targetBase), targetBase, "Value must be between 0 & 257");
            
            if (sourceBase < 0 || sourceBase > 256)
                throw new ArgumentOutOfRangeException(nameof(sourceBase), sourceBase, "Value must be between 0 & 257");

            // Set initial capacity estimate if the size is small.
            var startCapacity = source.Length < 1028 
                ? (int)(source.Length * 1.5) 
                : source.Length;

            var result = new List<int>(startCapacity);
            var quotient = new List<byte>((int)(source.Length * 0.5));
            int count;
            int initialStartOffset = 0;

            // This is a bug fix for the following issue:
            // https://github.com/ghost1face/base62/issues/4
            while (source[initialStartOffset] == 0)
            {
                result.Add(0);
                initialStartOffset++;
            }

            int startOffset = initialStartOffset;

            while ((count = source.Length) > 0)
            {
                quotient.Clear();
                int remainder = 0;
                for (var i = initialStartOffset; i != count; i++)
                {
                    int accumulator = source[i] + remainder * sourceBase;
                    byte digit = (byte)((accumulator - (accumulator % targetBase)) / targetBase);
                    remainder = accumulator % targetBase;
                    if (quotient.Count > 0 || digit != 0)
                    {
                        quotient.Add(digit);
                    }
                }

                result.Insert(startOffset, remainder);
                source = quotient.ToArray();
                initialStartOffset = 0;
            }

            var output = new byte[result.Count];

            for (int i = 0; i < result.Count; i++)
                output[i] = (byte)result[i];

            return output;
        }

        /// <summary>
        /// Character set to use for encoding/decoding.
        /// </summary>
        public enum CharacterSet
        {
            /// <summary>
            /// Alpha numeric character set, using capital letters first before lowercase.
            /// </summary>
            DEFAULT,

            /// <summary>
            /// Alpha numeric character set, using lower case letters first before uppercase.
            /// </summary>
            INVERTED
        }
    }
}
