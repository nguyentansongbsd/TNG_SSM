using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin_SAP_CreateAccount.Models
{
    public class Return
    {
        public int line;
        public String partner;
        public String bu_type;
        public String status;
        public String message;
        public String zdate;
        public String ztime;
    }
    public class CmdDataResult
    {
        public List<Return> it_return;
    }
}
