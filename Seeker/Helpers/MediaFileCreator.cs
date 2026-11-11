using AndroidX.DocumentFile.Provider;
using Common;

namespace Seeker.Helpers
{
    public static class MediaFileCreator
    {
        // Always the full name: if we strip the extension it lets the provider pick one from the MIME type (.ogg became .oga).
        //   We always want to respect whatever filename the uploader gave us.
        public static DocumentFile CreateMediaFile(DocumentFile parent, string name)
        {
            if (parent.Uri.Scheme == "file")
            {
                return CreateRawFile(parent, name);
            }
            return parent.CreateFile(MediaMimeTypes.GetMimeTypeForCreate(name), name);
        }

        // RawDocumentFile.createFile always appends the MIME type's extension (a.flac -> a.flac.flac), so bypass it.
        //   Names an existing file "a (1).flac" like FileSystemProvider (never overwrites). null on failure.
        private static DocumentFile CreateRawFile(DocumentFile parent, string name)
        {
            var (baseName, ext) = SplitFileName(MediaMimeTypes.GetMimeTypeForCreate(name), name);
            var dir = new Java.IO.File(parent.Uri.Path);
            var target = BuildFile(dir, baseName, ext);
            try
            {
                for (int n = 1; target.Exists(); n++)
                {
                    if (n > 32)
                    {
                        Logger.Firebase("CreateRawFile no unique name for " + name + " in " + dir.AbsolutePath);
                        return null;
                    }
                    target = BuildFile(dir, baseName + " (" + n + ")", ext);
                }
                if (!target.CreateNewFile())
                {
                    Logger.Firebase("CreateRawFile lost a race for " + target.AbsolutePath);
                    return null;
                }
                return DocumentFile.FromFile(target);
            }
            catch (Java.IO.IOException e)
            {
                Logger.Debug("CreateRawFile failed for " + target.AbsolutePath + ": " + e.Message);
                Logger.Firebase("CreateRawFile failed for " + target.AbsolutePath + ": " + e.Message);
                return null;
            }
        }

        // Port of AOSP FileUtils.splitFileName (files only), so the " (N)" lands where the provider puts it:
        //   before an extension that maps back to the MIME type, else after the whole name.
        private static (string name, string ext) SplitFileName(string mimeType, string displayName)
        {
            const string defaultMime = "application/octet-stream";
            var map = Android.Webkit.MimeTypeMap.Singleton;

            string name = displayName;
            string ext = null;
            string mimeTypeFromExt = null;
            int lastDot = displayName.LastIndexOf('.');
            if (lastDot >= 0)
            {
                name = displayName.Substring(0, lastDot);
                ext = displayName.Substring(lastDot + 1);
                mimeTypeFromExt = map.GetMimeTypeFromExtension(ext.ToLowerInvariant());
            }
            mimeTypeFromExt ??= defaultMime;

            string extFromMimeType = (mimeType == null || mimeType == defaultMime) ? null : map.GetExtensionFromMimeType(mimeType);
            if (mimeType != mimeTypeFromExt && ext != extFromMimeType)
            {
                name = displayName;
                ext = extFromMimeType;
            }
            return (name, ext);
        }

        private static Java.IO.File BuildFile(Java.IO.File dir, string name, string ext)
        {
            return new Java.IO.File(dir, string.IsNullOrEmpty(ext) ? name : name + "." + ext);
        }
    }
}
