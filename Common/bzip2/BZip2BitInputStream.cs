// Bzip2 library for .net
// By Jaime Olivares
// Location: http://github.com/jaime-olivares/bzip2
// Ported from the Java implementation by Matthew Francis: https://github.com/MateuszBartosiewicz/bzip2

using System;
using System.IO;

namespace Bzip2
{
    /// <summary>
    /// Implements a bit-wise input stream
    /// </summary>
    internal class BZip2BitInputStream
    {
        #region Private fields
        // The stream from which bits are read
		private readonly Stream inputStream;

		// A buffer of bits read from the input stream that have not yet been returned
		private uint bitBuffer;

		// The number of bits currently buffered in bitBuffer
		private int bitCount;
        #endregion

        #region Public methods
        /// <summary>Public constructor</summary>
        /// <param name="inputStream">The input stream to wrap</param>
        public BZip2BitInputStream(Stream inputStream)
        {
            this.inputStream = inputStream;
        }

        /// <summary>Reads a single bit from the wrapped input stream</summary>
        /// <return>true if the bit read was 1, otherwise false</return>
        /// <exception>if no more bits are available in the input stream</exception>
        public bool ReadBoolean() 
		{
			if (bitCount > 0)
            {
				bitCount--;
			} else {
				int byteRead = this.inputStream.ReadByte();

				if (byteRead < 0) 
					throw new Exception ("Insufficient data");

				bitBuffer = (bitBuffer << 8) | (uint)byteRead;
				bitCount += 7;
			}

			return ((this.bitBuffer & (1 << this.bitCount))) != 0;
		}

        /// <summary>Reads a zero-terminated unary number from the wrapped input stream</summary>
        /// <return>The unary number</return>
        /// <exception>if no more bits are available in the input stream</exception>
        public uint ReadUnary()  
        {
			for (uint unaryCount = 0; ; unaryCount++)
            {
				if (bitCount > 0)
                {
					bitCount--;
				} else  {
					var byteRead = this.inputStream.ReadByte();

					if (byteRead < 0) 
						throw new Exception ("Insufficient data");

					bitBuffer = (bitBuffer << 8) | (uint)byteRead;
					bitCount += 7;
				}

				if (((bitBuffer & (1 << bitCount))) == 0) 
					return unaryCount;
			}
		}

        /// <summary>Reads up to 32 bits from the wrapped input stream</summary>
        /// <param name="count">The number of bits to read (maximum 32)</param>
        /// <return>The bits requested, right-aligned within the integer</return>
        /// <exception>if no more bits are available in the input stream</exception>
        public uint ReadBits(int count) 
		{
			if (bitCount < count)
            {
				while (bitCount < count) {
					int byteRead = this.inputStream.ReadByte();

					if (byteRead < 0) 
						throw new Exception ("Insufficient data");

					bitBuffer = (bitBuffer << 8) | (uint)byteRead;
					bitCount += 8;
				}
			}

			bitCount -= count;

			return (uint)((bitBuffer >> bitCount) & ((1 << count) - 1));
		}

        /**
         * Reads 32 bits of input as an integer
         * @return The integer read
         * @ if 32 bits are not available in the input stream
         */
        public uint ReadInteger()
        {
            return (this.ReadBits(16) << 16) | (this.ReadBits(16));
        }
        #endregion
    }
}