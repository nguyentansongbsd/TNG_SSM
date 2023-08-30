using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin_SAP_CreateHDMB.Models
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class HOSOVAYNGANHANG
    {
        public string id_hso { get; set; }
        public string so_phieu { get; set; }
        public string ngay_ky { get; set; }
        public string loai_the_chap { get; set; }
        public string goi_vay { get; set; }
        public int sthang_htro { get; set; }
        public int tien_lai_vay { get; set; }
        public int so_tien_htls_thuc_te { get; set; }
        public string hthuc_gngan { get; set; }
        public object zgtkv { get; set; }
        public string zhhv { get; set; }
        public object zstgntt { get; set; }
        public string zngn { get; set; }
        public string zhtlstn { get; set; }
        public string zhtlsdn { get; set; }
        public object zlsht { get; set; }
        public object zstgmk { get; set; }
        public string znhv { get; set; }
        public string zcnnh { get; set; }
        public string zcbtdql { get; set; }
        public string zsdtccb { get; set; }
        public object zsthtls { get; set; }
        public string zngay_thong_bao { get; set; }
        public string zdien_giai { get; set; }
    }
}
