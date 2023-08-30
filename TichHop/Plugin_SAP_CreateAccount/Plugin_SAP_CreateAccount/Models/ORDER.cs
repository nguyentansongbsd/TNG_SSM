using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin_SAP_CreateAccount.Models
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class ORDER
    {
        public string VKORG { get; set; }
        public string VTWEG { get; set; }
        public string SPART { get; set; }
        public string BZIRK { get; set; }
        public string KDGRP { get; set; }
        public string VKBUR { get; set; }
        public string VKGRP { get; set; }
    }
}
