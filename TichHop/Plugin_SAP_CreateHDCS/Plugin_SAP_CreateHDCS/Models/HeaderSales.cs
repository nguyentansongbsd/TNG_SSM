using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin_SAP_CreateHDCS.Models
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class HeaderSales
    {
        public string vkorg { get; set; }
        public string vtweg { get; set; }
        public string spart { get; set; }
        public string KETDAT { get; set; }
        public string pltyp { get; set; }
        public string audat { get; set; }
        public string prsdt { get; set; }
        public string vbegdat { get; set; }
        public string venddat { get; set; }
        public string augru { get; set; }
        public string abrvw { get; set; }
        public string gwldt { get; set; }
    }
}
