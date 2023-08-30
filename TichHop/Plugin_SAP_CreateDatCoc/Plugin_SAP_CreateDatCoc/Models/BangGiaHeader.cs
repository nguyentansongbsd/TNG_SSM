using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin_SAP_CreateDatCoc.Models
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class BangGiaHeader
    {
        public string id_banggia { get; set; }
        public string ten_banggia { get; set; }
        public decimal giatri_sp { get; set; }
        public decimal giatri_ck { get; set; }
        public decimal giatri_sp_sau_ck { get; set; }
        public decimal netwr { get; set; }
        public string lp_truoc_ba { get; set; }
        public string lp_chuyen_nhuong { get; set; }
        public string lp_bien_dong { get; set; }
        public string phi_thanh_ly { get; set; }
    }
}
