using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin_SAP_CreateHDMB.Models
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class HSOVAYNHANG
    {
        public HOSOVAYNGANHANG HOSO_VAY_NGANHANG { get; set; }
        public List<QUATRINHHTROLSUAT> QUA_TRINH_HTRO_LSUAT { get; set; }
    }
}
