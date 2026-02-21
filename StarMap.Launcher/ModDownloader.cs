using StarMap.Types.Proto.IPC;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarMapLoader
{
    internal class ModDownloader
    {
        public bool DownloadMod(IPCMod mod, IPCModVersion version, string location)
        {
            try
            {
                return true;
            }
            catch { return false; }
        }
    }
}
