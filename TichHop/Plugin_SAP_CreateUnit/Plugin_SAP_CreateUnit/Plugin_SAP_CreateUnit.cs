using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json;
using Plugin_SAP_CreateUnit.Configns;
using Plugin_SAP_CreateUnit.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Plugin_SAP_CreateUnit
{
    public class Plugin_SAP_CreateUnit : IPlugin
    {
        IPluginExecutionContext context = null;
        IOrganizationServiceFactory factory = null;
        IOrganizationService service = null;
        ITracingService tracingService = null;
        Entity target = null;
        Entity en = null;

        CmdData cmdData = new CmdData();
        ThamSo thamSo = new ThamSo();
        BaseData baseData = new BaseData();
        Classification classification = new Classification();
        SaleOrg1 saleOrg1 = new SaleOrg1();
        SaleOrg2 saleOrg2 = new SaleOrg2();
        RequestApi body = new RequestApi();
        
        public void Execute(IServiceProvider serviceProvider)
        {
            context = serviceProvider.GetService(typeof(IPluginExecutionContext)) as IPluginExecutionContext;
            factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            service = (IOrganizationService)factory.CreateOrganizationService(context.UserId);
            tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            if (context.MessageName != "Create") return;
            target = (Entity)context.InputParameters["Target"];
            tracingService.Trace(target.Id.ToString());
            en = service.Retrieve(target.LogicalName, target.Id, new ColumnSet(true));
            tracingService.Trace("Done khai bao");

            Init();
        }
        private void Init()
        {
            body.ApiToken = Confign.apiToken;

            string bsd_typeunit = ((OptionSetValue)en["bsd_typeunit"]).Value.ToString();
            string unitCodeSAP = en.Contains("bsd_unitcodesap") ? en["bsd_unitcodesap"].ToString() : null;
            if (bsd_typeunit == "100000001" && string.IsNullOrWhiteSpace(unitCodeSAP)) //Cao Tầng
            {
                body.ApiType = "100"; // Tao moi sp cao tang SAP
                Task.WaitAll(
                   addThamSo(),
                   addBaseDataCaoTang(),
                   addClassificationCaoTang(),
                   addSaleOrg2CaoTang()
                   );
            }
            else if (bsd_typeunit == "100000001" && !string.IsNullOrWhiteSpace(unitCodeSAP)) // Cao Tầng - update
            {
                body.ApiType = "101"; // Cap nhat sp cao tang SAP
                Task.WaitAll(
                    addBaseDataCaoTang(true),
                    addClassificationCaoTang()
                    );
            }
            else if (bsd_typeunit == "100000000" && string.IsNullOrWhiteSpace(unitCodeSAP)) // Thấp Tầng 
            {
                body.ApiType = "102"; // Tao moi sp thap tang SAP
                Task.WaitAll(
                    addThamSo(),
                    addBaseDataThapTang(),
                    addClassificationThapTang(),
                    addSaleOrg2ThapTang()
                    );
            }
            else if (bsd_typeunit == "100000000" && !string.IsNullOrWhiteSpace(unitCodeSAP)) // Thấp Tầng - update
            {
                body.ApiType = "103"; // Cap nhat sp thap tang SAP
                Task.WaitAll(
                    addBaseDataThapTang(),
                    addClassificationThapTang(),
                    addSaleOrg2ThapTang()
                    );
            }
            addSaleOrg1();

            string _content = JsonConvert.SerializeObject(cmdData);
            string crmDataBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(_content));
            body.CmdData = crmDataBase64;
            tracingService.Trace("input: " + _content);
            tracingService.Trace("base64: " + crmDataBase64);
            
            ApiRequest(body);
        }
        private async Task addThamSo()
        {
            var fetchXml = $@"<?xml version=""1.0"" encoding=""utf-16""?>
                    <fetch>
                      <entity name=""product"">
                        <attribute name=""bsd_projectcode"" />
                        <filter>
                          <condition attribute=""productid"" operator=""eq"" value=""{en.Id}"" />
                        </filter>
                        <link-entity name=""bsd_project"" from=""bsd_projectid"" to=""bsd_projectcode"" alias=""project"">
                          <attribute name=""bsd_projectcode"" alias=""projectCode"" />
                          <link-entity name=""account"" from=""accountid"" to=""bsd_investor"" alias=""khdn"">
                            <attribute name=""bsd_companycodesap"" alias=""companyCode"" />
                          </link-entity>
                        </link-entity>
                      </entity>
                    </fetch>";
            var result = service.RetrieveMultiple(new FetchExpression(fetchXml));
            if (result.Entities.Count <= 0)
                throw new InvalidPluginExecutionException("Sản phẩm không tồn tại");
            Entity _enProduct = result.Entities[0];
            string _companyCode = _enProduct.Contains("companyCode") ? ((AliasedValue)_enProduct["companyCode"]).Value.ToString() : "1101";
            string _projectCode = _enProduct.Contains("projectCode") ? ((AliasedValue)_enProduct["projectCode"]).Value.ToString() : "";

            thamSo.bukrs = _companyCode;
            thamSo.werks = _projectCode.All(char.IsDigit) ? _projectCode : "2011";
            thamSo.vkorg = _companyCode;
            thamSo.vtweg = "00";
            tracingService.Trace("Done tham so");

            cmdData.tham_so = thamSo;
        }
        private async Task addClassificationCaoTang()
        {
            classification.mwert_khu = en.Contains("bsd_blocknumber") ? ((EntityReference)en["bsd_blocknumber"]).Name : null;
            classification.mwert_phan_khu = "";
            classification.mwert_toa = en.Contains("bsd_blocknumber") ? ((EntityReference)en["bsd_blocknumber"]).Name : null;
            classification.mwert_tang_co_so = en.Contains("bsd_floortkcs") ? en["bsd_floortkcs"].ToString() : null;
            classification.mwert_tang_thuong_mai = en.Contains("bsd_floottm") ? en["bsd_floottm"].ToString() : null;
            classification.mwert_ma_can_thuong_mai = en.Contains("bsd_lottmnumber") ? en["bsd_lottmnumber"].ToString() : null;
            classification.mwert_ma_can_co_so = "";
            classification.mwert_so_can_thuong_mai = en.Contains("bsd_unitsnumber") ? en["bsd_unitsnumber"].ToString() : null;
            classification.mwert_so_can_co_so = en.Contains("bsd_units") ? en["bsd_units"].ToString() : null;
            classification.mwert_so_phong_ngu = en.Contains("bsd_bedroomnumber") ? en["bsd_bedroomnumber"].ToString() : null;
            classification.mwert_huong = en.Contains("bsd_direction") ? en.FormattedValues["bsd_direction"] : null;
            classification.mwert_view = en.Contains("bsd_view") ? en.FormattedValues["bsd_view"] : null;
            classification.mwert_loai_can_ho = en.Contains("bsd_unittype") ? getUnitType() : null;
            classification.mwert_goi_hoan_thien = "";
            classification.mwert_dien_tich_san_vuon = "";
            classification.goc = "";
            classification.giai_doan = "";
            classification.ten_duong = en.Contains("bsd_street") ? en["bsd_street"].ToString() : null;
            classification.ten_duong_thuong_mai = en.Contains("bsd_streetname") ? en["bsd_streetname"].ToString() : null;
            classification.ky_hieu_duong = "";
            classification.long_duong = en.Contains("bsd_roadway") ? en["bsd_roadway"].ToString() : null;
            classification.san_giao_dich = "";
            classification.so_day_phe_duyet = en.Contains("bsd_approverownumber") ? en["bsd_approverownumber"].ToString() : null;
            classification.so_tai_khoan = en.Contains("bsd_banknumber") ? en["bsd_banknumber"].ToString() : null;
            classification.ngan_hang = "";
            classification.chi_nhanh_ngan_hang = "";
            classification.so_bang_hang = en.Contains("bsd_phasenumber") ? en["bsd_phasenumber"].ToString() : null;
            classification.ngay_ra_bang_hang = en.Contains("bsd_phasedate") ? ((DateTime)en["bsd_phasedate"]).ToString("dd.MM.yyyy") : null;
            classification.phan_loai_theo_quy_hoach = "";
            classification.phan_loai_theo_bang_hang = "";
            tracingService.Trace("Done classification");

            cmdData.classification = classification;
        }
        private async Task addClassificationThapTang()
        {
            classification.mwert_khu = en.Contains("bsd_blocknumber") ? ((EntityReference)en["bsd_blocknumber"]).Name : null;
            classification.mwert_phan_khu = en.Contains("bsd_subdivision") ? en["bsd_subdivision"].ToString() : null;
            classification.mwert_truc_duong = en.Contains("bsd_roads") ? en["bsd_roads"].ToString() : null;
            classification.mwert_khieu_ldat_qhoach = en.Contains("bsd_commercialplotssymbol") ? en["bsd_commercialplotssymbol"].ToString() : null;
            classification.mwert_khieu_o_qhoach = "";
            classification.mwert_so_lo = en.Contains("bsd_lotnumber") ? en["bsd_lotnumber"].ToString() : null;
            classification.mwert_khieu_ldat_tmai = en.Contains("bsd_lotnumber") ? en["bsd_lotnumber"].ToString() : null;
            classification.mwert_so_can_thuong_mai = en.Contains("bsd_lottmnumber") ? en["bsd_lottmnumber"].ToString() : null;
            classification.mwert_so_phong_ngu = en.Contains("bsd_bedroomnumber") ? en["bsd_bedroomnumber"].ToString() : null;
            classification.mwert_huong = en.Contains("bsd_direction") ? en.FormattedValues["bsd_direction"] : null;
            classification.mwert_view = en.Contains("bsd_view") ? en.FormattedValues["bsd_view"] : null;
            classification.mwert_mdxd = en.Contains("bsd_buildingdensity") ? en["bsd_buildingdensity"].ToString() : null;
            classification.mwert_tang_cao = en.Contains("bsd_highfloor") ? en["bsd_highfloor"].ToString() : null;
            classification.mwert_hthong_sdung_dat = en.Contains("bsd_floorarearatiofar") ? en["bsd_floorarearatiofar"].ToString() : null;
            classification.mwert_dtich_xdung_qh = "";
            classification.mwert_dtich_svuon = en.Contains("bsd_campusarea") ? en["bsd_campusarea"].ToString() : null;
            classification.mwert_dtich_nha_mai_san = en.Contains("bsd_constructionareat2m2") ? en["bsd_constructionareat2m2"].ToString() : null;
            classification.mwert_dtich_nha_kh_mai_san = "";
            classification.mwert_dtich_nha_xay_chiem_dat = "";
            classification.mwert_day = "";
            classification.ma_lo_pd = "";
            classification.so_o_tm = "";
            classification.so_o_pd = "";
            classification.ten_tang = en.Contains("bsd_floor") ? ((EntityReference)en["bsd_floor"]).Name : null;

            classification.goc = en.Contains("bsd_corner") ? en["bsd_corner"].ToString() : null;
            classification.giai_doan = en.Contains("bsd_stageofclearance") ? en["bsd_stageofclearance"].ToString() : null;
            classification.ten_duong = en.Contains("bsd_street") ? en["bsd_street"].ToString() : null;
            classification.ten_duong_thuong_mai = en.Contains("bsd_streetname") ? en["bsd_streetname"].ToString() : null;
            classification.ky_hieu_duong = "";
            classification.long_duong = en.Contains("bsd_roadway") ? en["bsd_roadway"].ToString() : null;
            classification.san_giao_dich = "";
            classification.so_day_phe_duyet = en.Contains("bsd_approverownumber") ? en["bsd_approverownumber"].ToString() : null;
            classification.so_tai_khoan = en.Contains("bsd_banknumber") ? en["bsd_banknumber"].ToString() : null;
            classification.ngan_hang = "";
            classification.chi_nhanh_ngan_hang = "";
            classification.so_bang_hang = en.Contains("bsd_phasenumber") ? en["bsd_phasenumber"].ToString() : null;
            classification.ngay_ra_bang_hang = en.Contains("bsd_phasedate") ? ((DateTime)en["bsd_phasedate"]).ToString("dd.MM.yyyy") : null;
            classification.phan_loai_theo_quy_hoach = "";
            classification.phan_loai_theo_bang_hang = "";
            classification.mat_tien = en.Contains("bsd_frontispiece") ? en["bsd_frontispiece"].ToString() : null;
            tracingService.Trace("Done classification");

            cmdData.classification = classification;
        }
        private async Task addBaseDataCaoTang(bool? isUpdate = null)
        {
            if (isUpdate.HasValue && isUpdate == true)
                baseData.matnr = en["bsd_unitcodesap"].ToString();

            baseData.maktx = en.Contains("name") ? en["name"].ToString() : "";
            baseData.bismt = en.Contains("productnumber") ? en["productnumber"].ToString() : "";
            baseData.spart = "01"; // Cao tang = 01
            baseData.matkl = "001"; // 002
            baseData.brgew = en.Contains("bsd_netsaleablearea") ? (decimal)en["bsd_netsaleablearea"] : 0;
            baseData.ntgew = en.Contains("bsd_wallarea") ? (decimal)en["bsd_wallarea"] : 0;
            baseData.gewei = "M2";
            tracingService.Trace("Done base data");

            cmdData.base_data = baseData;
        }
        private async Task addBaseDataThapTang()
        {
            baseData.maktx = en.Contains("name") ? en["name"].ToString() : "";
            baseData.bismt = en.Contains("productnumber") ? en["productnumber"].ToString() : "";
            baseData.meins = "CAN";
            baseData.spart = "02"; // Thap tang = 02
            baseData.matkl = "010";
            baseData.labor = "001";
            baseData.brgew = en.Contains("bsd_areasqm") ? (decimal)en["bsd_areasqm"] : 0;
            baseData.brgew2 = en.Contains("bsd_constructionareat2m2") ? (decimal)en["bsd_constructionareat2m2"] : 0;
            tracingService.Trace("Done base data");

            cmdData.base_data = baseData;
        }
        private async Task addSaleOrg2CaoTang()
        {
            saleOrg2.ktgrm = "01"; //ktgrm = tai khoan hach toan
            saleOrg2.mvgr1 = getMaterialGroup1();
            //saleOrg2.mvgr4 = "401";
            tracingService.Trace("Done sale org 2");

            cmdData.sale_org2 = saleOrg2;
        }
        private async Task addSaleOrg2ThapTang()
        {
            saleOrg2.ktgrm = "01"; //ktgrm = tai khoan hach toan
            saleOrg2.mvgr1 = getMaterialGroup1();
            //saleOrg2.mvgr4 = "402";
            tracingService.Trace("Done sale org 2");

            cmdData.sale_org2 = saleOrg2;
        }
        private void addSaleOrg1()
        {
            string status = ((OptionSetValue)en["statuscode"]).Value.ToString();
            if (status == "1")
            {
                saleOrg1.vmsta = "01";// chua mo ban
            }
            else
            {
                saleOrg1.vmsta = "02";// mo ban
            }

            saleOrg1.vmstd = "2023-06-04";
            tracingService.Trace("Done sale org 1");

            cmdData.sale_org1 = saleOrg1;
        }
        private void ApiRequest(RequestApi body)
        {
            using (var client = new HttpClient())
            {
                string objContent = JsonConvert.SerializeObject(body);
                HttpContent fromBody = new StringContent(objContent, Encoding.UTF8, "application/json");
                var byteArray = Encoding.ASCII.GetBytes(Confign.apiAuth);
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
                tracingService.Trace("Body: " + objContent);
                tracingService.Trace("Done khai bao client.");

                var response = client.PostAsync(Confign.apiUrl, fromBody).Result;
                var content = response.Content.ReadAsStringAsync().Result;
                tracingService.Trace("Result content: " + content);
                Output output = JsonConvert.DeserializeObject<Output>(content);
                if (output.MT_API_OUT.status != "S")
                {
                    throw new InvalidPluginExecutionException("SAP error: " + output.MT_API_OUT.message);
                }
                else
                {
                    byte[] bytes = Convert.FromBase64String(output.MT_API_OUT.data);
                    string jsonData = Encoding.UTF8.GetString(bytes);
                    CmdData cmdData = new CmdData();
                    if (body.ApiType == "100" || body.ApiType == "101") // Cao tang
                    {
                        cmdData = JsonConvert.DeserializeObject<CmdData>(jsonData);
                    }
                    else if (body.ApiType == "102" || body.ApiType == "103") // Thap tang
                    {
                        List<CmdData> cmdDataResult = JsonConvert.DeserializeObject<List<CmdData>>(jsonData);
                        cmdData = cmdDataResult[0];
                    }

                    Entity enCurrentUnit = service.Retrieve(target.LogicalName, target.Id, new ColumnSet(new string[1] { "bsd_unitcodesap" }));
                    enCurrentUnit["bsd_unitcodesap"] = cmdData.base_data.matnr;
                    service.Update(enCurrentUnit);
                }
            }
        }
        private string getUnitType()
        {
            Entity enUnitType = service.Retrieve(((EntityReference)en["bsd_unittype"]).LogicalName, ((EntityReference)en["bsd_unittype"]).Id, new ColumnSet(new string[1] { "bsd_producttype" }));
            return enUnitType.FormattedValues["bsd_producttype"].ToString();
        }
        private string getMaterialGroup1()
        {
            string bsd_typeunit = ((OptionSetValue)en["bsd_typeunit"]).Value.ToString();
            if (bsd_typeunit == "100000001") // Cap tang
            {
                string completepackage = ((OptionSetValue)en["bsd_completepackage"]).Value.ToString(); // Muc do hoan thien
                if (completepackage == "100000000")//Hoàn thiện thô/Không gian sáng tạo
                {
                    return "120";
                }
                else if (completepackage == "100000001") //Hoàn thiện cơ bản
                {
                    return "121";
                }
                else if (completepackage == "100000002") //Hoàn thiện đầy đủ
                {
                    return "122";
                }
                else  // Hoàn thiện cao cấp
                {
                    return "123";
                }
            }
            else // Thap tang
            {
                string typeUnit = ((OptionSetValue)en["bsd_typyeunit2"]).Value.ToString(); // Loai chi tiet
                if (typeUnit == "100000000")//Đất
                {
                    return "100";
                }
                else if (typeUnit == "100000001") //Đất Móng
                {
                    return "101";
                }
                else if (typeUnit == "100000002") //Đất Nhà
                {
                    return "102";
                }
                else  // Đất nền
                {
                    return "100";
                }
            }
        }
    }
}
