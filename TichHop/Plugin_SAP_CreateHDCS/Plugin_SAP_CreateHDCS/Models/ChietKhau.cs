using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin_SAP_CreateHDCS.Models
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class ChietKhau
    {
        public string id_ck { get; set; }
        public string ten_ck { get; set; }
        public string phuongthuc_ck { get; set; }
        public decimal sotien_ck { get; set; }
        public decimal tyle_ck { get; set; }
    }
}
