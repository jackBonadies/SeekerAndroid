using Seeker.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Seeker
{
    public static class ImportHelper
    {
        /// <summary>
        /// Entry point
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static ImportedData ImportFile(string fileName, System.IO.Stream stream)
        {
            ImportType importType = ImportType.Unknown;
            if (System.IO.Path.GetExtension(fileName) == ".scd1" || System.IO.Path.GetExtension(fileName) == ".dat")
            {
                importType = ImportType.SoulseekQT;
            }
            else if (System.IO.Path.GetExtension(fileName) == ".bz2")
            {
                importType = ImportType.NicotineTarBz2;
            }
            else// if (string.IsNullOrEmpty(System.IO.Path.GetExtension(fileName)) || System.IO.Path.GetExtension(fileName) == ".txt" || System.IO.Path.GetExtension(fileName) == ".xml")
            {
                importType = DetermineImportTypeByFirstLine(stream);
            }

            //if import type still unknown then assume QT
            ImportedData? data = null;
            switch (importType)
            {
                case ImportType.SoulseekQT:
                case ImportType.Unknown:
                    data = ParseSoulseekQTData(stream);
                    break;
                case ImportType.NicotineTarBz2:
                    //unzip
                    Bzip2.BZip2InputStream zippedStream = new Bzip2.BZip2InputStream(stream, false);
                    MemoryStream memStream = new MemoryStream();
                    zippedStream.CopyTo(memStream);
                    memStream.Seek(0, SeekOrigin.Begin);
                    //seek past tar header
                    SkipTar(memStream);
                    //parse actual config file
                    data = ParseNicotine(memStream);
                    break;
                case ImportType.Nicotine:
                    data = ParseNicotine(stream);
                    break;
                case ImportType.Seeker:
                    data = ParseSeeker(stream);
                    break;
            }
            return data.Value;
        }



        public static ImportedData ParseSoulseekQTData(System.IO.Stream stream)
        {
            byte[] fourBytes = new byte[4];
            stream.Read(fourBytes, 0, 4);
            int numberOfTables = BitConverter.ToInt32(fourBytes);

            //here is our file check. basically, we cant just "try our best" since that will likely lead to memory allocations of GB causing crashes
            //(for example string length, for an invalid file, will just be any value from 0 to 4GB, leading to Java out of memory and process termination,
            //rather than a simple exception or even just an activity crash).
            //the simple check is, if the number of tables is 0 or greater than 10k throw.  
            //its very very very lenient since I think this number will in reality be very close to 47 +- 5 say.
            if (numberOfTables > 10000 || numberOfTables <= 0)
            {
                throw new Exception("The QT File does not seem to be valid.  Number of tables is: " + numberOfTables);
            }

            if (!BitConverter.IsLittleEndian)
            {
                throw new Exception("Big Endian");
            }
            List<string> tablesOfInterest =
                new List<string>() { 
                    //definitely: in_user_list, user (parsing helper - has the (key, len, username) tuples for every single user), user_note, is_ignored + unshared (both are combined in seeker)
                    //potentially: user_online_alert, wish_list_item
                    "in_user_list",
                    "user_note",
                    "user",
                    "is_ignored",
                    "unshared",
                    "user_online_alert",
                    "wish_list_item",
                };


            List<Tuple<int, byte[]>> user_list_table = null;
            List<Tuple<int, string>> user_note_table = null;
            Dictionary<int, string> user_table = null;
            List<Tuple<int, byte[]>> is_ignored_table = null;
            List<Tuple<int, byte[]>> unshared_table = null;
            List<Tuple<int, byte[]>> user_online_alert_table = null;
            List<Tuple<int, string>> wish_list_item_table = null;


            while (numberOfTables > 0)
            {
                stream.Read(fourBytes, 0, 4);

                int tableNameLength = BitConverter.ToInt32(fourBytes);
                byte[] tableNameBytes = new byte[tableNameLength];
                stream.Read(tableNameBytes, 0, tableNameBytes.Length);


                string tableName = System.Text.Encoding.UTF8.GetString(tableNameBytes); //ascii works fine, but just in case.

                System.Console.WriteLine(tableName);

                stream.Read(fourBytes, 0, 4);
                int itemsInTable = BitConverter.ToInt32(fourBytes);
                //if not a table of interest, read through it
                if (tablesOfInterest.Contains(tableName))
                {
                    switch (tableName)
                    {
                        case "in_user_list":
                            user_list_table = GetTableAsBytes(stream, itemsInTable);
                            //my linux box got results 1 byte 49, 50, 51
                            //also some of these are not users they are user_groups, but we can just skip those..
                            break;
                        case "user_note":
                            user_note_table = GetTableAsString(stream, itemsInTable);
                            break;
                        case "user":
                            user_table = GetTableAsDictString(stream, itemsInTable);
                            break;
                        case "is_ignored":
                            is_ignored_table = GetTableAsBytes(stream, itemsInTable);
                            break;
                        case "unshared":
                            unshared_table = GetTableAsBytes(stream, itemsInTable);
                            break;
                        case "user_online_alert":
                            user_online_alert_table = GetTableAsBytes(stream, itemsInTable);
                            break;
                        case "wish_list_item":
                            wish_list_item_table = GetTableAsString(stream, itemsInTable);
                            break;
                    }
                }
                else
                {
                    //lets just speed through here to get to the tables we care about...
                    SkipTable(stream, itemsInTable);
                }
                numberOfTables--;
            }

            //now read the final table
            stream.Read(fourBytes, 0, 4);
            int numOfMappings = BitConverter.ToInt32(fourBytes);

            if (numOfMappings * 8 != stream.Length - stream.Position) //these 2*4 byte entries take us to the very end of the stream.
            {
                throw new Exception("Unexpected size");
            }
            Dictionary<int, List<int>> mappingTable = new Dictionary<int, List<int>>();
            Dictionary<int, List<int>> reverseMappingTable = new Dictionary<int, List<int>>();
            while (numOfMappings > 0)
            {
                stream.Read(fourBytes, 0, 4);
                int keyA = BitConverter.ToInt32(fourBytes);
                stream.Read(fourBytes, 0, 4);
                int keyB = BitConverter.ToInt32(fourBytes);

                if (mappingTable.ContainsKey(keyA))
                {
                    mappingTable[keyA].Add(keyB);
                }
                else
                {
                    mappingTable[keyA] = new List<int> { keyB };
                }
                if (reverseMappingTable.ContainsKey(keyB))
                {
                    reverseMappingTable[keyB].Add(keyA);
                }
                else
                {
                    reverseMappingTable[keyB] = new List<int> { keyA };
                }
                numOfMappings--;

            }

            //now time to resolve our IDs
            List<string> user_list = new List<string>();
            foreach (var user in user_list_table) //fix this, we dont care about byte[] in this case..
            {
                if (mappingTable.ContainsKey(user.Item1))
                {
                    foreach (int key in mappingTable[user.Item1])
                    {
                        if (user_table.ContainsKey(key))
                        {
                            user_list.Add(user_table[key]);
                            break;
                        }
                    }
                }
            }

            List<string> ignored_unshared_list = new List<string>();
            //the ignored key is an index into the mapping table for the user keys
            if (is_ignored_table.Count > 0) //if not ignoring anyone this will be empty...
            {
                if (mappingTable.ContainsKey(is_ignored_table[0].Item1))
                {
                    foreach (int key in mappingTable[is_ignored_table[0].Item1])
                    {
                        if (user_table.ContainsKey(key))
                        {
                            ignored_unshared_list.Add(user_table[key]);
                        }
                    }
                }
            }
            //for unshared you need to do a reverse lookup..... 
            //if you are unsharing from 3 people then it will be a resulting value for three people.
            if (unshared_table.Count > 0)
            {
                if (reverseMappingTable.ContainsKey(unshared_table[0].Item1))
                {
                    foreach (int key in reverseMappingTable[unshared_table[0].Item1])
                    {
                        if (user_table.ContainsKey(key))
                        {
                            string user = user_table[key];
                            //a lot of these are probably duplicate with ignored.
                            if (!ignored_unshared_list.Contains(user))
                            {
                                ignored_unshared_list.Add(user_table[key]);
                            }
                        }
                    }
                }
            }

            //now time to resolve our IDs
            List<Tuple<string, string>> user_notes = new List<Tuple<string, string>>();
            foreach (var user in user_note_table) //fix this, we dont care about byte[] in this case..
            {
                if (mappingTable.ContainsKey(user.Item1))
                {
                    foreach (int key in mappingTable[user.Item1])
                    {
                        if (user_table.ContainsKey(key))
                        {
                            user_notes.Add(new Tuple<string, string>(user_table[key], user.Item2));
                            break;
                        }
                    }
                }
            }
            return new ImportedData(user_list, ignored_unshared_list, wish_list_item_table.Select(item => item.Item2).ToList(), user_notes);
        }

        private static void SkipTable(System.IO.Stream stream, int itemsInTable)
        {
            byte[] fourBytes = new byte[4];
            for (int i = 0; i < itemsInTable; i++)
            {
                //item's key (skip)
                //stream.Read(fourBytes, 0, 4);
                stream.Seek(4, System.IO.SeekOrigin.Current);
                //item's length in bytes (read so we know how much to skip)
                stream.Read(fourBytes, 0, 4);
                int itemLen = BitConverter.ToInt32(fourBytes);
                //just skip the item
                stream.Seek(itemLen, System.IO.SeekOrigin.Current);
            }
        }

        private static List<Tuple<int, string>> GetTableAsString(System.IO.Stream stream, int itemsInTable)
        {
            List<Tuple<int, string>> items = new List<Tuple<int, string>>();
            byte[] fourBytes = new byte[4];
            for (int i = 0; i < itemsInTable; i++)
            {
                //item's key
                stream.Read(fourBytes, 0, 4);
                int key = BitConverter.ToInt32(fourBytes);
                //item's length in bytes (read so we know how much to skip)
                stream.Read(fourBytes, 0, 4);
                int itemLen = BitConverter.ToInt32(fourBytes);
                //just skip the item
                byte[] itemBytes = new byte[itemLen];
                stream.Read(itemBytes, 0, itemLen);
                //the first 128 chars in utf8 and ascii are the same. so all valid ascii text is valid utf8 text.
                string itemValue = System.Text.Encoding.UTF8.GetString(itemBytes);
                items.Add(new Tuple<int, string>(key, itemValue));
            }
            return items;
        }

        /// <summary>
        /// TODO: if the item data doesnt actually matter then we can optimize by skipping it...
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="itemsInTable"></param>
        /// <returns></returns>
        private static List<Tuple<int, byte[]>> GetTableAsBytes(System.IO.Stream stream, int itemsInTable)
        {
            List<Tuple<int, byte[]>> items = new List<Tuple<int, byte[]>>();
            byte[] fourBytes = new byte[4];
            for (int i = 0; i < itemsInTable; i++)
            {
                //item's key
                stream.Read(fourBytes, 0, 4);
                int key = BitConverter.ToInt32(fourBytes);
                //item's length in bytes (read so we know how much to skip)
                stream.Read(fourBytes, 0, 4);
                int itemLen = BitConverter.ToInt32(fourBytes);
                //just skip the item
                byte[] itemBytes = new byte[itemLen];
                stream.Read(itemBytes, 0, itemLen);
                items.Add(new Tuple<int, byte[]>(key, itemBytes));
            }
            return items;
        }

        private static Dictionary<int, string> GetTableAsDictString(System.IO.Stream stream, int itemsInTable)
        {
            Dictionary<int, string> items = new Dictionary<int, string>();
            byte[] fourBytes = new byte[4];
            for (int i = 0; i < itemsInTable; i++)
            {
                //item's key
                stream.Read(fourBytes, 0, 4);
                int key = BitConverter.ToInt32(fourBytes);
                //item's length in bytes (read so we know how much to skip)
                stream.Read(fourBytes, 0, 4);
                int itemLen = BitConverter.ToInt32(fourBytes);
                //just skip the item
                byte[] itemBytes = new byte[itemLen];
                stream.Read(itemBytes, 0, itemLen);
                string itemValue = System.Text.Encoding.UTF8.GetString(itemBytes);
#if DEBUG
                if (items.ContainsKey(key))
                {
                    throw new Exception("unexpected");
                }
#endif
                items[key] = itemValue;
            }
            return items;
        }

        public static void SkipTar(System.IO.Stream stream)
        {
            byte[] paxHeader = new byte[14];
            stream.Read(paxHeader, 0, 14);
            bool diagContainsPaxHeader = false;
            if (Encoding.ASCII.GetString(paxHeader, 0, 14) == "././@PaxHeader")
            {
                //skip 1024 byte pax header that was present on both windows and linux. (python version 3.9 tarfile.DEFAULT_FORMAT==2==PAX)
                stream.Seek(1024, SeekOrigin.Begin);
            }
            else
            {
                //depending on python version there may not be a pax header... (version 2.7 to 3.7 tarfile.DEFAULT_FORMAT==1==gnu)
                stream.Seek(0, SeekOrigin.Begin);
            }
            var buffer = new byte[100];
            stream.Read(buffer, 0, 100);
            var name = Encoding.ASCII.GetString(buffer).Trim('\0');
            if (String.IsNullOrWhiteSpace(name))
            {
                throw new Exception("SkipTar - null or whitespace");
            }
            stream.Seek(24, SeekOrigin.Current);
            stream.Read(buffer, 0, 12);

            stream.Seek(376L, SeekOrigin.Current);
        }

        /// <summary>
        /// This function is only used for nicotine parsing. not QT.
        /// </summary>
        /// <param name="currentLine"></param>
        /// <param name="restOfLine"></param>
        /// <param name="stringObtained"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static bool GetNextString(string currentLine, out string restOfLine, out string stringObtained)
        {
            //nicotine uses the built in python str() ex. str(["x","y"]), which has some things to watch out for.
            //normal strings -> 'xyz'
            //string containing ' -> "xy'x"

            //["xyz", r"xyz'x", r"xyz\"x", r"xyz'yf\"fds'xf\""]
            //gets serialized to ['xyz', "xyz'x", 'xyz\\"x', 'xyz\'yf\\"fds\'xf\\"']
            bool doubleQuotesUsedInsteadOfSingle = false;
            if (!currentLine.StartsWith('\''))
            {
                if (!currentLine.StartsWith('"'))
                {
                    throw new Exception("doesnt start with \" or '");
                }
                else
                {
                    doubleQuotesUsedInsteadOfSingle = true;
                }
            }
            char termSeparator = doubleQuotesUsedInsteadOfSingle ? '"' : '\'';
            //get index of first non-escaped '
            int index = 0;
            while (true)
            {
                if (!IsEscaped(currentLine, index) && currentLine[index + 1] == termSeparator)
                {
                    break;
                }
                index++;
            }
            restOfLine = currentLine.Substring(index + 2);
            stringObtained = currentLine.Substring(1, index);
            stringObtained = stringObtained.Replace("\\\'", "\'").Replace("\\\\", "\\"); //replace \' with ' and two \\ with one.
            return true;
        }

        public static bool IsEscaped(string currentLine, int index)
        {
            //check if odd number of '\\' before '\''
            //otherwise it is just an escaped slash.
            int count = 0;
            while (true)
            {
                if (currentLine[index] != '\\')
                {
                    return count % 2 == 1;
                }
                index--;
                count++;
            }
        }

        public static void SkipNextValue(string currentLine, out string restOfLine)
        {
            //go to next ','
            restOfLine = currentLine.Substring(currentLine.IndexOf(','));
        }

        public static List<string> GetListOfString(string line)
        {
            List<string> listOfStrings = new List<string>();
            int keySep = line.IndexOf(" = ");
            string valuePortion = line.Substring(keySep + 3);
            if (valuePortion.Length <= 2) // []
            {
                return new List<string>();
            }
            //discard [
            valuePortion = valuePortion.Substring(1);
            while (GetNextString(valuePortion, out string restOfString, out string stringObtained))
            {
                valuePortion = restOfString;
                listOfStrings.Add(stringObtained);
                if (!valuePortion.Contains(", "))
                {
                    //no more items
                    return listOfStrings;
                }
                else
                {
                    valuePortion = valuePortion.Substring(2);
                }
            }
            return listOfStrings;
        }

        public static List<string> GetListOfStringFromDictValues(string line)
        {
            List<string> listOfStrings = new List<string>();
            int keySep = line.IndexOf(" = ");
            string valuePortion = line.Substring(keySep + 3);
            if (valuePortion.Length <= 2) // {}
            {
                return new List<string>();
            }
            //discard {
            valuePortion = valuePortion.Substring(1);
            while (true)
            {
                //the key
                GetNextString(valuePortion, out string restOfString, out string _);
                restOfString = restOfString.Substring(2); //skip ": "
                GetNextString(restOfString, out restOfString, out string stringObtained);
                valuePortion = restOfString;
                listOfStrings.Add(stringObtained);
                if (!valuePortion.Contains(", "))
                {
                    //no more items
                    return listOfStrings;
                }
                else
                {
                    valuePortion = valuePortion.Substring(2);
                }
            }
            return listOfStrings;
        }

        public static List<string> ParseUserList(string line, out List<Tuple<string, string>> listOfNotes)
        {
            //this is a list of lists
            List<string> listOfUsernames = new List<string>();
            listOfNotes = new List<Tuple<string, string>>();
            int keySep = line.IndexOf(" = ");
            string valuePortion = line.Substring(keySep + 3);
            if (valuePortion.Length <= 2) // {}
            {
                return new List<string>();
            }
            //discard out [
            valuePortion = valuePortion.Substring(1);
            while (true)
            {
                //discard inner [
                valuePortion = valuePortion.Substring(1);
                //this is username
                GetNextString(valuePortion, out string restOfString, out string username);
                listOfUsernames.Add(username);
                restOfString = restOfString.Substring(2); //skip ", "
                GetNextString(restOfString, out restOfString, out string note);
                if (note != string.Empty)
                {
                    listOfNotes.Add(new Tuple<string, string>(username, note));
                }
                //this is for simple values i.e. False
                restOfString = restOfString.Substring(2);
                SkipNextValue(restOfString, out restOfString);
                restOfString = restOfString.Substring(2);
                SkipNextValue(restOfString, out restOfString);
                restOfString = restOfString.Substring(2);
                SkipNextValue(restOfString, out restOfString);
                restOfString = restOfString.Substring(2);
                //time
                GetNextString(restOfString, out restOfString, out string _);
                restOfString = restOfString.Substring(2);
                //flag
                GetNextString(restOfString, out restOfString, out string _);
                valuePortion = restOfString;
                if (!valuePortion.Contains("], "))
                {
                    //no more items
                    return listOfUsernames;
                }
                else
                {
                    valuePortion = valuePortion.Substring(3);
                }
            }
            return listOfUsernames;
        }

        public class NicotineParsingException : System.Exception
        {
            public System.Exception InnerException;
            public string MessageToToast;
            public NicotineParsingException(System.Exception ex, string msgToToast)
            {
                InnerException = ex;
                MessageToToast = msgToToast;
            }
        }

        public static ImportedData ParseSeeker(System.IO.Stream stream)
        {
            var data = new XmlSerializer(typeof(SeekerImportExportData)).Deserialize(stream) as SeekerImportExportData;
            List<Tuple<string, string>> userNotes = new List<Tuple<string, string>>();
            foreach (KeyValueEl keyValueEl in data.UserNotes)
            {
                userNotes.Add(new Tuple<string, string>(keyValueEl.Key, keyValueEl.Value));
            }
            var importData = new ImportedData(data.Userlist, data.BanIgnoreList, data.Wishlist, userNotes);
            return importData;
        }

        /// <summary>
        /// Note: there is an older config file version that has userlist in a section 
        /// called columns that can mess things up if we dont consider the section...
        /// </summary>
        public const string sectionOfInterest = "[server]";
        public static ImportedData ParseNicotine(System.IO.Stream stream)
        {
            List<string> userList = new List<string>();
            List<string> bannedIgnoredList = new List<string>();
            List<string> wishlists = new List<string>();
            List<Tuple<string, string>> notes = new List<Tuple<string, string>>();
            string currentSection = string.Empty;
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {

                    //possible key
                    if (line.Contains(" = "))
                    {
                        if (currentSection != sectionOfInterest)
                        {
                            continue;
                        }
                        int keySep = line.IndexOf(" = ");
                        string keyname = line.Substring(0, keySep);
                        switch (keyname)
                        {
                            //all lists with = [] as default.
                            case "userlist":
                                //this is also the only place where notes appear
                                try
                                {
                                    userList = ParseUserList(line, out notes);
                                }
                                catch (Exception ex)
                                {
                                    throw new NicotineParsingException(ex, "Error reading UserList");
                                }
                                break;
                            case "banlist":
                                try
                                {
                                    bannedIgnoredList.AddRange(GetListOfString(line));
                                }
                                catch (Exception ex)
                                {
                                    throw new NicotineParsingException(ex, "Error reading BanList");
                                }
                                break;
                            case "ignorelist":
                                try
                                {
                                    bannedIgnoredList.AddRange(GetListOfString(line));
                                }
                                catch (Exception ex)
                                {
                                    throw new NicotineParsingException(ex, "Error reading IgnoreList");
                                }
                                break;
                            case "ipignorelist": //these are dicts and they are not redundant with the other above 2 lists.
                                try
                                {
                                    bannedIgnoredList.AddRange(GetListOfStringFromDictValues(line));
                                }
                                catch (Exception ex)
                                {
                                    throw new NicotineParsingException(ex, "Error reading IpIgnoreList");
                                }
                                break;
                            case "ipblocklist":  //ipblocklist = {'x.x.x.x': 'name', 'y.y.y.y': 'name'}
                                try
                                {
                                    bannedIgnoredList.AddRange(GetListOfStringFromDictValues(line));
                                }
                                catch (Exception ex)
                                {
                                    throw new NicotineParsingException(ex, "Error reading IpBlockList");
                                }
                                break;
                            case "autosearch":
                                try
                                {
                                    wishlists = GetListOfString(line);
                                }
                                catch (Exception ex)
                                {
                                    throw new NicotineParsingException(ex, "Error reading Wishlist");
                                }
                                break;
                            default:
                                continue;
                        }
                    }
                    else
                    {
                        if (line.StartsWith("[") && line.EndsWith("]"))
                        {
                            currentSection = line;
                        }
                    }


                }
            }
            return new ImportedData(userList, bannedIgnoredList.Distinct().ToList(), wishlists, notes);
        }

        private static ImportType DetermineImportTypeByFirstLine(Stream stream)
        {
            System.IO.StreamReader fStream = new System.IO.StreamReader(stream);
            string firstLine = fStream.ReadLine();
            stream.Seek(0, SeekOrigin.Begin);
            if (firstLine.StartsWith("<?xml"))
            {
                return ImportType.Seeker;
            }
            else if (firstLine.StartsWith("["))
            {
                return ImportType.Nicotine;
            }
            else
            {
                Logger.Debug("Unsure of filetype.  Firstline = " + firstLine);
                return ImportType.Nicotine;
            }
        }

    }
    public enum ImportType : int
    {
        Unknown = -1,
        SoulseekQT = 0,
        NicotineTarBz2 = 1,
        Nicotine = 2,
        Seeker = 3

    }
}
