using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YtDownloader
{
    public class FileInfo
    {
        private string topic;
        private string name;
        private string url;
        public string Topic { get => topic; set => topic = value; }
        public string Name { get => name; set => name = value; }
        public string Url { get => url; set => url = value; }
    }
}
