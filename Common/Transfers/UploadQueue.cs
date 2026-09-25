using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Seeker
{
    public sealed class UploadQueue
    {
        public sealed class Entry
        {
            internal Entry(string username, string filename)
            {
                Username = username;
                Filename = filename;
            }

            public string Username { get; }
            public string Filename { get; }
            public bool Started { get; internal set; }
            public bool HasSlot { get; internal set; }

            // non-null while waiting for a slot
            internal TaskCompletionSource<bool> SlotWaiter;
            // global order
            internal long WaitSequence;
        }

        // In order of accepted upload requests
        private readonly List<Entry> entries = new List<Entry>();

        private readonly Func<string, bool> isPrivileged;
        private int slotLimit;
        private int usedSlots;

        // global wait counter
        private long waitCounter;

        public UploadQueue(Func<string, bool>? isPrivileged = null, int slotLimit = int.MaxValue)
        {
            this.isPrivileged = isPrivileged ?? (_ => false);
            this.slotLimit = Math.Max(1, slotLimit);
        }

        // lowering it never revokes a slot, just let what is running finish
        public int SlotLimit
        {
            get
            {
                lock (entries)
                {
                    return slotLimit;
                }
            }
            set
            {
                lock (entries)
                {
                    slotLimit = Math.Max(1, value);
                    GrantWaitingSlots();
                }
            }
        }

        public int Count
        {
            get
            {
                lock (entries)
                {
                    return entries.Count;
                }
            }
        }

        public Entry Enqueue(string username, string filename)
        {
            var entry = new Entry(username, filename);
            lock (entries)
            {
                entries.Add(entry);
            }
            return entry;
        }

        public void Remove(Entry entry)
        {
            lock (entries)
            {
                entries.Remove(entry);
                if (entry.SlotWaiter != null)
                {
                    var waiter = entry.SlotWaiter;
                    entry.SlotWaiter = null;
                    waiter.TrySetCanceled();
                }
                if (entry.HasSlot)
                {
                    entry.HasSlot = false;
                    usedSlots--;
                }
                GrantWaitingSlots();
            }
        }

        // we pass this to the slsk.net
        public Task AwaitSlotAsync(Entry entry, CancellationToken cancellationToken)
        {
            TaskCompletionSource<bool> waiter;
            lock (entries)
            {
                if (entry.SlotWaiter != null || entry.HasSlot)
                {
                    throw new InvalidOperationException($"{entry.Filename} for {entry.Username} is already waiting for or holding a slot");
                }
                waiter = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                entry.SlotWaiter = waiter;
                entry.WaitSequence = ++waitCounter;
                GrantWaitingSlots();
            }

            if (!waiter.Task.IsCompleted && cancellationToken.CanBeCanceled)
            {
                var registration = cancellationToken.Register(() => CancelWait(entry, waiter, cancellationToken));
                waiter.Task.ContinueWith(_ => registration.Dispose(), TaskScheduler.Default);
            }
            return waiter.Task;
        }

        // we pass this to the slsk.net
        public void ReleaseSlot(Entry entry)
        {
            lock (entries)
            {
                if (!entry.HasSlot)
                {
                    return;
                }
                entry.HasSlot = false;
                usedSlots--;
                GrantWaitingSlots();
            }
        }

        private void CancelWait(Entry entry, TaskCompletionSource<bool> waiter, CancellationToken cancellationToken)
        {
            lock (entries)
            {
                // already granted: the library will call ReleaseSlot
                if (entry.SlotWaiter != waiter)
                {
                    return;
                }
                entry.SlotWaiter = null;
                waiter.TrySetCanceled(cancellationToken);
            }
        }

        private void GrantWaitingSlots()
        {
            while (usedSlots < slotLimit)
            {
                var next = SelectNextWaiter();
                if (next == null)
                {
                    return;
                }
                var waiter = next.SlotWaiter;
                next.SlotWaiter = null;
                next.HasSlot = true;
                // sticky, unlike HasSlot: the entry stays listed after release until the upload task ends
                next.Started = true;
                usedSlots++;
                waiter.TrySetResult(true);
            }
        }

        // Privileged first, oldest request first (like slskd)
        // Otherwise order by wait sequence.  We only assign a number after the transfer gets past
        //   the per user semaphore (limit: 1) so this doesnt cause any issues (if say a userA requests 
        //   a full folder. then later userA requests a full folder. we will still round robin
        //   even though all userB's come in after).
        private Entry? SelectNextWaiter()
        {
            Entry? best = null;
            bool isBestPrivileged = false;
            foreach (var entry in entries)
            {
                if (entry.SlotWaiter == null)
                {
                    continue;
                }
                bool privileged = isPrivileged(entry.Username);
                bool better;
                if (best == null)
                {
                    better = true;
                }
                else if (privileged != isBestPrivileged)
                {
                    // i.e. if we are privileged and they arent we are better
                    //   vice versa we are worse. else we check wait sequence
                    better = privileged;
                }
                else
                {
                    better = entry.WaitSequence < best.WaitSequence;
                }
                if (better)
                {
                    best = entry;
                    isBestPrivileged = privileged;
                }
            }
            return best;
        }

        // round robin
        // if we are privileged then we only consider other privileged
        // if we are not privileged then all privileged has priority ahead of us
        public int? EstimatePosition(string targetUsername, string filename)
        {
            lock (entries)
            {
                bool targetPrivileged = isPrivileged(targetUsername);
                Entry target = null;
                int ahead = 0;
                int privilegedQueued = 0;
                var seen = new HashSet<(string, string)>();
                var queuedPerOtherUser = new Dictionary<string, int>();
                foreach (var entry in entries)
                {
                    if (!seen.Add((entry.Username, entry.Filename)))
                    {
                        continue;
                    }
                    if (entry.Username == targetUsername && entry.Filename == filename)
                    {
                        target = entry;
                        continue;
                    }
                    if (entry.Started)
                    {
                        continue;
                    }
                    bool privileged = entry.Username == targetUsername ? targetPrivileged : isPrivileged(entry.Username);
                    if (!targetPrivileged && privileged)
                    {
                        // these are always ahead of us
                        privilegedQueued++;
                        continue;
                    }
                    if (targetPrivileged && !privileged)
                    {
                        // ignore non privileged users
                        continue;
                    }
                    if (entry.Username == targetUsername)
                    {
                        // all of our downloads which were queued first will always be ahead of us
                        if (target == null)
                        {
                            ahead++;
                        }
                    }
                    else
                    {
                        queuedPerOtherUser.TryGetValue(entry.Username, out int count);
                        queuedPerOtherUser[entry.Username] = count + 1;
                    }
                }

                if (target == null || target.Started)
                {
                    return null;
                }

                int position = 1 + privilegedQueued + ahead;
                foreach (int count in queuedPerOtherUser.Values)
                {
                    // if we go round robin (i.e. for each of our transfers in front of us (n) we also service
                    //   another x other users then we will have at most x*n
                    position += Math.Min(ahead, count);
                }
                return position;
            }
        }
    }
}
