using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin_SAP_CreateContact.Models
{
    public class CmdData
    {
        public List<Data> DATA { get; set; }
    }
    public class Data
    {
        public THAMSO THAM_SO { get; set; }
        public ADDRESS ADDRESS { get; set; }
        public IDENTIFICATION IDENTIFICATION { get; set; }
        public ORDER ORDER { get; set; }
        public ADDITIONALDATA ADDITIONAL_DATA { get; set; }
        public ACCOUNTMANAGEMENT ACCOUNT_MANAGEMENT { get; set; }
    }
}
