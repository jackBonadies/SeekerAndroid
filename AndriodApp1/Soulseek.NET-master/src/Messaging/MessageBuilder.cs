// <copyright file="MessageBuilder.cs" company="JP Dillingham">
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
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Soulseek.Messaging.Compression;

    /// <summary>
    ///     Builds a message.
    /// </summary>
    internal sealed class MessageBuilder
    {
        private List<byte> CodeBytes { get; set; } = new List<byte>();
        private bool Compressed { get; set; } = false;
        private List<byte> PayloadBytes { get; set; } = new List<byte>();

        /// <summary>
        ///     Builds the message.
        /// </summary>
        /// <returns>The built message.</returns>
        public byte[] Build()
        {
            if (CodeBytes.Count == 0)
            {
                throw new InvalidOperationException("Unable to build the message without having set the message Code");
            }

            var withLength = new List<byte>(BitConverter.GetBytes(CodeBytes.Count + PayloadBytes.Count));
            withLength.AddRange(CodeBytes);
            withLength.AddRange(PayloadBytes);
            return withLength.ToArray();
        }

        /// <summary>
        ///     Compresses the message payload.
        /// </summary>
        /// <returns>This MessageBuilder.</returns>
        /// <exception cref="InvalidOperationException">Thrown when attempting to compress an empty message.</exception>
        /// <exception cref="MessageCompressionException">
        ///     Thrown when an error is encountered while compressing the message payload.
        /// </exception>
        public MessageBuilder Compress()
        {
            if (PayloadBytes.Count == 0)
            {
                throw new InvalidOperationException("Unable to compress an empty message");
            }

            if (Compressed)
            {
                throw new InvalidOperationException("The message has already been compressed");
            }

            Compress(PayloadBytes.ToArray(), out var compressedBytes);

            PayloadBytes = compressedBytes.ToList();
            Compressed = true;

            return this;
        }

        /// <summary>
        ///     Writes the specified byte <paramref name="value"/> to the message.
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <returns>This MessageBuilder.</returns>
        /// <exception cref="InvalidOperationException">
        ///     Thrown when attempting to write additional data to a message that has been compressed.
        /// </exception>
        public MessageBuilder WriteByte(byte value)
        {
            return WriteBytes(new[] { value });
        }

        /// <summary>
        ///     Writes the specified <paramref name="bytes"/> to the message.
        /// </summary>
        /// <param name="bytes">The bytes to write.</param>
        /// <returns>This MessageBuilder.</returns>
        /// <exception cref="InvalidOperationException">
        ///     Thrown when attempting to write additional data to a message that has been compressed.
        /// </exception>
        public MessageBuilder WriteBytes(byte[] bytes)
        {
            if (Compressed)
            {
                throw new InvalidOperationException("Unable to write additional data after message has been compressed");
            }

            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes), "Invalid attempt to write a null byte array to message");
            }

            PayloadBytes.AddRange(bytes);
            return this;
        }

        /// <summary>
        ///     Sets the message code.
        /// </summary>
        /// <param name="code">The desired message code.</param>
        /// <returns>This MessageBuilder.</returns>
        public MessageBuilder WriteCode(MessageCode.Distributed code)
        {
            byte codeByte = Convert.ToByte(code, CultureInfo.InvariantCulture);
            return WriteCode(new byte[] { codeByte });
        }

        /// <summary>
        ///     Sets the message code.
        /// </summary>
        /// <param name="code">The desired message code.</param>
        /// <returns>This MessageBuilder.</returns>
        public MessageBuilder WriteCode(MessageCode.Initialization code)
        {
            byte codeByte = Convert.ToByte(code, CultureInfo.InvariantCulture);
            return WriteCode(new byte[] { codeByte });
        }

        /// <summary>
        ///     Sets the message code.
        /// </summary>
        /// <param name="code">The desired message code.</param>
        /// <returns>This MessageBuilder.</returns>
        public MessageBuilder WriteCode(MessageCode.Server code)
        {
            return WriteCode(BitConverter.GetBytes((int)code));
        }

        /// <summary>
        ///     Sets the message code.
        /// </summary>
        /// <param name="code">The desired message code.</param>
        /// <returns>This MessageBuilder.</returns>
        public MessageBuilder WriteCode(MessageCode.Peer code)
        {
            return WriteCode(BitConverter.GetBytes((int)code));
        }

        /// <summary>
        ///     Writes the specified integer <paramref name="value"/> to the message.
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <returns>This MessageBuilder.</returns>
        /// <exception cref="InvalidOperationException">
        ///     Thrown when attempting to write additional data to a message that has been compressed.
        /// </exception>
        public MessageBuilder WriteInteger(int value)
        {
            return WriteBytes(BitConverter.GetBytes(value));
        }

        /// <summary>
        ///     Writes the specified long <paramref name="value"/> to the message.
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <returns>This MessageBuilder.</returns>
        /// <exception cref="InvalidOperationException">
        ///     Thrown when attempting to write additional data to a message that has been compressed.
        /// </exception>
        public MessageBuilder WriteLong(long value)
        {
            return WriteBytes(BitConverter.GetBytes(value));
        }

        /// <summary>
        ///     Writes the specified string <paramref name="value"/> to the message.
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <param name="attemptLatin1File">Whether to attempt Latin1 (necessary if SoulseekNS) or to use UTF8 (prevents string appearing incorrectly if any special characters like accents)</param>
        /// <returns>This MessageBuilder.</returns>
        /// <exception cref="InvalidOperationException">
        ///     Thrown when attempting to write additional data to a message that has been compressed.
        /// </exception>
        public MessageBuilder WriteString(string value, bool attemptLatin1File = false, bool attemptLatin1Folder = false)
        {
            byte[] bytes;


            //if(tryUndoMojibake)
            //{
            //    // try to undo mojibake that occurs when a client sends a string as latin1 and reads it as utf8
            //    // i.e. they have a file fÃ¶r. they send it to us and we decode it as unicode as för. we need to send
            //    //  them back fÃ¶r.
            //    try
            //    {
            //        bytes = Encoding.GetEncoding("UTF-8", EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback).GetBytes(value);
            //        string theirString = Encoding.GetEncoding("ISO-8859-1", EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback).GetString(bytes);
            //        bytes = Encoding.GetEncoding("UTF-8", EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback).GetBytes(theirString);
            //        string finalString = Encoding.GetEncoding("UTF-8", EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback).GetString(bytes);
            //    }
            //    catch (Exception)
            //    {
            //        bytes = Encoding.GetEncoding("UTF-8").GetBytes(value);
            //    }
            //}
            if(attemptLatin1File || attemptLatin1Folder)
            {

                // if when reading the filename, we failed to decode it as UTF-8 (which means the client sent a Latin1 string).
                // then make sure to encode it the way we decoded it, so that they get the same byte sequence back.

                try
                {
                    if(attemptLatin1File && attemptLatin1Folder)
                    {
                        bytes = Encoding.GetEncoding("ISO-8859-1", EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback).GetBytes(value);
                    }
                    else
                    {
                        int split = value.LastIndexOf('\\');
                        string folderNameWithSlash = value.Substring(0, split + 1);
                        string fileName = value.Substring(split + 1);
                        if (attemptLatin1Folder)
                        {
                            bytes = Encoding.GetEncoding("ISO-8859-1", EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback).GetBytes(folderNameWithSlash);
                        }
                        else
                        {
                            bytes = Encoding.GetEncoding("UTF-8").GetBytes(folderNameWithSlash);
                        }

                        var previous = bytes.ToList();
                        if (attemptLatin1File)
                        {
                            bytes = Encoding.GetEncoding("ISO-8859-1", EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback).GetBytes(fileName);
                        }
                        else
                        {
                            bytes = Encoding.GetEncoding("UTF-8").GetBytes(fileName);
                        }

                        foreach(byte b in bytes)
                        {
                            previous.Add(b);
                        }

                        bytes = previous.ToArray();
                    }
                }
                catch (Exception)
                {
                    bytes = Encoding.GetEncoding("UTF-8").GetBytes(value);
                }
            }
            else
            {
                bytes = Encoding.GetEncoding("UTF-8").GetBytes(value);
            }

            return WriteBytes(BitConverter.GetBytes(bytes.Length))
                .WriteBytes(bytes);
        }

        private void Compress(byte[] inData, out byte[] outData)
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
                using MemoryStream outMemoryStream = new MemoryStream();
                using ZOutputStream outZStream = new ZOutputStream(outMemoryStream, zlibConst.Z_DEFAULT_COMPRESSION);
                using Stream inMemoryStream = new MemoryStream(inData);

                CopyStream(inMemoryStream, outZStream);
                outZStream.finish();
                outData = outMemoryStream.ToArray();
            }
            catch (Exception ex)
            {
                throw new MessageCompressionException("Failed to compress the message payload", ex);
            }
        }

        private MessageBuilder WriteCode(byte[] code)
        {
            CodeBytes = code.ToList();
            return this;
        }
    }
}