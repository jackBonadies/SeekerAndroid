using System.IO;

namespace Seeker.Serialization
{
    public interface ICacheDataProvider
    {
        bool CacheExists();
        void EnsureCacheExists();
        Stream OpenRead(string filename);
        void Write(string filename, byte[] data);
        int GetCachedFileCount();
        void SaveCachedFileCount(int count);
    }
}
