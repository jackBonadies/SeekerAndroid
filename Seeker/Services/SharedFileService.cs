using Android.Content;
using Android.Provider;
using AndroidX.DocumentFile.Provider;
using Common;
using Seeker.Helpers;
using Seeker.Serialization;
using Seeker.Transfers;
using SlskHelp;
using Soulseek;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using static Android.Provider.DocumentsContract;

namespace Seeker.Services
{
    public static class SharedFileService
    {
        public static Dictionary<string, Tuple<long, string, Tuple<int, int, int, int>, bool, bool>> ParseSharedDirectoryFastDocContract(UploadDirectoryInfo newlyAddedDirectoryIfApplicable,
            Dictionary<string, Tuple<long, string, Tuple<int, int, int, int>, bool, bool>> previousFileInfoToUse, ref int directoryCount, out BrowseResponse br,
            out List<Tuple<string, string>> dirMappingFriendlyNameToUri, out Dictionary<int, string> index, out List<Soulseek.Directory> allHiddenDirs)
        {
            //searchable name (just folder/song), uri.ToString (to actually get it), size (for ID purposes and to send), presentablename (to send - this is the name that is supposed to show up as the folder that the QT and nicotine clients send)
            //so the presentablename should be FolderSelected/path to rest
            //there due to the way android separates the sdcard root (or primary:) and other OS.  wherewas other OS use path separators, Android uses primary:FolderName vs say C:\Foldername.  If primary: is part of the presentable name then I will change 
            //it to primary:\Foldername similar to C:\Foldername.  I think this makes most sense of the things I have tried.
            Dictionary<string, Tuple<long, string, Tuple<int, int, int, int>, bool, bool>> pairs = new Dictionary<string, Tuple<long, string, Tuple<int, int, int, int>, bool, bool>>();
            List<Soulseek.Directory> allDirs = new List<Soulseek.Directory>();
            List<Soulseek.Directory> allLockedDirs = new List<Soulseek.Directory>();
            allHiddenDirs = new List<Soulseek.Directory>();
            dirMappingFriendlyNameToUri = new List<Tuple<string, string>>();

            //UploadDirectoryManager.UpdateWithDocumentFileAndErrorStates();
            //if (UploadDirectoryManager.AreAllFailed()) //the newly added one is always good.
            //{
            //    throw new DirectoryAccessFailure("All Failed");
            //}

            HashSet<string> volNames = UploadDirectoryManager.GetInterestedVolNames();

            Dictionary<string, List<Tuple<string, int, int>>> allMediaStoreInfo = new Dictionary<string, List<Tuple<string, int, int>>>();
            PopulateAllMediaStoreInfo(allMediaStoreInfo, volNames);


            index = new Dictionary<int, string>();
            int indexNum = 0;
            var tmpUploadDirs = UploadDirectoryManager.UploadDirectories.ToList(); //avoid race conditions and enumeration modified exceptions.
            foreach (var uploadDirectoryInfo in tmpUploadDirs)
            {
                if (uploadDirectoryInfo.IsSubdir || uploadDirectoryInfo.HasError())
                {
                    continue;
                }

                DocumentFile dir = uploadDirectoryInfo.UploadDirectory;
                GetAllFolderInfo(uploadDirectoryInfo, out bool overrideCase, out string volName, out string toStrip, out string rootFolderDisplayName, out _);

                traverseDirectoryEntriesInternal(SeekerState.ActiveActivityRef.ContentResolver, dir.Uri, DocumentsContract.GetTreeDocumentId(dir.Uri), dir.Uri,
                    pairs, true, volName, allDirs, allLockedDirs, allHiddenDirs, dirMappingFriendlyNameToUri, toStrip, index, dir, allMediaStoreInfo, previousFileInfoToUse, overrideCase, overrideCase ? rootFolderDisplayName : null,
                    ref directoryCount, ref indexNum);
            }


            br = new BrowseResponse(allDirs, allLockedDirs);
            return pairs;
        }

        public static void GetAllFolderInfo(UploadDirectoryInfo uploadDirectoryInfo, out bool overrideCase, out string volName, out string toStrip, out string rootFolderDisplayName, out string presentableNameToUse)
        {
            DocumentFile dir = uploadDirectoryInfo.UploadDirectory;
            Android.Net.Uri uri = dir.Uri;//Android.Net.Uri.Parse(uploadDirectoryInfo.UploadDataDirectoryUri);
            Logger.InfoFirebase("case " + uri.ToString() + " - - - - " + uri.LastPathSegment);
            //string lastPathSegment = null;
            //bool msdCase = false;
            //if (uploadDirectoryInfo.UploadDirectory != null)
            //{
            string lastPathSegment = CommonHelpers.GetLastPathSegmentWithSpecialCaseProtection(dir, out bool msdCase);
            //}
            //else
            //{

            //    lastPathSegment = uri.LastPathSegment.Replace('/', '\\');
            //}
            toStrip = string.Empty;
            //can be reproduced with pixel emulator API 28 (android 9). the last path segment for the downloads dir is "downloads" but the last path segment for its child is "raw:/storage/emulated/0/Download/Soulseek Complete" (note it is still a content scheme, raw: is the volume)
            volName = null;
            if (msdCase)
            {
                //in this case we assume the volume is primary..
            }
            else
            {
                volName = FileFilterHelper.GetVolumeName(lastPathSegment, true, out _);
                //if(volName==null)
                //{
                //    Logger.Firebase("volName is null: " + dir.Uri.ToString());
                //}
                if (lastPathSegment.Contains('\\'))
                {
                    int stripIndex = lastPathSegment.LastIndexOf('\\');
                    toStrip = lastPathSegment.Substring(0, stripIndex + 1);
                }
                else if (volName != null && lastPathSegment.Contains(volName))
                {
                    if (lastPathSegment == volName)
                    {
                        toStrip = null;
                    }
                    else
                    {
                        toStrip = volName;
                    }
                }
                else
                {
                    Logger.Firebase("contains neither: " + lastPathSegment); //Download (on Android 9 emu)
                }
            }


            rootFolderDisplayName = uploadDirectoryInfo.DisplayNameOverride;
            overrideCase = false;

            if (msdCase)
            {
                overrideCase = true;
                if (string.IsNullOrEmpty(rootFolderDisplayName))
                {
                    rootFolderDisplayName = "downloads";
                }
                volName = null; //i.e. nothing to strip out!
                toStrip = string.Empty;
            }

            if (!string.IsNullOrEmpty(rootFolderDisplayName))
            {
                overrideCase = true;
                volName = null; //i.e. nothing to strip out!
                toStrip = string.Empty;
            }

            // Forcing Override Case
            // Basically there are two ways we construct the tree. One by appending each new name to the base as we go
            // (the 'Override' Case) the other by taking current.Uri minus root.Uri to get the difference.  
            // The latter does not work because sometimes current.Uri will be say "home:" and root will be say "primary:".
            overrideCase = true;

            if (!string.IsNullOrEmpty(rootFolderDisplayName))
            {
                presentableNameToUse = rootFolderDisplayName;
            }
            else
            {
                presentableNameToUse = FileFilterHelper.GetPresentableName(uri, toStrip, volName);
                rootFolderDisplayName = presentableNameToUse;
            }
        }

