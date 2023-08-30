using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Action_SAP_RequestAPI.Models
{
    public class RequestApi
    {
        public string MessID { get; set; }
        public string ApiToken { get; set; }
        public string ApiType { get; set; }
        public string CmdData { get; set; }
    }
}
