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
        //   Same semantics otherwise: an existing file is returned as is, an IOException gives null.
        private static DocumentFile CreateRawFile(DocumentFile parent, string name)
        {
            var target = new Java.IO.File(parent.Uri.Path, name);
            try
            {
                target.CreateNewFile();
                return DocumentFile.FromFile(target);
            }
            catch (Java.IO.IOException e)
            {
                Logger.Debug("CreateRawFile failed for " + target.AbsolutePath + ": " + e.Message);
                return null;
            }
        }
    }
}
