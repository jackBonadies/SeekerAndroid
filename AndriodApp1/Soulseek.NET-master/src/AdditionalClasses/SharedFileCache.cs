namespace SlskHelp
{
    //using Microsoft.Data.Sqlite; //Microsoft Data SQLite Core from NuGet
    //could not load assembly perhaps it does not exist in the Mono for Android profile?
    using Soulseek;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading;

    /// <summary>
    ///     Caches shared files.
    /// </summary>
    public class SharedFileCache : ISharedFileCache
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="SharedFileCache"/> class.
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="ttl"></param>
        public SharedFileCache(string directory, long ttl)
        {
            //Directory = directory;
            //TTL = ttl;
        }

        public SharedFileCache(Dictionary<string,Tuple<long,string, Tuple<int, int, int, int>>> fullInfo, int direcotryCount, BrowseResponse browseResponse, List<Tuple<string, string>> friendlyDirNameToUriMapping, Dictionary<string,List<int>> tokenIndex, Dictionary<int,string> helperIndex)
        {
            FullInfo = fullInfo; //this is the full info, i.e. keys and and their corresponding URI and length
            DirectoryCount = direcotryCount;
            BrowseResponse = browseResponse;
            FriendlyDirNameToUriMapping = friendlyDirNameToUriMapping;
            TokenIndex = tokenIndex;
            HelperIndex = helperIndex;

            SuccessfullyInitialized = false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="keyFilename"></param>
        /// <param name="fullPath">is of form
        ///             //if a user tries to download a file from our browseResponse then their filename will be
        ///  "Soulseek Complete\\document\\primary:Pictures\\Soulseek Complete\\(2009.09.23) Sufjan Stevens - Live from Castaways\\(2009.09.23) Sufjan Stevens - Live from Castaways\\09 Between Songs 4.mp3" 
        /// 
        /// </param>
        /// <returns></returns>
        public Tuple<long, string, Tuple<int, int, int, int>> GetFullInfoFromSearchableName(string keyFilename, string fullPath, bool toStripVolumeName, string volumeName, Func<string,string> GetLastEncoded, out string errorMessage)
        {
            //Tuple<string, string, long> ourFileInfo = FullInfo.Where((Tuple<string, string, long> fullInfoTuple) => { return fullInfoTuple.Item1 == keyFilename; }).FirstOrDefault();
            Tuple<long, string, Tuple<int, int, int, int>> ourFileInfo = FullInfo[keyFilename];
            if(ourFileInfo==null)
            {
                errorMessage = "ourFileInfo 1 is null keyFilename is: " + keyFilename + "FullInfo.Count" + FullInfo.Count;
                return null;
            }
            //if(ourFileInfo.Item1==-1)
            //{
            //    ////look through the aux structure
            //    //foreach(var tup in AuxilaryDuplicates[ourFileInfo.Item1])
            //    //{
            //    //    if(ConvertUriToBrowseResponsePath(tup.Item2, toStripVolumeName, volumeName, GetLastEncoded).EndsWith(fullPath))
            //    //    {
            //    //        errorMessage = string.Empty;
            //    //        return tup;
            //    //    }
            //    //}
            //    //THIS SHOULD NEVER HAPPEN!!!!! we do log this is parent method
            //    errorMessage = "our Auxilary lookup failed: fullpath: " + fullPath + "duplicates Count is: ";
            //    return null;
            //}
            //else
            //{
                errorMessage = string.Empty;
                return ourFileInfo;
            //}
        }

        public static IEnumerable<FileAttribute> GetFileAttributesFromTuple(Tuple<int, int, int, int> attributeTuple)
        {
            if (attributeTuple == null)
            {
                return null;
            }
            List<FileAttribute> fileAttributes = new List<FileAttribute>();
            if (attributeTuple.Item1 >= 0)
            {
                fileAttributes.Add(new FileAttribute(FileAttributeType.Length, attributeTuple.Item1)); //in seconds
            }
            if (attributeTuple.Item2 >= 0)
            {
                fileAttributes.Add(new FileAttribute(FileAttributeType.BitRate, attributeTuple.Item2)); //in bits
            }
            if (attributeTuple.Item3 >= 0)
            {
                fileAttributes.Add(new FileAttribute(FileAttributeType.BitDepth, attributeTuple.Item3)); //in bits
            }
            if (attributeTuple.Item4 >= 0)
            {
                fileAttributes.Add(new FileAttribute(FileAttributeType.SampleRate, attributeTuple.Item4)); //in Hz
            }
            return fileAttributes.Count > 0 ? fileAttributes : null;
        }


        public static string ConvertUriToBrowseResponsePath(string uri, bool toStripVolume, string volumeName, Func<string, string> GetLastEncoded)
        {
            string lastEncoded = GetLastEncoded(uri);
            if(toStripVolume && volumeName!=null)
            {
                if(lastEncoded.StartsWith(volumeName))
                {
                    return lastEncoded.Substring(volumeName.Length).Replace('/', '\\');
                }
            }
            return lastEncoded.Replace('/', '\\');
        }

        public static string MatchSpecialCharAgnostic(string input)
        {
            return input.Replace(".", "").Replace(",", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace("[", "").Replace("]", "");
        }

        public List<Soulseek.File> GetSlskFilesFromMatches(IEnumerable<int> matches)
        {
            IEnumerable<string> presentableNames = matches.Select(match=> HelperIndex[match]);


            List<Soulseek.File> response = new List<Soulseek.File>();
            foreach (var fName in presentableNames)
            {
                var fullInfoFile = FullInfo[fName];
                Soulseek.File f = new Soulseek.File(1, fName, fullInfoFile.Item1, System.IO.Path.GetExtension(fName), GetFileAttributesFromTuple(fullInfoFile.Item3));
                response.Add(f);
            }
            return response;
        }

        Dictionary<string, List<int>> TokenIndex {get;}
        Dictionary<int,string> HelperIndex {get;}
        public event EventHandler<(int Directories, int Files)> Refreshed;
        public bool SuccessfullyInitialized { get;set;}
        public BrowseResponse BrowseResponse { get;}
        public Dictionary<string,Tuple<long,string, Tuple<int, int, int, int>>> FullInfo {get; }
        public int DirectoryCount = -1;
        public int FileCount
        {
            get
            {
                if(FullInfo == null)
                {
                    return 0;
                }
                else
                {
                    return FullInfo.Keys.Count;
                }
            }
        }

        public List<Tuple<string, string>> FriendlyDirNameToUriMapping { get;}
        //private SqliteConnection SQLite { get; set; }
        private ReaderWriterLockSlim SyncRoot { get; } = new ReaderWriterLockSlim();

        /// <summary>
        ///     Scans the configured <see cref="Directory"/> and fills the cache.
        /// </summary>
        public void Fill()
        {
            //var sw = new Stopwatch();
            //sw.Start();

            //Console.WriteLine($"[SHARED FILE CACHE]: Refreshing...");

            //SyncRoot.EnterWriteLock();

            //try
            //{
            //    CreateTable();

            //    int directoryCount  = 2;
            //    Refreshed?.Invoke(this, (directoryCount, Files.Count));
            //}
            //finally
            //{
            //    SyncRoot.ExitWriteLock();
            //}

            //sw.Stop();

            //Console.WriteLine($"[SHARED FILE CACHE]: Refreshed in {sw.ElapsedMilliseconds}ms.  Found {Files.Count} files.");
            //LastFill = DateTime.UtcNow;
        }

        //public void InformServer()
        //{
        //    Refreshed?.Invoke(this, (DirectoryCount, FullInfo?.Keys?.Count ?? 0));
        //}

        /// <summary>
        ///     Searches the cache for files matching the specified <paramref name="query"/>.
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public IEnumerable<Soulseek.File> Search(SearchQuery query)
        {
            return QueryTable(query.Terms, query.Exclusions);
        }

        private void CreateTable()
        {
            //SQLite = new SqliteConnection("Data Source=:memory:");
            //SQLite.Open();

            //using var cmd = new SqliteCommand("CREATE VIRTUAL TABLE cache USING fts5(filename)", SQLite);
            //cmd.ExecuteNonQuery();
        }

        private void InsertFilename(string filename)
        {
            //using var cmd = new SqliteCommand($"INSERT INTO cache(filename) VALUES('{filename.Replace("'", "''")}')", SQLite);
            //cmd.ExecuteNonQuery();
        }

#pragma warning disable S1144 // Unused private types or members should be removed
        //private bool Matches(string query)
        //{
            
        //}
#pragma warning restore S1144 // Unused private types or members should be removed

        private IEnumerable<Soulseek.File> QueryTable(IReadOnlyCollection<string> includeTerms, IReadOnlyCollection<string> excludeTerms)
        {
            if(FileCount == 0)
            {
                return Enumerable.Empty<Soulseek.File>();
            }
            // sanitize the query string. there's probably more to it than this.
            //text = text
            //    .Replace("/", " ")
            //    .Replace("\\", " ")
            //    .Replace(":", " ")
            //    .Replace("\"", " ");

            

            //SyncRoot.EnterReadLock();

            try
            {
                IEnumerable<int> matches = null;
                foreach(string includeTerm in includeTerms)
                {
                    string includeTermAgnostic = SharedFileCache.MatchSpecialCharAgnostic(includeTerm);
                    if(includeTermAgnostic==string.Empty)
                    {
                        continue;
                    }
                    if (!TokenIndex.ContainsKey(includeTermAgnostic))
                    {
                        return Enumerable.Empty<Soulseek.File>();
                    }
                    else
                    {
                        if(matches==null)
                        {
                            matches = TokenIndex[includeTermAgnostic];
                        }
                        else
                        {
                            IEnumerable<int> nextTermMatches = TokenIndex[includeTermAgnostic];
                            matches = matches.Intersect(nextTermMatches);
                            if(!matches.Any())
                            {
                                return Enumerable.Empty<Soulseek.File>();
                            }
                        }
                    }
                }

                if(matches==null || !matches.Any())
                {
                    return Enumerable.Empty<Soulseek.File>();
                }

                foreach (string excludeTerm in excludeTerms)
                {
                    string excludeTermAgnostic = SharedFileCache.MatchSpecialCharAgnostic(excludeTerm);
                    if (TokenIndex.ContainsKey(excludeTermAgnostic))
                    {
                        matches.Except(TokenIndex[excludeTermAgnostic]);
                    }
                }

                if (!matches.Any())
                {
                    return Enumerable.Empty<Soulseek.File>();
                }

                return GetSlskFilesFromMatches(matches);//results.Select(r => Files[r.Replace("''", "'")]);
            }
            catch (Exception ex)
            {
                // temporary error trap to refine substitution rules
                Console.WriteLine($"[MALFORMED QUERY]: {""} ({ex.Message})");
                return Enumerable.Empty<Soulseek.File>();
            }
            finally
            {
                //SyncRoot.ExitReadLock();
            }
        }
    }
}