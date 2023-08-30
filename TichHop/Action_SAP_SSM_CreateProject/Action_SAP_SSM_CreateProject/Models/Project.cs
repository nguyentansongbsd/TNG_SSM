using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Action_SAP_SSM_CreateProject.Models
{
    public class Project
    {
        public string bsd_projectcode { get; set; }
        public string bsd_projectcodesap { get; set; }
        public string bsd_name { get; set; }
        public string bsd_investor { get; set; }
        public string bsd_country { get; set; }
        public string bsd_province { get; set; }
        public string bsd_district { get; set; }
        public string bsd_ward2 { get; set; }
        public string bsd_street { get; set; }
        public string bsd_address { get; set; }
        public string isUpdate { get; set; }
    }
}
