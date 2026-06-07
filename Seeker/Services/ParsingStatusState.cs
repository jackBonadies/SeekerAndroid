using System.Collections.Concurrent;

namespace Seeker.Services
{
    /// <summary>
    /// Read-only view of the in-progress share parse. To be used outside of SharedFileService.
    /// </summary>
    public interface IParsingStatus
    {
        bool IsParsing { get; }

        /// <summary>Global files parsed so far</summary>
        int NumberParsed { get; }

        /// <summary>True if the most recent parse failed.</summary>
        bool FailedShareParse { get; }

        /// <summary>True once the file scan is done and the token index is being built.</summary>
        bool IsFinishingUp { get; }

        /// <summary>Files parsed under the given root (by upload URI), -1 if it hasnt started.</summary>
        int GetCountForRoot(string uriKey);

        /// <summary>True once the given root has finished parsing (during an in-progress parse).</summary>
        bool IsRootComplete(string uriKey);
    }

    /// <summary>
    /// Parsing Status State
    /// </summary>
    public sealed class ParsingStatusState : IParsingStatus
    {
        public const int FinishingUpSentinel = int.MaxValue;

        private volatile bool _isParsing;

        public bool IsParsing
        {
            get => _isParsing;
            set
            {
                _isParsing = value;
                if (value)
                {
                    // reset
                    NumberParsed = 0;
                    _parsedByRoot.Clear();
                    _completedRoots.Clear();
                    _currentRootKey = null;
                }
            }
        }

        public int NumberParsed { get; set; } = 0;

        public bool FailedShareParse { get; set; } = false;

        public bool IsFinishingUp => NumberParsed == FinishingUpSentinel;

        // per root counts
        private readonly ConcurrentDictionary<string, int> _parsedByRoot = new();
        private readonly ConcurrentDictionary<string, byte> _completedRoots = new();
        private string _currentRootKey;
        private int _currentRootBaseCount;

        /// <summary>Start counting for current root</summary>
        public void BeginRoot(string uriKey, int dictCountBefore)
        {
            _currentRootKey = uriKey;
            _currentRootBaseCount = dictCountBefore;
            if (uriKey != null)
            {
                _parsedByRoot[uriKey] = 0;
            }
        }

        /// <summary>Update the current root's parsed-file count from the global file count.</summary>
        public void UpdateCurrentRoot(int dictCountNow)
        {
            if (_currentRootKey != null)
            {
                _parsedByRoot[_currentRootKey] = dictCountNow - _currentRootBaseCount;
            }
        }

        /// <summary>End counting for current root</summary>
        public void EndRoot(string uriKey, int dictCountNow)
        {
            if (uriKey != null)
            {
                _parsedByRoot[uriKey] = dictCountNow - _currentRootBaseCount;
                _completedRoots[uriKey] = 1;
            }
            _currentRootKey = null;
        }

        public int GetCountForRoot(string uriKey)
        {
            return (uriKey != null && _parsedByRoot.TryGetValue(uriKey, out int c)) ? c : -1;
        }

        public bool IsRootComplete(string uriKey)
        {
            return uriKey != null && _completedRoots.ContainsKey(uriKey);
        }
    }
}
