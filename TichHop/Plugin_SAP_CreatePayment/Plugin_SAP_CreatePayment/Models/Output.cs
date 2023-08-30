using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin_SAP_CreatePayment.Models
{
    public class Output
    {
        public MT_API_OUT MT_API_OUT { get; set; }
    }
    public class MT_API_OUT
    {
        public string status { get; set; }
        public string data { get; set; }
        public string message { get; set; }

    }
}
