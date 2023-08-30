using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Action_SAP_CreateUnit.Models
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class BaseData
    {
        public string matnr { get; set; }
        public string maktx { get; set; }
        public string bismt { get; set; }
        public string spart { get; set; }
        public string meins { get; set; }
        public string matkl { get; set; }
        public decimal brgew { get; set; }
        public decimal ntgew { get; set; }
        public string gewei { get; set; }
        public string labor { get; set; }
        public decimal brgew2 { get; set; }
    }
}
