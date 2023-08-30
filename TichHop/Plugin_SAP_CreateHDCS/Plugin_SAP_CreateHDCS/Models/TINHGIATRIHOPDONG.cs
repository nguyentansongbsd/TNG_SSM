using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin_SAP_CreateHDCS.Models
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class TINHGIATRIHOPDONG
    {
        public string KSCHL { get; set; }
        public List<CONDITION> CONDITION { get; set; }
    }
}
