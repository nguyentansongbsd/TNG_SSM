using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Action_SAP_SSM_ConfirmPayment.Models
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class Item
    {
        public string NEWBS { get; set; }
        public string NEWKO { get; set; }
        public string CF_ID { get; set; }
        public string PRCTR { get; set; }
        public string ZPCHD { get; set; }
        public string ZMASP { get; set; }
        public string ZDTTN { get; set; }
        public string ZDTTC { get; set; }
        public string ZVACODE { get; set; }
        public string WRBTR { get; set; }
        public string SGTXT { get; set; }
        public string ZEILE { get; set; }
        public string HKONT { get; set; }
        public string ZTERM { get; set; }
        public string ZFBDT { get; set; }
    }
}
