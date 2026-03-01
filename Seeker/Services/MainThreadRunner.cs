using System;

namespace Seeker.Services
{
    public class MainThreadRunner : IMainThreadRunner
    {
        public void RunOnUiThread(Action action)
        {
            SeekerState.ActiveActivityRef?.RunOnUiThread(action);
        }
    }
}
