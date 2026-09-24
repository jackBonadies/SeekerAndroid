using System;
using System.Collections.Generic;

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
        }

        // In order of accepted upload requests
        private readonly List<Entry> entries = new List<Entry>();

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

        public void MarkStarted(Entry entry)
        {
            lock (entries)
            {
                entry.Started = true;
            }
        }

        public void Remove(Entry entry)
        {
            lock (entries)
            {
                entries.Remove(entry);
            }
        }

        // round robin
        public int? EstimatePosition(string username, string filename)
        {
            lock (entries)
            {
                Entry target = null;
                int ahead = 0;
                var seen = new HashSet<(string, string)>();
                var queuedPerOtherUser = new Dictionary<string, int>();
                foreach (var entry in entries)
                {
                    if (!seen.Add((entry.Username, entry.Filename)))
                    {
                        continue;
                    }
                    if (entry.Username == username && entry.Filename == filename)
                    {
                        target = entry;
                        continue;
                    }
                    if (entry.Started)
                    {
                        continue;
                    }
                    if (entry.Username == username)
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

                int position = 1 + ahead;
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
