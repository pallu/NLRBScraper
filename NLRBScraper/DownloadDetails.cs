using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NLRBScraper
{
    public class DownloadDetails
        {
        //public DownloadDetailsDatum[] data { get; set; }
        public DownloadDetailsDatum data { get; set; } = new();
        //public int current_page { get; set; }
        //public int last_page { get; set; }
        public string method { get; set; }
        }

        public class DownloadDetailsDatum
        {
            public int id { get; set; }
            public string name { get; set; }
            public int total { get; set; }
            public int processed { get; set; }
            public int finished { get; set; }
            public int created { get; set; }
            public Single progress { get; set; }
            public string sessid { get; set; }
            public string filename { get; set; }
            public string started { get; set; }
        }

    /////////////
    
    
}
