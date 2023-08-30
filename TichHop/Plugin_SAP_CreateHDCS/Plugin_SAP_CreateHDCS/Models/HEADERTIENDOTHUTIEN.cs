using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin_SAP_CreateHDCS.Models
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class HEADERTIENDOTHUTIEN
    {
        public string MA_LO_THUONG_MAI { get; set; }
        public string MA_LO_QUY_HOACH { get; set; }
        public string AFDAT { get; set; }
        public string TETXT { get; set; }
        public int FPROZ { get; set; }
        public int SO_TIEN_PHAI_THU_DAT_NEN { get; set; }
        public int SO_TIEN_DA_THU_DAT_NEN { get; set; }
        public int SO_TIEN_CON_LAI_DAT_NEN { get; set; }
        public int TY_LE_PHAI_THANH_TOAN_MONG { get; set; }
        public int SO_TIEN_PHAI_THU_MONG { get; set; }
        public int SO_TIEN_DA_THU_MONG { get; set; }
        public int SO_TIEN_CON_LAI_MONG { get; set; }
        public int TY_LE_PHAI_THANH_TOAN_NHA { get; set; }
        public int SO_TIEN_PHAI_THU_NHA { get; set; }
        public int SO_TIEN_DA_THU_NHA { get; set; }
        public int SO_TIEN_CON_LAI_NHA { get; set; }
        public decimal TONG_SO_TIEN_PHAI_THU { get; set; }
        public decimal TONG_SO_TIEN_DA_THU { get; set; }
        public decimal TONG_SO_TIEN_CON_LAI { get; set; }
        public decimal ZCN_TLLS { get; set; }
        public decimal ZCN_SN { get; set; }
        public decimal ZTIENLAI_PT { get; set; }
        public decimal ZTIENLAI_DT { get; set; }
        public decimal ZTIENLAI_CL { get; set; }
        public string ZML_FROM { get; set; }
        public string ZML_TO { get; set; }
        public int ZML_MONEY { get; set; }
        public string GHI_CHU { get; set; }
        public int zlptb { get; set; }
        public double zttb { get; set; }
        public int zlpxcgcn { get; set; }
        public int zlpdkbd { get; set; }
        public int ztpnn { get; set; }
    }
}
