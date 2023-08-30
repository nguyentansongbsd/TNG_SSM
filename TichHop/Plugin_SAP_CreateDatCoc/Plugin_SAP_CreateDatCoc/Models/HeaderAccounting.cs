using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin_SAP_CreateDatCoc.Models
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class HeaderAccounting
    {
        public string zuonr { get; set; }
        public string xblnr { get; set; }
        public string xblnr_rf { get; set; }
    }
}
