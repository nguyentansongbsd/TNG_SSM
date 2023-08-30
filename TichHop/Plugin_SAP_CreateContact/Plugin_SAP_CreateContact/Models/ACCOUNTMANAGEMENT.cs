using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin_SAP_CreateContact.Models
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class ACCOUNTMANAGEMENT
    {
        public string BUKRS { get; set; }
        public string AKONT { get; set; }
    }
}
