using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin_SAP_CreateHDCS.Models
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class Header
    {
        public string vbeln { get; set; }
        public string vblen_rf { get; set; }
        public string vbeln_rf { get; set; }
        public string vbeln_ls { get; set; }
        public string vbeln_ref_ls { get; set; }
        public Header header { get; set; }
        public HeaderSales header_sales { get; set; }
        public HeaderAccounting header_accounting { get; set; }
        public HeaderStatus header_status { get; set; }
        public HeaderInfoAdd header_info_add { get; set; }
        public List<HeaderPartner> header_partner { get; set; }
        public HeaderThongTinHopDong HEADER_THONG_TIN_HOP_DONG { get; set; }
        public List<LICHTRALAIVAYNH> LICH_TRA_LAI_VAY_NH { get; set; }
        public string auart { get; set; }
        public string auart_rf { get; set; }
        public string kunnr { get; set; }
        public string bstkd { get; set; }
        public string bstdk { get; set; }
        public string zlhdct { get; set; }
    }
}
