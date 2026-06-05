using Seeker.Helpers;

namespace Seeker.Settings.Rows
{
    internal static class PreferenceWriter
    {
        private static readonly object _lock = new object();

        public static void WriteBool(string key, bool value)
        {
            lock (_lock)
            {
                var editor = SeekerState.SharedPreferences.Edit();
                editor.PutBoolean(key, value);
                editor.Apply();
            }
        }

        public static void WriteInt(string key, int value)
        {
            lock (_lock)
            {
                var editor = SeekerState.SharedPreferences.Edit();
                editor.PutInt(key, value);
                editor.Apply();
            }
        }
    }
}
