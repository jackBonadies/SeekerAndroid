using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seeker.Transfers
{
    [Flags]
    public enum TransferItemExtras : byte
    {
        /// <summary>
        /// Do not create a subfolder.
        /// Used when single download and option "Do not create subfolder
        /// for single items" is checked.
        /// </summary>
        NoSubfolder = 1,
    }
}
