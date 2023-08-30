using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin_SAP_CreateHDMB.Models
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class HeaderInfoAdd
    {
        public int zpbt { get; set; }
        public decimal zpdvql { get; set; }
        public string znpp { get; set; }
        public string znkcn { get; set; }
        public string zkhcn { get; set; }
        public string zkhncn { get; set; }
        public string zlcn { get; set; }
        public string znccvbcn { get; set; }
        public string zscnvbcn { get; set; }
        public string zncnvbcn { get; set; }
        public string zdcccvbcn { get; set; }
        public string ZSQCCVBCN { get; set; }
        public string znghsptda { get; set; }
        public string znghscqnn { get; set; }
    }
}
