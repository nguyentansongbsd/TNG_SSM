using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin_SAP_CreateHDCS.Models
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class TienDoThanhToan
    {
        public string id_tdtt { get; set; }
        public string ten_tdtt { get; set; }
        public List<TDTTDETAIL> TDTT_DETAIL { get; set; }
    }
}
