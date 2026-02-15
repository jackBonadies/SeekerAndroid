#if DEBUG
using Seeker.Helpers;
using Soulseek;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public static class TestClient
{
    public static async Task<IReadOnlyCollection<SearchResponse>> SearchAsync(string searchString, Action<SearchResponseReceivedEventArgs> actionToInvoke, CancellationToken ct)
    {

        await Task.Delay(2).ConfigureAwait(false);
        var responseBag = new System.Collections.Concurrent.ConcurrentBag<SearchResponse>();
        Random r = new Random();
        int x = r.Next(0, 3);
        int maxSleep = 100;
        switch (x)
        {
            case 0:
                maxSleep = 1; //v fast case
                break;
            case 1:
                maxSleep = 10; //fast case
                break;
            case 2:
                maxSleep = 200; //trickling in case
                break;
        }
        Logger.Debug("max sleep: " + maxSleep);
        for (int i = 0; i < 1000; i++)
        {
            List<Soulseek.File> fs = new List<Soulseek.File>();
            for (int j = 0; j < 15; j++)
            {
                fs.Add(new Soulseek.File(1, searchString + i + "\\" + $"{j}. test filename " + i, 0, ".mp3", null));
            }
            //1 in 15 chance of being locked
            bool locked = false;
            if (r.Next(0, 15) == 0)
            {
                locked = true;
            }
            SearchResponse response = new SearchResponse("test" + i, r.Next(0, 100000), r.Next(0, 10), r.Next(0, 12345), (long)(r.Next(0, 14556)), locked ? null : fs, locked ? fs : null);
            var eventArgs = new SearchResponseReceivedEventArgs(response, null);
            responseBag.Add(response);
            actionToInvoke(eventArgs);
            ct.ThrowIfCancellationRequested();
            System.Threading.Thread.Sleep(r.Next(0, maxSleep));
        }
        return responseBag.ToList().AsReadOnly();
    }
}
#endif
