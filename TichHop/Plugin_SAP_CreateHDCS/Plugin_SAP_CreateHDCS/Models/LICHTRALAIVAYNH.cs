using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin_SAP_CreateHDCS.Models
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class LICHTRALAIVAYNH
    {
        public string id_hso { get; set; }
        public string SO_DOT { get; set; }
        public string NGAY_THANH_TOAN { get; set; }
        public double TIEN_LAI { get; set; }
        public string GHI_CHU { get; set; }
    }
}
