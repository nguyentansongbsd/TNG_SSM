using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin_SAP_CreateHDMB.Models
{
    public class CmdDataResult
    {
        public HeaderResult header { get; set; }
        public List<ItemResult> item { get; set; }
    }
    public class DetailAccountAssignment
    {
        public string prctr { get; set; }
    }

    public class DetailBillingDocument
    {
        public string ktgrm { get; set; }
    }

    public class DetailSalesA
    {
        public string abgru { get; set; }
    }

    public class DetailSalesB
    {
        public string matkl { get; set; }
    }

    public class DetailShippingResult
    {
        public double ntgew { get; set; }
        public double brgew { get; set; }
        public double brgew2 { get; set; }
        public string gewei { get; set; }
        public string werks { get; set; }
        public string vstel { get; set; }
    }

    public class DetailStatus
    {
        public string asttx { get; set; }
    }

    public class DetailTabAdditional
    {
        public string mvgr1 { get; set; }
        public string mvgr4 { get; set; }
    }

    public class HeaderResult
    {
        public string vbeln { get; set; }
        public string vbeln_rf { get; set; }
        public string vbeln_ls { get; set; }
        public string vbeln_ref_ls { get; set; }
        public HeaderResult header { get; set; }
        public HeaderSalesResult header_sales { get; set; }
        public HeaderAccounting header_accounting { get; set; }
        public HeaderStatus header_status { get; set; }
        public HeaderInfoAddResult header_info_add { get; set; }
        public HeaderBilling header_billing { get; set; }
        public List<HeaderPartnerResult> header_partner { get; set; }
        public HeaderOrderData header_order_data { get; set; }
        public List<HeaderTienDoThuTien> header_tien_do_thu_tien { get; set; }
        public HsoVayNhang hso_vay_nhang { get; set; }
        public List<LichTraLaiVayNh> lich_tra_lai_vay_nh { get; set; }
        public string auart { get; set; }
        public string auart_rf { get; set; }
        public string kunnr { get; set; }
        public string bstkd { get; set; }
        public int bstdk { get; set; }
        public string vblen_rf { get; set; }
        public string create_f02 { get; set; }
    }


    public class HeaderBilling
    {
        public string zterm { get; set; }
    }

    public class HeaderInfoAddResult
    {
        public int zpbt { get; set; }
        public int zpdvql { get; set; }
        public string znpp { get; set; }
        public int znkcn { get; set; }
        public string zkhcn { get; set; }
        public string zkhncn { get; set; }
        public int zlcn { get; set; }
        public int znccvbcn { get; set; }
        public string zscnvbcn { get; set; }
        public string zncnvbcn { get; set; }
        public string zdcccvbcn { get; set; }
        public string zsqccvbcn { get; set; }
        public int znghsptda { get; set; }
        public int znghscqnn { get; set; }
        public double zlptb { get; set; }
        public double zttb { get; set; }
        public double zlpxcgcn { get; set; }
        public double zlpdkbd { get; set; }
        public double ztpnn { get; set; }
    }

    public class HeaderOrderData
    {
        public string submi { get; set; }
    }

    public class HeaderPartnerResult
    {
        public string parvw { get; set; }
        public string partner_ext { get; set; }
        public string name1 { get; set; }
        public string street { get; set; }
        public string city1 { get; set; }
    }

    public class HeaderSalesResult
    {
        public string vkorg { get; set; }
        public string vtweg { get; set; }
        public string spart { get; set; }
        public string ketdat { get; set; }
        public string pltyp { get; set; }
        public int audat { get; set; }
        public int prsdt { get; set; }
        public int vbegdat { get; set; }
        public int venddat { get; set; }
        public string augru { get; set; }
        public string abrvw { get; set; }
        public int gwldt { get; set; }
    }

    public class HeaderTienDoThuTien
    {
        public string ma_lo_thuong_mai { get; set; }
        public string ma_lo_quy_hoach { get; set; }
        public string csbh_apdung { get; set; }
        public string tdtt_apdung { get; set; }
        public string ctkm_apdung { get; set; }
        public int afdat { get; set; }
        public string tetxt { get; set; }
        public int fproz { get; set; }
        public int so_tien_phai_thu_dat_nen { get; set; }
        public int so_tien_da_thu_dat_nen { get; set; }
        public int so_tien_con_lai_dat_nen { get; set; }
        public int ty_le_phai_thanh_toan_mong { get; set; }
        public int so_tien_phai_thu_mong { get; set; }
        public int so_tien_da_thu_mong { get; set; }
        public int so_tien_con_lai_mong { get; set; }
        public int ty_le_phai_thanh_toan_nha { get; set; }
        public int so_tien_phai_thu_nha { get; set; }
        public int so_tien_da_thu_nha { get; set; }
        public int so_tien_con_lai_nha { get; set; }
        public string waers { get; set; }
        public int tong_so_tien_phai_thu { get; set; }
        public int tong_so_tien_da_thu { get; set; }
        public int tong_so_tien_con_lai { get; set; }
        public int zcn_tlls { get; set; }
        public int zcn_sn { get; set; }
        public int ztienlai_pt { get; set; }
        public int ztienlai_dt { get; set; }
        public int ztienlai_cl { get; set; }
        public int zml_from { get; set; }
        public int zml_to { get; set; }
        public int zml_money { get; set; }
        public string ghi_chu { get; set; }
        public double zlptb { get; set; }
        public double zttb { get; set; }
        public double zlpxcgcn { get; set; }
        public double zlpdkbd { get; set; }
        public double ztpnn { get; set; }
    }

    public class HosoVayNganhang
    {
        public string id_hso { get; set; }
        public string ghi_chu { get; set; }
        public string so_phieu { get; set; }
        public int ngay_ky { get; set; }
        public string loai_the_chap { get; set; }
        public string goi_vay { get; set; }
        public int sthang_htro { get; set; }
        public double tien_lai_vay { get; set; }
        public double so_tien_htls_thuc_te { get; set; }
        public string hthuc_gngan { get; set; }
        public double zgtkv { get; set; }
        public string zhhv { get; set; }
        public double zstgntt { get; set; }
        public int zngn { get; set; }
        public int zhtlstn { get; set; }
        public int zhtlsdn { get; set; }
        public double zlsht { get; set; }
        public double zstgmk { get; set; }
        public string znhv { get; set; }
        public string zcnnh { get; set; }
        public string zcbtdql { get; set; }
        public string zsdtccb { get; set; }
        public double zsthtls { get; set; }
        public int zngay_thong_bao { get; set; }
        public string zdien_giai { get; set; }
        public string ztrang_thai { get; set; }
    }

    public class HsoVayNhang
    {
        public HosoVayNganhang hoso_vay_nganhang { get; set; }
        public List<object> qua_trinh_htro_lsuat { get; set; }
    }

    public class ItemResult
    {
        public string vbeln { get; set; }
        public string vbeln_ls { get; set; }
        public int posnr { get; set; }
        public string mabnr { get; set; }
        public string matnr { get; set; }
        public int kwmeng { get; set; }
        public string vrkme { get; set; }
        public string arktx { get; set; }
        public string condition_type_map { get; set; }
        public DetailSalesA detail_sales_a { get; set; }
        public DetailSalesB detail_sales_b { get; set; }
        public DetailShippingResult detail_shipping { get; set; }
        public DetailBillingDocument detail_billing_document { get; set; }
        public DetailAccountAssignment detail_account_assignment { get; set; }
        public DetailTabAdditional detail_tab_additional { get; set; }
        public DetailStatus detail_status { get; set; }
    }

    public class LichTraLaiVayNh
    {
        public string id_hso { get; set; }
        public string so_dot { get; set; }
        public int ngay_thanh_toan { get; set; }
        public double tien_lai { get; set; }
        public string ghi_chu { get; set; }
    }
}
