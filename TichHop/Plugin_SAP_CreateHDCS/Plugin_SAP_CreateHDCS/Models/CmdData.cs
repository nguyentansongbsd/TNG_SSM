using Plugin_SAP_CreateHDCS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin_SAP_CreateHDCS.Models
{
    public class CmdData
    {
        public Header header { get; set; }
        public List<Item> item { get; set; }
    }
}
