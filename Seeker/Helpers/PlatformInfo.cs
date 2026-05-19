using System;

namespace Seeker.Helpers
{
    public static class PlatformInfo
    {
        public static bool RequiresEitherOpenDocumentTreeOrManageAllFiles()
        {
            //29 does has the requestExternalStorage workaround.
            return OperatingSystem.IsAndroidVersionAtLeast(30);
        }

        public static bool UseLegacyStorage()
        {
            return !OperatingSystem.IsAndroidVersionAtLeast(29);
        }

        public static bool PreMoveDocument()
        {
            return !OperatingSystem.IsAndroidVersionAtLeast(24);
        }

        public static bool IsLowDpi()
        {
            return Android.Content.Res.Resources.System.DisplayMetrics.WidthPixels < 768;
        }
    }
}
