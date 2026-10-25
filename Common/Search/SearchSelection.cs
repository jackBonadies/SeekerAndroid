using Soulseek;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Common.Search
{
    /// <summary>
    /// Stable identity for a grouped search-result row. Rows are not identified by adapter
    /// position: sorting, filtering and streaming insertions can move a row, while the same
    /// peer + representative file keeps its identity across ordinary result updates.
    /// </summary>
    public static class SearchResultIdentity
    {
        // Unit separator: not valid in a Slsk filename, so it cannot make two distinct pairs collide.
        private const string Separator = "\u001f";

        public static string Get(SearchResponse response)
        {
            if (response == null)
            {
                return string.Empty;
            }

            string file = string.Empty;
            if (response.Files.Count != 0)
            {
                file = response.Files.First().Filename ?? string.Empty;
            }
            else if (response.LockedFiles.Count != 0)
            {
                file = response.LockedFiles.First().Filename ?? string.Empty;
            }

            return (response.Username ?? string.Empty) + Separator + file;
        }
    }

    /// <summary>
    /// Pure, Android-free multi-selection state for the search result list. It stores stable
    /// result identities (never positions) so the selected peers survive sorts and streaming
    /// updates. The UI prunes it when a filter hides rows and clears it when a tab/search is
    /// replaced.
    /// </summary>
    public sealed class SearchSelection
    {
        private readonly HashSet<string> selected = new HashSet<string>(StringComparer.Ordinal);

        public int Count => selected.Count;

        public bool IsEmpty => selected.Count == 0;

        public bool IsSelected(SearchResponse response) => IsSelected(SearchResultIdentity.Get(response));

        public bool IsSelected(string key) => key != null && selected.Contains(key);

        /// <summary>Adds or removes the result; returns true when it is selected afterwards.</summary>
        public bool Toggle(SearchResponse response)
        {
            string key = SearchResultIdentity.Get(response);
            if (selected.Remove(key))
            {
                return false;
            }
            selected.Add(key);
            return true;
        }

        public void Select(SearchResponse response) => selected.Add(SearchResultIdentity.Get(response));

        public void SelectAll(IEnumerable<SearchResponse> responses)
        {
            if (responses == null)
            {
                return;
            }
            foreach (var response in responses)
            {
                Select(response);
            }
        }

        public void Clear() => selected.Clear();

        /// <summary>
        /// Drops identities that are no longer present in the current view (for example after a
        /// filter change). Streaming additions do not call this: they do not remove old rows.
        /// </summary>
        public void PruneTo(IEnumerable<SearchResponse> visibleResponses)
        {
            if (visibleResponses == null)
            {
                selected.Clear();
                return;
            }

            var visibleKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var response in visibleResponses)
            {
                visibleKeys.Add(SearchResultIdentity.Get(response));
            }
            selected.RemoveWhere(key => !visibleKeys.Contains(key));
        }

        /// <summary>
        /// Resolves the selected identities against the current visible order. Only currently
        /// displayed rows are returned, so hidden/stale selections cannot leak into a batch.
        /// </summary>
        public List<SearchResponse> Resolve(IEnumerable<SearchResponse> visibleResponses)
        {
            var result = new List<SearchResponse>();
            if (visibleResponses == null)
            {
                return result;
            }
            foreach (var response in visibleResponses)
            {
                if (IsSelected(response))
                {
                    result.Add(response);
                }
            }
            return result;
        }
    }
}
