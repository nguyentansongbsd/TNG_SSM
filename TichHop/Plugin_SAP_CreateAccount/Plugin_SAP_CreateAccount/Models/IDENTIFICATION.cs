using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin_SAP_CreateAccount.Models
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class IDENTIFICATION
    {
        public string BIRTHDT { get; set; }
        public string ID_TYPE { get; set; }
        public string IDNUMBER { get; set; }
        public string INSTITUTE { get; set; }
        public string ZEILE1 { get; set; }
        public string ZEILE2 { get; set; }
        public string ZEILE3 { get; set; }
        public string ZEILE4 { get; set; }
        public string ZEILE5 { get; set; }
        public string ZEILE6 { get; set; }
        public string ZEILE7 { get; set; }
        public string ZEILE8 { get; set; }
        public string ZEILE9 { get; set; }
        public string ZEILE10 { get; set; }
        public string ZEILE11 { get; set; }
        public string ZEILE12 { get; set; }
        public string ZEILE13 { get; set; }
        public string ZEILE14 { get; set; }
        public string ZEILE15 { get; set; }
        public string ZEILE16 { get; set; }
        public string ZEILE17 { get; set; }
        public string ZEILE18 { get; set; }
        public string ZEILE19 { get; set; }
        public string ZEILE20 { get; set; }
    }
}
