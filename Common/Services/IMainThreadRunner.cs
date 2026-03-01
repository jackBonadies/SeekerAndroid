using System;

namespace Seeker.Services
{
    public interface IMainThreadRunner
    {
        void RunOnUiThread(Action action);
    }
}
