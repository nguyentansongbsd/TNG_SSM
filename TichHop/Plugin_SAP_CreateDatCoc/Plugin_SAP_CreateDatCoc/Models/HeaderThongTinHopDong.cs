using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin_SAP_CreateDatCoc.Models
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class HeaderThongTinHopDong
    {
        public CSBH CSBH { get; set; }
        public List<BangGiaHeader> BANG_GIA_HEADER { get; set; }
        public TienDoThanhToan TIEN_DO_THANH_TOAN { get; set; }
        public List<ChietKhau> CHIET_KHAU { get; set; }
        public List<KhuyenMai> KHUYEN_MAI { get; set; }
    }
}
