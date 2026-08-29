namespace WebAPI.Trackers
{
    using Soulseek;
    using System;
    using System.Collections.Concurrent;

    /// <summary>
    ///     Tracks active searches.
    /// </summary>
    public interface ISearchTracker
    {
        /// <summary>
        ///     Gets active searches.
        /// </summary>
        ConcurrentDictionary<Guid, Search> Searches { get; }

        /// <summary>
        ///     Adds or updates a tracked search.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="search"></param>
        void AddOrUpdate(Guid id, Search search);

        /// <summary>
        ///     Removes all tracked searches.
        /// </summary>
        void Clear();

        /// <summary>
        ///     Removes a tracked search.
        /// </summary>
        /// <param name="id"></param>
        void TryRemove(Guid id);
    }
}