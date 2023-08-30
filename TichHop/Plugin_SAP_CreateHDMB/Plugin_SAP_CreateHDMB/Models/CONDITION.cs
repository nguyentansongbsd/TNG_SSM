using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin_SAP_CreateHDMB.Models
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class CONDITION
    {
        public string kschl { get; set; }
        public decimal kbetr { get; set; }
        public decimal kwert { get; set; }
        public decimal NETWR { get; set; }
        public string ZDOTTT { get; set; }
        public string ZBHANG_CSBH { get; set; }
        public decimal BRGEW { get; set; }
        public decimal VAT { get; set; }
        public decimal TIEN_NOI_THAT { get; set; }
        public decimal PHI_BAO_TRI { get; set; }
        public decimal TONG_GIA_TRI { get; set; }
    }
}
