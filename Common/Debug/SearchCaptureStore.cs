#if DEBUG || MOCK
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using MessagePack;
using Seeker.Helpers;
using Soulseek;
using IoFile = System.IO.File;

namespace Seeker.Debug
{
    public static class SearchCaptureStore
    {
        private static string? _captureRoot;

        public static EncryptedFileHelper? FileHelper { get; private set; }

        public static bool IsConfigured => FileHelper != null && _captureRoot != null;

        public static void Configure(string captureRootPath, string passphrase)
        {
            if (string.IsNullOrWhiteSpace(passphrase))
            {
                Logger.Debug("SearchCaptureStore: no passphrase supplied, capture/replay disabled.");
                FileHelper = null;
                _captureRoot = null;
                return;
            }

            FileHelper = new EncryptedFileHelper(passphrase);
            _captureRoot = captureRootPath;
            Logger.Debug("SearchCaptureStore: configured at " + captureRootPath);
        }

        public static string GetFilename(string normalizedQuery)
        {
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(normalizedQuery));
            var sb = new StringBuilder(16 * 2 + 7);
            sb.Append("sc_");
            for (int i = 0; i < 8; i++) sb.Append(hash[i].ToString("x2"));
            sb.Append(".bin");
            return sb.ToString();
        }

        public static void Save(string query, IReadOnlyCollection<SearchResponse> responses)
        {
            if (!IsConfigured || responses == null || responses.Count == 0) return;

            try
            {
                var payload = new CapturePayload
                {
                    Query = query,
                    TimestampUtcTicks = DateTime.UtcNow.Ticks,
                    Responses = new List<SearchResponse>(responses),
                };
                byte[] plaintext = MessagePackSerializer.Serialize(payload, options: SerializationHelper.SearchResponseOptions);
                var path = Path.Combine(_captureRoot!, GetFilename(Normalize(query)));
                FileHelper!.WriteToFile(path, plaintext);
                Logger.Debug($"SearchCaptureStore: saved {responses.Count} responses for \"{query}\" -> {path}");
            }
            catch (Exception ex)
            {
                Logger.Debug("SearchCaptureStore.Save failed: " + ex);
            }
        }

        public static bool TryLoad(string query, out List<SearchResponse> responses, out string? originalQuery)
        {
            responses = new List<SearchResponse>();
            originalQuery = null;
            if (!IsConfigured) return false;

            try
            {
                var path = Path.Combine(_captureRoot!, GetFilename(Normalize(query)));
                var payload = LoadRaw(path);
                responses = payload.Responses ?? new List<SearchResponse>();
                originalQuery = payload.Query;
                return responses.Count > 0;
            }
            catch (Exception ex)
            {
                Logger.Debug("SearchCaptureStore.TryLoad failed: " + ex);
                return false;
            }
        }
        
        public static CapturePayload LoadRaw(string path)
        {
            if (!FileHelper!.TryReadFromFile(path, out byte[] plaintext)) throw new Exception("Failed to read file");
            var payload = MessagePackSerializer.Deserialize<CapturePayload>(plaintext, options: SerializationHelper.SearchResponseOptions);
            return payload;
        }

        private static string Normalize(string query) => (query ?? string.Empty).Trim().ToLowerInvariant();

        [MessagePackObject(keyAsPropertyName: true)]
        public class CapturePayload
        {
            public string Query { get; set; } = string.Empty;
            public long TimestampUtcTicks { get; set; }
#pragma warning disable MsgPack003 // Use MessagePackObjectAttribute
            public List<SearchResponse>? Responses { get; set; }
#pragma warning restore MsgPack003 // Use MessagePackObjectAttribute
        }
    }
}
#endif
