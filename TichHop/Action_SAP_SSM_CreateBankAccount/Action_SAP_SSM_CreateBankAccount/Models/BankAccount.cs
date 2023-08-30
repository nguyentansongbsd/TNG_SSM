using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Action_SAP_SSM_CreateBankAccount.Models
{
    public class BankAccount
    {
        public string bsd_name { get; set; }
        public string bsd_cashaccount { get; set; }
        public string bsd_investor { get; set; }
        public string bsd_bank { get; set; }
        public string bsd_bankbranch { get; set; }
        public string isUpdate { get; set; }
        public string isUnActive { get; set; }
    }
}
