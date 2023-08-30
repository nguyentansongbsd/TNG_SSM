using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin_SAP_CreateContact.Models
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class ADDITIONALDATA
    {
        public string KVGR1 { get; set; }
    }
}
