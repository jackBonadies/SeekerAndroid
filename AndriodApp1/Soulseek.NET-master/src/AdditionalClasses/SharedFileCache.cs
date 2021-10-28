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
            TTL = ttl;
        }

        public SharedFileCache(List<Tuple<string,string,long, string>> keys, int direcotryCount, BrowseResponse browseResponse, List<Tuple<string, string>> friendlyDirNameToUriMapping)
        {
            Keys=keys.Select(_=>_.Item1).ToList(); //if its here then we have it.
            FullInfo = keys; //this is the full info, i.e. keys and and their corresponding URI and length
            DirectoryCount = direcotryCount;
            TTL = 1000*3600*24; //1day in ms
            BrowseResponse = browseResponse;
            FriendlyDirNameToUriMapping = friendlyDirNameToUriMapping;

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
        public Tuple<string, string, long, string> GetFullInfoFromSearchableName(string keyFilename, string fullPath, bool toStripVolumeName, string volumeName, Func<string,string> GetLastEncoded, out string errorMessage)
        {
            //Tuple<string, string, long> ourFileInfo = FullInfo.Where((Tuple<string, string, long> fullInfoTuple) => { return fullInfoTuple.Item1 == keyFilename; }).FirstOrDefault();
            Tuple<string, string, long, string> ourFileInfo = FullInfo.FirstOrDefault((Tuple<string, string, long, string> fullInfoTuple) => { return fullInfoTuple.Item4 == keyFilename; });
            if(ourFileInfo==null)
            {
                errorMessage = "ourFileInfo 1 is null keyFilename is: " + keyFilename + "FullInfo.Count" + FullInfo.Count;
                return null;
            }
            if(ourFileInfo.Item3==-1)
            {
                ////look through the aux structure
                //foreach(var tup in AuxilaryDuplicates[ourFileInfo.Item1])
                //{
                //    if(ConvertUriToBrowseResponsePath(tup.Item2, toStripVolumeName, volumeName, GetLastEncoded).EndsWith(fullPath))
                //    {
                //        errorMessage = string.Empty;
                //        return tup;
                //    }
                //}
                //THIS SHOULD NEVER HAPPEN!!!!! we do log this is parent method
                errorMessage = "our Auxilary lookup failed: fullpath: " + fullPath + "duplicates Count is: ";
                return null;
            }
            else
            {
                errorMessage = string.Empty;
                return ourFileInfo;
            }
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

        public List<Soulseek.File> GetSlskFilesFromFilenames(IEnumerable<Tuple<string,string,long,string>> results)
        {

            IEnumerable<string> fileNames = results.Select(_=>_.Item4).Distinct();//.Select(r => Files[r.Replace("''", "'")]);
            List<Soulseek.File> response = new List<Soulseek.File>();
            foreach (var fName in fileNames)
            {
                Soulseek.File f = Files[fName];
                if (f.Length != -1)
                {
                    response.Add(f);
                }
                else
                {
                    response.AddRange(DuplicateFiles[fName]);
                }
            }
            return response;
        }

        public event EventHandler<(int Directories, int Files)> Refreshed;
        public bool SuccessfullyInitialized { get;set;}
        public BrowseResponse BrowseResponse { get;}
        public List<string> Keys { get; }
        public List<Tuple<string, string, long, string>> FullInfo {get; }
        public int DirectoryCount = -1;
        public int FileCount
        {
            get
            {
                if(Keys==null)
                {
                    return 0;
                }
                else
                {
                    return Keys.Count;
                }
            }
        }

        public DateTime? LastFill { get; set; }
        public long TTL { get; }
        private Dictionary<string, Soulseek.File> Files { get; set; }
        private Dictionary<string, List<Soulseek.File>> DuplicateFiles { get; set; }
        public List<Tuple<string, string>> FriendlyDirNameToUriMapping { get;}
        //private SqliteConnection SQLite { get; set; }
        private ReaderWriterLockSlim SyncRoot { get; } = new ReaderWriterLockSlim();

        /// <summary>
        ///     Scans the configured <see cref="Directory"/> and fills the cache.
        /// </summary>
        public void Fill()
        {
            var sw = new Stopwatch();
            sw.Start();

            Console.WriteLine($"[SHARED FILE CACHE]: Refreshing...");

            SyncRoot.EnterWriteLock();

            try
            {
                CreateTable();

                //var directoryCount = System.IO.Directory.GetDirectories(Directory, "*", SearchOption.AllDirectories).Length;


                //FullInfo is a list.. we want to transform it into a dictionary. but only 1 problem, that is duplicate keys.
                Files = FullInfo
                    .Select(f => new Soulseek.File(1, f.Item4.Replace("/", @"\"), f.Item3, Path.GetExtension(f.Item1)))
                    .ToDictionary(f => f.Filename, f => f);
                DuplicateFiles = new Dictionary<string, List<Soulseek.File>>();

                // potentially optimize with multi-valued insert https://stackoverflow.com/questions/16055566/insert-multiple-rows-in-sqlite
                //TODO DIRECTORY COUNT
                int directoryCount  = 2;
                Refreshed?.Invoke(this, (directoryCount, Files.Count));
            }
            finally
            {
                SyncRoot.ExitWriteLock();
            }

            sw.Stop();

            Console.WriteLine($"[SHARED FILE CACHE]: Refreshed in {sw.ElapsedMilliseconds}ms.  Found {Files.Count} files.");
            LastFill = DateTime.UtcNow;
        }

        /// <summary>
        ///     Searches the cache for files matching the specified <paramref name="query"/>.
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public IEnumerable<Soulseek.File> Search(SearchQuery query)
        {
            //if (!LastFill.HasValue || LastFill.Value.AddMilliseconds(TTL) < DateTime.UtcNow)
            //{
            //    Fill();
            //}

            return QueryTable(query.Query);
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

        private IEnumerable<Soulseek.File> QueryTable(string text)
        {
            // sanitize the query string. there's probably more to it than this.
            text = text
                .Replace("/", " ")
                .Replace("\\", " ")
                .Replace(":", " ")
                .Replace("\"", " ");

            SyncRoot.EnterReadLock();

            try
            {
                //using var cmd = new SqliteCommand(query, SQLite);
                var results = FullInfo.Where( //keys is a list.. so it can contain duplicates just fine..
                    (Tuple<string,string,long,string> fileKey) =>
                    {
                        if(fileKey.Item1.ToLower().Contains(text.ToLower()))
                        {
                            return true;
                        }
                        return false;
                    }
                    
                    );
                //var reader = cmd.ExecuteReader();

                //while (reader.Read())
                //{
                //    results.Add(reader.GetString(0));
                //}



                //results will be just the strings..  Files is the "mapping" to the slsk Files..

                return GetSlskFilesFromFilenames(results);//results.Select(r => Files[r.Replace("''", "'")]);
            }
            catch (Exception ex)
            {
                // temporary error trap to refine substitution rules
                Console.WriteLine($"[MALFORMED QUERY]: {""} ({ex.Message})");
                return Enumerable.Empty<Soulseek.File>();
            }
            finally
            {
                SyncRoot.ExitReadLock();
            }
        }
    }
}