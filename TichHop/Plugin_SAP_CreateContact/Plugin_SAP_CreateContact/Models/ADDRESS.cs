using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin_SAP_CreateContact.Models
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class ADDRESS
    {
        public string TITLE_MEDI { get; set; }
        public string NAME_FIRST { get; set; }
        public string NAME_LAST { get; set; }
        public string BU_SORT1_TXT { get; set; }
        public string STREET { get; set; }
        public string STREET2 { get; set; }
        public string STREET3 { get; set; }
        public string HOUSE_NUM1 { get; set; }
        public string CITY1 { get; set; }
        public string CITY2 { get; set; }
        public string STREET_P { get; set; }
        public string STR_SUPPL1 { get; set; }
        public string STR_SUPPL2 { get; set; }
        public string STR_SUPPL3 { get; set; }
        public string LOCATION { get; set; }
        public string HOUSE_NUM1_P { get; set; }
        public string CITY1_P { get; set; }
        public string CITY2_P { get; set; }
        public string COUNTRY { get; set; }
        public string LANGUCORR { get; set; }
        public string TEL_NUMBER1 { get; set; }
        public string TEL_NUMBER2 { get; set; }
        public string MOB_NUMBER1 { get; set; }
        public string MOB_NUMBER2 { get; set; }
        public string MOB_NUMBER3 { get; set; }
        public string FAX_NUMBER { get; set; }
        public string SMTP_ADDR { get; set; }
        public string XDELE { get; set; }
    }
}
