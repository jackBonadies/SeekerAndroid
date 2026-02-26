using Android.Content;
using AndroidX.DocumentFile.Provider;
using Common;
using Seeker.Helpers;
using Seeker.Serialization;
using System.IO;

namespace Seeker
{
    public class AndroidCacheDataProvider : ICacheDataProvider
    {
        private readonly Context _context;
        private readonly Java.IO.File _cacheDir;

        public AndroidCacheDataProvider(Context context)
        {
            _context = context;
            _cacheDir = new Java.IO.File(context.FilesDir, KeyConsts.M_fileshare_cache_dir);
        }

        public bool CacheExists() => _cacheDir.Exists();

        public void EnsureCacheExists()
        {
            if (!_cacheDir.Exists())
            {
                _cacheDir.Mkdir();
            }
        }

        public Stream OpenRead(string filename)
        {
            var file = new Java.IO.File(_cacheDir, filename);
            if (!file.Exists())
            {
                return null;
            }

            return _context.ContentResolver.OpenInputStream(DocumentFile.FromFile(file).Uri);
        }

        public void Write(string filename, byte[] data)
        {
            CommonHelpers.SaveToDisk(_context, data, _cacheDir, filename);
        }

        public int GetCachedFileCount()
        {
            return SeekerState.SharedPreferences.GetInt(KeyConsts.M_CACHE_nonHiddenFileCount_v3, -1);
        }

        public void SaveCachedFileCount(int count)
        {
            PreferencesManager.SaveCachedFileCount(count);
        }
    }
}
