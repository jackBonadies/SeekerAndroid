namespace SlskHelp
{
    //using Microsoft.Data.Sqlite; //Microsoft Data SQLite Core from NuGet
    //could not load assembly perhaps it does not exist in the Mono for Android profile?
    using Soulseek;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading;

    public interface IUserListChecker
    {
        public bool IsInUserList(string user);
    }

    public static class CommonHelpers
    {
        public static IUserListChecker UserListChecker;

        //this is a cache for localized strings accessed in tight loops...
        private static string strings_kbs;
        public static string STRINGS_KBS
        {
            get
            {
                return strings_kbs;
            }
            set
            {
                strings_kbs = value;
            }
        }

        private static string strings_kHz;
        public static string STRINGS_KHZ
        {
            get
            {
                return strings_kHz;
            }
            set
            {
                strings_kHz = value;
            }
        }

        static CommonHelpers()
        {
            KNOWN_TYPES = new List<string>() { ".mp3", ".flac", ".wav", ".aiff", ".wma", ".aac" }.AsReadOnly();
        }
        public static ReadOnlyCollection<string> KNOWN_TYPES;
    }

    /// <summary>
    ///     Caches shared files.
    /// </summary>
    public class SharedFileCache : ISharedFileCache
    {
        

        public SharedFileCache(Dictionary<string,Tuple<long,string, Tuple<int, int, int, int>, bool, bool>> fullInfo, 
            int direcotryCount, BrowseResponse browseResponse, List<Tuple<string, string>> friendlyDirNameToUriMapping, 
            Dictionary<string,List<int>> tokenIndex, Dictionary<int,string> helperIndex, List<Soulseek.Directory> hiddenDirectories,
            int _nonHiddenFileCountForServer)
        {
            FullInfo = fullInfo; //this is the full info, i.e. keys and and their corresponding URI and length
            DirectoryCount = direcotryCount;
            BrowseResponse = browseResponse;
            FriendlyDirNameToUriMapping = friendlyDirNameToUriMapping;
            TokenIndex = tokenIndex;
            HelperIndex = helperIndex;
            BrowseResponseHiddenPortion = hiddenDirectories;
            SuccessfullyInitialized = false;
            nonHiddenFileCountForServer = _nonHiddenFileCountForServer;
        }
        private int nonHiddenFileCountForServer = -1;
        public int GetNonHiddenFileCountForServer()
        {
            return nonHiddenFileCountForServer;
        }

        public static SharedFileCache GetEmptySharedFileCache()
        {
            return new SharedFileCache(new Dictionary<string, Tuple<long, string, Tuple<int, int, int, int>, bool, bool>>(), 0, null, new List<Tuple<string, string>>(), new Dictionary<string, List<int>>(), new Dictionary<int, string>(), new List<Soulseek.Directory>(), 0);
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
        public Tuple<long, string, Tuple<int, int, int, int>, bool, bool> GetFullInfoFromSearchableName(string keyFilename, out string errorMessage)
        {
            //Tuple<string, string, long> ourFileInfo = FullInfo.Where((Tuple<string, string, long> fullInfoTuple) => { return fullInfoTuple.Item1 == keyFilename; }).FirstOrDefault();
            Tuple<long, string, Tuple<int, int, int, int>, bool, bool> ourFileInfo = FullInfo[keyFilename];
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

        public IEnumerable<Soulseek.File> GetSlskFilesFromMatches(IEnumerable<int> matches, string uname, out IEnumerable<Soulseek.File> lockedFiles)
        {
            IEnumerable<string> presentableNames = matches.Select(match=> HelperIndex[match]);
            bool? inUserList = null;
            lockedFiles = new List<Soulseek.File>();
            List<Soulseek.File> response = new List<Soulseek.File>();
            foreach (var fName in presentableNames)
            {
                var fullInfoFile = FullInfo[fName];
                
                //should we skip it
                if(fullInfoFile.Item5)
                {
                    if(!inUserList.HasValue)
                    {
                        inUserList = CommonHelpers.UserListChecker.IsInUserList(uname);
                    }
                    if(!inUserList.Value)
                    {
                        continue;
                    }
                }

                Soulseek.File f = new Soulseek.File(1, fName, fullInfoFile.Item1, System.IO.Path.GetExtension(fName), GetFileAttributesFromTuple(fullInfoFile.Item3));
                if (fullInfoFile.Item4)
                {
                    (lockedFiles as List<Soulseek.File>).Add(f);
                }
                else
                {
                    response.Add(f);
                }
            }

            //if any hidden files. I suppose its faster to just do this up front.. an extra bit per each boolean per file. 1 million shared files = 250kB is okay if it means less processing power used
            //if(anyLockedPaths)
            //{
            //    for(int i = response.Count - 1; i >= 0; i--)
            //    {
            //        foreach(string lockedPath in LockedPathsPresentablePaths)
            //        {
            //            if(response[i].Filename.StartsWith($"{lockedPath}\\"))
            //            {
            //                lockedFiles = lockedFiles.Append(response[i]);
            //                response.RemoveAt(i);
            //                continue;
            //            }
            //        }
            //    }
            //}

            //if any non visible files



            return response;
        }

        Dictionary<string, List<int>> TokenIndex {get;}
        Dictionary<int,string> HelperIndex {get;}
        public event EventHandler<(int Directories, int Files)> Refreshed;
        public bool SuccessfullyInitialized { get;set;}
        private BrowseResponse BrowseResponse;
        private List<Soulseek.Directory> BrowseResponseHiddenPortion = null; //considered locked.
        public BrowseResponse GetBrowseResponseForUser(string user, bool force = false)
        {
            if(!force && (BrowseResponseHiddenPortion == null || BrowseResponse == null || BrowseResponseHiddenPortion.Count == 0 || string.IsNullOrEmpty(user) || !CommonHelpers.UserListChecker.IsInUserList(user)))
            {
                return BrowseResponse;
            }
            else
            {
                List<Soulseek.Directory> AllLockedDirs = BrowseResponse.LockedDirectories.ToList();
                AllLockedDirs.AddRange(BrowseResponseHiddenPortion);
                return new BrowseResponse(BrowseResponse.Directories, AllLockedDirs);
            }
        }

        public Dictionary<string,Tuple<long,string, Tuple<int, int, int, int>, bool, bool>> FullInfo {get; }
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
        public IEnumerable<Soulseek.File> Search(SearchQuery query, string uname, out IEnumerable<Soulseek.File> lockedFiles)
        {
            return QueryTable(query.Terms, query.Exclusions, uname, out lockedFiles);
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

        private IEnumerable<Soulseek.File> QueryTable(IReadOnlyCollection<string> includeTerms, IReadOnlyCollection<string> excludeTerms, string uname, out IEnumerable<Soulseek.File> lockedFiles)
        {
            if(FileCount == 0)
            {
                lockedFiles = Enumerable.Empty<Soulseek.File>();
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
                        lockedFiles = Enumerable.Empty<Soulseek.File>();
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
                                lockedFiles = Enumerable.Empty<Soulseek.File>();
                                return Enumerable.Empty<Soulseek.File>();
                            }
                        }
                    }
                }

                if(matches==null || !matches.Any())
                {
                    lockedFiles = Enumerable.Empty<Soulseek.File>();
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
                    lockedFiles = Enumerable.Empty<Soulseek.File>();
                    return Enumerable.Empty<Soulseek.File>();
                }

                return GetSlskFilesFromMatches(matches, uname, out lockedFiles);//results.Select(r => Files[r.Replace("''", "'")]);
            }
            catch (Exception ex)
            {
                // temporary error trap to refine substitution rules
                Console.WriteLine($"[MALFORMED QUERY]: {""} ({ex.Message})");
                lockedFiles = Enumerable.Empty<Soulseek.File>();
                return Enumerable.Empty<Soulseek.File>();
            }
            finally
            {
                //SyncRoot.ExitReadLock();
            }
        }
    }
}