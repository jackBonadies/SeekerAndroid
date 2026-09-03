namespace Console
{
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;

    public static class MusicBrainz
    {
        public static DateTime ToFuzzyDateTime(this string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return default;
            }

            if (s.Length == 2)
            {
                if (int.Parse(s) < 30)
                {
                    return DateTime.Parse($"1-1-20{s}");
                }

                return DateTime.Parse($"1-1-19{s}");
            }

            if (s.Length == 4)
            {
                return DateTime.Parse($"1-1-{s}");
            }

            else return DateTime.Parse(s);
        }

        private static readonly HttpClient Http = new HttpClient();
#pragma warning disable S1075 // URIs should not be hardcoded
        private static readonly Uri API_ROOT = new Uri("https://musicbrainz.org/ws/2");
#pragma warning restore S1075 // URIs should not be hardcoded

        static MusicBrainz()
        {
            Http.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("Soulseek.NET", "1.0.0"));
        }

        private static Uri GetArtistSearchRequestUri(string query) => new Uri($"{API_ROOT}/artist/?query={Uri.EscapeDataString(query)}&fmt=json");

        private static Uri GetReleaseGroupRequestUri(Guid artistMbid, int offset, int limit) => new Uri($"{API_ROOT}/release-group?artist={artistMbid}&offset={offset}&limit={limit}&fmt=json");

        private static Uri GetReleaseRequestUri(Guid releaseGroupMbid, int offset, int limit) => new Uri($"{API_ROOT}/release?release-group={releaseGroupMbid}&offset={offset}&limit={limit}&inc=media+recordings&fmt=json");

        public static async Task<IEnumerable<Artist>> GetMatchingArtists(string query)
        {
            var result = await Http.GetAsync(GetArtistSearchRequestUri(query));
            result.EnsureSuccessStatusCode();
            var content = await result.Content.ReadAsStringAsync();
            var artistResponse = JsonConvert.DeserializeObject<ArtistResponse>(content);

            return artistResponse.Artists;
        }

        public static async Task<IEnumerable<ReleaseGroup>> GetArtistReleaseGroups(Guid artistId)
        {
            var limit = 100;
            int releaseGroupCount;
            var releaseGroups = new List<ReleaseGroup>();

            do
            {
                var result = await Http.GetAsync(GetReleaseGroupRequestUri(artistId, releaseGroups.Count, limit));
                result.EnsureSuccessStatusCode();
                var content = await result.Content.ReadAsStringAsync();
                var releaseGroupResponse = JsonConvert.DeserializeObject<ReleaseGroupResponse>(content);

                releaseGroupCount = releaseGroupResponse.ReleaseGroupCount;

                releaseGroups.AddRange(releaseGroupResponse.ReleaseGroups);
            } while (releaseGroupCount > releaseGroups.Count);

            return releaseGroups;
        }

        public static async Task<IEnumerable<Release>> GetReleaseGroupReleases(Guid releaseGroupId)
        {
            var limit = 100;
            int releaseCount;
            var releases = new List<Release>();

            do
            {
                var result = await Http.GetAsync(GetReleaseRequestUri(releaseGroupId, releases.Count, limit));
                result.EnsureSuccessStatusCode();
                var content = await result.Content.ReadAsStringAsync();
                var releaseGroupResponse = JsonConvert.DeserializeObject<ReleaseResponse>(content);

                releaseCount = releaseGroupResponse.ReleaseCount;

                releases.AddRange(releaseGroupResponse.Releases);
            } while (releaseCount > releases.Count);

            return releases;
        }
    }

    public class ArtistResponse
    {
        public DateTime Created { get; set; }
        public int Count { get; set; }
        public int Offset { get; set; }
        public IEnumerable<Artist> Artists { get; set; }
    }

    public class ReleaseGroupResponse
    {
        [JsonProperty("release-group-count")]
        public int ReleaseGroupCount { get; set; }

        [JsonProperty("release-group-offset")]
        public int ReleaseGroupOffset { get; set; }

        [JsonProperty("release-groups")]
        public IEnumerable<ReleaseGroup> ReleaseGroups { get; set; }
    }

    public class ReleaseResponse
    {
        [JsonProperty("release-offset")]
        public int ReleaseOffset { get; set; }

        [JsonProperty("release-count")]
        public int ReleaseCount { get; set; }

        public IEnumerable<Release> Releases { get; set; }
    }

    public class Alias
    {
        [JsonProperty("begin-date")]
        public string BeginDate { get; set; }

        [JsonProperty("end-date")]
        public string EndDate { get; set; }

        public string Locale { get; set; }

        public string Name { get; set; }

        public bool? Primary { get; set; }

        [JsonProperty("sort-name")]
        public string ShortName { get; set; }

        public string Type { get; set; }
    }

    public class Area
    {
        public string ID { get; set; }

        [JsonProperty("life-span")]
        public Lifespan Lifespan { get; set; }

        public string Name { get; set; }

        [JsonProperty("sort-name")]
        public string SortName { get; set; }

        public string Type { get; set; }

        [JsonProperty("type-id")]
        public string TypeID { get; set; }
    }

    public class Artist
    {
        public IEnumerable<Alias> Aliases { get; set; }
        public Area Area { get; set; }

        [JsonProperty("begin-area")]
        public Area BeginArea { get; set; }

        public string Country { get; set; }
        public string Disambiguation { get; set; }
        public string Gender { get; set; }
        public string ID { get; set; }

        [JsonProperty("life-span")]
        public Lifespan Lifespan { get; set; }

        public string Name { get; set; }
        public int Score { get; set; }
        public string SortName { get; set; }
        public IEnumerable<Tag> Tags { get; set; }
        public string Type { get; set; }

        [JsonProperty("type-id")]
        public string TypeID { get; set; }

        [JsonProperty("disambiguated-name")]
        public string DisambiguatedName => $"{Name} {(string.IsNullOrEmpty(Disambiguation) ? string.Empty : $"({Disambiguation})")}";
    }

    public class CoverArtArchive
    {
        public bool? Artwork { get; set; }
        public bool? Back { get; set; }
        public int Count { get; set; }
        public bool? Darkened { get; set; }
        public bool? Front { get; set; }
    }

    public class Lifespan
    {
        public string Begin { get; set; }
        public string End { get; set; }
        public bool? Ended { get; set; }
    }

    public class Media
    {
        public string Format { get; set; }

        [JsonProperty("format-id")]
        public string FormatID { get; set; }

        public int Position { get; set; }

        public string Title { get; set; }

        [JsonProperty("track-count")]
        public int TrackCount { get; set; }

        [JsonProperty("track-offset")]
        public int TrackOffset { get; set; }

        public IEnumerable<Track> Tracks { get; set; }
    }

    public class Recording
    {
        public string Disambiguation { get; set; }
        public string ID { get; set; }
        public int? Length { get; set; }
        public string Title { get; set; }
        public bool? Video { get; set; }

        [JsonProperty("disambiguated-title")]
        public string DisambiguatedTitle => $"{Title} {(string.IsNullOrEmpty(Disambiguation) ? string.Empty : $"({Disambiguation})")}";
    }

    public class Release
    {
        public string Asin { get; set; }

        public string Barcode { get; set; }

        public string Country { get; set; }

        [JsonProperty("cover-art-archive")]
        public CoverArtArchive CoverArtArchive { get; set; }

        public string Date { get; set; }

        public string Disambiguation { get; set; }

        public string ID { get; set; }

        public IEnumerable<Media> Media { get; set; }

        public string Packaging { get; set; }

        [JsonProperty("packaging-id")]
        public string PackagingID { get; set; }

        public string Quality { get; set; }

        [JsonProperty("release-events")]
        public IEnumerable<ReleaseEvent> ReleaseEvents { get; set; }

        public double Score { get; set; }

        public string Status { get; set; }

        [JsonProperty("status-id")]
        public string StatusID { get; set; }

        [JsonProperty("text-representation")]
        public TextRepresentation TextRepresentation { get; set; }

        public string Title { get; set; }

        [JsonProperty("disambiguated-title")]
        public string DisambiguatedTitle => $"{Title} {(string.IsNullOrEmpty(Disambiguation) ? string.Empty : $"({Disambiguation})")}";

        public string Format => string.Join("+", Media.Select(m => m.Format));
        public string TrackCountExtended => string.Join("+", Media.Select(m => m.TrackCount));
        public int TrackCount => Media.Sum(m => m.TrackCount);
    }

    public class ReleaseEvent
    {
        public Area Area { get; set; }
        public string Date { get; set; }
    }

    public class ReleaseGroup
    {
        public string Disambiguation { get; set; }

        [JsonProperty("first-release-date")]
        public string FirstReleaseDate { get; set; }

        public string ID { get; set; }

        [JsonProperty("primary-type")]
        public string PrimaryType { get; set; }

        [JsonProperty("primary-type-id")]
        public string PrimaryTypeID { get; set; }

        [JsonProperty("secondary-type-ids")]
        public IEnumerable<string> SecondaryTypeIDs { get; set; }

        [JsonProperty("secondary-types")]
        public IEnumerable<string> SecondaryTypes { get; set; }

        public double Score { get; set; }

        public string Title { get; set; }

        public string Type =>  string.Join(" + ", new string[] { PrimaryType ?? "Unknown" }.Concat(SecondaryTypes));

        [JsonProperty("disambiguated-title")]
        public string DisambiguatedTitle => $"{Title} {(string.IsNullOrEmpty(Disambiguation) ? string.Empty : $"({Disambiguation})")}";

        public string Year => string.IsNullOrEmpty(FirstReleaseDate) ? "----" : FirstReleaseDate.ToFuzzyDateTime().Year.ToString();

        public ReleaseGroup WithScore(double score)
        {
            Score = score;
            return this;
        }
    }

    public class Tag
    {
        public int Count { get; set; }
        public string Name { get; set; }
    }

    public class TextRepresentation
    {
        public string Language { get; set; }
        public string Script { get; set; }
    }

    public class Track
    {
        [JsonProperty("alternate-titles")]
        public IEnumerable<string> AlternateTitles { get; set; }

        public string ID { get; set; }
        public int? Length { get; set; }
        public string Number { get; set; }
        public int Position { get; set; }
        public Recording Recording { get; set; }
        public double Score { get; set; }
        public string Title { get; set; }
    }
}
