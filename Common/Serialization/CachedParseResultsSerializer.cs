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

            var fileKeyToPresentableName = DeserializeFromProvider<Dictionary<int, string>>(provider, KeyConsts.M_HelperIndex_Filename);
            var searchTermTokenToListOfFileKeys = DeserializeFromProvider<Dictionary<string, List<int>>>(provider, KeyConsts.M_TokenIndex_Filename);
            var presentableNameToFullFileInfo = DeserializeFromProvider<Dictionary<string, Tuple<long, string, Tuple<int, int, int, int>, bool, bool>>>(provider, KeyConsts.M_Keys_Filename);
            var browseResponse = DeserializeFromProvider<BrowseResponse>(provider, KeyConsts.M_BrowseResponse_Filename, SerializationHelper.BrowseResponseOptions);
            var browseResponseHidden = DeserializeFromProvider<List<Soulseek.Directory>>(provider, KeyConsts.M_BrowseResponse_Hidden_Filename, SerializationHelper.BrowseResponseOptions);
            var presentableDirectoryNameToDirectoryUriMappings = DeserializeFromProvider<List<Tuple<string, string>>>(provider, KeyConsts.M_FriendlyDirNameToUri_Filename);

            int nonHiddenFileCount = provider.GetCachedFileCount();

            return new CachedParseResults(
                presentableNameToFullFileInfo,
                browseResponse.DirectoryCount,
                browseResponse,
                browseResponseHidden,
                presentableDirectoryNameToDirectoryUriMappings,
                searchTermTokenToListOfFileKeys,
                fileKeyToPresentableName,
                nonHiddenFileCount);
        }

        public static void Store(ICacheDataProvider provider, CachedParseResults cached)
        {
            provider.EnsureCacheExists();

            byte[] data = MessagePackSerializer.Serialize(cached.FileKeyToPresentableName);
            provider.Write(KeyConsts.M_HelperIndex_Filename, data);

            data = MessagePackSerializer.Serialize(cached.SearchTermTokenToListOfFileKeys);
            provider.Write(KeyConsts.M_TokenIndex_Filename, data);

            data = MessagePackSerializer.Serialize(cached.PresentableNameToFullFileInfo);
            provider.Write(KeyConsts.M_Keys_Filename, data);

            data = MessagePackSerializer.Serialize(cached.BrowseResponse, options: SerializationHelper.BrowseResponseOptions);
            provider.Write(KeyConsts.M_BrowseResponse_Filename, data);

            data = MessagePackSerializer.Serialize(cached.BrowseResponseHiddenPortion, options: SerializationHelper.BrowseResponseOptions);
            provider.Write(KeyConsts.M_BrowseResponse_Hidden_Filename, data);

            data = MessagePackSerializer.Serialize(cached.PresentableDirectoryNameToDirectoryUriMappings);
            provider.Write(KeyConsts.M_FriendlyDirNameToUri_Filename, data);

            provider.SaveCachedFileCount(cached.NonHiddenFileCount);
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
