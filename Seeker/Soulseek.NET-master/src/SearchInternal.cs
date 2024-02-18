// <copyright file="SearchInternal.cs" company="JP Dillingham">
//     Copyright (c) JP Dillingham. All rights reserved.
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
//
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see https://www.gnu.org/licenses/.
// </copyright>

namespace Soulseek
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using SystemTimer = System.Timers.Timer;

    /// <summary>
    ///     A single file search.
    /// </summary>
    internal sealed class SearchInternal : IDisposable
    {
        private int fileCount = 0;
        private int lockedFileCount = 0;
        private int responseCount = 0;

        /// <summary>
        ///     Initializes a new instance of the <see cref="SearchInternal"/> class.
        /// </summary>
        /// <param name="searchText">The text for which to search.</param>
        /// <param name="token">The unique search token.</param>
        /// <param name="options">The options for the search.</param>
        public SearchInternal(string searchText, int token, SearchOptions options = null)
        {
            SearchText = searchText;
            Token = token;

            Options = options ?? new SearchOptions();

            SearchTimeoutTimer = new SystemTimer()
            {
                Interval = Options.SearchTimeout,
                Enabled = false,
                AutoReset = false,
            };

            SearchTimeoutTimer.Elapsed += (sender, e) => { Complete(SearchStates.TimedOut); };
            SearchTimeoutTimer.Reset();
        }

        /// <summary>
        ///     Gets the total number of files contained within received responses.
        /// </summary>
        public int FileCount => fileCount;

        /// <summary>
        ///     Gets the total number of locked files contained within received responses.
        /// </summary>
        public int LockedFileCount => lockedFileCount;

        /// <summary>
        ///     Gets the options for the search.
        /// </summary>
        public SearchOptions Options { get; }

        /// <summary>
        ///     Gets the current number of responses received.
        /// </summary>
        public int ResponseCount => responseCount;

        /// <summary>
        ///     Gets or sets the Action to invoke when a new search response is received.
        /// </summary>
        public Action<SearchResponse> ResponseReceived { get; set; }

        /// <summary>
        ///     Gets the text for which to search.
        /// </summary>
        public string SearchText { get; }

        /// <summary>
        ///     Gets or sets the state of the search.
        /// </summary>
        public SearchStates State { get; set; } = SearchStates.None;

        /// <summary>
        ///     Gets the unique identifier for the search.
        /// </summary>
        public int Token { get; }

        private bool Disposed { get; set; } = false;
        private SystemTimer SearchTimeoutTimer { get; set; }
        private TaskCompletionSource<int> TaskCompletionSource { get; } = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>
        ///     Cancels the search.
        /// </summary>
        public void Cancel()
        {
            TaskCompletionSource.TrySetException(new OperationCanceledException());
        }

        /// <summary>
        ///     Completes the search with the specified <paramref name="state"/>.
        /// </summary>
        /// <param name="state">The terminal state of the search.</param>
        public void Complete(SearchStates state)
        {
            SearchTimeoutTimer.Stop();
            State = SearchStates.Completed | state;
            TaskCompletionSource.TrySetResult(0);
        }

        /// <summary>
        ///     Releases the managed and unmanaged resources used by the <see cref="SearchInternal"/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Releases the managed and unmanaged resources used by the <see cref="SearchInternal"/>.
        /// </summary>
        /// <param name="disposing">A value indicating whether the object is in the process of disposing.</param>
        public void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                if (disposing)
                {
                    SearchTimeoutTimer.Dispose();
                }

                Disposed = true;
            }
        }

        /// <summary>
        ///     Adds the specified <paramref name="response"/> to the list of responses after applying the filters specified in
        ///     the search options.
        /// </summary>
        /// <param name="response">The response to add.</param>
        public void TryAddResponse(SearchResponse response)
        {
            if (!Disposed && State.HasFlag(SearchStates.InProgress) && response.Token == Token)
            {
                if (!ResponseMeetsOptionCriteria(response))
                {
                    return;
                }

                if (Options.FilterResponses)
                {
                    // apply custom filter, if one was provided
                    if (!(Options.ResponseFilter?.Invoke(response) ?? true))
                    {
                        return;
                    }

                    // apply individual file filter, if one was provided
                    var filteredFiles = response.Files.Where(f => Options.FileFilter?.Invoke(f) ?? true);
                    var filteredLockedFiles = response.LockedFiles.Where(f => Options.FileFilter?.Invoke(f) ?? true);

                    response = new SearchResponse(response, filteredFiles, filteredLockedFiles);

                    // ensure the filtered file count still meets the response criteria
                    if (response.FileCount + response.LockedFileCount < Options.MinimumResponseFileCount)
                    {
                        return;
                    }
                }

                Interlocked.Increment(ref responseCount);
                Interlocked.Add(ref fileCount, response.FileCount);
                Interlocked.Add(ref lockedFileCount, response.LockedFileCount);

                ResponseReceived?.Invoke(response);
                SearchTimeoutTimer.Reset();

                if (responseCount >= Options.ResponseLimit)
                {
                    Complete(SearchStates.ResponseLimitReached);
                }
                else if (fileCount >= Options.FileLimit)
                {
                    Complete(SearchStates.FileLimitReached);
                }
            }
        }

        /// <summary>
        ///     Asynchronously waits for the search to be completed.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The collection of received search responses.</returns>
        public async Task WaitForCompletion(CancellationToken cancellationToken)
        {
            var cancellationTaskCompletionSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            using (cancellationToken.Register(() => cancellationTaskCompletionSource.TrySetException(new OperationCanceledException("Operation cancelled"))))
            {
                var completedTask = await Task.WhenAny(TaskCompletionSource.Task, cancellationTaskCompletionSource.Task).ConfigureAwait(false);
                await completedTask.ConfigureAwait(false);
            }
        }

        private bool ResponseMeetsOptionCriteria(SearchResponse response)
        {
            if (Options.FilterResponses && (
                    response.FileCount + response.LockedFileCount < Options.MinimumResponseFileCount ||
                    response.FreeUploadSlots < Options.MinimumPeerFreeUploadSlots ||
                    response.UploadSpeed < Options.MinimumPeerUploadSpeed ||
                    response.QueueLength >= Options.MaximumPeerQueueLength))
            {
                return false;
            }

            return true;
        }
    }
}