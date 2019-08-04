using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SongDownloadManager
{
    public class SongDownload
    {
        public string Key { get; set; }
        public string Hash { get; set; }
        public string Name { get; set; }
        public string Mapper { get; set; }
        public DirectoryInfo LocalDirectory { get; set; }
    }
}
