#if DEBUG
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using IoFile = System.IO.File;
using IoDirectory = System.IO.Directory;

namespace Seeker.Debug
{
    // AES-256-GCM encrypt/decrypt helper for any byte payload.
    // One instance per passphrase; safe to share across callers.
    //
    // File format (33-byte header):
    //   4  bytes  magic  "SEFD" (Seeker Encrypted File Data)
    //   1  byte   version = 1
    //   12 bytes  AES-GCM nonce (random per write)
    //   16 bytes  AES-GCM auth tag
    //   N  bytes  ciphertext
    public sealed class EncryptedFileHelper
    {
        private const uint MagicLe = 0x44464553; // "SEFD" LE
        private const byte Version = 1;
        private const int NonceSize = 12;
        private const int TagSize = 16;
        public const int HeaderSize = 4 + 1 + NonceSize + TagSize;

        private byte[] _key;

        public EncryptedFileHelper()
        {
            string passphrase = Environment.GetEnvironmentVariable("SeekerEncryptKey");
            setup(passphrase);
        }

        public EncryptedFileHelper(string encryptionKey)
        {
            setup(encryptionKey);
        }

        private void setup(string passphrase)
        {
            if (String.IsNullOrEmpty(passphrase))
            {
                throw new Exception("Encryption key not set. Please set the SeekerEncryptKey environment variable to a non-empty value.");
            }
            using var sha = SHA256.Create();
            _key = sha.ComputeHash(Encoding.UTF8.GetBytes(passphrase));
        }

        public byte[] Encrypt(byte[] plaintext)
        {
            byte[] nonce = new byte[NonceSize];
            RandomNumberGenerator.Fill(nonce);
            byte[] ciphertext = new byte[plaintext.Length];
            byte[] tag = new byte[TagSize];
            using (var aes = new AesGcm(_key))
            {
                aes.Encrypt(nonce, plaintext, ciphertext, tag);
            }
            using var ms = new MemoryStream(HeaderSize + ciphertext.Length);
            WriteHeader(ms, nonce, tag);
            ms.Write(ciphertext, 0, ciphertext.Length);
            return ms.ToArray();
        }

        public byte[] Decrypt(byte[] blob)
        {
            using var ms = new MemoryStream(blob);
            return ReadFrom(ms);
        }

        public void WriteTo(Stream output, byte[] plaintext)
        {
            byte[] blob = Encrypt(plaintext);
            output.Write(blob, 0, blob.Length);
        }

        public byte[] ReadFrom(Stream input)
        {
            using var br = new BinaryReader(input, Encoding.UTF8, leaveOpen: true);
            uint magic = br.ReadUInt32();
            if (magic != MagicLe)
            {
                throw new InvalidDataException($"Bad magic 0x{magic:X8} — not an SEFD file.");
            }
            byte version = br.ReadByte();
            if (version != Version)
            {
                throw new InvalidDataException($"Unsupported version {version}.");
            }
            byte[] nonce = br.ReadBytes(NonceSize);
            byte[] tag = br.ReadBytes(TagSize);

            // Read remaining bytes as ciphertext
            using var cipherMs = new MemoryStream();
            input.CopyTo(cipherMs);
            byte[] ciphertext = cipherMs.ToArray();

            byte[] plaintext = new byte[ciphertext.Length];
            using (var aes = new AesGcm(_key))
            {
                aes.Decrypt(nonce, ciphertext, tag, plaintext);
            }
            return plaintext;
        }

        public void WriteToFile(string path, byte[] plaintext)
        {
            IoDirectory.CreateDirectory(Path.GetDirectoryName(path)!);
            IoFile.WriteAllBytes(path, Encrypt(plaintext));
        }

        public bool TryReadFromFile(string path, out byte[] plaintext)
        {
            plaintext = Array.Empty<byte>();
            if (!IoFile.Exists(path)) return false;
            try
            {
                plaintext = Decrypt(IoFile.ReadAllBytes(path));
                return true;
            }
            catch (Exception ex)
            {
                Seeker.Helpers.Logger.Debug($"EncryptedFileHelper: failed to read {path}: {ex.Message}");
                return false;
            }
        }

        private static void WriteHeader(Stream s, byte[] nonce, byte[] tag)
        {
            using var bw = new BinaryWriter(s, Encoding.UTF8, leaveOpen: true);
            bw.Write(MagicLe);
            bw.Write(Version);
            bw.Write(nonce);
            bw.Write(tag);
        }
    }
}
#endif
