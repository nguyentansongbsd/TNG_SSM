using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin_SAP_CreateUnit.Models
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class Classification
    {
        public string mwert_khu { get; set; }
        public string mwert_phan_khu { get; set; }
        public string mwert_toa { get; set; }
        public string mwert_tang_co_so { get; set; }
        public string mwert_tang_thuong_mai { get; set; }
        public string mwert_ma_can_thuong_mai { get; set; }
        public string mwert_ma_can_co_so { get; set; }
        public string mwert_so_can_thuong_mai { get; set; }
        public string mwert_so_can_co_so { get; set; }
        public string mwert_so_phong_ngu { get; set; }
        public string mwert_huong { get; set; }
        public string mwert_view { get; set; }
        public string mwert_loai_can_ho { get; set; }
        public string mwert_goi_hoan_thien { get; set; }
        public string mwert_dien_tich_san_vuon { get; set; }
        public string mwert_truc_duong { get; set; }
        public string mwert_khieu_ldat_qhoach { get; set; }
        public string mwert_khieu_o_qhoach { get; set; }
        public string mwert_so_lo { get; set; }
        public string mwert_khieu_ldat_tmai { get; set; }
        public string mwert_mdxd { get; set; }
        public string mwert_tang_cao { get; set; }
        public string mwert_hthong_sdung_dat { get; set; }
        public string mwert_dtich_xdung_qh { get; set; }
        public string mwert_dtich_svuon { get; set; }
        public string mwert_dtich_nha_mai_san { get; set; }
        public string mwert_dtich_nha_kh_mai_san { get; set; }
        public string mwert_dtich_nha_xay_chiem_dat { get; set; }
        public string mwert_day { get; set; }
        public string ma_lo_pd { get; set; }
        public string so_o_tm { get; set; }
        public string so_o_pd { get; set; }
        public string ten_tang { get; set; }
        public string goc { get; set; }
        public string giai_doan { get; set; }
        public string ten_duong { get; set; }
        public string ten_duong_thuong_mai { get; set; }
        public string ky_hieu_duong { get; set; }
        public string long_duong { get; set; }
        public string san_giao_dich { get; set; }
        public string so_day_phe_duyet { get; set; }
        public string so_tai_khoan { get; set; }
        public string ngan_hang { get; set; }
        public string chi_nhanh_ngan_hang { get; set; }
        public string so_bang_hang { get; set; }
        public string ngay_ra_bang_hang { get; set; }
        public string phan_loai_theo_quy_hoach { get; set; }
        public string phan_loai_theo_bang_hang { get; set; }
        public string mat_tien { get; set; }
    }
}
