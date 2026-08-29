// <copyright file="MessageReader.cs" company="JP Dillingham">
//     Copyright (c) JP Dillingham. All rights reserved.
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
//
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see https://www.gnu.org/licenses/.
// </copyright>

namespace Soulseek.Messaging
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using Soulseek.Diagnostics;
    using Soulseek.Messaging.Compression;

    /// <summary>
    ///     Reads data from a Message payload.
    /// </summary>
    /// <typeparam name="T">The Type of the message code.</typeparam>
    /// <remarks>Only to be used for messages with a code length of 4 bytes.</remarks>
    internal sealed class MessageReader<T>
        where T : Enum
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="MessageReader{T}"/> class from the specified <paramref name="bytes"/>.
        /// </summary>
        /// <param name="bytes">The byte array with which to initialize the reader.</param>
        public MessageReader(byte[] bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes), "Invalid attempt to initialize MessageReader with a null byte array");
            }

            CodeLength = Enum.GetUnderlyingType(typeof(T)) == typeof(byte) ? 1 : 4;

            if (bytes.Length < 4 + CodeLength)
            {
                throw new ArgumentOutOfRangeException(nameof(bytes), bytes.Length, $"Invalid attempt to initialize MessageReader with byte array of length less than the minimum ({4 + CodeLength} bytes)");
            }

            Message = bytes.AsMemory();
            Payload = Message.Slice(4 + CodeLength);
        }

        /// <summary>
        ///     Gets a value indicating whether additional, unread data exists in the payload.
        /// </summary>
        public bool HasMoreData => Position < Payload.Length;

        /// <summary>
        ///     Gets the Message payload length.
        /// </summary>
        public int Length => Payload.Length;

        /// <summary>
        ///     Gets the Message payload.
        /// </summary>
        public Memory<byte> Payload { get; private set; }

        /// <summary>
        ///     Gets the current position of the head of the reader.
        /// </summary>
        public int Position { get; private set; } = 0;

        /// <summary>
        ///     Gets the length of the remaining payload data.
        /// </summary>
        public int Remaining => Length - Position;

        private int CodeLength { get; }
        private bool Decompressed { get; set; } = false;
        private Memory<byte> Message { get; }

        /// <summary>
        ///     Decompresses the message payload.
        /// </summary>
        /// <returns>This MessageReader.</returns>
        public MessageReader<T> Decompress()
        {
            if (Payload.Length == 0)
            {
                throw new InvalidOperationException("Unable to decompress an empty message");
            }

            if (Decompressed)
            {
                throw new InvalidOperationException("The message has already been decompressed");
            }

            Decompress(Payload.ToArray(), out byte[] decompressedPayload);
            Payload = decompressedPayload;

            Decompressed = true;

            return this;
        }

        /// <summary>
        ///     Reads a single byte at the head of the reader.
        /// </summary>
        /// <returns>The read byte.</returns>
        public int ReadByte()
        {
            try
            {
                var retVal = Payload.Span[Position];
                Position += 1;
                return retVal;
            }
            catch (Exception ex)
            {
                throw new MessageReadException($"Failed to read a byte from position {Position} of the message", ex);
            }
        }

        /// <summary>
        ///     Reads a byte array of length <paramref name="count"/> at the head of the reader.
        /// </summary>
        /// <param name="count">The number of bytes to read.</param>
        /// <returns>The read bytes.</returns>
        public byte[] ReadBytes(int count)
        {
            if (count > Payload.Length - Position)
            {
                throw new MessageReadException("Requested bytes extend beyond the length of the message payload");
            }

            var retVal = Payload.Slice(Position, count).ToArray();
            Position += count;
            return retVal;
        }

        /// <summary>
        ///     Reads the message code.
        /// </summary>
        /// <returns>The message code.</returns>
        public T ReadCode()
        {
            var codeBytes = Message.Slice(4, CodeLength).ToArray();
            var codeInt = codeBytes.Length > 1 ? BitConverter.ToInt32(codeBytes, 0) : codeBytes[0];
            return (T)Enum.Parse(typeof(T), codeInt.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        ///     Reads an integer at the head of the reader.
        /// </summary>
        /// <returns>The read integer.</returns>
        public int ReadInteger()
        {
            try
            {
                var retVal = Payload.Span[Position] | (Payload.Span[Position + 1] << 8) | (Payload.Span[Position + 2] << 16) | (Payload.Span[Position + 3] << 24);
                Position += 4;
                return retVal;
            }
            catch (Exception ex)
            {
                throw new MessageReadException($"Failed to read an integer (4 bytes) from position {Position} of the message", ex);
            }
        }

        /// <summary>
        ///     Reads a long at the head of the reader.
        /// </summary>
        /// <returns>The read long.</returns>
        public long ReadLong()
        {
            try
            {
                var retVal = BitConverter.ToInt64(Payload.Slice(Position, 8).ToArray(), 0);
                Position += 8;
                return retVal;
            }
            catch (Exception ex)
            {
                throw new MessageReadException($"Failed to read a long integer (8 bytes) from position {Position} of the message", ex);
            }
        }

        /// <summary>
        ///     Reads a string at the head of the reader.
        /// </summary>
        /// <remarks>
        ///     If no <paramref name="encoding"/> is specified, <see cref="CharacterEncoding.UTF8"/> will be attempted first,
        ///     falling back to <see cref="CharacterEncoding.ISO88591"/> if encoding fails.
        /// </remarks>
        /// <param name="encoding">The optional character encoding to use.</param>
        /// <returns>The read string.</returns>
        public string ReadString(CharacterEncoding encoding = null)
        {
            return ReadStringAndEncoding(encoding).Value;
        }

        /// <summary>
        ///     Reads a string at the head of the reader and returns both the string and the <see cref="CharacterEncoding"/> used to encode it.
        /// </summary>
        /// <remarks>
        ///     If no <paramref name="encoding"/> is specified, <see cref="CharacterEncoding.UTF8"/> will be attempted first,
        ///     falling back to <see cref="CharacterEncoding.ISO88591"/> if encoding fails.
        /// </remarks>
        /// <param name="encoding">The optional character encoding to use.</param>
        /// <returns>The read string.</returns>
        public (string Value, CharacterEncoding Encoding) ReadStringAndEncoding(CharacterEncoding encoding = null)
        {
            var length = ReadInteger();

            if (length > Payload.Length - Position)
            {
                throw new MessageReadException("Specified string length extends beyond the length of the message payload");
            }

            encoding ??= CharacterEncoding.UTF8;
            var bytes = Payload.Slice(Position, length).ToArray();
            string retVal;

            try
            {
                retVal = Encoding.GetEncoding(encoding, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback).GetString(bytes);
            }
            catch (Exception ex)
            {
                var requestedEncoding = encoding;
                encoding = CharacterEncoding.ISO88591;
                retVal = Encoding.GetEncoding(encoding).GetString(bytes);
                GlobalDiagnostic.Trace($"Failed to decode {requestedEncoding} for string {retVal}; resorted to fallback encoding {CharacterEncoding.ISO88591} (base64: {Convert.ToBase64String(bytes)})", ex);
            }

            Position += length;
            return (retVal, encoding);
        }

        /// <summary>
        ///     Moves the head of the reader to the specified <paramref name="position"/>.
        /// </summary>
        /// <param name="position">The desired position.</param>
        public void Seek(int position)
        {
            if (position < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(position), "Attempt to seek to a negative position");
            }

            if (position > Payload.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(position), $"Seek to position {position} would extend beyond the length of the message");
            }

            Position = position;
        }

        private void Decompress(byte[] inData, out byte[] outData)
        {
            static void CopyStream(Stream input, Stream output)
            {
                byte[] buffer = new byte[2000];
                int len;

                while ((len = input.Read(buffer, 0, 2000)) > 0)
                {
                    output.Write(buffer, 0, len);
                }

                output.Flush();
            }

            try
            {
                using var outMemoryStream = new MemoryStream();
                using var outZStream = new ZOutputStream(outMemoryStream);
                using var inMemoryStream = new MemoryStream(inData);
                CopyStream(inMemoryStream, outZStream);
                outZStream.finish();
                outData = outMemoryStream.ToArray();
            }
            catch (Exception ex)
            {
                throw new MessageCompressionException("Failed to decompress the message payload", ex);
            }
        }
    }
}