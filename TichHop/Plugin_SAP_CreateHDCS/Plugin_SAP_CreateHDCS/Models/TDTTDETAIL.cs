using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin_SAP_CreateHDCS.Models
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class TDTTDETAIL
    {
        public string ten_dot { get; set; }
        public string ngay_han_tt { get; set; }
        public decimal tyle_caotang { get; set; }
        public decimal tyle_qsdd { get; set; }
        public decimal tien_dat { get; set; }
        public decimal tyle_xd { get; set; }
        public decimal tien_nha { get; set; }
        public decimal tyle_mong { get; set; }
        public decimal tien_mong { get; set; }
        public decimal tong_tien { get; set; }
        public string phi_bao_tri { get; set; }
        public string phi_ql { get; set; }
    }
}
