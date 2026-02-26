using System;
using System.Collections.Generic;

namespace Seeker
{
    public struct ImportedData
    {
        public ImportedData(List<string> userList, List<string> ignoreBanned, List<string> wishlist, List<Tuple<string, string>> userNotes)
        {
            UserList = userList; IgnoredBanned = ignoreBanned; Wishlist = wishlist; UserNotes = userNotes;
        }

        public List<string> Wishlist { private set; get; }
        public List<string> UserList { private set; get; }
        public List<string> IgnoredBanned { private set; get; }
        public List<Tuple<string, string>> UserNotes { private set; get; }
    }

    /// <summary>
    /// For friendliness with XmlSerializer
    /// </summary>
    [Serializable]
    public class SeekerImportExportData
    {
        public List<string> Wishlist;
        public List<string> Userlist;
        public List<string> BanIgnoreList;
        public List<KeyValueEl> UserNotes;
        //public string AddedAfterTheFact; //XmlSerializer IS backward compatible.
        //You can add extra fields (such as messages) to SeekerImportExportData without worry.
        //They will just have the default value (empty string in this case).
    }

    /// <summary>
    /// Since Xml Serializer does not do dictionaries.
    /// </summary>
    [Serializable]
    public class KeyValueEl
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }
}
