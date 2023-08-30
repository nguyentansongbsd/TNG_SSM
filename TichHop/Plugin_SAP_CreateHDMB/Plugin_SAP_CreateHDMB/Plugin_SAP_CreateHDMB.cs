using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json;
using Plugin_SAP_CreateHDMB.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Plugin_SAP_CreateHDMB
{
    public class Plugin_SAP_CreateHDMB : IPlugin
    {
        IPluginExecutionContext context = null;
        IOrganizationServiceFactory factory = null;
        IOrganizationService service = null;
        ITracingService tracingService = null;
        Entity target = null;
        Entity en = null;

        CmdData cmdData = new CmdData();
        Header header = new Header();
        Header header2 = new Header();
        Item item = new Item();
        HeaderSales headerSales = new HeaderSales();
        HeaderAccounting headerAccounting = new HeaderAccounting();
        HeaderStatus headerStatus = new HeaderStatus();
        HeaderInfoAdd headerInfoAdd = new HeaderInfoAdd();
        DetailShipping detailShipping = new DetailShipping();
        BangGiaHeader bangGiaHeader = new BangGiaHeader();
        HeaderThongTinHopDong headerThongTinHopDong = new HeaderThongTinHopDong();
        CSBH chinhSachBanHang = new CSBH();

        TINHGIATRIHOPDONG tINHGIATRIHOPDONG1 = new TINHGIATRIHOPDONG();
        TINHGIATRIHOPDONG tINHGIATRIHOPDONG3 = new TINHGIATRIHOPDONG();
        TINHGIATRIHOPDONG tINHGIATRIHOPDONG4 = new TINHGIATRIHOPDONG();
        TINHGIATRIHOPDONG tINHGIATRIHOPDONG5 = new TINHGIATRIHOPDONG();
        CONDITION cONDITION1 = new CONDITION();
        CONDITION cONDITION3 = new CONDITION();
        CONDITION cONDITION4 = new CONDITION();
        CONDITION cONDITION5 = new CONDITION();

        List<Entity> listCoOwner = new List<Entity>();
        List<Entity> listInstallment = new List<Entity>();
        List<Entity> listSummaryInstallment = new List<Entity>();
        bool isSyncSAP = false;
        bool isProjectFromSAP = false;
        string customerCode = string.Empty;

        public void Execute(IServiceProvider serviceProvider)
        {
            context = serviceProvider.GetService(typeof(IPluginExecutionContext)) as IPluginExecutionContext;
            factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            service = (IOrganizationService)factory.CreateOrganizationService(context.UserId);
            tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            if (context.Depth > 4) return;
            if (context.MessageName != "Update") return;
            target = (Entity)context.InputParameters["Target"];
            en = service.Retrieve(target.LogicalName, target.Id, new ColumnSet(true));

            Init();
        }
        private void Init()
        {
            Task.WaitAll(checkConfignSyncSAP(), checkProjectFromSAP());
            if (isSyncSAP == false || isProjectFromSAP == false) return;
            if (((OptionSetValue)en["statuscode"]).Value != 100000001 && ((OptionSetValue)en["statuscode"]).Value != 100000004) return;
            
            checkAndUpdateDeveloperForCustomer();
            Task.WaitAll(
                getCustomer(),
                addHeader(),
                addHeaderSale(),
                addHeaderAccounting(),
                addHeaderStatus(),
                addHeaderPartner(),
                addBangGiaHeader(),
                addTienDoThanhToan(),
                addKhuyenMai(),
                addCSBH(),
                addCSBHBS(),
                addCK(),
                addDetailShipping(),
                addItem()
                );
            if (string.IsNullOrWhiteSpace(this.customerCode)) throw new InvalidPluginExecutionException("Khách hàng chưa được đồng bộ SAP.");

            string _apiType = "124";
            if (en.Contains("bsd_contractcodesap") && en["bsd_contractcodesap"] != null)
            {
                header2.vbeln = en["bsd_contractcodesap"].ToString();
            }
            else if (this.en.Contains("bsd_deposit") && !this.en.Contains("quoteid"))
            {
                Entity enDatCoc = service.Retrieve(((EntityReference)this.en["bsd_deposit"]).LogicalName, ((EntityReference)this.en["bsd_deposit"]).Id, new ColumnSet(new string[] { "bsd_datcoccodesap", "bsd_mahethong" }));
                header2.auart_rf = "ZPDC";
                header2.vblen_rf = enDatCoc.Contains("bsd_datcoccodesap") ? enDatCoc["bsd_datcoccodesap"].ToString() : null;
                headerAccounting.xblnr_rf = enDatCoc.Contains("bsd_mahethong") ? enDatCoc["bsd_mahethong"].ToString() : null;
            }else if (this.en.Contains("quoteid"))
            {
                Entity enHDCS = service.Retrieve(((EntityReference)this.en["quoteid"]).LogicalName, ((EntityReference)this.en["quoteid"]).Id, new ColumnSet(new string[] { "bsd_contractcodesap", "bsd_mahethong" }));
                header2.auart_rf = "ZDCS";
                header2.vblen_rf = enHDCS.Contains("bsd_contractcodesap") ? enHDCS["bsd_contractcodesap"].ToString() : null;
                headerAccounting.xblnr_rf = enHDCS.Contains("bsd_mahethong") ? enHDCS["bsd_mahethong"].ToString() : null;
            }

            header.HEADER_THONG_TIN_HOP_DONG = headerThongTinHopDong;
            cmdData.header = header;
            cmdData.item = new List<Item>();
            cmdData.item.Add(item);

            string _content = JsonConvert.SerializeObject(cmdData);
            string crmDataBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(_content));
            tracingService.Trace("Content: " + _content);
            tracingService.Trace("CmdData: " + crmDataBase64);

            ApiRequest(_apiType, crmDataBase64);
        }
        private async Task addHeader()
        {
            header2.auart = "ZHBD";
            header2.kunnr = customerCode;
            header2.bstkd = this.en.Contains("bsd_optionno") ? this.en["bsd_optionno"].ToString() : null;
            header2.bstdk = this.en.Contains("bsd_signdate") ? (RetrieveLocalTimeFromUTCTime((DateTime)this.en["bsd_signdate"],service)).ToString("yyyy-MM-dd") : null;
            tracingService.Trace("Done header2");

            header.header = header2;
        }
        private async Task addHeaderSale()
        {
            headerSales.vkorg = getCompanyCode();
            headerSales.vtweg = getKenhPhanPhoi();
            headerSales.spart = getTypeUnit();
            headerSales.KETDAT = en.Contains("bsd_estimatehandoverdateold") ? (RetrieveLocalTimeFromUTCTime((DateTime)en["bsd_estimatehandoverdateold"],service)).ToString("yyyy-MM-dd") : null;
            headerSales.prsdt = this.en.Contains("bsd_datediscount") ? (RetrieveLocalTimeFromUTCTime((DateTime)en["bsd_datediscount"],service)).ToString("yyyy-MM-dd") : null;
            headerSales.vbegdat = this.en.Contains("bsd_signdatecontract") ? (RetrieveLocalTimeFromUTCTime((DateTime)en["bsd_signdatecontract"],service)).ToString("yyyy-MM-dd") : null;
            tracingService.Trace("Done headerSales.");

            header.header_sales = headerSales;
        }
        private async Task addHeaderAccounting()
        {
            headerAccounting.xblnr = this.en.Contains("bsd_mahethong") ? en["bsd_mahethong"].ToString() : null;
            tracingService.Trace("Done headerAccounting.");

            header.header_accounting = headerAccounting;
        }
        private async Task addHeaderStatus()
        {
            headerStatus.asttx = ((OptionSetValue)en["statuscode"]).Value != 100000004 ? "REL1" : "REL4"; // giam doc duyet
            header.header_status = headerStatus;
            tracingService.Trace("Done header status");
        }
        private async Task addHeaderInfor()
        {
            headerInfoAdd.zpbt = en.Contains("bsd_numberofmonthspaidmf") ? (int)en["bsd_numberofmonthspaidmf"] : 0;
            headerInfoAdd.zpdvql = en.Contains("bsd_numberofmonthspaidmf") ? (int)en["bsd_numberofmonthspaidmf"] : 0; //en.Contains("bsd_managementfee") ? (decimal)en["bsd_managementfee"] : 0;
            headerInfoAdd.znpp = en.Contains("bsd_distributor") ? getNPPName((EntityReference)en["bsd_distributor"]) : "NPP_011";
            headerInfoAdd.znkcn = "2022-07-15";
            headerInfoAdd.zkhcn = en.Contains("customerid") ? ((EntityReference)en["customerid"]).Name : "KHCN_012";
            headerInfoAdd.zkhncn = en.Contains("customerid") ? ((EntityReference)en["customerid"]).Name : "KHNCN_013";
            //headerInfoAdd.zlcn = "0001";
            //headerInfoAdd.znccvbcn ="2022-07-16";
            //headerInfoAdd.zscnvbcn = "SCC_014";
            //headerInfoAdd.zncnvbcn = "Ha Noi";
            //headerInfoAdd.zdcccvbcn = "Nam Tu Liem";
            //headerInfoAdd.ZSQCCVBCN = "Nam Tu Liem";
            //headerInfoAdd.znghsptda = "2022-07-13";
            //headerInfoAdd.znghscqnn = "2022-07-14";
            tracingService.Trace("Done headerInfoAdd.");

            header.header_info_add = headerInfoAdd;
        }
        private async Task addHeaderPartner()
        {
            header.header_partner = new List<HeaderPartner>();
            await getListCoOwner();
            for (int i = 0; i < this.listCoOwner.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(listCoOwner[i]["customerCodeSAP"].ToString()))
                {
                    HeaderPartner headerPartner = new HeaderPartner();
                    headerPartner.parvw = "C" + (i + 1);
                    headerPartner.partner_ext = ((AliasedValue)listCoOwner[i]["customerCodeSAP"]).Value.ToString();

                    header.header_partner.Add(headerPartner);
                }
            }
            tracingService.Trace("Done headerPartner");
        }
        private async Task addBangGiaHeader()
        {
            tracingService.Trace("Start bang gia");
            Entity enBangGia = getBangGia();
            tracingService.Trace("Done get bang gia");
            bangGiaHeader.id_banggia = enBangGia.Contains("bsd_pricelistcode") ? enBangGia["bsd_pricelistcode"].ToString() : null;
            bangGiaHeader.ten_banggia = enBangGia.Contains("name") ? enBangGia["name"].ToString() : null;
            bangGiaHeader.giatri_sp = this.en.Contains("bsd_detailamount") ? Math.Round(((Money)this.en["bsd_detailamount"]).Value) : 0;
            bangGiaHeader.giatri_ck = this.en.Contains("bsd_discount") ? Math.Round(((Money)this.en["bsd_discount"]).Value) : 0;
            bangGiaHeader.giatri_sp_sau_ck = this.en.Contains("bsd_totalamountlessfreight") ? Math.Round(((Money)this.en["bsd_totalamountlessfreight"]).Value) : 0;
            bangGiaHeader.netwr = this.en.Contains("bsd_totalamountdetails") ? Math.Round(((Money)this.en["bsd_totalamountdetails"]).Value) : 0;

            headerThongTinHopDong.BANG_GIA_HEADER = new List<BangGiaHeader>();
            headerThongTinHopDong.BANG_GIA_HEADER.Add(bangGiaHeader);
            tracingService.Trace("Done bang gia");
        }
        private async Task addTienDoThanhToan()
        {
            tracingService.Trace("Start TDTT");
            TienDoThanhToan tienDoThanhToan = new TienDoThanhToan();
            tienDoThanhToan.TDTT_DETAIL = new List<TDTTDETAIL>();
            Entity _enTDTT = getTienDoThanhToan();

            tienDoThanhToan.id_tdtt = _enTDTT.Contains("bsd_paymentschemecodenew") ? _enTDTT["bsd_paymentschemecodenew"].ToString() : null;
            tienDoThanhToan.ten_tdtt = _enTDTT.Contains("bsd_name") ? _enTDTT["bsd_name"].ToString() : null;

            tracingService.Trace("Check");
            if (this.en.Contains("bsd_paymentschemeland"))// cao tang
            {
                await getListInstallment();
                if (this.listInstallment.Count <= 0) return;
                foreach (var item in this.listInstallment)
                {
                    TDTTDETAIL TDTT = new TDTTDETAIL();
                    TDTT.ten_dot = "H" + ((int)item["bsd_ordernumber"]).ToString("D3");
                    TDTT.ngay_han_tt = item.Contains("bsd_duedate") ? ((DateTime)item["bsd_duedate"]).ToString("yyyy-MM-dd") : null;
                    TDTT.tyle_caotang = item.Contains("bsd_amountpercent") ? Math.Round((decimal)item["bsd_amountpercent"]) : 0;
                    TDTT.tong_tien = item.Contains("bsd_amountofthisphase") ? Math.Round(((Money)item["bsd_amountofthisphase"]).Value) : 0;
                    TDTT.phi_bao_tri = item.Contains("bsd_maintenancefees") && (bool)item["bsd_maintenancefees"] == true ? "Có" : "Không";
                    TDTT.phi_ql = item.Contains("bsd_managementfee") && (bool)item["bsd_managementfee"] == true ? "Có" : "Không";
                    //TDTT.ten_dot = "C001";
                    //TDTT.ngay_han_tt = en.Contains("bsd_paymentdepositdate") ? ((DateTime)en["bsd_paymentdepositdate"]).ToString("yyyy-MM-dd") : null;
                    //TDTT.tyle_caotang = 100;
                    //TDTT.tong_tien = en.Contains("bsd_depositfee") ? Math.Round(((Money)en["bsd_depositfee"]).Value) : 0;

                    tienDoThanhToan.TDTT_DETAIL.Add(TDTT);
                }
            }
            else
            {
                await getListSummaryInstallment();
                if (this.listSummaryInstallment.Count <= 0) return;
                foreach (var item in this.listSummaryInstallment)
                {
                    TDTTDETAIL TDTT = new TDTTDETAIL();
                    string name = item["bsd_name"].ToString();
                    string pattern = @"Đợt\s+(\d+)";
                    Match match = Regex.Match(name, pattern);
                    TDTT.ten_dot = "H" + int.Parse(match.Groups[1].Value).ToString("D3");
                    TDTT.ngay_han_tt = item.Contains("bsd_ngaydenhan") ? ((DateTime)item["bsd_ngaydenhan"]).ToString("yyyy-MM-dd") : null;
                    tracingService.Trace("1");
                    TDTT.tyle_qsdd = item.Contains("bsd_phantramdat") ? Math.Round((decimal)item["bsd_phantramdat"]) : 0;
                    TDTT.tien_dat = item.Contains("bsd_tienthanhtoancuadat") ? Math.Round(((Money)item["bsd_tienthanhtoancuadat"]).Value) : 0;
                    TDTT.tyle_xd = item.Contains("bsd_phantramnha") ? Math.Round((decimal)item["bsd_phantramnha"]) : 0;
                    tracingService.Trace("2");
                    TDTT.tien_nha = item.Contains("bsd_tiendotthanhtoancuanha") ? Math.Round(((Money)item["bsd_tiendotthanhtoancuanha"]).Value) : 0;
                    TDTT.tyle_mong = item.Contains("bsd_phantrammong") ? Math.Round((decimal)item["bsd_phantrammong"]) : 0;
                    TDTT.tien_mong = item.Contains("bsd_tiendotthanhtoancuamong") ? Math.Round(((Money)item["bsd_tiendotthanhtoancuamong"]).Value) : 0;
                    TDTT.tong_tien = item.Contains("bsd_tongtienthanhtoan") ? Math.Round(((Money)item["bsd_tongtienthanhtoan"]).Value) : 0;
                    tracingService.Trace("3");
                    //TDTT.ten_dot = "C001";
                    //TDTT.ngay_han_tt = en.Contains("bsd_paymentdepositdate") ? ((DateTime)en["bsd_paymentdepositdate"]).ToString("yyyy-MM-dd") : null;
                    //TDTT.tyle_qsdd = 100;
                    //TDTT.tong_tien = en.Contains("bsd_depositfee") ? Math.Round(((Money)en["bsd_depositfee"]).Value) : 0;

                    tienDoThanhToan.TDTT_DETAIL.Add(TDTT);
                }
            }

            headerThongTinHopDong.TIEN_DO_THANH_TOAN = tienDoThanhToan;
            tracingService.Trace("Done hEADERTIENDOTHUTIEN");
        }
        private async Task addDetailShipping()
        {
            detailShipping.werks = getProjectCode();
            detailShipping.brgew = this.en.Contains("bsd_typeunit") && ((OptionSetValue)this.en["bsd_typeunit"]).Value == 100000001 ? Math.Round((decimal)this.en["bsd_netusablearea"],2) : this.en.Contains("bsd_areasqm") ? Math.Round((decimal)this.en["bsd_areasqm"],2) : 0;
            detailShipping.brgew2 = this.en.Contains("bsd_constructionarea") ? Math.Round((decimal)this.en["bsd_constructionarea"], 2) : 0;
            tracingService.Trace("Done detailShipping");

            item.detail_shipping = detailShipping;
        }
        private async Task addKhuyenMai()
        {
            tracingService.Trace("Start Khuyen mai");
            string ids = this.en.Contains("bsd_promotionid") ? this.en["bsd_promotionid"].ToString() : null;
            if (string.IsNullOrWhiteSpace(ids)) return;
            List<string> listId = ids.Split(',').ToList();
            headerThongTinHopDong.KHUYEN_MAI = new List<KhuyenMai>();

            for (int i = 0; i < listId.Count; i++)
            {
                tracingService.Trace("Khuyen mai id: " + listId[i]);
                if (string.IsNullOrWhiteSpace(listId[i])) break;
                var fetchXml = $@"<?xml version=""1.0"" encoding=""utf-16""?>
                    <fetch>
                      <entity name=""bsd_discount"">
                        <attribute name=""bsd_name"" />
                        <attribute name=""bsd_discountnumber"" />
                        <attribute name=""bsd_amount"" />
                        <filter>
                          <condition attribute=""bsd_discountid"" operator=""eq"" value=""{listId[i]}""/>
                        </filter>
                      </entity>
                    </fetch>";
                var result = service.RetrieveMultiple(new FetchExpression(fetchXml));
                tracingService.Trace("Khuyen mãi : " + result.Entities.Count);
                if (result != null && result.Entities.Count > 0)
                {
                    Entity enKhuyenMai = result.Entities[0];
                    KhuyenMai khuyenMai = new KhuyenMai();
                    khuyenMai.id_km = enKhuyenMai.Contains("bsd_discountnumber") ? enKhuyenMai["bsd_discountnumber"].ToString() : null;
                    khuyenMai.ten_km = enKhuyenMai.Contains("bsd_name") ? enKhuyenMai["bsd_name"].ToString() : null;
                    khuyenMai.giatri_km = enKhuyenMai.Contains("bsd_amount") ? Math.Round(((Money)enKhuyenMai["bsd_amount"]).Value, 2) : 0;
                    headerThongTinHopDong.KHUYEN_MAI.Add(khuyenMai);
                }
            }

            tracingService.Trace("Done khuyen mai");
        }
        private async Task addCSBH()
        {
            tracingService.Trace("Start CSBH");
            if (!this.en.Contains("bsd_phaseslaunch")) return;
            Entity enCSBH = service.Retrieve(((EntityReference)this.en["bsd_phaseslaunch"]).LogicalName, ((EntityReference)this.en["bsd_phaseslaunch"]).Id, new ColumnSet(new string[] { "bsd_phaselaunchnumber", "bsd_name" }));
            chinhSachBanHang.id_csbh = enCSBH.Contains("bsd_phaselaunchnumber") ? enCSBH["bsd_phaselaunchnumber"].ToString() : null;
            chinhSachBanHang.ten_csbh = enCSBH.Contains("bsd_name") ? enCSBH["bsd_name"].ToString() : null;
            headerThongTinHopDong.CSBH = chinhSachBanHang;
            tracingService.Trace("Done CSBH");
        }
        private async Task addCSBHBS()
        {
            tracingService.Trace("Start CSBH");
            string ids = this.en.Contains("bsd_listidcsbhbs") ? this.en["bsd_listidcsbhbs"].ToString() : null;
            if (string.IsNullOrWhiteSpace(ids)) return;
            List<string> listId = ids.Replace("{", "").Replace("}", "").Split(',').ToList();
            chinhSachBanHang.CSBH_BS = new List<CSBHB>();
            for (int i = 0; i < listId.Count; i++)
            {
                tracingService.Trace("id: " + listId[i]);
                if (string.IsNullOrWhiteSpace(listId[i])) break;
                var fetchXml = $@"<?xml version=""1.0"" encoding=""utf-16""?>
                    <fetch>
                      <entity name=""bsd_phaseslaunch"">
                        <attribute name=""bsd_phaselaunchnumber"" />
                        <attribute name=""bsd_name"" />
                        <filter>
                          <condition attribute=""bsd_phaseslaunchid"" operator=""eq"" value=""{listId[i]}""/>
                          <condition attribute=""bsd_phaseslaunchplus"" operator=""eq"" value=""1""/>
                        </filter>
                      </entity>
                    </fetch>";
                var result = service.RetrieveMultiple(new FetchExpression(fetchXml));
                if (result != null && result.Entities.Count > 0)
                {
                    Entity enCSBHBS = result.Entities[0];
                    CSBHB csbhbs = new CSBHB();
                    csbhbs.id_csbh_bs = enCSBHBS.Contains("bsd_phaselaunchnumber") ? enCSBHBS["bsd_phaselaunchnumber"].ToString() : null;
                    csbhbs.ten_csbh_bs = enCSBHBS.Contains("bsd_name") ? enCSBHBS["bsd_name"].ToString() : null;
                    chinhSachBanHang.CSBH_BS.Add(csbhbs);
                    tracingService.Trace("Add done");
                }
            }
            headerThongTinHopDong.CSBH = chinhSachBanHang;
            tracingService.Trace("Done CSBHBS");
        }
        private async Task addCK()
        {
            string idsCkTuyetDoi = this.en.Contains("bsd_discounts") ? this.en["bsd_discounts"].ToString() : null;
            string idsCkTuongDoi = this.en.Contains("bsd_discountsupportid") ? this.en["bsd_discountsupportid"].ToString() : null;
            string idsCkNoiBo = this.en.Contains("bsd_interneldiscount") ? this.en["bsd_interneldiscount"].ToString() : null;
            if (string.IsNullOrWhiteSpace(idsCkTuyetDoi) && string.IsNullOrWhiteSpace(idsCkTuongDoi) && string.IsNullOrWhiteSpace(idsCkNoiBo)) return;

            headerThongTinHopDong.CHIET_KHAU = new List<ChietKhau>();
            List<string> ListId = new List<string>();
            if (!string.IsNullOrWhiteSpace(idsCkTuyetDoi))
            {
                List<string> idscktuyetdoi = idsCkTuyetDoi.Split(',').ToList();
                foreach (var item in idscktuyetdoi)
                {
                    if (string.IsNullOrWhiteSpace(item)) break;
                    ListId.Add(item);
                }
            }
            if (!string.IsNullOrWhiteSpace(idsCkTuongDoi))
            {
                List<string> idscktuongdoi = idsCkTuongDoi.Split(',').ToList();
                foreach (var item in idscktuongdoi)
                {
                    if (string.IsNullOrWhiteSpace(item)) break;
                    ListId.Add(item);
                }
            }
            if (!string.IsNullOrWhiteSpace(idsCkNoiBo))
            {
                List<string> idscknoibo = idsCkNoiBo.Split(',').ToList();
                foreach (var item in idscknoibo)
                {
                    tracingService.Trace("Ck Noi Bo: " + item);
                    if (string.IsNullOrWhiteSpace(item)) break;
                    ListId.Add(item);
                }
            }
            tracingService.Trace("danh sach ck: " + ListId.Count);
            for (int i = 0; i < ListId.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(ListId[i])) break;
                var fetchXml = $@"<?xml version=""1.0"" encoding=""utf-16""?>
                    <fetch>
                      <entity name=""bsd_discount"">
                        <attribute name=""bsd_name"" />
                        <attribute name=""bsd_amount"" />
                        <attribute name=""bsd_discountnumber"" />
                        <attribute name=""bsd_discounttype"" />
                        <attribute name=""bsd_method"" />
                        <attribute name=""bsd_percentage"" />
                        <attribute name=""bsd_calculation"" />
                        <filter>
                          <condition attribute=""bsd_discountid"" operator=""eq"" value=""{ListId[i]}""/>
                        </filter>
                      </entity>
                    </fetch>";
                var result = service.RetrieveMultiple(new FetchExpression(fetchXml));
                if (result != null && result.Entities.Count > 0)
                {
                    Entity enChietKhau = result.Entities[0];
                    ChietKhau chietKhau = new ChietKhau();
                    chietKhau.id_ck = enChietKhau.Contains("bsd_discountnumber") ? enChietKhau["bsd_discountnumber"].ToString() : null;
                    chietKhau.ten_ck = enChietKhau.Contains("bsd_name") ? enChietKhau["bsd_name"].ToString() : null;
                    chietKhau.sotien_ck = enChietKhau.Contains("bsd_amount") ? Math.Round(((Money)(enChietKhau["bsd_amount"])).Value, 2) : 0;
                    chietKhau.tyle_ck = enChietKhau.Contains("bsd_percentage") ? Math.Round((decimal)(enChietKhau["bsd_percentage"]), 2) : 0;
                    if (enChietKhau.Contains("bsd_discounttype") && ((OptionSetValue)enChietKhau["bsd_discounttype"]).Value == 100000004) //Chiết khấu tương đối
                    {
                        chietKhau.phuongthuc_ck = enChietKhau.Contains("bsd_calculation") ? enChietKhau.FormattedValues["bsd_calculation"].ToString() : null;
                    }
                    else
                    {
                        chietKhau.phuongthuc_ck = enChietKhau.Contains("bsd_method") ? enChietKhau.FormattedValues["bsd_method"].ToString() : null;
                    }
                    headerThongTinHopDong.CHIET_KHAU.Add(chietKhau);
                }
            }
            tracingService.Trace("Done CSBHBS");
        }
        private void addCondition1() // Don gia thong thuy => sp cao tang
        {
            tracingService.Trace("Start addCondition1");
            cONDITION1.kschl = "ZP01";
            cONDITION1.kbetr = this.en.Contains("bsd_priceafterdiscount") ? Math.Round(((Money)this.en["bsd_priceafterdiscount"]).Value, 2) : 0;
            cONDITION1.kwert = this.en.Contains("bsd_totalamountlessfreight") ? Math.Round(((Money)this.en["bsd_totalamountlessfreight"]).Value, 2) : 0;
            cONDITION1.NETWR = this.en.Contains("bsd_totalamountdetails") ? Math.Round(((Money)this.en["bsd_totalamountdetails"]).Value, 2) : 0;
            cONDITION1.BRGEW = this.en.Contains("bsd_netusablearea") ? Math.Round((decimal)this.en["bsd_netusablearea"], 2) : 0;
            cONDITION1.TIEN_NOI_THAT = this.en.Contains("bsd_packagesellingamount") ? Math.Round(((Money)this.en["bsd_packagesellingamount"]).Value, 2) : 0;
            cONDITION1.PHI_BAO_TRI = this.en.Contains("bsd_freightamount") ? Math.Round(((Money)this.en["bsd_freightamount"]).Value, 2) : 0;
            //cONDITION1.TONG_GIA_TRI = this.en.Contains("bsd_totalamountdetails") ? Math.Round(((Money)this.en["bsd_totalamountdetails"]).Value, 2) : 0;
            tracingService.Trace("Done addCondition1");

            tINHGIATRIHOPDONG1.KSCHL = "ZP01";
            tINHGIATRIHOPDONG1.CONDITION = new List<CONDITION>();
            tINHGIATRIHOPDONG1.CONDITION.Add(cONDITION1);
        }
        private void addCondition3() // Don gia dat => sp thap tang
        {
            tracingService.Trace("Start addCondition3");
            cONDITION3.kschl = "ZP03";
            cONDITION3.kbetr = this.en.Contains("bsd_landpriceafterdiscount") ? Math.Round(((Money)this.en["bsd_landpriceafterdiscount"]).Value, 2) : 0;
            cONDITION3.kwert = this.en.Contains("bsd_netsellingpriceland") ? Math.Round(((Money)this.en["bsd_netsellingpriceland"]).Value, 2) : 0; ;
            cONDITION3.NETWR = this.en.Contains("bsd_netsellingpriceland") ? Math.Round(((Money)this.en["bsd_netsellingpriceland"]).Value, 2) : 0; ;
            cONDITION3.BRGEW = this.en.Contains("bsd_areasqm") ? Math.Round((decimal)this.en["bsd_areasqm"], 2) : 0;

            tINHGIATRIHOPDONG3.KSCHL = "ZP03";
            tINHGIATRIHOPDONG3.CONDITION = new List<CONDITION>();
            tINHGIATRIHOPDONG3.CONDITION.Add(cONDITION3);
        }
        private void addCondition4() // Don gia xay => sp thap tang
        {
            tracingService.Trace("Start addCondition4");
            cONDITION4.kschl = "ZP04";
            cONDITION4.kbetr = en.Contains("bsd_housepricreafterdiscount") ? Math.Round(((Money)en["bsd_housepricreafterdiscount"]).Value, 2) : 0;
            cONDITION4.kwert = en.Contains("bsd_netsellingpricehouse") ? Math.Round(((Money)en["bsd_netsellingpricehouse"]).Value, 2) : 0;
            cONDITION4.NETWR = en.Contains("bsd_totalamounthouse") ? Math.Round(((Money)en["bsd_totalamounthouse"]).Value, 2) : 0;
            cONDITION4.BRGEW = en.Contains("bsd_constructionarea") ? Math.Round((decimal)en["bsd_constructionarea"], 2) : 0;
            cONDITION4.TIEN_NOI_THAT = this.en.Contains("bsd_handovercondition") ? Math.Round(((Money)this.en["bsd_handovercondition"]).Value, 2) : 0;
            //cONDITION4.PHI_BAO_TRI = this.en.Contains("bsd_freightamount") ? Math.Round(((Money)this.en["bsd_freightamount"]).Value, 2) : 0;
            //cONDITION4.TONG_GIA_TRI = this.en.Contains("bsd_totalamounthouse") ? Math.Round(((Money)this.en["bsd_totalamounthouse"]).Value, 2) : 0;

            tINHGIATRIHOPDONG4.KSCHL = "ZP04";
            tINHGIATRIHOPDONG4.CONDITION = new List<CONDITION>();
            tINHGIATRIHOPDONG4.CONDITION.Add(cONDITION4);
        }
        private void addCondition5()  // Don gia mong => sp thap tang
        {
            tracingService.Trace("Start addCondition5");
            cONDITION5.kschl = "ZP05";
            cONDITION5.kbetr = this.en.Contains("bsd_landvalueofunitsqm") ? Math.Round(((Money)this.en["bsd_landvalueofunitsqm"]).Value, 2) : 0;
            cONDITION5.kwert = this.en.Contains("bsd_netsellingpricefoundation") ? Math.Round(((Money)this.en["bsd_netsellingpricefoundation"]).Value, 2) : 0;
            cONDITION5.NETWR = this.en.Contains("bsd_netsellingpricefoundation") ? Math.Round(((Money)this.en["bsd_netsellingpricefoundation"]).Value, 2) : 0;
            cONDITION5.BRGEW = this.en.Contains("bsd_netusablearea") ? Math.Round((decimal)this.en["bsd_netusablearea"], 2) : 0;

            tINHGIATRIHOPDONG5.KSCHL = "ZP05";
            tINHGIATRIHOPDONG5.CONDITION = new List<CONDITION>();
            tINHGIATRIHOPDONG5.CONDITION.Add(cONDITION5);
        }
        private async Task addItem()
        {
            Entity _enUnit = getUnit();
            item.matnr = _enUnit.Contains("bsd_unitcodesap") ? _enUnit["bsd_unitcodesap"].ToString() : null;
            item.matnr2 = _enUnit.Contains("bsd_unitcodesap2") ? _enUnit["bsd_unitcodesap2"].ToString() : null;
            item.arktx = en.Contains("name") ? en["name"].ToString() : "";

            item.TINH_GIA_TRI_HOP_DONG = new List<TINHGIATRIHOPDONG>();
            if (this.en.Contains("bsd_typeunit") && ((OptionSetValue)this.en["bsd_typeunit"]).Value == 100000001) // Cao Tầng
            {
                addCondition1();
                item.TINH_GIA_TRI_HOP_DONG.Add(tINHGIATRIHOPDONG1); // Don gia thong thuy
            }
            else
            {
                if (this.en.Contains("bsd_loaibangtinhgia") && ((OptionSetValue)this.en["bsd_loaibangtinhgia"]).Value == 100000006) // Dat
                {
                    addCondition3();
                    item.TINH_GIA_TRI_HOP_DONG.Add(tINHGIATRIHOPDONG3); // Don gia dat
                }
                else if (this.en.Contains("bsd_loaibangtinhgia") && ((OptionSetValue)this.en["bsd_loaibangtinhgia"]).Value == 100000007) // Đất Móng
                {
                    addCondition5();
                    addCondition3();
                    item.TINH_GIA_TRI_HOP_DONG.Add(tINHGIATRIHOPDONG3); // Don gia dat
                    item.TINH_GIA_TRI_HOP_DONG.Add(tINHGIATRIHOPDONG5);// Don gia mong
                }
                else if (this.en.Contains("bsd_loaibangtinhgia") && ((OptionSetValue)this.en["bsd_loaibangtinhgia"]).Value == 100000005) // Đất Nhà
                {
                    addCondition4();
                    addCondition3();
                    item.TINH_GIA_TRI_HOP_DONG.Add(tINHGIATRIHOPDONG3); // Don gia dat
                    item.TINH_GIA_TRI_HOP_DONG.Add(tINHGIATRIHOPDONG4); // Don gia xay
                }
            }
        }

        private void ApiRequest(string apiType, string cmdData)
        {
            OrganizationRequest request = new OrganizationRequest("bsd_Action_SAP_RequestAPI");
            request["apitype"] = apiType;
            request["cmddata"] = cmdData;
            request["key"] = this.en.Contains("bsd_mahethong") ? en["bsd_mahethong"].ToString() : null;
            OrganizationResponse response = service.Execute(request);

            if (response.Results.Contains("output"))
            {
                string result = response.Results["output"].ToString();
                Output output = JsonConvert.DeserializeObject<Output>(result);
                if (output.MT_API_OUT.status == "S")
                {
                    tracingService.Trace("Done.....");
                    byte[] bytes = Convert.FromBase64String(output.MT_API_OUT.data);
                    string jsonData = Encoding.UTF8.GetString(bytes);
                    CmdDataResult cmdDataResult = JsonConvert.DeserializeObject<CmdDataResult>(jsonData);
                    //throw new InvalidPluginExecutionException(cmdDataResult.header.vbeln);
                    Entity enCurrentContract = service.Retrieve(target.LogicalName, target.Id, new ColumnSet(new string[1] { "bsd_contractcodesap" }));
                    enCurrentContract["bsd_contractcodesap"] = cmdDataResult.header.vbeln;
                    service.Update(enCurrentContract);
                }
            }
        }

        private string getNPPName(EntityReference npp)
        {
            if (npp.LogicalName == "contact")
            {
                Entity encontact = service.Retrieve(npp.LogicalName, npp.Id, new ColumnSet(new string[1] { "fullname" }));
                return encontact["fullname"].ToString();
            }
            else
            {
                Entity enaccount = service.Retrieve(npp.LogicalName, npp.Id, new ColumnSet(new string[1] { "bsd_name" }));
                return enaccount["bsd_name"].ToString();
            }
        }
        private string getCompanyCode()
        {
            if (!this.en.Contains("bsd_developer")) return null;
            Entity enAccount = service.Retrieve(((EntityReference)this.en["bsd_developer"]).LogicalName, ((EntityReference)this.en["bsd_developer"]).Id, new ColumnSet(new string[1] { "bsd_companycodesap" }));
            if (!enAccount.Contains("bsd_companycodesap")) return null;
            return enAccount["bsd_companycodesap"].ToString();
        }
        private async Task getCustomer()
        {
            if (!this.en.Contains("customerid")) customerCode = null;
            if (((EntityReference)en["customerid"]).LogicalName == "contact")
            {
                Entity enKH = service.Retrieve(((EntityReference)en["customerid"]).LogicalName, ((EntityReference)en["customerid"]).Id, new ColumnSet(new string[2] { "fullname", "bsd_customercodesap" }));
                if (!enKH.Contains("bsd_customercodesap")) customerCode = null;
                customerCode = enKH["bsd_customercodesap"].ToString();
            }
            else
            {
                Entity enKH = service.Retrieve(((EntityReference)en["customerid"]).LogicalName, ((EntityReference)en["customerid"]).Id, new ColumnSet(new string[2] { "bsd_name", "bsd_customercodesap" }));
                if (!enKH.Contains("bsd_customercodesap")) customerCode = null;
                customerCode = enKH["bsd_customercodesap"].ToString();
            }
        }
        private string getKenhPhanPhoi()
        {
            if (!this.en.Contains("bsd_kenhphanphoi")) return null;
            Entity enKenhPhanPhoi = service.Retrieve(((EntityReference)this.en["bsd_kenhphanphoi"]).LogicalName, ((EntityReference)this.en["bsd_kenhphanphoi"]).Id, new ColumnSet(new string[1] { "bsd_codesap" }));
            return enKenhPhanPhoi["bsd_codesap"].ToString();
        }
        private string getTypeUnit()
        {
            if (!this.en.Contains("bsd_unitnumber")) return null;
            Entity enUnit = service.Retrieve(((EntityReference)this.en["bsd_unitnumber"]).LogicalName, ((EntityReference)this.en["bsd_unitnumber"]).Id, new ColumnSet(new string[1] { "bsd_typeunit" }));
            string fetchXml = $@"<?xml version=""1.0"" encoding=""utf-16""?>
                <fetch top=""1"">
                  <entity name=""bsd_producttype"">
                    <attribute name=""bsd_codesap"" />
                    <filter>
                      <condition attribute=""bsd_code"" operator=""eq"" value=""{((OptionSetValue)enUnit["bsd_typeunit"]).Value}"" />
                    </filter>
                  </entity>
                </fetch>";
            var result = service.RetrieveMultiple(new FetchExpression(fetchXml));
            if (result == null || result.Entities.Count <= 0) return null;
            return result.Entities[0]["bsd_codesap"].ToString();
        }
        private async Task getListCoOwner()
        {
            var fetchXml = $@"<?xml version=""1.0"" encoding=""utf-16""?>
                <fetch>
                  <entity name=""bsd_coowner"">
                    <filter>
                      <condition attribute=""bsd_optionentry"" operator=""eq"" value=""{en.Id}""/>
                    </filter>
                    <link-entity name=""contact"" from=""contactid"" to=""bsd_relatives"" alias=""khachHang"">
                      <attribute name=""bsd_customercodesap"" alias=""customerCodeSAP"" />
                    </link-entity>
                  </entity>
                </fetch>";
            var result = service.RetrieveMultiple(new FetchExpression(fetchXml));
            if (result == null || result.Entities.Count <= 0) return;
            this.listCoOwner = result.Entities.ToList();
        }
        private Entity getBangGia()
        {
            if (!this.en.Contains("pricelevelid")) throw new InvalidPluginExecutionException("Không có bảng giá.");
            Entity enBangGia = service.Retrieve(((EntityReference)this.en["pricelevelid"]).LogicalName, ((EntityReference)this.en["pricelevelid"]).Id, new ColumnSet(new string[2] { "bsd_pricelistcode" , "name" }));
            tracingService.Trace("Done bang gia");
            return enBangGia;
        }
        private Entity getTienDoThanhToan()
        {
            tracingService.Trace("Start tien do thanh toan");
            if (!this.en.Contains("bsd_paymentscheme") && !this.en.Contains("bsd_paymentschemeland")) return null;
            string atributePaymentScheme = this.en.Contains("bsd_paymentschemeland") ? "bsd_paymentschemeland" : "bsd_paymentscheme";
            Entity enCSBH = service.Retrieve(((EntityReference)this.en[atributePaymentScheme]).LogicalName, ((EntityReference)this.en[atributePaymentScheme]).Id, new ColumnSet(new string[2] { "bsd_paymentschemecodenew", "bsd_name" }));
            tracingService.Trace("Done tien do thanh toan");
            return enCSBH;
        }
        private async Task getListInstallment()
        {
            var fetchXml = $@"<?xml version=""1.0"" encoding=""utf-16""?>
            <fetch>
              <entity name=""bsd_paymentschemedetail"">
                <order attribute=""bsd_ordernumber"" descending=""false"" />
                <filter>
                  <condition attribute=""bsd_optionentry"" operator=""eq"" value=""{en.Id}""/>
                </filter>
              </entity>
            </fetch>";
            var result = service.RetrieveMultiple(new FetchExpression(fetchXml));
            if (result == null || result.Entities.Count <= 0) return;
            this.listInstallment = result.Entities.ToList();
        }
        private async Task getListSummaryInstallment()
        {
            tracingService.Trace("Get summary installment");
            var fetchXml = $@"<?xml version=""1.0"" encoding=""utf-16""?>
                <fetch>
                  <entity name=""bsd_summaryinstallment"">
                    <order attribute=""bsd_name"" descending=""false"" />
                    <filter>
                      <condition attribute=""bsd_optionentry"" operator=""eq"" value=""{this.en.Id}"" />
                    </filter>
                  </entity>
                </fetch>";
            var result = service.RetrieveMultiple(new FetchExpression(fetchXml));
            if (result == null || result.Entities.Count <= 0) return;
            this.listSummaryInstallment = result.Entities.ToList();
            tracingService.Trace("Done Get summary installment");
        }
        private Entity getUnit()
        {
            if (!this.en.Contains("bsd_unitnumber")) return null;
            Entity enUnit = service.Retrieve(((EntityReference)this.en["bsd_unitnumber"]).LogicalName, ((EntityReference)this.en["bsd_unitnumber"]).Id, new ColumnSet(new string[2] { "bsd_unitcodesap", "bsd_unitcodesap2" }));
            return enUnit;
        }
        private string getProjectCode()
        {
            if (!en.Contains("bsd_project")) return null;
            Entity enProject = service.Retrieve(((EntityReference)en["bsd_project"]).LogicalName, ((EntityReference)en["bsd_project"]).Id, new ColumnSet(new string[] { "bsd_projectcode" }));
            var projectCode = enProject.Contains("bsd_projectcode") ? enProject["bsd_projectcode"].ToString() : null;
            return projectCode;
        }
        private void checkAndUpdateDeveloperForCustomer()
        {
            tracingService.Trace("Start check update");
            EntityReference enfCustomer = (EntityReference)this.en["customerid"];
            EntityReference enfDeveloper = (EntityReference)this.en["bsd_developer"];
            Entity enCustomer = this.service.Retrieve(enfCustomer.LogicalName, enfCustomer.Id, new ColumnSet(new string[] { "bsd_chudautu" }));
            if (!enCustomer.Contains("bsd_chudautu")||(enCustomer.Contains("bsd_chudautu") && ((EntityReference)enCustomer["bsd_chudautu"]).Id != enfDeveloper.Id))
            {
                updateDeveloperForCustomerSSM(enCustomer, enfDeveloper);
            }
            tracingService.Trace("End check update");
        }
        private void updateDeveloperForCustomerSSM(Entity customer, EntityReference developer)
        {
            tracingService.Trace("Start update");
            Entity enCustomer = new Entity(customer.LogicalName, customer.Id);
            enCustomer["bsd_chudautu"] = new EntityReference(developer.LogicalName,developer.Id);
            this.service.Update(enCustomer);
            tracingService.Trace("End update");
        }
        private async Task checkConfignSyncSAP()
        {
            var fetchXml = $@"<?xml version=""1.0"" encoding=""utf-16""?>
                <fetch top=""1"">
                  <entity name=""bsd_configsap"">
                    <attribute name=""bsd_name"" />
                    <filter>
                      <condition attribute=""bsd_issyncsap"" operator=""eq"" value=""1"" />
                    </filter>
                  </entity>
                </fetch>";
            var result = service.RetrieveMultiple(new FetchExpression(fetchXml));
            if (result == null || result.Entities.Count <= 0) this.isSyncSAP = false;
            else this.isSyncSAP = true;
        }
        private async Task checkProjectFromSAP()
        {
            Entity enProject = service.Retrieve(((EntityReference)this.en["bsd_project"]).LogicalName, ((EntityReference)this.en["bsd_project"]).Id, new ColumnSet(new string[] { "bsd_projectformsap" }));
            if (enProject.Contains("bsd_projectformsap") && (bool)enProject["bsd_projectformsap"] == true) this.isProjectFromSAP = true;
            else this.isProjectFromSAP = false;
        }

        private DateTime RetrieveLocalTimeFromUTCTime(DateTime utcTime, IOrganizationService service)
        {
            int? timeZoneCode = RetrieveCurrentUsersSettings(service);
            if (!timeZoneCode.HasValue)
                throw new InvalidPluginExecutionException("Can't find time zone code");
            var request = new LocalTimeFromUtcTimeRequest
            {
                TimeZoneCode = timeZoneCode.Value,
                UtcTime = utcTime.ToUniversalTime()
            };
            var response = (LocalTimeFromUtcTimeResponse)service.Execute(request);

            return response.LocalTime;
            //var utcTime = utcTime.ToString("MM/dd/yyyy HH:mm:ss");
            //var localDateOnly = response.LocalTime.ToString("dd-MM-yyyy");
        }
        private int? RetrieveCurrentUsersSettings(IOrganizationService service)
        {
            var currentUserSettings = service.RetrieveMultiple(
            new QueryExpression("usersettings")
            {
                ColumnSet = new ColumnSet("localeid", "timezonecode"),
                Criteria = new FilterExpression
                {
                    Conditions = { new ConditionExpression("systemuserid", ConditionOperator.EqualUserId) }
                }
            }).Entities[0].ToEntity<Entity>();

            return (int?)currentUserSettings.Attributes["timezonecode"];
        }
    }
}
