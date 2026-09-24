using System;

namespace Seeker.Helpers
{
    public interface ILoggerBackend
    {
        void Debug(string msg);
        void Info(string msg);
        void Firebase(string msg);
        void FirebaseError(string msg, Exception e);
        void InfoFirebase(string msg);
    }
}
