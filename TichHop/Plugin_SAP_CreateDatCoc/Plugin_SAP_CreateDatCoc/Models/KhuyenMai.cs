using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin_SAP_CreateDatCoc.Models
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class KhuyenMai
    {
        public string id_km { get; set; }
        public string ten_km { get; set; }
        public decimal giatri_km { get; set; }
    }
}
