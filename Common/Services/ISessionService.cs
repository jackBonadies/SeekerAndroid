using System;

namespace Seeker.Services
{
    public interface ISessionService
    {
        bool RunWithReconnect(Action action, bool silent = false);
    }
}
