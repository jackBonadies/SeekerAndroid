using Soulseek;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Common
{
    public class Helpers
    {
        /// <summary>
        /// Replaces d.Name.Contains(prevDirName) which fails for Mu, Music
        /// </summary>
        /// <param name="possibleChild"></param>
        /// <param name="possibleParent"></param>
        /// <returns></returns>
        public static bool IsChildDirString(string possibleChild, string possibleParent, bool rootCase)
        {
            if (rootCase)
            {
                if (possibleChild.LastIndexOf("\\") == -1 && possibleParent.LastIndexOf("\\") == -1)
                {
                    if (possibleParent.IndexOf(':') == (possibleParent.Length - 1)) //i.e. primary:
                    {
                        return possibleChild.Contains(possibleParent);
                    }
                    else if (possibleChild.Equals(possibleParent))
                    {
                        return true; //else the primary:music case fails.
                    }
                }
            }
            int pathSep = possibleChild.LastIndexOf("\\");
            if (pathSep == -1)
            {
                return false;
            }
            else
            {
                //fails in possibleChild="Music (1)\\test" possibleParent="Music" case
                //return possibleChild.Substring(0, pathSep).Contains(possibleParent);

                return possibleChild.Substring(0, pathSep + 1).StartsWith(possibleParent + "\\") || possibleChild.Substring(0, pathSep) == possibleParent || possibleParent == String.Empty;
            }
        }

        public static string GetFullPathFromFile(string fullFilename)
        {
            var lastIndex = fullFilename.LastIndexOf('\\');
            return fullFilename.Substring(0, lastIndex);
        }

        public static string GetFolderNameFromFile(string filename, int levels = 1)
        {
            try
            {
                int folderCount = 0;
                int index = -1; //-1 is important.  i.e. in the case of Folder\test.mp3, it can be Folder.
                int firstIndex = int.MaxValue;
                for (int i = filename.Length - 1; i >= 0; i--)
                {
                    if (filename[i] == '\\')
                    {
                        folderCount++;
                        if (firstIndex == int.MaxValue)
                        {
                            //strip off the file name
                            firstIndex = i;
                        }
                        if (folderCount == (levels + 1))
                        {
                            index = i;
                            break;
                        }
                    }
                }
                return filename.Substring(index + 1, firstIndex - index - 1);
            }
            catch
            {
                return "";
            }
        }

        public static string GetParentFolderNameFromFile(string filename)
        {
            try
            {
                string parent = filename.Substring(0, filename.LastIndexOf('\\'));
                parent = parent.Substring(0, parent.LastIndexOf('\\'));
                parent = parent.Substring(parent.LastIndexOf('\\') + 1);
                return parent;
            }
            catch
            {
                return "";
            }
        }
    }

}
