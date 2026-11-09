namespace Seeker.Helpers
{
    // Android Mimetype fails for m4a files, regarding them as mp3 making them unplayable
    //   Other failures: .alac, .ape -> null.  .m4p -> mp4
    //   Here we capture those cases
    public static class MediaMimeTypes
    {
        public const string M4A_MIME = "audio/m4a";
        public const string APE_MIME = "audio/x-ape";

        public static string GetMimeTypeFromFilename(string filename)
        {
            string ext = System.IO.Path.GetExtension(filename).ToLowerInvariant();
            string mimeType = @"audio/mpeg"; //default
            if (ext != null && ext != string.Empty)
            {
                switch (ext)
                {
                    case ".ape":
                        mimeType = APE_MIME;
                        break;
                    case ".m4a":
                        mimeType = M4A_MIME;
                        break;
                    default:
                        ext = ext.TrimStart('.');
                        mimeType = Android.Webkit.MimeTypeMap.Singleton.GetMimeTypeFromExtension(ext);
                        break;
                }

            }
            return mimeType;
        }

        public static string GetMimeTypeForCreate(string filename)
        {
            if (string.IsNullOrEmpty(System.IO.Path.GetExtension(filename)))
            {
                return null;
            }
            return GetMimeTypeFromFilename(filename);
        }
    }
}
