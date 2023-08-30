using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Action_SAP_CreateUnit.Models
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class ThamSo
    {
        public string bukrs { get; set; }
        public string werks { get; set; }
        public string vkorg { get; set; }
        public string vtweg { get; set; }
    }
}
