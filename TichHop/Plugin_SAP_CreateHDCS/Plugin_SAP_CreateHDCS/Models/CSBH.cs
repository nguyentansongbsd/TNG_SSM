﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin_SAP_CreateHDCS.Models
{
    public class CSBH
    {
        public string id_csbh { get; set; }
        public string ten_csbh { get; set; }
        public List<CSBHB> CSBH_BS { get; set; }
    }
}