using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin_SAP_CreatePriceListItems.Models
{
    public class CmdData
    {
        public string MATNR { get; set; }
        public string MAKTX { get; set; }
        public string KSCHL { get; set; }
        public string VKORG { get; set; }
        public string ZWERKS { get; set; }
        public List<ZDETAIL> ZDETAIL { get; set; }
    }
}
