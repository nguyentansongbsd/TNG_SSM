using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Action_SAP_CreateUnit.Models
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class SaleOrg2
    {
        public string ktgrm { get; set; }
        public string mvgr1 { get; set; }
        public string mvgr4 { get; set; }
        public string mvgr5 { get; set; }
        public string mtpos_mara { get; set; }
        public string mtpos { get; set; }
    }
}
