using Android.Content;
using Android.Provider;
using Java.IO;
using AndroidX.DocumentFile.Provider;
using Seeker.Helpers;
using System;
using static Android.Provider.DocumentsContract;

using Common;
namespace Seeker.Services
{
    public static class FileSystemService
    {
        public static object lock_toplevel_ifexist_create = new object();
        public static object lock_album_ifexist_create = new object();

        public static void GetOrCreateIncompleteLocation(string username, string fullfilename, int depth, out Android.Net.Uri incompleteUri, out Android.Net.Uri parentUri, out long partialLength)
        {
            string name = SimpleHelpers.GetFileNameFromFile(fullfilename);
            //string dir = Helpers.GetFolderNameFromFile(fullfilename);
            string filePath = string.Empty;

            bool useDownloadDir = false;
            if (PreferencesState.CreateCompleteAndIncompleteFolders && !SettingsActivity.UseIncompleteManualFolder())
            {
                useDownloadDir = true;
            }
            bool useTempDir = false;
            if (SettingsActivity.UseTempDirectory())
            {
                useTempDir = true;
            }
            bool useCustomDir = false;
            if (SettingsActivity.UseIncompleteManualFolder())
            {
                useCustomDir = true;
            }

            bool fileExists = false;
            if (SeekerState.UseLegacyStorage() && (SeekerState.RootDocumentFile == null && useDownloadDir))
            {
                Java.IO.File incompleteDir = null;
                Java.IO.File musicDir = null;
                try
                {
                    string rootdir = string.Empty;
                    //if (SeekerState.RootDocumentFile==null)
                    //{
                    rootdir = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMusic).AbsolutePath;
                    //}
                    //else
                    //{
                    //    rootdir = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMusic).AbsolutePath;
                    //    rootdir = SeekerState.RootDocumentFile.Uri.Path; //returns junk...
                    //}

                    if (!(new Java.IO.File(rootdir)).Exists())
                    {
                        (new Java.IO.File(rootdir)).Mkdirs();
                    }
                    //string rootdir = GetExternalFilesDir(Android.OS.Environment.DirectoryMusic)
                    string incompleteDirString = rootdir + @"/Soulseek Incomplete/";
                    lock (lock_toplevel_ifexist_create)
                    {
                        incompleteDir = new Java.IO.File(incompleteDirString);
                        if (!incompleteDir.Exists())
                        {
                            //make it and add nomedia...
                            incompleteDir.Mkdirs();
                            CreateNoMediaFileLegacy(incompleteDirString);
                        }
                    }

                    string fullDir = rootdir + @"/Soulseek Incomplete/" + SimpleHelpers.GenerateIncompleteFolderName(username, fullfilename, depth); //+ @"/" + name;
                    musicDir = new Java.IO.File(fullDir);
                    lock (lock_album_ifexist_create)
                    {
                        if (!musicDir.Exists())
                        {
                            musicDir.Mkdirs();
                            CreateNoMediaFileLegacy(fullDir);
                        }
                    }
                    parentUri = Android.Net.Uri.Parse(new Java.IO.File(fullDir).ToURI().ToString());
                    filePath = fullDir + @"/" + name;
                    Java.IO.File f = new Java.IO.File(filePath);
                    if (f.Exists())
                    {
                        fileExists = true;
                        partialLength = f.Length();
                    }
                    else
                    {
                        partialLength = 0;
                    }
                    incompleteUri = Android.Net.Uri.Parse(new Java.IO.File(filePath).ToURI().ToString()); //using incompleteUri.Path gives you filePath :)
                }
                catch (Exception e)
                {
                    Logger.Firebase("Legacy Filesystem Issue: " + e.Message + e.StackTrace + System.Environment.NewLine + incompleteDir.Exists() + musicDir.Exists() + fileExists);
                    throw;
                }
            }
            else
            {
                DocumentFile folderDir1 = null; //this is the desired location.
                DocumentFile rootdir = null;

                bool diagRootDirExistsAndCanWrite = false;
                bool diagDidWeCreateSoulSeekDir = false;
                bool diagSlskDirExistsAfterCreation = false;
                bool rootDocumentFileIsNull = SeekerState.RootDocumentFile == null;

                if (rootDocumentFileIsNull)
                {
                    throw new DownloadDirectoryNotSetException();
                }

                //Logger.Debug("rootDocumentFileIsNull: " + rootDocumentFileIsNull);
                try
                {
                    if (useDownloadDir)
                    {
                        rootdir = SeekerState.RootDocumentFile;
                        Logger.Debug("using download dir" + rootdir.Uri.LastPathSegment);
                    }
                    else if (useTempDir)
                    {
                        Java.IO.File appPrivateExternal = SeekerState.ActiveActivityRef.GetExternalFilesDir(null);
                        rootdir = DocumentFile.FromFile(appPrivateExternal);
                        Logger.Debug("using temp incomplete dir");
                    }
                    else if (useCustomDir)
                    {
                        rootdir = SeekerState.RootIncompleteDocumentFile;
                        Logger.Debug("using custom incomplete dir" + rootdir.Uri.LastPathSegment);
                    }
                    else
                    {
                        Logger.Firebase("!! should not get here, no dirs");
                    }

                    if (!rootdir.Exists())
                    {
                        Logger.Firebase("rootdir (nonnull) does not exist: " + rootdir.Uri);
                        diagRootDirExistsAndCanWrite = false;
                        //dont know how to create self
                        //rootdir.CreateDirectory();
                    }
                    else if (!rootdir.CanWrite())
                    {
                        diagRootDirExistsAndCanWrite = false;
                        Logger.Firebase("rootdir (nonnull) exists but cant write: " + rootdir.Uri);
                    }
                    else
                    {
                        diagRootDirExistsAndCanWrite = true;
                    }


                    //string diagMessage10 = CheckPermissions(rootdir.Uri); //TEST CODE REMOVE

                    //var slskCompleteUri = rootdir.Uri + @"/Soulseek Complete/";
                    DocumentFile slskDir1 = null;
                    lock (lock_toplevel_ifexist_create)
                    {
                        slskDir1 = rootdir.FindFile("Soulseek Incomplete"); //does Soulseek Complete folder exist
                        if (slskDir1 == null || !slskDir1.Exists())
                        {
                            slskDir1 = rootdir.CreateDirectory("Soulseek Incomplete");
                            //slskDir1 = rootdir.FindFile("Soulseek Incomplete");
                            if (slskDir1 == null)
                            {
                                string diagMessage = CheckPermissions(rootdir.Uri);
                                Logger.Firebase("slskDir1 is null" + rootdir.Uri + "parent: " + diagMessage);
                                Logger.InfoFirebase("slskDir1 is null" + rootdir.Uri + "parent: " + diagMessage);
                            }
                            else if (!slskDir1.Exists())
                            {
                                Logger.Firebase("slskDir1 does not exist" + rootdir.Uri);
                            }
                            else if (!slskDir1.CanWrite())
                            {
                                Logger.Firebase("slskDir1 cannot write" + rootdir.Uri);
                            }
                            CreateNoMediaFile(slskDir1);
                        }
                    }
                    //slskDir1 = rootdir.FindFile("Soulseek Incomplete"); //if it does not exist you then have to get it again!!
                    //                                                  //else first time the song will be outside the directory
                    if (slskDir1 == null)
                    {
                        diagSlskDirExistsAfterCreation = false;
                        Logger.Firebase("slskDir1 is null");
                        Logger.InfoFirebase("slskDir1 is null");
                    }
                    else
                    {
                        diagSlskDirExistsAfterCreation = true;
                    }


                    string album_folder_name = SimpleHelpers.GenerateIncompleteFolderName(username, fullfilename, depth);
                    lock (lock_album_ifexist_create)
                    {
                        folderDir1 = slskDir1.FindFile(album_folder_name); //does the folder we want to save to exist
                        if (folderDir1 == null || !folderDir1.Exists())
                        {
                            folderDir1 = slskDir1.CreateDirectory(album_folder_name);
                            //folderDir1 = slskDir1.FindFile(album_folder_name); //if it does not exist you then have to get it again!!
                            if (folderDir1 == null)
                            {
                                string rootUri = string.Empty;
                                if (SeekerState.RootDocumentFile != null)
                                {
                                    rootUri = SeekerState.RootDocumentFile.Uri.ToString();
                                }
                                bool slskDirExistsWriteable = false;
                                if (slskDir1 != null)
                                {
                                    slskDirExistsWriteable = slskDir1.Exists() && slskDir1.CanWrite();
                                }
                                string diagMessage = CheckPermissions(slskDir1.Uri);
                                Logger.InfoFirebase("folderDir1 is null:" + album_folder_name + "root: " + rootUri + "slskDirExistsWriteable" + slskDirExistsWriteable + "slskDir: " + diagMessage);
                                Logger.Firebase("folderDir1 is null:" + album_folder_name + "root: " + rootUri + "slskDirExistsWriteable" + slskDirExistsWriteable + "slskDir: " + diagMessage);
                            }
                            else if (!folderDir1.Exists())
                            {
                                Logger.Firebase("folderDir1 does not exist:" + album_folder_name);
                            }
                            else if (!folderDir1.CanWrite())
                            {
                                Logger.Firebase("folderDir1 cannot write:" + album_folder_name);
                            }
                            CreateNoMediaFile(folderDir1);
                        }
                    }

                    //THE VALID GOOD flags for a directory is Supports Dir Create.  The write is off in all of my cases and not necessary..

                    //string diagMessage2 = CheckPermissions(slskDir1.Uri); //TEST CODE DELETE
                    //string diagMessage3 = CheckPermissions(folderDir1.Uri); //TEST CODE DELETE


                }
                catch (Exception e)
                {
                    string rootDirUri = SeekerState.RootDocumentFile?.Uri == null ? "null" : SeekerState.RootDocumentFile.Uri.ToString();
                    Logger.Firebase("Filesystem Issue: " + rootDirUri + " " + e.Message + diagSlskDirExistsAfterCreation + diagRootDirExistsAndCanWrite + diagDidWeCreateSoulSeekDir + rootDocumentFileIsNull + e.StackTrace);
                }

                if (rootdir == null && !SeekerState.UseLegacyStorage())
                {
                    SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.seeker_cannot_access_files), Android.Widget.ToastLength.Long);
                }

