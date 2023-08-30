using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin_SAP_CreatePayment.Models
{
    public class Header
    {
        public string belnr { get; set; }
        public string bldat { get; set; }
        public string budat { get; set; }
        public string blart { get; set; }
        public string bukrs { get; set; }
        public string waers { get; set; }
        public string bktxt { get; set; }
        public string api_type { get; set; }
        public string monat { get; set; }
        public string gjahr { get; set; }
        public string xblnr { get; set; }
    }
}
