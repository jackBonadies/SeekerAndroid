using Soulseek;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Seeker
{
    // limit the number of simultaneous users we download from. protocol makes this imperfect, that is, we cannot 
    //   join a queue and then pause when its our turn without the remote client aborting.  therefore, we make a tradeoff
    //   where we will temporarily have sum(queued + in progress) > N which may result in sum(in progress) > N for brief
    //   periods.  The problem with sum(queued + in progress) <= N, is if N are queued, then the user is just sitting 
    //   there when they could be downloading.
    public sealed class DownloadUserSlotsLimiter
    {
        // after 20 seconds of being in queue, a user is considered idle and its slot given to another user
        // has the downside that once its our turn we will temporarily be over the limit.
        public static readonly TimeSpan DefaultGracePeriod = TimeSpan.FromSeconds(20);
        // a hard cap, sum(queued + in progress) <= N
        public static readonly TimeSpan InfiniteGracePeriod = Timeout.InfiniteTimeSpan;

        private sealed class UserState
        {
            // admitted batch loops still requesting
            public int Batches;
            // current in progress in the library
            public int InFlight;
            // Queued | Remotely in the library
            public int Queued;
            // when Batches and InFlight last both reached 0
            public DateTime IdleSinceUtc;
        }

        private sealed class Waiter
        {
            public string Username = string.Empty;
            public TaskCompletionSource<bool> Tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        internal enum FileState
        {
            None,
            InFlight,
            Queued,
        }

        // tracks the state of the files in the batch
        public sealed class FileTracker
        {
            private readonly DownloadUserSlotsLimiter owner;
            internal readonly string Username;
            internal FileState Current = FileState.None;
            internal bool Finished;

            internal FileTracker(DownloadUserSlotsLimiter owner, string username)
            {
                this.owner = owner;
                Username = username;
            }

            public void Update(TransferStates state)
            {
                owner.SetFileState(this, Classify(state));
            }

            // idempotent
            public void Finish()
            {
                owner.SetFileState(this, FileState.None, finish: true);
            }
        }

        private readonly object syncRoot = new object();
        private readonly Dictionary<string, UserState> users = new Dictionary<string, UserState>();
        private readonly LinkedList<Waiter> waiters = new LinkedList<Waiter>();
        private readonly Func<int> limit;
        private readonly TimeSpan gracePeriod;
        private readonly Func<DateTime> utcNow;
        private readonly Action<string>? log;
        private readonly Timer? timer;

        public DownloadUserSlotsLimiter(Func<int> limit, TimeSpan gracePeriod, Func<DateTime>? utcNow = null, Action<string>? log = null, bool useTimer = true)
        {
            this.limit = limit ?? throw new ArgumentNullException(nameof(limit));
            if (gracePeriod < TimeSpan.Zero && gracePeriod != InfiniteGracePeriod)
            {
                throw new ArgumentOutOfRangeException(nameof(gracePeriod));
            }
            this.gracePeriod = gracePeriod;
            this.utcNow = utcNow ?? (() => DateTime.UtcNow);
            this.log = log;
            if (useTimer)
            {
                timer = new Timer(_ => AdmitWaiters(), null, Timeout.Infinite, Timeout.Infinite);
            }
        }

        private static FileState Classify(TransferStates state)
        {
            if (state.HasFlag(TransferStates.Completed) || state == TransferStates.None)
            {
                return FileState.None;
            }
            if (state.HasFlag(TransferStates.Queued) && state.HasFlag(TransferStates.Remotely))
            {
                return FileState.Queued;
            }
            return FileState.InFlight;
        }

        private int CurrentLimit()
        {
            return Math.Max(1, limit());
        }

        private bool IsCounted(UserState s, DateTime now)
        {
            if (s.Batches > 0 || s.InFlight > 0)
            {
                return true;
            }
            // here we allow for another slot if there was a slot stuck in queue past the grace period
            return s.Queued > 0 && InGracePeriod(s, now);
        }

        private bool InGracePeriod(UserState s, DateTime now)
        {
            if (gracePeriod == InfiniteGracePeriod)
            {
                return true;
            }
            return now < s.IdleSinceUtc + gracePeriod;
        }

        private int CountedUsers(DateTime now)
        {
            int n = 0;
            foreach (var s in users.Values)
            {
                if (IsCounted(s, now))
                {
                    n++;
                }
            }
            return n;
        }

        private bool IsUserCounted(string username, DateTime now)
        {
            return users.TryGetValue(username, out var s) && IsCounted(s, now);
        }

        private UserState GetOrAdd(string username)
        {
            if (!users.TryGetValue(username, out var s))
            {
                s = new UserState();
                users[username] = s;
            }
            return s;
        }

        private void RemoveIfEmpty(string username, UserState s)
        {
            if (s.Batches == 0 && s.InFlight == 0 && s.Queued == 0)
            {
                users.Remove(username);
            }
        }

        // returns task which completes once the user can proceed. cancelling removes the wait.
        public Task AcquireBatchAsync(string username, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }
            Waiter waiter;
            int counted;
            lock (syncRoot)
            {
                var now = utcNow();
                if (IsUserCounted(username, now) || (waiters.Count == 0 && CountedUsers(now) < CurrentLimit()))
                {
                    GetOrAdd(username).Batches++;
                    return Task.CompletedTask;
                }
                waiter = new Waiter { Username = username };
                waiters.AddLast(waiter);
                counted = CountedUsers(now);
                ScheduleTimer(now);
            }
            log?.Invoke($"download slots: {username} waiting ({counted}/{CurrentLimit()} users)");
            if (cancellationToken.CanBeCanceled)
            {
                var registration = cancellationToken.Register(() =>
                {
                    lock (syncRoot)
                    {
                        if (!waiters.Remove(waiter))
                        {
                            // already admitted
                            return;
                        }
                    }
                    waiter.Tcs.TrySetCanceled();
                    AdmitWaiters();
                });
                // disposed from a continuation, never under the lock (Dispose waits for a callback in flight)
                waiter.Tcs.Task.ContinueWith(_ => registration.Dispose(), CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
            }
            return waiter.Tcs.Task;
        }

        public void ReleaseBatch(string username)
        {
            lock (syncRoot)
            {
                if (!users.TryGetValue(username, out var s) || s.Batches == 0)
                {
                    return;
                }
                s.Batches--;
                if (s.Batches == 0 && s.InFlight == 0)
                {
                    s.IdleSinceUtc = utcNow();
                }
                RemoveIfEmpty(username, s);
            }
            AdmitWaiters();
        }

        public FileTracker Track(string username)
        {
            return new FileTracker(this, username);
        }

        private void SetFileState(FileTracker tracker, FileState next, bool finish = false)
        {
            lock (syncRoot)
            {
                if (tracker.Finished)
                {
                    return;
                }
                if (finish)
                {
                    tracker.Finished = true;
                }
                var prev = tracker.Current;
                if (prev == next)
                {
                    return;
                }
                tracker.Current = next;
                var s = GetOrAdd(tracker.Username);
                Adjust(s, prev, -1);
                Adjust(s, next, +1);
                if (prev == FileState.InFlight && s.InFlight == 0 && s.Batches == 0)
                {
                    s.IdleSinceUtc = utcNow();
                }
                RemoveIfEmpty(tracker.Username, s);
            }
            AdmitWaiters();
        }

        private static void Adjust(UserState s, FileState state, int delta)
        {
            if (state == FileState.InFlight)
            {
                s.InFlight += delta;
            }
            else if (state == FileState.Queued)
            {
                s.Queued += delta;
            }
        }

        // admits waiters in order while there is room. a waiter whose user holds a slot is always admitted.
        public void AdmitWaiters()
        {
            List<Waiter>? admitted = null;
            List<string>? messages = null;
            lock (syncRoot)
            {
                if (waiters.Count == 0)
                {
                    return;
                }
                var now = utcNow();
                int counted = CountedUsers(now);
                int max = CurrentLimit();
                var node = waiters.First;
                while (node != null)
                {
                    var next = node.Next;
                    var w = node.Value;
                    bool userCounted = IsUserCounted(w.Username, now);
                    if (userCounted || counted < max)
                    {
                        if (!userCounted)
                        {
                            counted++;
                        }
                        GetOrAdd(w.Username).Batches++;
                        waiters.Remove(node);
                        (admitted ??= new List<Waiter>()).Add(w);
                        if (log != null)
                        {
                            (messages ??= new List<string>()).Add($"download slots: {w.Username} admitted ({counted}/{max} users)");
                        }
                    }
                    node = next;
                }
                ScheduleTimer(now);
            }
            if (admitted == null)
            {
                return;
            }
            if (messages != null)
            {
                foreach (var m in messages)
                {
                    log!.Invoke(m);
                }
            }
            foreach (var w in admitted)
            {
                w.Tcs.TrySetResult(true);
            }
        }

        // wake at the next grace expiry, to check if a user has been stuck in queue
        //   and if so admit another user
        private void ScheduleTimer(DateTime now)
        {
            if (timer == null)
            {
                return;
            }
            // with an infinite grace nothing ever expires
            if (waiters.Count == 0 || gracePeriod == InfiniteGracePeriod)
            {
                timer.Change(Timeout.Infinite, Timeout.Infinite);
                return;
            }
            DateTime? earliest = null;
            foreach (var s in users.Values)
            {
                if (s.Batches == 0 && s.InFlight == 0 && s.Queued > 0)
                {
                    var expiry = s.IdleSinceUtc + gracePeriod;
                    if (expiry > now && (earliest == null || expiry < earliest))
                    {
                        earliest = expiry;
                    }
                }
            }
            if (earliest == null)
            {
                timer.Change(Timeout.Infinite, Timeout.Infinite);
                return;
            }
            var due = earliest.Value - now + TimeSpan.FromMilliseconds(50);
            timer.Change(due, Timeout.InfiniteTimeSpan);
        }

        public int CountedUsersForTest()
        {
            lock (syncRoot)
            {
                return CountedUsers(utcNow());
            }
        }

        public int WaitingForTest()
        {
            lock (syncRoot)
            {
                return waiters.Count;
            }
        }
    }
}