                //BACKUP IF FOLDER DIR IS NULL
                if (folderDir1 == null)
                {
                    folderDir1 = rootdir; //use the root instead..
                }

                parentUri = folderDir1.Uri;

                filePath = folderDir1.Uri + @"/" + name;

                DocumentFile potentialFile = folderDir1.FindFile(name); //this will return null if does not exist!!
                if (potentialFile != null && potentialFile.Exists())  //dont do a check for length 0 because then it will go to else and create another identical file (2)
                {
                    partialLength = potentialFile.Length();
                    incompleteUri = potentialFile.Uri;
                }
                else
                {
                    partialLength = 0;
                    DocumentFile mFile = CommonHelpers.CreateMediaFile(folderDir1, name); //on samsung api 19 it renames song.mp3 to song.mp3.mp3. //TODO fix this! (tho below api 29 doesnt use this path anymore)
                    //String: name of new document, without any file extension appended; the underlying provider may choose to append the extension.. Whoops...
                    incompleteUri = mFile.Uri; //nullref TODO TODO: if null throw custom exception so you can better handle it later on in DL continuation action
                }
            }
        }

        /// <summary>
        /// Opens a stream for writing to the incomplete file. Call after GetOrCreateIncompleteLocation.
        /// </summary>
        public static System.IO.Stream OpenIncompleteStream(Android.Net.Uri incompleteUri, long partialLength)
        {
            if (SeekerState.UseLegacyStorage() && incompleteUri.Scheme == "file")
            {
                string filePath = incompleteUri.Path;
                if (partialLength > 0)
                {
                    return new System.IO.FileStream(filePath, System.IO.FileMode.Append, System.IO.FileAccess.Write, System.IO.FileShare.None);
                }
                else
                {
                    return System.IO.File.Create(filePath);
                }
            }
            else
            {
                System.IO.Stream stream;
                if (partialLength > 0)
                {
                    stream = SeekerState.MainActivityRef.ContentResolver.OpenOutputStream(incompleteUri, "wa");
                }
                else
                {
                    stream = SeekerState.MainActivityRef.ContentResolver.OpenOutputStream(incompleteUri);
                }

                return new PositionTrackingOutputStream(stream, 0);
            }
        }

        /// <summary>
        /// Check Permissions if things dont go right for better diagnostic info
        /// </summary>
        /// <param name="folder"></param>
        /// <returns></returns>
        public static string CheckPermissions(Android.Net.Uri folder)
        {
            if (SeekerState.ActiveActivityRef != null)
            {
                var cursor = SeekerState.ActiveActivityRef.ContentResolver.Query(folder, new string[] { DocumentsContract.Document.ColumnFlags }, null, null, null);
                int flags = 0;
                if (cursor.MoveToFirst())
                {
                    flags = cursor.GetInt(0);
                }
                cursor.Close();
                bool canWrite = (flags & (int)DocumentContractFlags.SupportsWrite) != 0;
                bool canDirCreate = (flags & (int)DocumentContractFlags.DirSupportsCreate) != 0;
                if (canWrite && canDirCreate)
                {
                    return "Can Write and DirSupportsCreate";
                }
                else if (canWrite)
                {
                    return "Can Write and not DirSupportsCreate";
                }
                else if (canDirCreate)
                {
                    return "Can not Write and can DirSupportsCreate";
                }
                else
                {
                    return "No permissions";
                }
            }
            return string.Empty;
        }

        public static string SaveToFile(
            string fullfilename,
            string username,
            byte[] bytes,
            Android.Net.Uri uriOfIncomplete,
            Android.Net.Uri parentUriOfIncomplete,
            bool memoryMode,
            int depth,
            bool noSubFolder,
            out string finalUri)
        {
            string name = SimpleHelpers.GetFileNameFromFile(fullfilename);
            string dir = Common.Helpers.GetFolderNameFromFile(fullfilename, depth);
            string filePath = string.Empty;

            if (memoryMode && (bytes == null || bytes.Length == 0))
            {
                Logger.Firebase("EMPTY or NULL BYTE ARRAY in mem mode");
            }

            if (!memoryMode && uriOfIncomplete == null)
            {
                Logger.Firebase("no URI in file mode");
            }
            finalUri = string.Empty;
            if (SeekerState.UseLegacyStorage() &&
                (SeekerState.RootDocumentFile == null && !SettingsActivity.UseIncompleteManualFolder())) //if the user didnt select a complete OR incomplete directory. i.e. pure java files.
            {

                //this method works just fine if coming from a temp dir.  just not a open doc tree dir.

                string rootdir = string.Empty;
                //if (PreferencesState.SaveDataDirectoryUri ==null || PreferencesState.SaveDataDirectoryUri == string.Empty)
                //{
                rootdir = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMusic).AbsolutePath;
                //}
                //else
                //{
                //    rootdir = PreferencesState.SaveDataDirectoryUri;
                //}
                if (!(new Java.IO.File(rootdir)).Exists())
                {
                    (new Java.IO.File(rootdir)).Mkdirs();
                }
                //string rootdir = GetExternalFilesDir(Android.OS.Environment.DirectoryMusic)
                string intermediateFolder = @"/";
                if (PreferencesState.CreateCompleteAndIncompleteFolders)
                {
                    intermediateFolder = @"/Soulseek Complete/";
                }
                if (PreferencesState.CreateUsernameSubfolders)
                {
                    intermediateFolder = intermediateFolder + username + @"/"; //TODO: escape? slashes? etc... can easily test by just setting username to '/' in debugger
                }
                string fullDir = rootdir + intermediateFolder + (noSubFolder ? "" : dir); //+ @"/" + name;
                Java.IO.File musicDir = new Java.IO.File(fullDir);
                musicDir.Mkdirs();
                filePath = fullDir + @"/" + name;
                Java.IO.File musicFile = new Java.IO.File(filePath);
                FileOutputStream stream = new FileOutputStream(musicFile);
                finalUri = musicFile.ToURI().ToString();
                if (memoryMode)
                {
                    stream.Write(bytes);
                    stream.Close();
                }
                else
                {
                    Java.IO.File inFile = new Java.IO.File(uriOfIncomplete.Path);
                    Java.IO.File inDir = new Java.IO.File(parentUriOfIncomplete.Path);
                    MoveFile(new FileInputStream(inFile), stream, inFile, inDir);
                }
            }
            else
            {
                bool useLegacyDocFileToJavaFileOverride = false;
                DocumentFile legacyRootDir = null;
                if (SeekerState.UseLegacyStorage() && SeekerState.RootDocumentFile == null && SettingsActivity.UseIncompleteManualFolder())
                {
                    //this means that even though rootfile is null, manual folder is set and is a docfile.
                    //so we must wrap the default root doc file.
                    string legacyRootdir = string.Empty;
                    //if (PreferencesState.SaveDataDirectoryUri ==null || PreferencesState.SaveDataDirectoryUri == string.Empty)
                    //{
                    legacyRootdir = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMusic).AbsolutePath;
                    //}
                    //else
                    //{
                    //    rootdir = PreferencesState.SaveDataDirectoryUri;
                    //}
                    Java.IO.File legacyRoot = (new Java.IO.File(legacyRootdir));
                    if (!legacyRoot.Exists())
                    {
                        legacyRoot.Mkdirs();
                    }

                    legacyRootDir = DocumentFile.FromFile(legacyRoot);

                    useLegacyDocFileToJavaFileOverride = true;

                }

                DocumentFile folderDir1 = null; //this is the desired location.
                DocumentFile rootdir = null;

                bool diagRootDirExists = true;
                bool diagDidWeCreateSoulSeekDir = false;
                bool diagSlskDirExistsAfterCreation = true;
                bool rootDocumentFileIsNull = SeekerState.RootDocumentFile == null;
                try
                {
                    rootdir = SeekerState.RootDocumentFile;

                    if (useLegacyDocFileToJavaFileOverride)
                    {
                        rootdir = legacyRootDir;
                    }

                    if (!rootdir.Exists())
                    {
                        diagRootDirExists = false;
                        //dont know how to create self
                        //rootdir.CreateDirectory();
                    }
                    //var slskCompleteUri = rootdir.Uri + @"/Soulseek Complete/";

                    DocumentFile slskDir1 = null;
                    if (PreferencesState.CreateCompleteAndIncompleteFolders)
                    {
                        slskDir1 = rootdir.FindFile("Soulseek Complete"); //does Soulseek Complete folder exist
                        if (slskDir1 == null || !slskDir1.Exists())
                        {
                            slskDir1 = rootdir.CreateDirectory("Soulseek Complete");
                            Logger.Debug("Creating Soulseek Complete");
                            diagDidWeCreateSoulSeekDir = true;
                        }

                        if (slskDir1 == null)
                        {
                            diagSlskDirExistsAfterCreation = false;
                        }
                        else if (!slskDir1.Exists())
                        {
                            diagSlskDirExistsAfterCreation = false;
                        }
                    }
                    else
                    {
                        slskDir1 = rootdir;
                    }

                    bool diagUsernameDirExistsAfterCreation = false;
                    bool diagDidWeCreateUsernameDir = false;
                    if (PreferencesState.CreateUsernameSubfolders)
                    {
                        DocumentFile tempUsernameDir1 = null;
                        lock (string.Intern("IfNotExistCreateAtomic_1"))
                        {
                            tempUsernameDir1 = slskDir1.FindFile(username); //does username folder exist
                            if (tempUsernameDir1 == null || !tempUsernameDir1.Exists())
                            {
                                tempUsernameDir1 = slskDir1.CreateDirectory(username);
                                Logger.Debug(string.Format("Creating {0} dir", username));
                                diagDidWeCreateUsernameDir = true;
                            }
                        }

                        if (tempUsernameDir1 == null)
                        {
                            diagUsernameDirExistsAfterCreation = false;
                        }
                        else if (!slskDir1.Exists())
                        {
                            diagUsernameDirExistsAfterCreation = false;
                        }
                        else
                        {
                            diagUsernameDirExistsAfterCreation = true;
                        }
                        slskDir1 = tempUsernameDir1;
                    }

                    if (depth == 1)
                    {
                        if(noSubFolder)
                        {
                            folderDir1 = slskDir1;
                        }
                        else
                        {
                            lock (string.Intern("IfNotExistCreateAtomic_2"))
                            {
                                folderDir1 = slskDir1.FindFile(dir); //does the folder we want to save to exist
                                if (folderDir1 == null || !folderDir1.Exists())
                                {
                                    Logger.Debug("Creating " + dir);
                                    folderDir1 = slskDir1.CreateDirectory(dir);
                                }
                                if (folderDir1 == null || !folderDir1.Exists())
                                {
                                    Logger.Firebase("folderDir is null or does not exists");
                                }
                            }
                        }
                    }
                    else
                    {
                        DocumentFile folderDirNext = null;
                        folderDir1 = slskDir1;
                        int _depth = depth;
                        while (_depth > 0)
                        {
                            var parts = dir.Split('\\');
                            string singleDir = parts[parts.Length - _depth];
                            lock (string.Intern("IfNotExistCreateAtomic_3"))
                            {
                                folderDirNext = folderDir1.FindFile(singleDir); //does the folder we want to save to exist
                                if (folderDirNext == null || !folderDirNext.Exists())
                                {
                                    Logger.Debug("Creating " + dir);
                                    folderDirNext = folderDir1.CreateDirectory(singleDir);
                                }
                                if (folderDirNext == null || !folderDirNext.Exists())
                                {
                                    Logger.Firebase("folderDir is null or does not exists, depth" + _depth);
                                }
                            }
                            folderDir1 = folderDirNext;
                            _depth--;
                        }
                    }
                    //folderDir1 = slskDir1.FindFile(dir); //if it does not exist you then have to get it again!!
                }
                catch (Exception e)
                {
                    Logger.Firebase("Filesystem Issue: " + e.Message + diagSlskDirExistsAfterCreation + diagRootDirExists + diagDidWeCreateSoulSeekDir + rootDocumentFileIsNull + PreferencesState.CreateUsernameSubfolders);
                }

                if (rootdir == null && !SeekerState.UseLegacyStorage())
                {
                    SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.seeker_cannot_access_files), Android.Widget.ToastLength.Long);
                }

                //BACKUP IF FOLDER DIR IS NULL
                if (folderDir1 == null)
                {
                    folderDir1 = rootdir; //use the root instead..
                }

                filePath = folderDir1.Uri + @"/" + name;

                //Java.IO.File musicFile = new Java.IO.File(filePath);
                //FileOutputStream stream = new FileOutputStream(mFile);
                if (memoryMode)
                {
                    DocumentFile mFile = CommonHelpers.CreateMediaFile(folderDir1, name);
                    finalUri = mFile.Uri.ToString();
                    System.IO.Stream stream = SeekerState.ActiveActivityRef.ContentResolver.OpenOutputStream(mFile.Uri);
                    stream.Write(bytes);
                    stream.Close();
                }
                else
                {

                    //106ms for 32mb
                    Android.Net.Uri uri = null;
                    if (SeekerState.PreMoveDocument() ||
                        SettingsActivity.UseTempDirectory() || //i.e. if use temp dir which is file: // rather than content: //
                        (SeekerState.UseLegacyStorage() && SettingsActivity.UseIncompleteManualFolder() && SeekerState.RootDocumentFile == null) || //i.e. if use complete dir is file: // rather than content: // but Incomplete is content: //
                        CommonHelpers.CompleteIncompleteDifferentVolume() || !PreferencesState.ManualIncompleteDataDirectoryUriIsFromTree || !PreferencesState.SaveDataDirectoryUriIsFromTree)
                    {
                        try
                        {
                            DocumentFile mFile = CommonHelpers.CreateMediaFile(folderDir1, name);
                            uri = mFile.Uri;
                            finalUri = mFile.Uri.ToString();
                            System.IO.Stream stream = SeekerState.ActiveActivityRef.ContentResolver.OpenOutputStream(mFile.Uri);
                            MoveFile(SeekerState.ActiveActivityRef.ContentResolver.OpenInputStream(uriOfIncomplete), stream, uriOfIncomplete, parentUriOfIncomplete);
                        }
                        catch (Exception e)
                        {
                            Logger.Firebase("CRITICAL FILESYSTEM ERROR pre" + e.Message);
                            SeekerApplication.Toaster.ShowToast("Error Saving File", Android.Widget.ToastLength.Long);
                            Logger.Debug(e.Message + " " + uriOfIncomplete.Path);
                        }
                    }
                    else
                    {
                        try
                        {
                            string realName = string.Empty;
                            if (SettingsActivity.UseIncompleteManualFolder()) //fix due to above^  otherwise "Play File" silently fails
                            {
                                var df = DocumentFile.FromSingleUri(SeekerState.ActiveActivityRef, uriOfIncomplete); //dont use name!!! in my case the name was .m4a but the actual file was .mp3!!
                                realName = df.Name;
                            }

                            uri = DocumentsContract.MoveDocument(SeekerState.ActiveActivityRef.ContentResolver, uriOfIncomplete, parentUriOfIncomplete, folderDir1.Uri); //ADDED IN API 24!!
                            DeleteParentIfEmpty(DocumentFile.FromTreeUri(SeekerState.ActiveActivityRef, parentUriOfIncomplete));
                            //"/tree/primary:musictemp/document/primary:music2/J when two different uri trees the uri returned from move document is a mismash of the two... even tho it actually moves it correctly.
                            //folderDir1.FindFile(name).Uri.Path is right uri and IsFile returns true...
                            if (SettingsActivity.UseIncompleteManualFolder()) //fix due to above^  otherwise "Play File" silently fails
                            {
                                uri = folderDir1.FindFile(realName).Uri; //dont use name!!! in my case the name was .m4a but the actual file was .mp3!!
                            }
                        }
                        catch (Exception e)
                        {
                            //move document fails if two different volumes:
                            //"Failed to move to /storage/1801-090D/Music/Soulseek Complete/folder/song.mp3"
                            //{content://com.android.externalstorage.documents/tree/primary%3A/document/primary%3ASoulseek%20Incomplete%2F/****.mp3}
                            //content://com.android.externalstorage.documents/tree/1801-090D%3AMusic/document/1801-090D%3AMusic%2FSoulseek%20Complete%2F/****}
                            if (e.Message.ToLower().Contains("already exists"))
                            {
                                try
                                {
                                    //set the uri to the existing file...
                                    var df = DocumentFile.FromSingleUri(SeekerState.ActiveActivityRef, uriOfIncomplete);
                                    string realName = df.Name;
                                    uri = folderDir1.FindFile(realName).Uri;

                                    if (folderDir1.Uri == parentUriOfIncomplete)
                                    {
                                        //case where SDCARD was full - all files were 0 bytes, folders could not be created, documenttree.CreateDirectory() returns null.
                                        //no errors until you tried to move it. then you would get "alreay exists" since (if Create Complete and Incomplete folders is checked and
                                        //the incomplete dir isnt changed) then the destination is the same as the incomplete file (since the incomplete and complete folders
                                        //couldnt be created.  This error is misleading though so do a more generic error.
                                        SeekerApplication.Toaster.ShowToast($"Filesystem Error for file {realName}.", Android.Widget.ToastLength.Long);
                                        Logger.Debug("complete and incomplete locations are the same");
                                    }
                                    else
                                    {
                                        SeekerApplication.Toaster.ShowToast(string.Format("File {0} already exists at {1}.  Delete it and try again if you want to overwrite it.", realName, uri.LastPathSegment.ToString()), Android.Widget.ToastLength.Long);
                                    }
                                }
                                catch (Exception e2)
                                {
                                    Logger.Firebase("CRITICAL FILESYSTEM ERROR errorhandling " + e2.Message);
                                }

                            }
                            else
                            {
                                if (uri == null) //this means doc file failed (else it would be after)
                                {
                                    Logger.InfoFirebase("uri==null");
                                    //lets try with the non MoveDocument way.
                                    //this case can happen (for a legitimate reason) if:
                                    //  the user is on api <29.  they start downloading an album.  then while its downloading they set the download directory.  the manual one will be file:\\ but the end location will be content:\\
                                    try
                                    {

                                        DocumentFile mFile = CommonHelpers.CreateMediaFile(folderDir1, name);
                                        uri = mFile.Uri;
                                        finalUri = mFile.Uri.ToString();
                                        Logger.InfoFirebase("retrying: incomplete: " + uriOfIncomplete + " complete: " + finalUri + " parent: " + parentUriOfIncomplete);
                                        //                                        Logger.InfoFirebase("using temp: " +
                                        System.IO.Stream stream = SeekerState.ActiveActivityRef.ContentResolver.OpenOutputStream(mFile.Uri);
                                        MoveFile(SeekerState.ActiveActivityRef.ContentResolver.OpenInputStream(uriOfIncomplete), stream, uriOfIncomplete, parentUriOfIncomplete);
                                    }
                                    catch (Exception secondTryErr)
                                    {
                                        Logger.Firebase("Legacy backup failed - CRITICAL FILESYSTEM ERROR pre" + secondTryErr.Message);
                                        SeekerApplication.Toaster.ShowToast("Error Saving File", Android.Widget.ToastLength.Long);
                                        Logger.Debug(secondTryErr.Message + " " + uriOfIncomplete.Path);
                                    }
                                }
                                else
                                {
                                    Logger.InfoFirebase("uri!=null");
                                    Logger.Firebase("CRITICAL FILESYSTEM ERROR " + e.Message + " path child: " + Android.Net.Uri.Decode(uriOfIncomplete.ToString()) + " path parent: " + Android.Net.Uri.Decode(parentUriOfIncomplete.ToString()) + " path dest: " + Android.Net.Uri.Decode(folderDir1?.Uri?.ToString()));
                                    SeekerApplication.Toaster.ShowToast("Error Saving File", Android.Widget.ToastLength.Long);
                                    Logger.Debug(e.Message + " " + uriOfIncomplete.Path); //Unknown Authority happens when source is file :/// storage/emulated/0/Android/data/com.companyname.andriodapp1/files/Soulseek%20Incomplete/
                                }
                            }
                        }
                        //throws "no static method with name='moveDocument' signature='(Landroid/content/ContentResolver;Landroid/net/Uri;Landroid/net/Uri;Landroid/net/Uri;)Landroid/net/Uri;' in class Landroid/provider/DocumentsContract;"
                    }

                    if (uri == null)
                    {
                        Logger.Firebase("DocumentsContract MoveDocument FAILED, override incomplete: " + PreferencesState.OverrideDefaultIncompleteLocations);
                    }

                    finalUri = uri.ToString();

                    //1220ms for 35mb so 10x slower
                    //DocumentFile mFile = folderDir1.CreateFile(@"audio/mp3", name);
                    //System.IO.Stream stream = SeekerState.ActiveActivityRef.ContentResolver.OpenOutputStream(mFile.Uri);
                    //MoveFile(SeekerState.ActiveActivityRef.ContentResolver.OpenInputStream(uriOfIncomplete), stream, uriOfIncomplete, parentUriOfIncomplete);


                    //stopwatch.Stop();
                    //Logger.Debug("DocumentsContract.MoveDocument took: " + stopwatch.ElapsedMilliseconds);

                }
            }

            return filePath;
        }

        public static void MoveFile(System.IO.Stream from, System.IO.Stream to, Android.Net.Uri toDelete, Android.Net.Uri parentToDelete)
        {
            byte[] buffer = new byte[4096];
            int read;
            while ((read = from.Read(buffer)) != 0) //C# does 0 for you've reached the end!
            {
                to.Write(buffer, 0, read);
            }
            from.Close();
            to.Flush();
            to.Close();

            if (SeekerState.PreOpenDocumentTree() || SettingsActivity.UseTempDirectory() || toDelete.Scheme == "file")
            {
                try
                {
                    if (!(new Java.IO.File(toDelete.Path)).Delete())
                    {
                        Logger.Firebase("Java.IO.File.Delete() failed to delete");
                    }
                }
                catch (Exception e)
                {
                    Logger.Firebase("Java.IO.File.Delete() threw" + e.Message + e.StackTrace);
                }
            }
            else
            {
                DocumentFile df = DocumentFile.FromSingleUri(SeekerState.ActiveActivityRef, toDelete); //this returns a file that doesnt exist with file ://

                if (!df.Delete()) //on API 19 this seems to always fail..
                {
                    Logger.Firebase("df.Delete() failed to delete");
                }
            }

            DocumentFile parent = null;
            if (SeekerState.PreOpenDocumentTree() || SettingsActivity.UseTempDirectory() || parentToDelete.Scheme == "file")
            {
                parent = DocumentFile.FromFile(new Java.IO.File(parentToDelete.Path));
            }
            else
            {
                parent = DocumentFile.FromTreeUri(SeekerState.ActiveActivityRef, parentToDelete); //if from single uri then listing files will give unsupported operation exception...  //if temp (file: //)this will throw (which makes sense as it did not come from open tree uri)
            }
            DeleteParentIfEmpty(parent);
        }

        public static void DeleteParentIfEmpty(DocumentFile parent)
        {
            if (parent == null)
            {
                Logger.Firebase("null parent");
                return;
            }
            //Logger.Debug(parent.Name + "p name");
            //Logger.Debug(parent.ListFiles().ToString());
            //Logger.Debug(parent.ListFiles().Length.ToString());
            //foreach (var f in parent.ListFiles())
            //{
            //    Logger.Debug("child: " + f.Name);
            //}
            try
            {
                if (parent.ListFiles().Length == 1 && parent.ListFiles()[0].Name == ".nomedia")
                {
                    if (!parent.ListFiles()[0].Delete())
                    {
                        Logger.Firebase("parent.Delete() failed to delete .nomedia child...");
                    }
                    if (!parent.Delete())
                    {
                        Logger.Firebase("parent.Delete() failed to delete parent");
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Index was outside"))
                {
                    //race condition between checking length of ListFiles() and indexing [0] (twice)
                }
                else
                {
                    throw ex; //this might be important..
                }
            }
        }


        public static void DeleteParentIfEmpty(Java.IO.File parent)
        {
            if (parent.ListFiles().Length == 1 && parent.ListFiles()[0].Name == ".nomedia")
            {
                if (!parent.ListFiles()[0].Delete())
                {
                    Logger.Firebase("LEGACY parent.Delete() failed to delete .nomedia child...");
                }
                if (!parent.Delete()) //this returns false... maybe delete .nomedia child??? YUP.  cannot delete non empty dir...
                {
                    Logger.Firebase("LEGACY parent.Delete() failed to delete parent");
                }
            }
        }


        public static void MoveFile(Java.IO.FileInputStream from, Java.IO.FileOutputStream to, Java.IO.File toDelete, Java.IO.File parent)
        {
            byte[] buffer = new byte[4096];
            int read;
            while ((read = from.Read(buffer)) != -1) //unlike C# this method does -1 for no more bytes left..
            {
                to.Write(buffer, 0, read);
            }
            from.Close();
            to.Flush();
            to.Close();
            if (!toDelete.Delete())
            {
                Logger.Firebase("LEGACY df.Delete() failed to delete ()");
            }
            DeleteParentIfEmpty(parent);
            //Logger.Debug(toDelete.ParentFile.Name + ":" + toDelete.ParentFile.ListFiles().Length.ToString());
        }

        internal static void SaveFileToMediaStore(string path)
        {
            //ContentValues contentValues = new ContentValues();
            //contentValues.Put(MediaStore.MediaColumns.DateAdded, Helpers.GetDateTimeNowSafe().Ticks / TimeSpan.TicksPerMillisecond);
            //contentValues.Put(MediaStore.Audio.AudioColumns.Data,path);
            //ContentResolver.Insert(MediaStore.Audio.Media.ExternalContentUri, contentValues);from


            Intent mediaScanIntent = new Intent(Intent.ActionMediaScannerScanFile);
            Java.IO.File f = new Java.IO.File(path);
            Android.Net.Uri contentUri = Android.Net.Uri.FromFile(f);
            mediaScanIntent.SetData(contentUri);
            SeekerState.ActiveActivityRef.ApplicationContext.SendBroadcast(mediaScanIntent);
        }

        private static void CreateNoMediaFileLegacy(string atDirectory)
        {
            Java.IO.File noMediaRootFile = new Java.IO.File(atDirectory + @"/.nomedia");
            if (!noMediaRootFile.Exists())
            {
                noMediaRootFile.CreateNewFile();
            }
        }

        private static void CreateNoMediaFile(DocumentFile atDirectory)
        {
            atDirectory.CreateFile("nomedia/customnomedia", ".nomedia");
        }
    }
}
