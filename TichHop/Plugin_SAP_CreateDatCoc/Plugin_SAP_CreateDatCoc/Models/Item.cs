﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin_SAP_CreateDatCoc.Models
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class Item
    {
        public string mabnr { get; set; }
        public string matnr { get; set; }
        public string matnr2 { get; set; }
        public string arktx { get; set; }
        public DetailShipping detail_shipping { get; set; }
        public List<TINHGIATRIHOPDONG> TINH_GIA_TRI_HOP_DONG { get; set; }
    }
}
