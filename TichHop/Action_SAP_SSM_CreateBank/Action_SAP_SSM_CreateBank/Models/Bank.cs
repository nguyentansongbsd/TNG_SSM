using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Action_SAP_SSM_CreateBank.Models
{
    public class Bank
    {
        public string bsd_abbreviation { get; set; }
        public string bsd_name { get; set; }
        public string bsd_othername { get; set; }
        public string bsd_typeofbank { get; set; }
        public string bsd_taxcode { get; set; }
        public decimal bsd_chartercapital { get; set; }
        public string bsd_swiftcode { get; set; }
        public string bsd_refcode { get; set; }
        public string bsd_phone { get; set; }
        public string bsd_email { get; set; }
        public string bsd_fax { get; set; }
        public string bsd_website { get; set; }
        public string bsd_address { get; set; }
        public string bsd_addressother { get; set; }
        public string isUpdate { get; set; }
    }
}
