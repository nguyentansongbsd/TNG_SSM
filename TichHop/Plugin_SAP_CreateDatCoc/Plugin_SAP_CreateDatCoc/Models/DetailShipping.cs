using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin_SAP_CreateDatCoc.Models
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class DetailShipping
    {
        public double ntgew { get; set; }
        public decimal brgew { get; set; }
        public decimal brgew2 { get; set; }
        public string werks { get; set; }
        public string vstel { get; set; }
    }
}
