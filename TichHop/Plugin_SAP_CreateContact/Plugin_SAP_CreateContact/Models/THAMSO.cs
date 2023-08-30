using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin_SAP_CreateContact.Models
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class THAMSO
    {
        public string PARTNER { get; set; }
        public string BU_TYPE { get; set; }
        public string RLTYP { get; set; }
        public string BU_GROUP { get; set; }
    }
}
