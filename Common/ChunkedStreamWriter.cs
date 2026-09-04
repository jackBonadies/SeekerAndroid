using System;
using System.IO;

namespace Common
{
    /// <summary>
    /// Writes a byte[] to a stream in small chunks, each copied into its own buffer.
    /// This fixes a previous issue where we used Java.IO.OutputStream.Write(byte[], int, int), 
    /// which marshals via JNIEnv.NewArray(buffer) effectively doubling the memory (i.e. 2 byte arrays c# and java)
    /// Now we cross the JNI in small chunks.
    /// Only used for MemoryMode downloads.
    /// </summary>
    public static class ChunkedStreamWriter
    {
        public const int DefaultChunkSize = 81920;

        public static void WriteChunked(Stream to, ArraySegment<byte> bytes, int chunkSize = DefaultChunkSize)
        {
            CopyInChunks(bytes, to.Write, chunkSize);
            to.Flush();
        }

        public static void CopyInChunks(ArraySegment<byte> bytes, Action<byte[], int, int> write, int chunkSize = DefaultChunkSize)
        {
            if (bytes.Array == null || bytes.Count == 0)
            {
                return;
            }
            byte[] buffer = new byte[Math.Min(chunkSize, bytes.Count)];
            int offset = bytes.Offset;
            int remaining = bytes.Count;
            while (remaining > 0)
            {
                int count = Math.Min(buffer.Length, remaining);
                Buffer.BlockCopy(bytes.Array, offset, buffer, 0, count);
                write(buffer, 0, count);
                offset += count;
                remaining -= count;
            }
        }
    }
}
