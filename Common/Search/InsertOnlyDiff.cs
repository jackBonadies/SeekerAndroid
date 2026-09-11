using System.Collections.Generic;

namespace Common.Search
{
    /// <summary>
    /// Diff for a list that only ever grows (so old list is subsequence of new one)
    /// </summary>
    public static class InsertOnlyDiff
    {
        /// <summary>
        /// Walks both lists front to back. Every item of <paramref name="oldList"/> must appear in
        /// <paramref name="newList"/> in the same relative order; whatever lies between two matches
        /// is an insert. Returns false (and null runs) if something was removed or reordered.
        /// </summary>
        public static bool TryComputeInsertRuns<T>(IReadOnlyList<T> oldList, IReadOnlyList<T> newList, out List<(int Start, int Count)>? runs) where T : class
        {
            runs = new List<(int Start, int Count)>();
            int oldCount = oldList.Count;
            int newCount = newList.Count;
            int i = 0;
            int j = 0;
            while (j < newCount)
            {
                if (i < oldCount && ReferenceEquals(oldList[i], newList[j]))
                {
                    i++;
                    j++;
                    continue;
                }
                int start = j;
                // keep moving in the new list until we get the next item in the old list
                //   everything in between got added
                while (j < newCount && (i >= oldCount || !ReferenceEquals(oldList[i], newList[j])))
                {
                    j++;
                }
                runs.Add((start, j - start));
            }
            if (i != oldCount)
            {
                // an old item never showed up in the new list, so this is not insert-only
                runs = null;
                return false;
            }
            return true;
        }
    }
}