        public static void PopulateAllMediaStoreInfo(Dictionary<string, List<Tuple<string, int, int>>> allMediaStoreInfo, HashSet<string> volumeNamesOfInterest)
        {

            bool hasAnyInfo = HasMediaStoreDurationColumn();
            if (hasAnyInfo)
            {
                bool hasBitRate = HasMediaStoreBitRateColumn();
                string[] selectionColumns = null;
                if (hasBitRate)
                {
                    selectionColumns = new string[] {
                        Android.Provider.MediaStore.IMediaColumns.Size,
                        Android.Provider.MediaStore.IMediaColumns.DisplayName,

                        Android.Provider.MediaStore.IMediaColumns.Data, //disambiguator if applicable
                                    Android.Provider.MediaStore.IMediaColumns.Duration,
                                    Android.Provider.MediaStore.IMediaColumns.Bitrate };
                }
                else //only has duration
                {
                    selectionColumns = new string[] {
                        Android.Provider.MediaStore.IMediaColumns.Size,
                        Android.Provider.MediaStore.IMediaColumns.DisplayName,

                        Android.Provider.MediaStore.IMediaColumns.Data, //disambiguator if applicable
                                    Android.Provider.MediaStore.IMediaColumns.Duration };
                }


                foreach (var chosenVolume in volumeNamesOfInterest)
                {
                    Android.Net.Uri mediaStoreUri = null;
                    if (!string.IsNullOrEmpty(chosenVolume))
                    {
                        mediaStoreUri = MediaStore.Audio.Media.GetContentUri(chosenVolume);
                    }
                    else
                    {
                        mediaStoreUri = MediaStore.Audio.Media.ExternalContentUri;
                    }

                    //metadata content resolver info
                    Android.Database.ICursor mediaStoreInfo = null;
                    try
                    {
                        mediaStoreInfo = SeekerState.ActiveActivityRef.ContentResolver.Query(mediaStoreUri, selectionColumns,
                            null, null, null);
                        while (mediaStoreInfo.MoveToNext())
                        {
                            string key = mediaStoreInfo.GetInt(0) + mediaStoreInfo.GetString(1);
                            if (!allMediaStoreInfo.ContainsKey(key))
                            {
                                var list = new List<Tuple<string, int, int>>();
                                list.Add(new Tuple<string, int, int>(mediaStoreInfo.GetString(2), mediaStoreInfo.GetInt(3), hasBitRate ? mediaStoreInfo.GetInt(4) : -1));
                                allMediaStoreInfo.Add(key, list);
                            }
                            else
                            {
                                allMediaStoreInfo[key].Add(new Tuple<string, int, int>(mediaStoreInfo.GetString(2), mediaStoreInfo.GetInt(3), hasBitRate ? mediaStoreInfo.GetInt(4) : -1));
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Firebase("pre get all mediaStoreInfo: " + e.Message + e.StackTrace);
                    }
                    finally
                    {
                        if (mediaStoreInfo != null)
                        {
                            mediaStoreInfo.Close();
                        }
                    }
                }
            }
        }



        public static Dictionary<string, Tuple<long, string, Tuple<int, int, int, int>, bool, bool>> ParseSharedDirectoryLegacy(
            UploadDirectoryInfo newlyAddedDirectoryIfApplicable, Dictionary<string, Tuple<long, string, Tuple<int, int, int, int>, bool, bool>> previousFileInfoToUse,
            ref int directoryCount, out BrowseResponse br, out List<Tuple<string, string>> dirMappingFriendlyNameToUri, out Dictionary<int, string> index, out List<Soulseek.Directory> allHiddenDirs)
        {
            //searchable name (just folder/song), uri.ToString (to actually get it), size (for ID purposes and to send), presentablename (to send - this is the name that is supposed to show up as the folder that the QT and nicotine clients send)
            //so the presentablename should be FolderSelected/path to rest
            //there due to the way android separates the sdcard root (or primary:) and other OS.  wherewas other OS use path separators, Android uses primary:FolderName vs say C:\Foldername.  If primary: is part of the presentable name then I will change 
            //it to primary:\Foldername similar to C:\Foldername.  I think this makes most sense of the things I have tried.
            Dictionary<string, Tuple<long, string, Tuple<int, int, int, int>, bool, bool>> pairs = new Dictionary<string, Tuple<long, string, Tuple<int, int, int, int>, bool, bool>>();
            List<Soulseek.Directory> allDirs = new List<Soulseek.Directory>();
            List<Soulseek.Directory> allLockedDirs = new List<Soulseek.Directory>();
            allHiddenDirs = new List<Soulseek.Directory>();


            //UploadDirectoryManager.UpdateWithDocumentFileAndErrorStates();
            //if(UploadDirectoryManager.AreAllFailed())
            //{
            //    throw new DirectoryAccessFailure("All Failed");
            //}

            dirMappingFriendlyNameToUri = new List<Tuple<string, string>>();
            index = new Dictionary<int, string>();
            int indexNum = 0;


            //string lastPathSegment = dir.Uri.Path.Replace('/', '\\');
            //string toStrip = string.Empty;
            //if (lastPathSegment.Contains('\\'))
            //{
            //    int stripIndex = lastPathSegment.LastIndexOf('\\');
            //    toStrip = lastPathSegment.Substring(0, stripIndex + 1);
            //}

            var tmpUploadDirs = UploadDirectoryManager.UploadDirectories.ToList(); //avoid race conditions and enumeration modified exceptions.
            foreach (var uploadDirectoryInfo in tmpUploadDirs)
            {
                if (uploadDirectoryInfo.IsSubdir || uploadDirectoryInfo.HasError())
                {
                    continue;
                }

                DocumentFile dir = uploadDirectoryInfo.UploadDirectory;
                GetAllFolderInfo(uploadDirectoryInfo, out bool overrideCase, out string volName, out string toStrip, out string rootFolderDisplayName, out _);

                traverseDirectoryEntriesLegacy(dir, pairs, true, allDirs, allLockedDirs,
                    allHiddenDirs, dirMappingFriendlyNameToUri, toStrip, index,
                    previousFileInfoToUse, overrideCase, overrideCase ? rootFolderDisplayName : null,
                    ref directoryCount, ref indexNum);
            }

            br = new BrowseResponse(allDirs, allLockedDirs);
            return pairs;
        }
        public static Soulseek.Directory SlskDirFromUri(ContentResolver contentResolver, Android.Net.Uri rootUri, Android.Net.Uri dirUri, string dirToStrip, bool diagFromDirectoryResolver, string volumePath)
        {


            string directoryPath = dirUri.LastPathSegment; //on the emulator this is /tree/downloads/document/docwonlowds but the dirToStrip is uppercase Downloads
            directoryPath = directoryPath.Replace("/", @"\");
            //try
            //{
            //    directoryPath = directoryPath.Substring(directoryPath.ToLower().IndexOf(dirToStrip.ToLower()));
            //    directoryPath = directoryPath.Replace("/", @"\"); //probably strip out the root shared dir...
            //}
            //catch(Exception e)
            //{
            //    //Non-fatal Exception: java.lang.Throwable: directoryPath: False\tree\msd:824\document\msd:825MusicStartIndex cannot be less than zero.
            //    //its possible for dirToStrip to be null
            //    //True\tree\0000-0000:Musica iTunes\document\0000-0000:Musica iTunesObject reference not set to an instance of an object 
            //    //Non-fatal Exception: java.lang.Throwable: directoryPath: True\tree\3061-6232:Musica\document\3061-6232:MusicaObject reference not set to an instance of an object  at AndriodApp1.MainActivity.SlskDirFromDocumentFile (AndroidX.DocumentFile.Provider.DocumentFile dirFile, System.String dirToStrip) [0x00024] in <778faaf2e13641b38ae2700aacc789af>:0 
            //    Logger.Firebase("directoryPath: " + (dirToStrip==null).ToString() + directoryPath + " from directory resolver: "+ diagFromDirectoryResolver+" toStrip: " + dirToStrip + e.Message + e.StackTrace);
            //}
            //friendlyDirNameToUriMapping.Add(new Tuple<string, string>(directoryPath, dirFile.Uri.ToString()));
            //strip out the shared root dir
            //directoryPath.Substring(directoryPath.IndexOf(dir.Name))
            Android.Net.Uri listChildrenUri = DocumentsContract.BuildChildDocumentsUriUsingTree(rootUri, DocumentsContract.GetDocumentId(dirUri));
            Android.Database.ICursor c = contentResolver.Query(listChildrenUri, new String[] { Document.ColumnDocumentId, Document.ColumnDisplayName, Document.ColumnMimeType, Document.ColumnSize }, null, null, null);
            List<Soulseek.File> files = new List<Soulseek.File>();
            try
            {
                while (c.MoveToNext())
                {
                    string docId = c.GetString(0);
                    string name = c.GetString(1);
                    string mime = c.GetString(2);
                    long size = c.GetLong(3);
                    var childUri = DocumentsContract.BuildDocumentUri(rootUri.Authority, docId);
                    //Logger.Debug("docId: " + docId + ", name: " + name + ", mime: " + mime);
                    if (isDirectory(mime))
                    {
                    }
                    else
                    {

                        string fname = CommonHelpers.GetFileNameFromFile(childUri.Path.Replace("/", @"\"));
                        string folderName = Common.Helpers.GetFolderNameFromFile(childUri.Path.Replace("/", @"\"));
                        string searchableName = /*folderName + @"\" + */fname; //for the brose response should only be the filename!!! 
                                                                               //when a user tries to download something from a browse resonse, the soulseek client on their end must create a fully qualified path for us
                                                                               //bc we get a path that is:
                                                                               //"Soulseek Complete\\document\\primary:Pictures\\Soulseek Complete\\(2009.09.23) Sufjan Stevens - Live from Castaways\\09 Between Songs 4.mp3"
                                                                               //not quite a full URI but it does add quite a bit..

                        //if (searchableName.Length > 7 && searchableName.Substring(0, 8).ToLower() == "primary:")
                        //{
                        //    searchableName = searchableName.Substring(8);
                        //}
                        var slskFile = new Soulseek.File(1, searchableName.Replace("/", @"\"), size, System.IO.Path.GetExtension(childUri.Path));
                        files.Add(slskFile);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Debug("Parse error with " + dirUri.Path + e.Message + e.StackTrace);
                Logger.Firebase("Parse error with " + dirUri.Path + e.Message + e.StackTrace);
            }
            finally
            {
                closeQuietly(c);
            }
            CommonHelpers.SortSlskDirFiles(files); //otherwise our browse response files will be way out of order

            if (volumePath != null)
            {
                if (directoryPath.Substring(0, volumePath.Length) == volumePath)
                {
                    //if (directoryPath.Length != volumePath.Length)
                    //{
                    directoryPath = directoryPath.Substring(volumePath.Length);
                    //}
                }
            }

            var slskDir = new Soulseek.Directory(directoryPath, files);
            return slskDir;
        }

        /// <summary>
        /// We only use this in Contents Response Resolver.
        /// </summary>
        /// <param name="dirFile"></param>
        /// <param name="dirToStrip"></param>
        /// <param name="diagFromDirectoryResolver"></param>
        /// <param name="volumePath"></param>
        /// <returns></returns>
        public static Soulseek.Directory SlskDirFromDocumentFile(DocumentFile dirFile, bool diagFromDirectoryResolver, string volumePath)
        {
            string directoryPath = dirFile.Uri.LastPathSegment; //on the emulator this is /tree/downloads/document/docwonlowds but the dirToStrip is uppercase Downloads
            directoryPath = directoryPath.Replace("/", @"\");
            //try
            //{
            //    directoryPath = directoryPath.Substring(directoryPath.ToLower().IndexOf(dirToStrip.ToLower()));
            //    directoryPath = directoryPath.Replace("/", @"\"); //probably strip out the root shared dir...
            //}
            //catch(Exception e)
            //{
            //    //Non-fatal Exception: java.lang.Throwable: directoryPath: False\tree\msd:824\document\msd:825MusicStartIndex cannot be less than zero.
            //    //its possible for dirToStrip to be null
            //    //True\tree\0000-0000:Musica iTunes\document\0000-0000:Musica iTunesObject reference not set to an instance of an object 
            //    //Non-fatal Exception: java.lang.Throwable: directoryPath: True\tree\3061-6232:Musica\document\3061-6232:MusicaObject reference not set to an instance of an object  at AndriodApp1.MainActivity.SlskDirFromDocumentFile (AndroidX.DocumentFile.Provider.DocumentFile dirFile, System.String dirToStrip) [0x00024] in <778faaf2e13641b38ae2700aacc789af>:0 
            //    Logger.Firebase("directoryPath: " + (dirToStrip==null).ToString() + directoryPath + " from directory resolver: "+ diagFromDirectoryResolver+" toStrip: " + dirToStrip + e.Message + e.StackTrace);
            //}
            //friendlyDirNameToUriMapping.Add(new Tuple<string, string>(directoryPath, dirFile.Uri.ToString()));
            //strip out the shared root dir
            //directoryPath.Substring(directoryPath.IndexOf(dir.Name))

            List<Soulseek.File> files = new List<Soulseek.File>();
            foreach (DocumentFile f in dirFile.ListFiles())
            {
                if (f.IsDirectory)
                {
                    continue;
                }
                try
                {
                    string fname = null;
                    string searchableName = null;

                    if (dirFile.Uri.Authority == "com.android.providers.downloads.documents" && !f.Uri.Path.Contains(dirFile.Uri.Path))
                    {
                        //msd, msf case
                        fname = f.Name;
                        searchableName = /*folderName + @"\" + */fname; //for the brose response should only be the filename!!! 
                    }
                    else
                    {
                        fname = CommonHelpers.GetFileNameFromFile(f.Uri.Path.Replace("/", @"\"));
                        searchableName = /*folderName + @"\" + */fname; //for the brose response should only be the filename!!! 
                    }
                    //when a user tries to download something from a browse resonse, the soulseek client on their end must create a fully qualified path for us
                    //bc we get a path that is:
                    //"Soulseek Complete\\document\\primary:Pictures\\Soulseek Complete\\(2009.09.23) Sufjan Stevens - Live from Castaways\\09 Between Songs 4.mp3"
                    //not quite a full URI but it does add quite a bit..

                    //{
                    //    searchableName = searchableName.Substring(8);
                    //}
                    var slskFile = new Soulseek.File(1, searchableName.Replace("/", @"\"), f.Length(), System.IO.Path.GetExtension(f.Uri.Path));
                    files.Add(slskFile);
                }
                catch (Exception e)
                {
                    Logger.Debug("Parse error with " + f.Uri.Path + e.Message + e.StackTrace);
                    Logger.Firebase("Parse error with " + f.Uri.Path + e.Message + e.StackTrace);
                }

            }
            CommonHelpers.SortSlskDirFiles(files); //otherwise our browse response files will be way out of order

            if (volumePath != null)
            {
                if (directoryPath.Substring(0, volumePath.Length) == volumePath)
                {
                    //if(directoryPath.Length != volumePath.Length)
                    //{
                    directoryPath = directoryPath.Substring(volumePath.Length);
                    //}
                }
            }

            var slskDir = new Soulseek.Directory(directoryPath, files);
            return slskDir;
        }

        // TODO org models  OR move into Sharing Folder
        public class CachedParseResults
        {
             public CachedParseResults(
                Dictionary<string, Tuple<long, string, Tuple<int, int, int, int>, bool, bool>> keys, 
                int directoryCount, 
                BrowseResponse browseResponse, 
                List<Directory> browseResponseHiddenPortion, 
                List<Tuple<string, string>> friendlyDirNameToUriMapping, 
                Dictionary<string, List<int>> tokenIndex, 
                Dictionary<int, string> helperIndex, 
                int nonHiddenFileCount)
            {
                this.keys = keys;
                this.directoryCount = directoryCount;
                this.browseResponse = browseResponse;
                this.browseResponseHiddenPortion = browseResponseHiddenPortion;
                this.friendlyDirNameToUriMapping = friendlyDirNameToUriMapping;
                this.tokenIndex = tokenIndex;
                this.helperIndex = helperIndex;
                this.nonHiddenFileCount = nonHiddenFileCount;
            }

            public CachedParseResults()
            {
            }

            public Dictionary<string, Tuple<long, string, Tuple<int, int, int, int>, bool, bool>> keys = null;
            public int directoryCount = -1;
            public BrowseResponse browseResponse = null;
            public List<Soulseek.Directory> browseResponseHiddenPortion = null;
            public List<Tuple<string, string>> friendlyDirNameToUriMapping = null;
            public Dictionary<string, List<int>> tokenIndex = null;
            public Dictionary<int, string> helperIndex = null;
            public int nonHiddenFileCount = -1;
        }

        public static void ClearLegacyParsedCacheResults()
        {
            try
            {
                lock (SeekerState.SharedPrefLock)
                {
                    var editor = SeekerState.SharedPreferences.Edit();
                    editor.Remove(KeyConsts.M_CACHE_stringUriPairs);
                    editor.Remove(KeyConsts.M_CACHE_browseResponse);
                    editor.Remove(KeyConsts.M_CACHE_friendlyDirNameToUriMapping);
                    editor.Remove(KeyConsts.M_CACHE_auxDupList);
                    editor.Remove(KeyConsts.M_CACHE_stringUriPairs_v2);
                    editor.Remove(KeyConsts.M_CACHE_stringUriPairs_v3);
                    editor.Remove(KeyConsts.M_CACHE_browseResponse_v2);
                    editor.Remove(KeyConsts.M_CACHE_friendlyDirNameToUriMapping_v2);
                    editor.Remove(KeyConsts.M_CACHE_tokenIndex_v2);
                    editor.Remove(KeyConsts.M_CACHE_intHelperIndex_v2);
                    editor.Commit();
                }
            }
            catch (Exception e)
            {
                Logger.Debug("ClearParsedCacheResults " + e.Message + e.StackTrace);
                Logger.Firebase("ClearParsedCacheResults " + e.Message + e.StackTrace);
            }
        }

        public static CachedParseResults GetLegacyCachedParseResult()
        {
#if !BinaryFormatterAvailable
            return null;
#else
            bool convertFrom2to3 = false;

            string s_stringUriPairs = SeekerState.SharedPreferences.GetString(KeyConsts.M_CACHE_stringUriPairs_v3, string.Empty);
            if (s_stringUriPairs == string.Empty)
            {
                s_stringUriPairs = SeekerState.SharedPreferences.GetString(KeyConsts.M_CACHE_stringUriPairs_v2, string.Empty);
                convertFrom2to3 = true;
            }

            string s_BrowseResponse = SeekerState.SharedPreferences.GetString(KeyConsts.M_CACHE_browseResponse_v2, string.Empty);
            string s_FriendlyDirNameMapping = SeekerState.SharedPreferences.GetString(KeyConsts.M_CACHE_friendlyDirNameToUriMapping_v2, string.Empty);
            string s_intHelperIndex = SeekerState.SharedPreferences.GetString(KeyConsts.M_CACHE_intHelperIndex_v2, string.Empty);
            int nonHiddenFileCount = SeekerState.SharedPreferences.GetInt(KeyConsts.M_CACHE_nonHiddenFileCount_v3, -1);
            string s_tokenIndex = SeekerState.SharedPreferences.GetString(KeyConsts.M_CACHE_tokenIndex_v2, string.Empty);
            string s_BrowseResponse_hiddenPortion = SeekerState.SharedPreferences.GetString(KeyConsts.M_CACHE_browseResponse_hidden_portion, string.Empty); //this one can be empty.

            if (s_intHelperIndex == string.Empty || s_tokenIndex == string.Empty || s_stringUriPairs == string.Empty || s_BrowseResponse == string.Empty || s_FriendlyDirNameMapping == string.Empty)
            {
                return null;
            }
            else
            {
                //deserialize..
                try
                {
                    System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
                    sw.Start();
                    byte[] b_stringUriPairs = Convert.FromBase64String(s_stringUriPairs);
                    byte[] b_BrowseResponse = Convert.FromBase64String(s_BrowseResponse);
                    byte[] b_FriendlyDirNameMapping = Convert.FromBase64String(s_FriendlyDirNameMapping);
                    byte[] b_intHelperIndex = Convert.FromBase64String(s_intHelperIndex);
                    byte[] b_tokenIndex = Convert.FromBase64String(s_tokenIndex);

                    using (System.IO.MemoryStream m_stringUriPairs = new System.IO.MemoryStream(b_stringUriPairs))
                    using (System.IO.MemoryStream m_BrowseResponse = new System.IO.MemoryStream(b_BrowseResponse))
                    using (System.IO.MemoryStream m_FriendlyDirNameMapping = new System.IO.MemoryStream(b_FriendlyDirNameMapping))
                    using (System.IO.MemoryStream m_intHelperIndex = new System.IO.MemoryStream(b_intHelperIndex))

                    using (System.IO.MemoryStream m_tokenIndex = new System.IO.MemoryStream(b_tokenIndex))
                    {
                        BinaryFormatter binaryFormatter = SerializationHelper.GetLegacyBinaryFormatter();
                        CachedParseResults cachedParseResults = new CachedParseResults();
                        if (convertFrom2to3)
                        {
                            Logger.Debug("convert from v2 to v3");
                            var oldKeys = binaryFormatter.Deserialize(m_stringUriPairs) as Dictionary<string, Tuple<long, string, Tuple<int, int, int, int>>>;
                            var newKeys = new Dictionary<string, Tuple<long, string, Tuple<int, int, int, int>, bool, bool>>();
                            if (oldKeys != null)
                            {
                                foreach (var oldkeyvaluepair in oldKeys)
                                {
                                    newKeys.Add(oldkeyvaluepair.Key, new Tuple<long, string, Tuple<int, int, int, int>, bool, bool>(oldkeyvaluepair.Value.Item1, oldkeyvaluepair.Value.Item2, oldkeyvaluepair.Value.Item3, false, false));
                                }
                            }
                            lock (SeekerState.SharedPrefLock)
                            {
                                var editor = SeekerState.SharedPreferences.Edit();
                                editor.PutString(KeyConsts.M_CACHE_stringUriPairs_v2, string.Empty);
                                using (System.IO.MemoryStream bstringUrimemoryStreamv3 = new System.IO.MemoryStream())
                                {
                                    BinaryFormatter formatter = SerializationHelper.GetLegacyBinaryFormatter();
                                    formatter.Serialize(bstringUrimemoryStreamv3, newKeys);
                                    string stringUrimemoryStreamv3 = Convert.ToBase64String(bstringUrimemoryStreamv3.ToArray());
                                    editor.PutString(KeyConsts.M_CACHE_stringUriPairs_v3, stringUrimemoryStreamv3);
                                    editor.Commit();
                                }
                            }
                            cachedParseResults.keys = newKeys;
                        }
                        else
                        {
                            Logger.Debug("v3");
                            cachedParseResults.keys = binaryFormatter.Deserialize(m_stringUriPairs) as Dictionary<string, Tuple<long, string, Tuple<int, int, int, int>, bool, bool>>;
                        }


                        cachedParseResults.browseResponse = binaryFormatter.Deserialize(m_BrowseResponse) as BrowseResponse;


                        if (!string.IsNullOrEmpty(s_BrowseResponse_hiddenPortion))
                        {
                            byte[] b_BrowseResponse_hiddenPortion = Convert.FromBase64String(s_BrowseResponse_hiddenPortion);
                            using (System.IO.MemoryStream m_BrowseResponse_hiddenPortion = new System.IO.MemoryStream(b_BrowseResponse_hiddenPortion))
                            {
                                cachedParseResults.browseResponseHiddenPortion = binaryFormatter.Deserialize(m_BrowseResponse_hiddenPortion) as List<Soulseek.Directory>;
                            }
                        }
                        else
                        {
                            cachedParseResults.browseResponseHiddenPortion = null;
                        }


                        cachedParseResults.friendlyDirNameToUriMapping = binaryFormatter.Deserialize(m_FriendlyDirNameMapping) as List<Tuple<string, string>>;
                        cachedParseResults.directoryCount = cachedParseResults.browseResponse.DirectoryCount;
                        cachedParseResults.helperIndex = binaryFormatter.Deserialize(m_intHelperIndex) as Dictionary<int, string>;
                        cachedParseResults.tokenIndex = binaryFormatter.Deserialize(m_tokenIndex) as Dictionary<string, List<int>>;
                        cachedParseResults.nonHiddenFileCount = nonHiddenFileCount;

                        if (cachedParseResults.keys == null || cachedParseResults.browseResponse == null || cachedParseResults.friendlyDirNameToUriMapping == null || cachedParseResults.helperIndex == null || cachedParseResults.tokenIndex == null)
                        {
                            return null;
                        }

                        sw.Stop();
                        Logger.Debug("time to deserialize all sharing helpers: " + sw.ElapsedMilliseconds);

                        return cachedParseResults;
                    }

                }
                catch (Exception e)
                {
                    Logger.Debug("error deserializing" + e.Message + e.StackTrace);
                    Logger.Firebase("error deserializing" + e.Message + e.StackTrace);
                    return null;
                }
            }
#endif
        }

        //Pretty much all clients send "attributes" or limited metadata.
        // if lossless - they send duration, bit rate, bit depth, and sample rate
        // if lossy - they send duration and bit rate.

        //notes:
        // for lossless - bit rate = sample rate * bit depth * num channels
        //                1411.2 kpbs = 44.1kHz * 16 * 2
        //  --if the formula is required note that typically sample rate is in (44.1, 48, 88.2, 96) with the last too being very rare (never seen it).
        //      and bit depth in (16, 24, 32) with 16 most common, sometimes 24, never seen 32.  
        // for both lossy and lossless - determining bit rate from file size and duration is a bit too imprecise.  
        //      for mp3 320kps cbr one will get 320.3, 314, 315, etc.

        //for the pre-indexed media store (note: its possible for one to revoke the photos&media permission and for seeker to work right in all places by querying mediastore)
        //  api 29+ we have duration
        //  api 30+ we have bit rate
        //  api 31+ (Android 12) we have sample rate and bit depth - proposed change?  I dont think this made it in..

        //for the built in media retreiver (which requires actually reading the file) we have duration, bit rate, with sample rate and bit depth for api31+

        //the library tag lib sharp can get us everything, tho it is 1 MB extra.



        public static bool HasMediaStoreDurationColumn()
        {
            return (int)Android.OS.Build.VERSION.SdkInt >= 29;
        }

        public static bool HasMediaStoreBitRateColumn()
        {
            return (int)Android.OS.Build.VERSION.SdkInt >= 30;
        }

        // never made it into Android 12
        //public static bool HasMediaStoreSampleRateBitDepthColumn()
        //{
        //    return (int)Android.OS.Build.VERSION.SdkInt >= 31;
        //}

        //public static bool HasMediaRetreiverSampleRateBitDepth()
        //{
        //    return (int)Android.OS.Build.VERSION.SdkInt >= 31;
        //}

        public static bool IsUncompressed(string name)
        {
            string ext = System.IO.Path.GetExtension(name);
            switch (ext)
            {
                case ".wav":
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsLossless(string name)
        {
            string ext = System.IO.Path.GetExtension(name);
            switch (ext)
            {
                case ".ape":
                case ".flac":
                case ".wav":
                case ".alac":
                case ".aiff":
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsSupportedAudio(string name)
        {
            string ext = System.IO.Path.GetExtension(name);
            switch (ext)
            {
                case ".ape":
                case ".flac":
                case ".wav":
                case ".alac":
                case ".aiff":
                case ".mp3":
                case ".m4a":
                case ".wma":
                case ".aac":
                case ".opus":
                case ".ogg":
                case ".oga":
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Any exceptions here get caught.  worst case, you just get no metadata...
        /// </summary>
        /// <param name="contentResolver"></param>
        /// <param name="displayName"></param>
        /// <param name="size"></param>
        /// <param name="presentableName"></param>
        /// <param name="childUri"></param>
        /// <param name="allMediaInfoDict"></param>
        /// <param name="prevInfoToUse"></param>
        /// <returns></returns>
        public static Tuple<int, int, int, int> GetAudioAttributes(ContentResolver contentResolver, string displayName, long size, string presentableName, Android.Net.Uri childUri, Dictionary<string, List<Tuple<string, int, int>>> allMediaInfoDict, Dictionary<string, Tuple<long, string, Tuple<int, int, int, int>, bool, bool>> prevInfoToUse)
        {
            try
            {
                if (prevInfoToUse != null)
                {
                    if (prevInfoToUse.ContainsKey(presentableName))
                    {
                        var tuple = prevInfoToUse[presentableName];
                        if (tuple.Item1 == size) //this is the file...
                        {
                            return tuple.Item3;
                        }
                    }
                }
                //get media attributes...
                bool supported = IsSupportedAudio(presentableName);
                if (!supported)
                {
                    return null;
                }
                bool lossless = IsLossless(presentableName);
                bool uncompressed = IsUncompressed(presentableName);
                int duration = -1;
                int bitrate = -1;
                int sampleRate = -1;
                int bitDepth = -1;
                bool useContentResolverQuery = HasMediaStoreDurationColumn();//else it has no more additional data for us..
                if (useContentResolverQuery)
                {
                    bool hasBitRate = HasMediaStoreBitRateColumn();
                    //querying it every time was slow...
                    //so now we query it all ahead of time (with 1 query request) and put it in a dict.
                    string key = size + displayName;
                    if (allMediaInfoDict.ContainsKey(key))
                    {
                        string nameToSearchFor = presentableName.Replace('\\', '/');
                        bool found = true;
                        var listInfo = allMediaInfoDict[key];
                        Tuple<string, int, int> infoItem = null;
                        if (listInfo.Count > 1)
                        {
                            found = false;
                            foreach (var item in listInfo)
                            {
                                if (item.Item1.Contains(nameToSearchFor))
                                {
                                    infoItem = item;
                                    found = true;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            infoItem = listInfo[0];
                        }
                        if (found)
                        {
                            duration = infoItem.Item2 / 1000; //in ms
                            if (hasBitRate)
                            {
                                bitrate = infoItem.Item3;
                            }
                        }
                    }
                }

                if ((SeekerState.PerformDeepMetadataSearch && (bitrate == -1 || duration == -1) && size != 0))
                {
                    try
                    {
                        Android.Media.MediaMetadataRetriever mediaMetadataRetriever = new Android.Media.MediaMetadataRetriever();
                        mediaMetadataRetriever.SetDataSource(SeekerState.ActiveActivityRef, childUri); //TODO: error file descriptor must not be null.
                        string? bitRateStr = mediaMetadataRetriever.ExtractMetadata(Android.Media.MetadataKey.Bitrate);
                        string? durationStr = mediaMetadataRetriever.ExtractMetadata(Android.Media.MetadataKey.Duration);
                        if (HasMediaStoreDurationColumn())
                        {
                            mediaMetadataRetriever.Close(); //added in api 29
                        }
                        else
                        {
                            mediaMetadataRetriever.Release();
                        }

                        if (bitRateStr != null)
                        {
                            bitrate = int.Parse(bitRateStr);
                        }
                        if (durationStr != null)
                        {
                            duration = int.Parse(durationStr) / 1000;
                        }
                    }
                    catch (Exception e)
                    {
                        //ape and aiff always fail with built in metadata retreiver.
                        if (System.IO.Path.GetExtension(presentableName).ToLower() == ".ape")
                        {
                            MicroTagReader.GetApeMetadata(contentResolver, childUri, out sampleRate, out bitDepth, out duration);
                        }
                        else if (System.IO.Path.GetExtension(presentableName).ToLower() == ".aiff")
                        {
                            MicroTagReader.GetAiffMetadata(contentResolver, childUri, out sampleRate, out bitDepth, out duration);
                        }

                        //if still not fixed
                        if (sampleRate == -1 || duration == -1 || bitDepth == -1)
                        {
                            Logger.Firebase("MediaMetadataRetriever: " + e.Message + e.StackTrace + " isnull" + (SeekerState.ActiveActivityRef == null) + childUri?.ToString());
                        }
                    }
                }

                //this is the mp3 vbr case, android meta data retriever and therefore also the mediastore cache fail
                //quite badly in this case.  they often return the min vbr bitrate of 32000.
                //if its under 128kbps then lets just double check it..
                //I did test .m4a vbr.  android meta data retriever handled it quite well.
                //on api 19 the vbr being reported at 32000 is reported as 128000.... both obviously quite incorrect...
                if (System.IO.Path.GetExtension(presentableName) == ".mp3" && (bitrate >= 0 && bitrate <= 128000) && size != 0)
                {
                    if (SeekerState.PerformDeepMetadataSearch)
                    {
                        MicroTagReader.GetMp3Metadata(contentResolver, childUri, duration, size, out bitrate);
                    }
                    else
                    {
                        bitrate = -1; //better to have nothing than for it to be so blatantly wrong..
                    }
                }




                if (SeekerState.PerformDeepMetadataSearch && System.IO.Path.GetExtension(presentableName) == ".flac" && size != 0)
                {
                    MicroTagReader.GetFlacMetadata(contentResolver, childUri, out sampleRate, out bitDepth);
                }

                //if uncompressed we can use this simple formula
                if (uncompressed)
                {
                    if (bitrate != -1)
                    {
                        //bitrate = 2 * sampleRate * depth
                        //so test pairs in order of precedence..
                        if ((bitrate) / (2 * 44100) == 16)
                        {
                            sampleRate = 44100;
                            bitDepth = 16;
                        }
                        else if ((bitrate) / (2 * 44100) == 24)
                        {
                            sampleRate = 44100;
                            bitDepth = 24;
                        }
                        else if ((bitrate) / (2 * 48000) == 16)
                        {
                            sampleRate = 48000;
                            bitDepth = 16;
                        }
                        else if ((bitrate) / (2 * 48000) == 24)
                        {
                            sampleRate = 48000;
                            bitDepth = 24;
                        }
                    }
                }
                if (duration == -1 && bitrate == -1 && bitDepth == -1 && sampleRate == -1)
                {
                    return null;
                }
                return new Tuple<int, int, int, int>(duration, (lossless || bitrate == -1) ? -1 : (bitrate / 1000), bitDepth, sampleRate); //for lossless do not send bitrate!! no other client does that!!
            }
            catch (Exception e)
            {
                Logger.Firebase("get audio attr failed: " + e.Message + e.StackTrace);
                return null;
            }
        }

        public static void traverseDirectoryEntriesInternal(ContentResolver contentResolver, Android.Net.Uri rootUri, string parentDoc, Android.Net.Uri parentUri,
            Dictionary<string, Tuple<long, string, Tuple<int, int, int, int>, bool, bool>> pairs, bool isRootCase, string volName, List<Directory> listOfDirs, List<Directory> listOfLockedDirs, List<Directory> listOfHiddenDirs,
            List<Tuple<string, string>> dirMappingFriendlyNameToUri, string folderToStripForPresentableNames, Dictionary<int, string> index, DocumentFile rootDirCase,
            Dictionary<string, List<Tuple<string, int, int>>> allMediaInfoDict, Dictionary<string, Tuple<long, string, Tuple<int, int, int, int>, bool, bool>> previousFileInfoToUse,
            bool msdMsfOrOverrideCase, string msdMsfOrOverrideBuildParentName, ref int totalDirectoryCount, ref int indexNum)
        {
            //this should be the folder before the selected to strip away..



            Android.Net.Uri listChildrenUri = DocumentsContract.BuildChildDocumentsUriUsingTree(rootUri, parentDoc);
            //Log.d(TAG, "node uri: ", childrenUri);
            Android.Database.ICursor c = contentResolver.Query(listChildrenUri, new String[] { Document.ColumnDocumentId, Document.ColumnDisplayName, Document.ColumnMimeType, Document.ColumnSize }, null, null, null);
            //c can be null... reasons are fairly opaque - if remote exception return null. if underlying content provider is null.
            if (c == null)
            {
                //diagnostic code.

                //would a non /children uri work?
                bool nonChildrenWorks = contentResolver.Query(rootUri, new string[] { Document.ColumnSize }, null, null, null) != null;

                //would app context work?
                bool wouldActiveWork = SeekerState.ActiveActivityRef.ApplicationContext.ContentResolver.Query(listChildrenUri, new String[] { Document.ColumnDocumentId, Document.ColumnDisplayName, Document.ColumnMimeType, Document.ColumnSize }, null, null, null) != null;

                //would list files work?
                bool docFileLegacyWork = DocumentFile.FromTreeUri(SeekerState.ActiveActivityRef, parentUri).Exists();

                Logger.Firebase("cursor is null: parentDoc" + parentDoc + " list children uri: " + listChildrenUri?.ToString() + "nonchildren: " + nonChildrenWorks + " activeContext: " + wouldActiveWork + " legacyWork: " + docFileLegacyWork);
            }

            List<Soulseek.File> files = new List<Soulseek.File>();
            try
            {
                while (c.MoveToNext())
                {
                    string docId = c.GetString(0);
                    string name = c.GetString(1);
                    string mime = c.GetString(2);
                    long size = c.GetLong(3);
                    var childUri = DocumentsContract.BuildDocumentUriUsingTree(rootUri, docId);
                    //Logger.Debug("docId: " + docId + ", name: " + name + ", mime: " + mime);
                    if (isDirectory(mime))
                    {
                        totalDirectoryCount++;
                        traverseDirectoryEntriesInternal(contentResolver, rootUri, docId, childUri, pairs, false, volName, listOfDirs, listOfLockedDirs, listOfHiddenDirs,
                            dirMappingFriendlyNameToUri, folderToStripForPresentableNames, index, null, allMediaInfoDict, previousFileInfoToUse,
                            msdMsfOrOverrideCase, msdMsfOrOverrideCase ? msdMsfOrOverrideBuildParentName + '\\' + name : null, ref totalDirectoryCount, ref indexNum);
                    }
                    else
                    {
                        string presentableName = null;
                        if (msdMsfOrOverrideCase)
                        {
                            presentableName = msdMsfOrOverrideBuildParentName + '\\' + name;
                        }
                        else
                        {
                            presentableName = FileFilterHelper.GetPresentableName(childUri, folderToStripForPresentableNames, volName);
                        }


                        string searchableName = Common.Helpers.GetFolderNameFromFile(presentableName) + @"\" + CommonHelpers.GetFileNameFromFile(presentableName);

                        Tuple<int, int, int, int> attributes = GetAudioAttributes(contentResolver, name, size, presentableName, childUri, allMediaInfoDict, previousFileInfoToUse);
                        if (attributes != null)
                        {
                            //Logger.Debug("fname: " + name + " attr: " + attributes.Item1 + "  " + attributes.Item2 + "  " + attributes.Item3 + "  " + attributes.Item4 + "  ");
                        }

                        pairs.Add(presentableName, new Tuple<long, string, Tuple<int, int, int, int>, bool, bool>(size, childUri.ToString(), attributes, FileFilterHelper.IsLockedFile(presentableName), FileFilterHelper.IsHiddenFile(presentableName)));
                        index.Add(indexNum, presentableName); //throws on same key (the file in question ends with unicode EOT char (\u04)).
                        indexNum++;
                        if (indexNum % 50 == 0)
                        {
                            //update public status variable every so often
                            SeekerState.NumberParsed = indexNum;
                        }
                        //                        pairs.Add(new Tuple<string, string, long, string>(searchableName, childUri.ToString(), size, presentableName));

                        string fname = CommonHelpers.GetFileNameFromFile(presentableName.Replace("/", @"\")); //use presentable name so that the filename will not be primary:file.mp3
                                                                                                              //for the brose response should only be the filename!!! 
                                                                                                              //when a user tries to download something from a browse resonse, the soulseek client on their end must create a fully qualified path for us
                                                                                                              //bc we get a path that is:
                                                                                                              //"Soulseek Complete\\document\\primary:Pictures\\Soulseek Complete\\album\\09 Between Songs 4.mp3"
                                                                                                              //not quite a full URI but it does add quite a bit..

                        //if (searchableName.Length > 7 && searchableName.Substring(0, 8).ToLower() == "primary:")
                        //{
                        //    searchableName = searchableName.Substring(8);
                        //}

                        var slskFile = new Soulseek.File(1, fname, size, System.IO.Path.GetExtension(childUri.Path), SharedFileCache.GetFileAttributesFromTuple(attributes)); //soulseekQT does not show attributes in browse tab, but nicotine does.
                        files.Add(slskFile);
                    }
                }
                CommonHelpers.SortSlskDirFiles(files);
                string lastPathSegment = null;
                if (msdMsfOrOverrideCase)
                {
                    lastPathSegment = msdMsfOrOverrideBuildParentName;
                }
                else if (isRootCase)
                {
                    lastPathSegment = CommonHelpers.GetLastPathSegmentWithSpecialCaseProtection(rootDirCase, out _);
                }
                else
                {
                    lastPathSegment = parentUri.LastPathSegment;
                }
                string directoryPath = lastPathSegment.Replace("/", @"\");

                if (!msdMsfOrOverrideCase)
                {
                    if (folderToStripForPresentableNames == null) //this means that the primary: is in the path so at least convert it from primary: to primary:\
                    {
                        if (volName != null && volName.Length != directoryPath.Length) //i.e. if it has something after it.. primary: should be primary: not primary:\ but primary:Alarms should be primary:\Alarms
                        {
                            if (volName.Length > directoryPath.Length)
                            {
                                Logger.Firebase("volName > directoryPath" + volName + " -- " + directoryPath + " -- " + isRootCase);
                            }
                            directoryPath = directoryPath.Substring(0, volName.Length) + '\\' + directoryPath.Substring(volName.Length);
                        }
                    }
                    else
                    {
                        directoryPath = directoryPath.Substring(folderToStripForPresentableNames.Length);
                    }
                }

                var slskDir = new Soulseek.Directory(directoryPath, files);
                if (FileFilterHelper.IsHiddenFolder(directoryPath))
                {
                    listOfHiddenDirs.Add(slskDir);
                }
                else if (FileFilterHelper.IsLockedFolder(directoryPath))
                {
                    listOfLockedDirs.Add(slskDir);
                }
                else
                {
                    listOfDirs.Add(slskDir);
                }

                dirMappingFriendlyNameToUri.Add(new Tuple<string, string>(directoryPath, parentUri.ToString()));
            }
            finally
            {
                closeQuietly(c);
            }
        }

        public static void traverseDirectoryEntriesLegacy(DocumentFile parentDocFile, Dictionary<string, Tuple<long, string, Tuple<int, int, int, int>, bool, bool>> pairs, bool isRootCase,
            List<Directory> listOfDirs, List<Directory> listOfLockedDirs, List<Directory> listOfHiddenDirs, List<Tuple<string, string>> dirMappingFriendlyNameToUri,
            string folderToStripForPresentableNames, Dictionary<int, string> index, Dictionary<string, Tuple<long, string, Tuple<int, int, int, int>, bool, bool>> previousFileInfoToUse,
            bool overrideCase, string msdMsfOrOverrideBuildParentName,
            ref int totalDirectoryCount, ref int indexNum)
        {
            //this should be the folder before the selected to strip away..
            List<Soulseek.File> files = new List<Soulseek.File>();
            foreach (var childDocFile in parentDocFile.ListFiles())
            {
                if (childDocFile.IsDirectory)
                {
                    totalDirectoryCount++;
                    traverseDirectoryEntriesLegacy(childDocFile, pairs, false, listOfDirs, listOfLockedDirs, listOfHiddenDirs,
                        dirMappingFriendlyNameToUri, folderToStripForPresentableNames, index, previousFileInfoToUse, overrideCase,
                        overrideCase ? msdMsfOrOverrideBuildParentName + '\\' + childDocFile.Name : null, ref totalDirectoryCount, ref indexNum);
                }
                else
                {
                    //for subAPI21 last path segment is:
                    //".android_secure" so just the filename whereas Path is more similar to last part segment:
                    //"/storage/sdcard/.android_secure"
                    string presentableName = childDocFile.Uri.Path.Replace('/', '\\');
                    if (overrideCase)
                    {
                        presentableName = msdMsfOrOverrideBuildParentName + '\\' + childDocFile.Name;
                    }
                    else if (folderToStripForPresentableNames != null) //this means that the primary: is in the path so at least convert it from primary: to primary:\
                    {
                        presentableName = presentableName.Substring(folderToStripForPresentableNames.Length);
                    }

                    Tuple<int, int, int, int> attributes = GetAudioAttributes(SeekerState.ActiveActivityRef.ContentResolver, childDocFile.Name, childDocFile.Length(), presentableName, childDocFile.Uri, null, previousFileInfoToUse);
                    if (attributes != null)
                    {
                        //Logger.Debug("fname: " + childDocFile.Name + " attr: " + attributes.Item1 + "  " + attributes.Item2 + "  " + attributes.Item3 + "  " + attributes.Item4 + "  ");
                    }

                    pairs.Add(presentableName, new Tuple<long, string, Tuple<int, int, int, int>, bool, bool>(childDocFile.Length(), childDocFile.Uri.ToString(), attributes, FileFilterHelper.IsLockedFile(presentableName), FileFilterHelper.IsHiddenFile(presentableName))); //todo attributes was null here???? before
                    index.Add(indexNum, presentableName);
                    indexNum++;
                    if (indexNum % 50 == 0)
                    {
                        //update public status variable every so often
                        SeekerState.NumberParsed = indexNum;
                    }
                    string fname = CommonHelpers.GetFileNameFromFile(presentableName.Replace("/", @"\")); //use presentable name so that the filename will not be primary:file.mp3
                                                                                                          //for the brose response should only be the filename!!! 
                                                                                                          //when a user tries to download something from a browse resonse, the soulseek client on their end must create a fully qualified path for us
                                                                                                          //bc we get a path that is:
                                                                                                          //"Soulseek Complete\\document\\primary:Pictures\\Soulseek Complete\\album\\09 Between Songs 4.mp3"
                                                                                                          //not quite a full URI but it does add quite a bit..

                    //if (searchableName.Length > 7 && searchableName.Substring(0, 8).ToLower() == "primary:")
                    //{
                    //    searchableName = searchableName.Substring(8);
                    //}
                    var slskFile = new Soulseek.File(1, fname, childDocFile.Length(), System.IO.Path.GetExtension(childDocFile.Uri.Path));
                    files.Add(slskFile);
                }
            }

            CommonHelpers.SortSlskDirFiles(files);
            string directoryPath = parentDocFile.Uri.Path.Replace("/", @"\");

            if (overrideCase)
            {
                directoryPath = msdMsfOrOverrideBuildParentName;
            }
            else if (folderToStripForPresentableNames != null)
            {
                directoryPath = directoryPath.Substring(folderToStripForPresentableNames.Length);
            }

            var slskDir = new Soulseek.Directory(directoryPath, files);
            if (FileFilterHelper.IsHiddenFolder(directoryPath))
            {
                listOfHiddenDirs.Add(slskDir);
            }
            else if (FileFilterHelper.IsLockedFolder(directoryPath))
            {
                listOfLockedDirs.Add(slskDir);
            }
            else
            {
                listOfDirs.Add(slskDir);
            }

            dirMappingFriendlyNameToUri.Add(new Tuple<string, string>(directoryPath, parentDocFile.Uri.ToString()));
        }

        // Util method to check if the mime type is a directory
        public static bool isDirectory(String mimeType)
        {
            return DocumentsContract.Document.MimeTypeDir.Equals(mimeType);
        }

        // Util method to close a closeable
        public static void closeQuietly(Android.Database.ICursor closeable)
        {
            if (closeable != null)
            {
                try
                {
                    closeable.Close();
                }
                catch
                {
                    // ignore exception
                }
            }
        }

        /// <summary>
        /// Check Cache should be false if setting a new dir.. true if on startup.
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="checkCache"></param>
        public static bool InitializeDatabase(UploadDirectoryInfo newlyAddedDirectoryIfApplicable, bool checkCache, out string errorMsg)
        {
            errorMsg = string.Empty;
            bool success = false;
            try
            {
                CachedParseResults cachedParseResults = null;
                if (checkCache)
                {
                    // migrate if applicable
                    cachedParseResults = GetLegacyCachedParseResult();
                    if(cachedParseResults != null)
                    {
                        StoreCachedParseResults(SeekerState.ActiveActivityRef, cachedParseResults);
                        ClearLegacyParsedCacheResults();
                    }

                    cachedParseResults = GetCachedParseResults(SeekerState.ActiveActivityRef);
                }

                if (cachedParseResults == null)
                {
                    System.Diagnostics.Stopwatch s = new System.Diagnostics.Stopwatch();
                    s.Start();
                    Dictionary<string, Tuple<long, string, Tuple<int, int, int, int>, bool, bool>> keys = null;
                    BrowseResponse browseResponse = null;
                    List<Tuple<string, string>> dirMappingFriendlyNameToUri = null;
                    List<Soulseek.Directory> hiddenDirectories = null;
                    Dictionary<int, string> helperIndex = null;
                    int directoryCount = 0;


                    //optimization - if new directory is a subdir we can skip this part. !!!! but we still have things to do like make all files that start with said presentableDir to be locked / hidden. etc.

                    UploadDirectoryManager.UpdateWithDocumentFileAndErrorStates();
                    if (UploadDirectoryManager.AreAllFailed())
                    {
                        throw new DirectoryAccessFailure("All Failed");
                    }
                    if (SeekerState.PreOpenDocumentTree() || UploadDirectoryManager.AreAnyFromLegacy())
                    {
                        keys = ParseSharedDirectoryLegacy(null, SeekerState.SharedFileCache?.FullInfo, ref directoryCount, out browseResponse, out dirMappingFriendlyNameToUri, out helperIndex, out hiddenDirectories);
                    }
                    else
                    {
                        keys = ParseSharedDirectoryFastDocContract(null, SeekerState.SharedFileCache?.FullInfo, ref directoryCount, out browseResponse, out dirMappingFriendlyNameToUri, out helperIndex, out hiddenDirectories);
                    }

                    int nonHiddenCountForServer = keys.Count(pair1 => !pair1.Value.Item5);
                    Logger.Debug($"Non Hidden Count for Server: {nonHiddenCountForServer}");

                    SeekerState.NumberParsed = int.MaxValue; //our signal that we are finishing up...
                    s.Stop();
                    Logger.Debug(string.Format("{0} Files parsed in {1} milliseconds", keys.Keys.Count, s.ElapsedMilliseconds));
                    s.Reset();
                    s.Start();

                    Dictionary<string, List<int>> tokenIndex = new Dictionary<string, List<int>>();
                    var reversed = helperIndex.ToDictionary(x => x.Value, x => x.Key);
                    foreach (string presentableName in keys.Keys)
                    {
                        string searchableName = Common.Helpers.GetFolderNameFromFile(presentableName) + " " + System.IO.Path.GetFileNameWithoutExtension(CommonHelpers.GetFileNameFromFile(presentableName));
                        searchableName = SharedFileCache.MatchSpecialCharAgnostic(searchableName);
                        int code = reversed[presentableName];
                        foreach (string token in searchableName.ToLower().Split(null)) //null means whitespace
                        {
                            if (token == string.Empty)
                            {
                                continue;
                            }
                            if (tokenIndex.ContainsKey(token))
                            {
                                tokenIndex[token].Add(code);
                            }
                            else
                            {
                                tokenIndex[token] = new List<int>();
                                tokenIndex[token].Add(code);
                            }
                        }
                    }
                    s.Stop();

                    //foreach(string token in tokenIndex.Keys)
                    //{
                    //    Logger.Debug(token);
                    //}

                    Logger.Debug(string.Format("Token index created in {0} milliseconds", s.ElapsedMilliseconds));

                    //s.Stop();
                    //Logger.Debug("ParseSharedDirectory: " + s.ElapsedMilliseconds);

                    var newCachedResults = new CachedParseResults(
                        keys,
                        browseResponse.DirectoryCount, // todo?
                        browseResponse,
                        hiddenDirectories,
                        dirMappingFriendlyNameToUri,
                        tokenIndex,
                        helperIndex,
                        nonHiddenCountForServer);
                    StoreCachedParseResults(SeekerState.ActiveActivityRef, newCachedResults);

                    UploadDirectoryManager.SaveToSharedPreferences(SeekerState.SharedPreferences); 


                    ////5 searches a second = 18,000 per hour.
                    //System.Random rand = new System.Random();
                    //List<string> searchTerms = new List<string>();
                    //for (int i = 0; i < 18000; i++)
                    //{
                    //    int a = rand.Next();
                    //    searchTerms.Add("item" + a.ToString() + " " + "item2" + a.ToString());
                    //}

                    //System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
                    //sw.Reset();
                    //sw.Start();
                    //foreach (string search in searchTerms)
                    //{
                    //    foreach (string file in stringUriPairs.Keys)
                    //    {
                    //        if (file.Contains(search))
                    //        {
                    //            System.Console.WriteLine("true");
                    //        }
                    //    }
                    //}
                    //sw.Stop();
                    //Logger.Debug(string.Format("linear search .5 million: {0}", sw.ElapsedMilliseconds));
                    //sw.Reset();
                    //sw.Start();
                    ////5ms vs 27000ms for 100k searches over 10k files.
                    ////0ms vs 600ms for 5k searches over 2k files.

                    //foreach (string search in searchTerms)
                    //{
                    //    if (tokenIndex.ContainsKey(search))
                    //    {
                    //        System.Console.WriteLine("true");
                    //    }
                    //}

                    //sw.Stop();
                    //Logger.Debug(string.Format("term index search .5 million: {0}", sw.ElapsedMilliseconds));

                    // TODO we do not save the directoryCount ?? and so subsequent times its just browseResponse.Count?
                    // would it ever be different?

                    SlskHelp.SharedFileCache sharedFileCache = new SlskHelp.SharedFileCache(keys, directoryCount, browseResponse, dirMappingFriendlyNameToUri, tokenIndex, helperIndex, hiddenDirectories, nonHiddenCountForServer);//.Select(_=>_.Item1).ToList());
                    SharedFileCache_Refreshed(null, (sharedFileCache.DirectoryCount, nonHiddenCountForServer != -1 ? nonHiddenCountForServer : sharedFileCache.FileCount));
                    SeekerState.SharedFileCache = sharedFileCache;

                    //*********Profiling********* for 2252 files - 13s initial parsing, 1.9 MB total
                    //2552 Files parsed in 13,161 milliseconds  - if the phone is locked it takes twice as long
                    //Token index created in 370 milliseconds   - if the phone is locked it takes twice as long
                    //Browse Response is 379,963 bytes
                    //File Dictionary is 769,386 bytes
                    //Directory Dictionary is 137,518 bytes
                    //int(helper) index is 258,237 bytes
                    //token index is 393,354 bytes
                    //cache:
                    //time to deserialize all sharing helpers is 664 ms for 2k files...

                    //searching an hour (18,000) worth of terms
                    //linear - 22,765 ms
                    //dictionary based - 27ms

                    //*********Profiling********* for 807 files - 3s initial parsing, .66 MB total
                    //807 Files parsed in 2,935 milliseconds
                    //Token index created in 182 milliseconds
                    //Browse Response is 114,432 bytes
                    //File Dictionary is 281,610 bytes
                    //Directory Dictionary is 38,250 bytes
                    //int(helper) index is 78,589 bytes
                    //token index is 156,274 bytes

                    //searching an hour (18,000) worth of terms
                    //linear - 6,570 ms
                    //dictionary based - 22ms

                    //*********Profiling********* for 807 files -- deep metadata retreival off. (i.e. only whats indexed in MediaStore) - 
                    //*********Profiling********* for 807 files -- metadata for flac and those not in MediaStore - 12,234
                    //*********Profiling********* for 807 files -- mediaretreiver for everything.  metadata for flac and those not in MediaStore - 38,063



                }
                else
                {
                    Logger.Debug("Using cached results");
                    UploadDirectoryManager.UpdateWithDocumentFileAndErrorStates();
                    if (UploadDirectoryManager.AreAllFailed())
                    {
                        throw new DirectoryAccessFailure("All Failed");
                    }
                    else
                    {
                        SlskHelp.SharedFileCache sharedFileCache = new SlskHelp.SharedFileCache(cachedParseResults.keys, // todo new constructor
                            cachedParseResults.directoryCount, cachedParseResults.browseResponse, cachedParseResults.friendlyDirNameToUriMapping,
                            cachedParseResults.tokenIndex, cachedParseResults.helperIndex, cachedParseResults.browseResponseHiddenPortion,
                            cachedParseResults.nonHiddenFileCount);

                        SharedFileCache_Refreshed(null, (sharedFileCache.DirectoryCount, sharedFileCache.GetNonHiddenFileCountForServer() != -1 ? sharedFileCache.GetNonHiddenFileCountForServer() : sharedFileCache.FileCount));
                        SeekerState.SharedFileCache = sharedFileCache;
                    }
                }
                success = true;
                SeekerState.FailedShareParse = false;
                SeekerState.SharedFileCache.SuccessfullyInitialized = true;
            }
            catch (Exception e)
            {
                string defaultUnspecified = "Shared Folder Error - Unspecified Error";
                errorMsg = defaultUnspecified;
                if (e.GetType().FullName == "Java.Lang.SecurityException" || e is Java.Lang.SecurityException)
                {
                    errorMsg = SeekerApplication.GetString(Resource.String.PermissionsIssueShared);
                }
                success = false;
                Logger.Debug("Error parsing files: " + e.Message + e.StackTrace);


                if (e is DirectoryAccessFailure)
                {
                    errorMsg = "Shared Folder Error - " + UploadDirectoryManager.GetCompositeErrorString();
                }
                else
                {
                    Logger.Firebase("Error parsing files: " + e.Message + e.StackTrace);
                }

                if (e.Message.Contains("An item with the same key"))
                {
                    try
                    {
                        Logger.Firebase("Possible encoding issue: " + ShowCodePoints(e.Message.Substring(e.Message.Length - 7)));
                        errorMsg = "Path Conflict. Same Name?";
                    }
                    catch
                    {
                        //just in case
                    }
                }

                if (errorMsg == defaultUnspecified)
                {
                    Logger.Firebase("Error Parsing Files Unspecified Error" + e.Message + e.StackTrace);
                }
            }
            finally
            {
                if (!success)
                {
                    //if(newlyAddedDirectoryIfApplicable!=null)
                    //{
                    //    UploadDirectoryManager.UploadDirectories.Remove(newlyAddedDirectoryIfApplicable);
                    //    UploadDirectoryChanged?.Invoke(null, new EventArgs());
                    //}
                    //SeekerState.UploadDataDirectoryUri = null;
                    //SeekerState.UploadDataDirectoryUriIsFromTree = true;
                    SeekerState.FailedShareParse = true;
                    //if success if false then SeekerState.SharedFileCache might be null still causing a crash!
                    if (SeekerState.SharedFileCache != null)
                    {
                        SeekerState.SharedFileCache.SuccessfullyInitialized = false;
                    }
                }
            }
            return success;
            //SeekerState.SoulseekClient.SearchResponseDelivered += SoulseekClient_SearchResponseDelivered;
            //SeekerState.SoulseekClient.SearchResponseDeliveryFailed += SoulseekClient_SearchResponseDeliveryFailed;
        }

        public static T deserializeFromDisk<T>(Context c, Java.IO.File dir, string filename, MessagePack.MessagePackSerializerOptions options = null) where T : class
        {
            Java.IO.File fileForOurInternalStorage = new Java.IO.File(dir, filename);

            if (!fileForOurInternalStorage.Exists())
            {
                return null;
            }

            using (System.IO.Stream inputStream = c.ContentResolver.OpenInputStream(AndroidX.DocumentFile.Provider.DocumentFile.FromFile(fileForOurInternalStorage).Uri))
            {
                return MessagePack.MessagePackSerializer.Deserialize<T>(inputStream, options);
            }
        }

        public static CachedParseResults GetCachedParseResults(Context c)
        {
            Java.IO.File fileshare_dir = new Java.IO.File(c.FilesDir, KeyConsts.M_fileshare_cache_dir);
            if (!fileshare_dir.Exists())
            {
                return null;
            }

            try
            {
                var helperIndex = deserializeFromDisk<Dictionary<int, string>>(c, fileshare_dir, KeyConsts.M_HelperIndex_Filename);
                var tokenIndex = deserializeFromDisk<Dictionary<string, List<int>>>(c, fileshare_dir, KeyConsts.M_TokenIndex_Filename);
                var keys = deserializeFromDisk<Dictionary<string, Tuple<long, string, Tuple<int, int, int, int>, bool, bool>>>(c, fileshare_dir, KeyConsts.M_Keys_Filename);
                var browseResponse = deserializeFromDisk<BrowseResponse>(c, fileshare_dir, KeyConsts.M_BrowseResponse_Filename, SerializationHelper.BrowseResponseOptions);
                var browseResponseHidden = deserializeFromDisk<List<Directory>>(c, fileshare_dir, KeyConsts.M_BrowseResponse_Hidden_Filename, SerializationHelper.BrowseResponseOptions);
                var friendlyDirToUri = deserializeFromDisk<List<Tuple<string, string>>>(c, fileshare_dir, KeyConsts.M_FriendlyDirNameToUri_Filename);

                int nonHiddenFileCount = SeekerState.SharedPreferences.GetInt(KeyConsts.M_CACHE_nonHiddenFileCount_v3, -1);

                var cachedParseResults = new CachedParseResults(
                    keys,
                    browseResponse.DirectoryCount, //todo
                    browseResponse,
                    browseResponseHidden,
                    friendlyDirToUri,
                    tokenIndex,
                    helperIndex,
                    nonHiddenFileCount);
                return cachedParseResults;
            }
            catch(Exception e)
            {
                Logger.Firebase("FAILED to restore sharing parse results: " + e.Message + e.StackTrace);
                return null;
            }
        }

        public static void ClearParsedCacheResults(Context c)
        {
            Java.IO.File fileshare_dir = new Java.IO.File(c.FilesDir, KeyConsts.M_fileshare_cache_dir);
            if (!fileshare_dir.Exists())
            {
                return;
            }
            foreach(var file in fileshare_dir.ListFiles())
            {
                file.Delete();
            }
        }

        public static void StoreCachedParseResults(Context c, CachedParseResults cachedParseResults)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            Java.IO.File fileShareCachedDir = new Java.IO.File(c.FilesDir, KeyConsts.M_fileshare_cache_dir);
            if (!fileShareCachedDir.Exists())
            {
                fileShareCachedDir.Mkdir();
            }

            byte[] data = MessagePack.MessagePackSerializer.Serialize(cachedParseResults.helperIndex);
            CommonHelpers.SaveToDisk(c, data, fileShareCachedDir, KeyConsts.M_HelperIndex_Filename);

            data = MessagePack.MessagePackSerializer.Serialize(cachedParseResults.tokenIndex);
            CommonHelpers.SaveToDisk(c, data, fileShareCachedDir, KeyConsts.M_TokenIndex_Filename);

            data = MessagePack.MessagePackSerializer.Serialize(cachedParseResults.keys); //TODO directoryCount
            CommonHelpers.SaveToDisk(c, data, fileShareCachedDir, KeyConsts.M_Keys_Filename);

            data = MessagePack.MessagePackSerializer.Serialize(cachedParseResults.browseResponse, options: SerializationHelper.BrowseResponseOptions);
            CommonHelpers.SaveToDisk(c, data, fileShareCachedDir, KeyConsts.M_BrowseResponse_Filename);

            data = MessagePack.MessagePackSerializer.Serialize(cachedParseResults.browseResponseHiddenPortion, options: SerializationHelper.BrowseResponseOptions);
            CommonHelpers.SaveToDisk(c, data, fileShareCachedDir, KeyConsts.M_BrowseResponse_Hidden_Filename);

            data = MessagePack.MessagePackSerializer.Serialize(cachedParseResults.friendlyDirNameToUriMapping);
            CommonHelpers.SaveToDisk(c, data, fileShareCachedDir, KeyConsts.M_FriendlyDirNameToUri_Filename);

            lock (SeekerState.SharedPrefLock)
            {
                var editor = SeekerState.SharedPreferences.Edit();
                editor.PutInt(KeyConsts.M_CACHE_nonHiddenFileCount_v3, cachedParseResults.nonHiddenFileCount);
                //editor.PutString(KeyConsts.M_UploadDirectoryUri, SeekerState.UploadDataDirectoryUri);
                //editor.PutBoolean(KeyConsts.M_UploadDirectoryUriIsFromTree, SeekerState.UploadDataDirectoryUriIsFromTree);


                //TODO TODO save upload dirs ---- do this now might as well....

                //before this line ^ ,its possible for the saved UploadDirectoryUri and the actual browse response to be different.
                //this is because upload data uri saves on MainActivity OnPause. and so one could set shared folder and then press home and then swipe up. never having saved uploadirectoryUri.
                editor.Commit();
            }
        }

        public static string ShowCodePoints(string str)
        {
            string codePointString = string.Empty;
            foreach (char c in str)
            {
                codePointString = codePointString + ($"_{(int)c:x4}");
            }
            return codePointString;
        }

        public static void SharedFileCache_Refreshed(object sender, (int Directories, int Files) e)
        {
            if (SeekerState.SoulseekClient.State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                SeekerState.SoulseekClient.SetSharedCountsAsync(e.Directories, e.Files);
                SeekerState.NumberOfSharedDirectoriesIsStale = false;
            }
            else
            {
                SeekerState.NumberOfSharedDirectoriesIsStale = true;
            }
        }

        /// <summary>
        /// Inform server the number of files we are sharing or 0,0 if not sharing...
        /// it looks like people typically report all including locked files. lets not report hidden files though.
        /// </summary>
        public static void InformServerOfSharedFiles()
        {
            try
            {
                if (SeekerState.SoulseekClient != null && SeekerState.SoulseekClient.State.HasFlag(SoulseekClientStates.LoggedIn))
                {
                    if (MeetsCurrentSharingConditions())
                    {
                        if (SeekerState.SharedFileCache != null)
                        {
                            Logger.Debug("Tell server we are sharing " + SeekerState.SharedFileCache.DirectoryCount + " dirs and " + SeekerState.SharedFileCache.GetNonHiddenFileCountForServer() + " files");
                            SeekerState.SoulseekClient.SetSharedCountsAsync(SeekerState.SharedFileCache.DirectoryCount,
                                SeekerState.SharedFileCache.GetNonHiddenFileCountForServer() != -1 ? SeekerState.SharedFileCache.GetNonHiddenFileCountForServer() : SeekerState.SharedFileCache.FileCount);
                        }
                        else
                        {
                            Logger.Debug("We would tell server but we are not successfully set up yet.");
                        }
                    }
                    else
                    {
                        Logger.Debug("Tell server we are sharing 0 dirs and 0 files");
                        SeekerState.SoulseekClient.SetSharedCountsAsync(0, 0);
                    }
                    SeekerState.NumberOfSharedDirectoriesIsStale = false;
                }
                else
                {
                    if (MeetsCurrentSharingConditions())
                    {
                        if (SeekerState.SharedFileCache != null)
                        {
                            Logger.Debug("We need to Tell server we are sharing " + SeekerState.SharedFileCache.DirectoryCount + " dirs and " + SeekerState.SharedFileCache.GetNonHiddenFileCountForServer() + " files on next log in");
                        }
                        else
                        {
                            Logger.Debug("we meet sharing conditions but our shared file cache is not successfully set up");
                        }
                    }
                    else
                    {
                        Logger.Debug("We need to Tell server we are sharing 0 dirs and 0 files on next log in");
                    }
                    SeekerState.NumberOfSharedDirectoriesIsStale = true;
                }
            }
            catch (Exception e)
            {
                Logger.Debug("Failed to InformServerOfSharedFiles " + e.Message + e.StackTrace);
                Logger.Firebase("Failed to InformServerOfSharedFiles " + e.Message + e.StackTrace);
            }
        }

        /// <summary>
        /// Has set things up properly and has sharing on.
        /// </summary>
        /// <returns></returns>
        public static bool MeetsSharingConditions()
        {
            return SeekerState.SharingOn && UploadDirectoryManager.UploadDirectories.Count != 0 && !SeekerState.IsParsing && !UploadDirectoryManager.AreAllFailed();
        }

        /// <summary>
        /// Has set things up properly and has sharing on + their network settings currently allow it.
        /// </summary>
        /// <returns></returns>
        public static bool MeetsCurrentSharingConditions()
        {
            return MeetsSharingConditions() && SeekerState.IsNetworkPermitting();
        }

        public static bool IsSharingSetUpSuccessfully()
        {
            if (SeekerState.SharedFileCache == null || !SeekerState.SharedFileCache.SuccessfullyInitialized)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }

    // TODOORG with other exception classes
    public class DirectoryAccessFailure : System.Exception
    {
        public DirectoryAccessFailure(string msg) : base(msg)
        {

        }
    }
}
