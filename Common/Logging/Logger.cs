using System;

namespace Seeker.Helpers
{
    public static class Logger
    {
        public static ILoggerBackend Backend { get; set; }

        public static void Debug(string msg) => Backend?.Debug(msg);
        public static void Firebase(string msg) => Backend?.Firebase(msg);
        public static void FirebaseError(string msg, Exception e) => Backend?.FirebaseError(msg, e);
        public static void InfoFirebase(string msg) => Backend?.InfoFirebase(msg);
    }
}
