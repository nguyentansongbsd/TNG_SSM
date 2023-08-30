using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin_SAP_CreateHDMB.Models
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class QUATRINHHTROLSUAT
    {
        public double LSUAT_NAM { get; set; }
        public double GTRI_HTLS { get; set; }
        public string NGAY_GIAI_NGAN { get; set; }
        public string NGAY_KTHUC { get; set; }
        public double STIEN_HTLS { get; set; }
        public int Z_GOI_VAY_HTLS { get; set; }
        public int ZGTKV_HTLS { get; set; }
    }
}
