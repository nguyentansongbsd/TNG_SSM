using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Action_SAP_CreateUnit.Models
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class CmdData
    {
        public ThamSo tham_so { get; set; }
        public BaseData base_data { get; set; }
        public Classification classification { get; set; }
        public SaleOrg1 sale_org1 { get; set; }
        public SaleOrg2 sale_org2 { get; set; }
        public GeneralPlant general_plant { get; set; }
    }
}
