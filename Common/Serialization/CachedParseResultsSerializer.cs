using Common;
using MessagePack;
using Soulseek;
using System;
using System.Collections.Generic;
using System.IO;

namespace Seeker.Serialization
{
    public static class CachedParseResultsSerializer
    {
        public static CachedParseResults? Restore(ICacheDataProvider provider)
        {
            if (!provider.CacheExists())
            {
                return null;
            }

            var helperIndex = DeserializeFromProvider<Dictionary<int, string>>(provider, KeyConsts.M_HelperIndex_Filename);
            var tokenIndex = DeserializeFromProvider<Dictionary<string, List<int>>>(provider, KeyConsts.M_TokenIndex_Filename);
            var keys = DeserializeFromProvider<Dictionary<string, Tuple<long, string, Tuple<int, int, int, int>, bool, bool>>>(provider, KeyConsts.M_Keys_Filename);
            var browseResponse = DeserializeFromProvider<BrowseResponse>(provider, KeyConsts.M_BrowseResponse_Filename, SerializationHelper.BrowseResponseOptions);
            var browseResponseHidden = DeserializeFromProvider<List<Soulseek.Directory>>(provider, KeyConsts.M_BrowseResponse_Hidden_Filename, SerializationHelper.BrowseResponseOptions);
            var friendlyDirToUri = DeserializeFromProvider<List<Tuple<string, string>>>(provider, KeyConsts.M_FriendlyDirNameToUri_Filename);

            int nonHiddenFileCount = provider.GetCachedFileCount();

            return new CachedParseResults(
                keys,
                browseResponse.DirectoryCount,
                browseResponse,
                browseResponseHidden,
                friendlyDirToUri,
                tokenIndex,
                helperIndex,
                nonHiddenFileCount);
        }

        public static void Store(ICacheDataProvider provider, CachedParseResults cached)
        {
            provider.EnsureCacheExists();

            byte[] data = MessagePackSerializer.Serialize(cached.helperIndex);
            provider.Write(KeyConsts.M_HelperIndex_Filename, data);

            data = MessagePackSerializer.Serialize(cached.tokenIndex);
            provider.Write(KeyConsts.M_TokenIndex_Filename, data);

            data = MessagePackSerializer.Serialize(cached.keys);
            provider.Write(KeyConsts.M_Keys_Filename, data);

            data = MessagePackSerializer.Serialize(cached.browseResponse, options: SerializationHelper.BrowseResponseOptions);
            provider.Write(KeyConsts.M_BrowseResponse_Filename, data);

            data = MessagePackSerializer.Serialize(cached.browseResponseHiddenPortion, options: SerializationHelper.BrowseResponseOptions);
            provider.Write(KeyConsts.M_BrowseResponse_Hidden_Filename, data);

            data = MessagePackSerializer.Serialize(cached.friendlyDirNameToUriMapping);
            provider.Write(KeyConsts.M_FriendlyDirNameToUri_Filename, data);

            provider.SaveCachedFileCount(cached.nonHiddenFileCount);
        }

        private static T DeserializeFromProvider<T>(ICacheDataProvider provider, string filename, MessagePackSerializerOptions? options = null) where T : class
        {
            using (Stream stream = provider.OpenRead(filename))
            {
                if (stream == null)
                {
                    return null;
                }

                return MessagePackSerializer.Deserialize<T>(stream, options);
            }
        }
    }
}
