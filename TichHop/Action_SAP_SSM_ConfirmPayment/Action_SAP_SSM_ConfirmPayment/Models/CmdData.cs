using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Action_SAP_SSM_ConfirmPayment.Models
{
    public class CmdData
    {
        public Header header { get; set; }
        public List<Item> item { get; set; }
    }
}
