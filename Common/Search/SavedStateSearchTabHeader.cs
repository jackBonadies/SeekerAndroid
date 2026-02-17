using System;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Seeker
{
    /// <summary>
    /// Small struct containing saved info on search tab header (i.e. search term, num results, last searched)
    /// </summary>
    [Serializable]
    public class SavedStateSearchTabHeader : ISerializable
    {
        [JsonInclude]
        public string LastSearchTerm { get; private set; }

        [JsonInclude]
        public long LastRanTime { get; private set; }

        [JsonInclude]
        public int LastSearchResultsCount { get; private set; }

        public SavedStateSearchTabHeader()
        {

        }

        /// <summary>
        /// Used for binary serializer, since members switched to properties from fields.
        /// Otherwise, properties will not be written to (default values)
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected SavedStateSearchTabHeader(SerializationInfo info,  StreamingContext context)
        {
            LastSearchTerm = info.GetString("LastSearchTerm");
            LastRanTime = info.GetInt64("LastRanTime");
            LastSearchResultsCount = info.GetInt32("LastSearchResultsCount");
        }

        /// <summary>
        /// Used for binary serializer
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("LastSearchTerm", LastSearchTerm);
            info.AddValue("LastRanTime", LastRanTime);
            info.AddValue("LastSearchResultsCount", LastSearchResultsCount);
        }

        /// <summary>
        /// Get what you need to display the tab (i.e. result count, term, last ran)
        /// </summary>
        public static SavedStateSearchTabHeader GetSavedStateHeaderFromTab(string lastSearchTerm, int lastSearchResultsCount, long lastRanTimeTicks)
        {
            SavedStateSearchTabHeader searchTabState = new SavedStateSearchTabHeader();
            searchTabState.LastSearchResultsCount = lastSearchResultsCount;
            searchTabState.LastSearchTerm = lastSearchTerm;
            searchTabState.LastRanTime = lastRanTimeTicks;
            return searchTabState;
        }
    }
}
