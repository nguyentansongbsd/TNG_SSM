using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Action_SAP_CreateUnit.Models
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class SaleOrg1
    {
        public string vmsta { get; set; }
        public string vmstd { get; set; }
        public string taxkm { get; set; }

    }
}
