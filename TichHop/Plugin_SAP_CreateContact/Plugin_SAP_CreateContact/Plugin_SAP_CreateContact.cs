using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json;
using Plugin_SAP_CreateContact.Configns;
using Plugin_SAP_CreateContact.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Plugin_SAP_CreateContact
{
    public class Plugin_SAP_CreateContact : IPlugin
    {
        IPluginExecutionContext context = null;
        IOrganizationServiceFactory factory = null;
        IOrganizationService service = null;
        ITracingService tracingService = null;
        Entity target = null;
        Entity en = null;

        CmdData cmdData = new CmdData();
        Data data = new Data();
        THAMSO thamSo = new THAMSO();
        ADDRESS address = new ADDRESS();
        IDENTIFICATION identification = new IDENTIFICATION();
        ORDER order = new ORDER();
        ADDITIONALDATA additionaldata = new ADDITIONALDATA();
        ACCOUNTMANAGEMENT accountmanagement = new ACCOUNTMANAGEMENT();
        RequestApi body = new RequestApi();
        public void Execute(IServiceProvider serviceProvider)
        {
            context = serviceProvider.GetService(typeof(IPluginExecutionContext)) as IPluginExecutionContext;
            factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            service = (IOrganizationService)factory.CreateOrganizationService(context.UserId);
            tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            if (context.Depth > 4) return;
            if (context.MessageName != "Create" && context.MessageName != "Update") return;
            target = (Entity)context.InputParameters["Target"];
            en = service.Retrieve(target.LogicalName, target.Id, new ColumnSet(true));
            if (context.MessageName == "Update" && !en.Contains("bsd_customercodesap")) return;

            Init();

            if (context.MessageName == "Create")
            {
                body.ApiType = "200";
            } else if (context.MessageName == "Update")
            {
                tracingService.Trace("Start Update");
                thamSo.PARTNER = en["bsd_customercodesap"].ToString();
                body.ApiType = "201";
            }

            List<Models.Data> _data = new List<Data>();
            _data.Add(data);
            cmdData.DATA = _data;

            string _content = JsonConvert.SerializeObject(cmdData);
            string crmDataBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(_content));
            body.ApiToken = Confign.apiToken;
            body.CmdData = crmDataBase64;
            tracingService.Trace("Content: " + _content);
            tracingService.Trace("Base64: " + crmDataBase64);
            
            ApiRequest(body);
        }
        private void Init()
        {
            Task.WaitAll(
                addThamSo(),
                addAddress(),
                addIndentification(),
                addOrder(),
                addAdditionalData(),
                addAccountManagement()
                );
        }
        private async Task addThamSo()
        {
            string customerGroupCode = en.Contains("bsd_groupcustomer") ? service.Retrieve(((EntityReference)en["bsd_groupcustomer"]).LogicalName, ((EntityReference)en["bsd_groupcustomer"]).Id, new ColumnSet(new string[1] { "bsd_customergroupcodesap" }))["bsd_customergroupcodesap"].ToString() : "N008";

            thamSo.BU_TYPE = "1";
            thamSo.RLTYP = "000000";
            thamSo.BU_GROUP = customerGroupCode;

            data.THAM_SO = thamSo;
            tracingService.Trace("Done tham so");
        }
        private async Task addAddress()
        {
            string vocative = en.Contains("bsd_vocative") ? ((OptionSetValue)en["bsd_vocative"]).Value.ToString() : "100000003";
            if (vocative == "100000003" || vocative == "100000000" || vocative == "100000002")// Ông/Anh/Khác
            {
                address.TITLE_MEDI = "0001";
            }
            else if (vocative == "100000004" || vocative == "100000001") // Bà/Chị
            {
                address.TITLE_MEDI = "0002";
            }

            address.NAME_FIRST = en["firstname"].ToString();
            address.NAME_LAST = en["lastname"].ToString();
            address.BU_SORT1_TXT = en["bsd_customercode"].ToString();
            address.STREET = en.Contains("bsd_permanentward2") ? getWardName((EntityReference)en["bsd_permanentward2"]) : null;
            address.STREET2 = null;
            address.STREET3 = null;
            address.HOUSE_NUM1 = en.Contains("bsd_permanentaddress") ? en["bsd_permanentaddress"].ToString() : null;
            address.CITY1 = en.Contains("bsd_permanentprovince") ? getPrviceName((EntityReference)en["bsd_permanentprovince"]) : null;
            address.CITY2 = en.Contains("bsd_permanentdistrict") ? getDistrictName((EntityReference)en["bsd_permanentdistrict"]) : null;
            address.COUNTRY = en.Contains("bsd_permanentcountry") ? getCountryName((EntityReference)en["bsd_permanentcountry"])["bsd_id"].ToString() : null;
            address.LANGUCORR = "EN";

            address.TEL_NUMBER1 = en.Contains("mobilephone") ? en["mobilephone"].ToString() : null;
            address.TEL_NUMBER2 = en.Contains("bsd_phone2") ? en["bsd_phone2"].ToString() : null;
            address.MOB_NUMBER1 = en.Contains("bsd_phone3") ? en["bsd_phone3"].ToString() : null;
            address.MOB_NUMBER2 = en.Contains("bsd_sinthoi4") ? en["bsd_sinthoi4"].ToString() : null;
            address.MOB_NUMBER3 = en.Contains("bsd_sinthoi5") ? en["bsd_sinthoi5"].ToString() : null;
            address.FAX_NUMBER = en.Contains("fax") ? en["fax"].ToString() : null;
            address.SMTP_ADDR = en.Contains("emailaddress1") ? en["emailaddress1"].ToString() : null;
            address.XDELE = "";

            data.ADDRESS = address;
            tracingService.Trace("Done address");
        }
        private async Task addIndentification()
        {
            tracingService.Trace("Start addIndentification");
            string soCMND = string.Empty;
            string ngayCMND = string.Empty;
            string noiCapCMND = string.Empty;
            if (((OptionSetValue)en["bsd_typeofidcardlead"]).Value == 100000000) // Identity Card Number (ID)
            {
                tracingService.Trace("Start CMND");
                soCMND = en.Contains("bsd_identitycardnumber") ? en["bsd_identitycardnumber"].ToString() : null;
                ngayCMND = en.Contains("bsd_dategrant") ? ((DateTime)en["bsd_dategrant"]).ToString("dd.MM.yyyy") : null;
                noiCapCMND = en.Contains("bsd_placeofissue") ? en["bsd_placeofissue"].ToString() : null;
            }
            else if (((OptionSetValue)en["bsd_typeofidcardlead"]).Value == 100000001) // Identity Card
            {
                tracingService.Trace("Start CCCD");
                soCMND = en.Contains("bsd_identitycard") ? en["bsd_identitycard"].ToString() : null;
                ngayCMND = en.Contains("bsd_identitycarddategrant") ? ((DateTime)en["bsd_identitycarddategrant"]).ToString("dd.MM.yyyy") : null;
                noiCapCMND = en.Contains("bsd_placeofissueidentitycardoption") ? en.FormattedValues["bsd_placeofissueidentitycardoption"].ToString() : null;
            }
            else if (((OptionSetValue)en["bsd_typeofidcardlead"]).Value == 100000002) // Passport
            {
                tracingService.Trace("Start Passport");
                soCMND = en.Contains("bsd_passport") ? en["bsd_passport"].ToString() : null;
                ngayCMND = en.Contains("bsd_issuedonpassport") ? ((DateTime)en["bsd_issuedonpassport"]).ToString("dd.MM.yyyy") : null;
                noiCapCMND = en.Contains("bsd_placeofissuepassport") ? en["bsd_placeofissuepassport"].ToString() : null;
            }
            else if (((OptionSetValue)en["bsd_typeofidcardlead"]).Value == 100000003) // Mã số khác
            {
                tracingService.Trace("Start Mã số khác");
                soCMND = en.Contains("bsd_othercode") ? en["bsd_othercode"].ToString() : null;
                ngayCMND = en.Contains("bsd_date") ? ((DateTime)en["bsd_date"]).ToString("dd.MM.yyyy") : null;
                noiCapCMND = en.Contains("bsd_place") ? en["bsd_place"].ToString() : null;
            }
            tracingService.Trace("Done CMND");

            identification.BIRTHDT = en.Contains("birthdate") ? ((DateTime)en["birthdate"]).ToString("dd.MM.yyyy") : null;
            identification.ID_TYPE = "CRM001";
            identification.IDNUMBER = soCMND;
            identification.INSTITUTE = noiCapCMND;

            data.IDENTIFICATION = identification;
            tracingService.Trace("Done identification");
        }
        private async Task addOrder()
        {
            order.VKORG = en.Contains("bsd_companycode") ? en["bsd_companycode"].ToString() : "1101";
            order.VTWEG = "00";
            order.SPART = "00";
            order.BZIRK = "016643";
            order.KDGRP = "01";
            order.VKBUR = "1100";
            order.VKGRP = "024";

            data.ORDER = order;
            tracingService.Trace("Done order");
        }
        private async Task addAdditionalData()
        {
            string gendercode = en.Contains("gendercode") && (((OptionSetValue)en["gendercode"]).Value == 1 || ((OptionSetValue)en["gendercode"]).Value == 100000000) ? "001" : "002"; 
            additionaldata.KVGR1 = gendercode;

            data.ADDITIONAL_DATA = additionaldata;
            tracingService.Trace("Done additionaldata");
        }
        private async Task addAccountManagement()
        {
            accountmanagement.BUKRS = "1101"; // en.Contains("bsd_companycode") ? en["bsd_companycode"].ToString() : "1101";
            accountmanagement.AKONT = "1311100000";

            data.ACCOUNT_MANAGEMENT = accountmanagement;
            tracingService.Trace("Done accountmanagement");
        }

        private void ApiRequest(RequestApi body)
        {
            tracingService.Trace("Start Request");
            using (var client = new HttpClient())
            {
                string objContent = JsonConvert.SerializeObject(body);
                HttpContent fromBody = new StringContent(objContent, Encoding.UTF8, "application/json");
                var byteArray = Encoding.ASCII.GetBytes(Confign.apiAuth);
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
                tracingService.Trace("Done input api");

                var response = client.PostAsync(Confign.apiUrl, fromBody).Result;
                var content = response.Content.ReadAsStringAsync().Result;
                Output output = JsonConvert.DeserializeObject<Output>(content);
                if (output.MT_API_OUT.status != "S")
                {
                    throw new InvalidPluginExecutionException("Error result data: " + output.MT_API_OUT.message);
                }
                else if (output.MT_API_OUT.status == "S" && !string.IsNullOrEmpty(output.MT_API_OUT.data))
                {
                    tracingService.Trace(output.MT_API_OUT.data);
                    byte[] bytes = Convert.FromBase64String(output.MT_API_OUT.data);
                    string jsonData = Encoding.UTF8.GetString(bytes);
                    CmdDataResult cmdDataResult = JsonConvert.DeserializeObject<CmdDataResult>(jsonData);
                    Entity enCurrentAccount = service.Retrieve(target.LogicalName, target.Id, new ColumnSet(new string[1] { "bsd_customercodesap" }));
                    enCurrentAccount["bsd_customercodesap"] = cmdDataResult.it_return[0].partner;
                    service.Update(enCurrentAccount);
                }
            }
        }
        private Entity getCountryName(EntityReference country)
        {
            Entity en = service.Retrieve(country.LogicalName, country.Id, new ColumnSet(new string[2] { "bsd_countryname" , "bsd_id" }));
            return en; ;
        }
        private string getPrviceName(EntityReference provice)
        {
            Entity en = service.Retrieve(provice.LogicalName, provice.Id, new ColumnSet(new string[1] { "bsd_provincename" }));
            return en["bsd_provincename"].ToString();
        }
        private string getDistrictName(EntityReference district)
        {
            Entity en = service.Retrieve(district.LogicalName, district.Id, new ColumnSet(new string[1] { "new_name" }));
            return en["new_name"].ToString();
        }
        private string getWardName(EntityReference ward)
        {
            Entity en = service.Retrieve(ward.LogicalName, ward.Id, new ColumnSet(new string[1] { "bsd_name" }));
            return en["bsd_name"].ToString();
        }
    }
}
