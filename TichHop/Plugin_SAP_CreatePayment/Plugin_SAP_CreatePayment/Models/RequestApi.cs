using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin_SAP_CreatePayment.Models
{
    public class RequestApi
    {
        public string ApiToken { get; set; }
        public string ApiType { get; set; }
        public string CmdData { get; set; }
    }
}
