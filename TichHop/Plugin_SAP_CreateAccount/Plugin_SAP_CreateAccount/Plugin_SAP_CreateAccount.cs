using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json;
using Plugin_SAP_CreateAccount.Configns;
using Plugin_SAP_CreateAccount.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Plugin_SAP_CreateAccount
{
    public class Plugin_SAP_CreateAccount : IPlugin
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
                body.ApiType = "202";
            }
            else if (context.MessageName == "Update")
            {
                tracingService.Trace("Start Update");
                thamSo.PARTNER = en["bsd_customercodesap"].ToString();
                body.ApiType = "203";
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
            tracingService.Trace("Apitype: " + body.ApiType);
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
            string customerGroupCode = en.Contains("bsd_groupcustomer") ? service.Retrieve(((EntityReference)en["bsd_groupcustomer"]).LogicalName,((EntityReference)en["bsd_groupcustomer"]).Id,new ColumnSet(new string[1] { "bsd_customergroupcodesap" }))["bsd_customergroupcodesap"].ToString() : "N008";
            
            thamSo.BU_TYPE = "2"; // Account
            thamSo.RLTYP = "000000";
            thamSo.BU_GROUP = customerGroupCode;

            data.THAM_SO = thamSo;
            tracingService.Trace("Done tham so");
        }
        private async Task addAddress()
        {
            address.TITLE_MEDI = "0003";
            address.NAME_ORG1 = en.Contains("bsd_accountnameother") ? en["bsd_accountnameother"].ToString() : null;
            address.NAME_ORG2 = en.Contains("bsd_name") ? en["bsd_name"].ToString() : null;
            address.BU_SORT1_TXT = en["bsd_customercode"].ToString();
            address.STREET = en.Contains("bsd_permanentward") ? getWardName((EntityReference)en["bsd_permanentward"]) : null;
            address.STREET2 = null;
            address.STREET3 = "";
            address.HOUSE_NUM1 = en.Contains("bsd_permanenthousenumberstreetwardvn") ? en["bsd_permanenthousenumberstreetwardvn"].ToString() : null;
            address.CITY1 = getPrviceName((EntityReference)en["bsd_permanentprovince"]);
            address.CITY2 = getDistrictName((EntityReference)en["bsd_permanentdistrict"]);

            //address.STREET_P = en.Contains("bsd_permanentward") ? getWardName((EntityReference)en["bsd_permanentward"]) : null;
            //address.STR_SUPPL1 = "";
            //address.STR_SUPPL2 = "";
            //address.STR_SUPPL3 = "";
            //address.LOCATION = "";
            //address.HOUSE_NUM1_P = en["bsd_permanenthousenumberstreetwardvn"].ToString();
            //address.CITY1_P = getDistrictName((EntityReference)en["bsd_permanentdistrict"]);
            //address.CITY2_P = getPrviceName((EntityReference)en["bsd_permanentprovince"]);
            address.COUNTRY = "VN";
            address.LANGUCORR = "EN";

            address.TEL_NUMBER1 = en["telephone1"].ToString();
            address.TEL_NUMBER2 = en.Contains("bsd_phone2") ? en["bsd_phone2"].ToString() : null;
            address.MOB_NUMBER1 = en.Contains("bsd_phone3") ? en["bsd_phone3"].ToString() : null;
            address.MOB_NUMBER2 = en.Contains("bsd_phone4") ? en["bsd_phone4"].ToString() : null;
            address.MOB_NUMBER3 = en.Contains("bsd_phone5") ? en["bsd_phone5"].ToString() : null;
            address.FAX_NUMBER = en.Contains("fax") ? en["fax"].ToString() : null;
            address.SMTP_ADDR = en["emailaddress1"].ToString();
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
            Entity enprimariContact = getPrimariContact((EntityReference)en["primarycontactid"]);
            tracingService.Trace("Done enprimariContact");
            tracingService.Trace("So cmnd: " + ((OptionSetValue)enprimariContact["bsd_typeofidcardlead"]).Value);
            if (((OptionSetValue)enprimariContact["bsd_typeofidcardlead"]).Value == 100000000) // Identity Card Number (ID)
            {
                tracingService.Trace("Start CMND");
                soCMND = enprimariContact.Contains("bsd_identitycardnumber") ? enprimariContact["bsd_identitycardnumber"].ToString() : null;
                ngayCMND = enprimariContact.Contains("bsd_dategrant") ? ((DateTime)enprimariContact["bsd_dategrant"]).ToString("dd.MM.yyyy") : null;
                noiCapCMND = enprimariContact.Contains("bsd_placeofissue") ? enprimariContact["bsd_placeofissue"].ToString() : null;
            }
            else if (((OptionSetValue)enprimariContact["bsd_typeofidcardlead"]).Value == 100000001) // Identity Card
            {
                tracingService.Trace("Start CCCD");
                soCMND = enprimariContact.Contains("bsd_identitycard") ? enprimariContact["bsd_identitycard"].ToString() : null;
                ngayCMND = enprimariContact.Contains("bsd_identitycarddategrant") ? ((DateTime)enprimariContact["bsd_identitycarddategrant"]).ToString("dd.MM.yyyy") : null;
                noiCapCMND = enprimariContact.Contains("bsd_placeofissueidentitycardoption") ? enprimariContact.FormattedValues["bsd_placeofissueidentitycardoption"].ToString() : null;
            }
            else if (((OptionSetValue)enprimariContact["bsd_typeofidcardlead"]).Value == 100000002) // Passport
            {
                tracingService.Trace("Start Passport");
                soCMND = enprimariContact.Contains("bsd_passport") ? enprimariContact["bsd_passport"].ToString() : null;
                ngayCMND = enprimariContact.Contains("bsd_issuedonpassport") ? ((DateTime)enprimariContact["bsd_issuedonpassport"]).ToString("dd.MM.yyyy") : null;
                noiCapCMND = enprimariContact.Contains("bsd_placeofissuepassport") ? enprimariContact["bsd_placeofissuepassport"].ToString() : null;
            }
            else if (((OptionSetValue)enprimariContact["bsd_typeofidcardlead"]).Value == 100000003) // Mã số khác
            {
                tracingService.Trace("Start Mã số khác");
                soCMND = enprimariContact.Contains("bsd_othercode") ? enprimariContact["bsd_othercode"].ToString() : null;
                ngayCMND = enprimariContact.Contains("bsd_date") ? ((DateTime)enprimariContact["bsd_date"]).ToString("dd.MM.yyyy") : null;
                noiCapCMND = enprimariContact.Contains("bsd_place") ? enprimariContact["bsd_place"].ToString() : null;
            }
            tracingService.Trace("Done CMND");
            Entity enMandatorySecondary = getMandatorySecondary();
            
            identification.ID_TYPE = "CRM002";
            identification.IDNUMBER = en.Contains("bsd_registrationcode") ? en["bsd_registrationcode"].ToString() : null;
            
            identification.ZEILE1 = enprimariContact.Contains("fullname") ? enprimariContact["fullname"].ToString() : null;
            identification.ZEILE2 = en.Contains("bsd_jobtitle") ? en["bsd_jobtitle"].ToString() : null;
            identification.ZEILE3 = ((OptionSetValue)enprimariContact["gendercode"]).Value == 1 ? "Name" : "Nữ";
            identification.ZEILE4 = enprimariContact.Contains("birthdate") ? ((DateTime)enprimariContact["birthdate"]).ToString("dd.MM.yyyy") : null;
            identification.ZEILE5 = soCMND;
            identification.ZEILE6 = ngayCMND;
            identification.ZEILE7 = noiCapCMND;
            identification.ZEILE8 = enprimariContact.Contains("bsd_permanentaddress1") ? enprimariContact["bsd_permanentaddress1"].ToString() : null;
            identification.ZEILE9 = enprimariContact.Contains("bsd_contactaddress") ? enprimariContact["bsd_contactaddress"].ToString() : null;
            identification.ZEILE10 = enprimariContact.Contains("mobilephone") ? enprimariContact["mobilephone"].ToString() : null;
            identification.ZEILE11 = enprimariContact.Contains("emailaddress1") ? enprimariContact["emailaddress1"].ToString() : null;
            identification.ZEILE12 = en.Contains("bsd_authorizationletter1") ? en["bsd_authorizationletter1"].ToString() : null;
            identification.ZEILE13 = en.Contains("bsd_authorizationtime") ? RetrieveLocalTimeFromUTCTime((DateTime)en["bsd_authorizationtime"], service).ToString("dd.MM.yyyy") : null;
            identification.ZEILE14 = en.Contains("bsd_authorizationtimeto") ? RetrieveLocalTimeFromUTCTime((DateTime)en["bsd_authorizationtimeto"], service).ToString("dd.MM.yyyy") : null;
            
            identification.ZEILE15 = enMandatorySecondary != null && enMandatorySecondary.Contains("soCongChung") ? ((AliasedValue)enMandatorySecondary["soCongChung"]).Value.ToString() : null;
            identification.ZEILE16 = enMandatorySecondary != null && enMandatorySecondary.Contains("noiCongChung") ? ((AliasedValue)enMandatorySecondary["noiCongChung"]).Value.ToString() : null;
            identification.ZEILE17 = null; //"182 Phạm Hùng";
            identification.ZEILE18 = null; // "Mr Cường";
            identification.ZEILE19 = enMandatorySecondary != null && enMandatorySecondary.Contains("nguoiNhanUQ") ? ((AliasedValue)enMandatorySecondary["nguoiNhanUQ"]).Value.ToString() : null; ;
            identification.ZEILE20 = null; //"02.02.2021";

            data.IDENTIFICATION = identification;
            tracingService.Trace("Done identification");
        }
        private async Task addOrder()
        {
            order.VKORG = "1101";
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
            accountmanagement.BUKRS = "1101";
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
                else if (output.MT_API_OUT.status == "S" &&  !string.IsNullOrEmpty(output.MT_API_OUT.data))
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
        private string getCountryName(EntityReference country)
        {
            Entity en = service.Retrieve(country.LogicalName, country.Id, new ColumnSet(new string[1] { "bsd_countryname" }));
            return en["bsd_countryname"].ToString();
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
        private Entity getPrimariContact(EntityReference primaricontact)
        {
            Entity en = service.Retrieve(primaricontact.LogicalName, primaricontact.Id, new ColumnSet(new string[20] { "fullname", "gendercode", "birthdate","bsd_typeofidcardlead",
                                    "bsd_identitycardnumber", "bsd_dategrant", "bsd_placeofissue",
                                    "bsd_identitycard", "bsd_identitycarddategrant", "bsd_placeofissueidentitycardoption",
                                    "bsd_passport", "bsd_issuedonpassport", "bsd_placeofissuepassport",
                                    "bsd_othercode", "bsd_date", "bsd_place",
                                    "bsd_contactaddress", "bsd_permanentaddress1", "emailaddress1", "mobilephone" }));
            return en;
        }
        private Entity getMandatorySecondary()
        {
            tracingService.Trace("Start NUQ");
            var fetchXml = $@"<?xml version=""1.0"" encoding=""utf-16""?>
                <fetch>
                  <entity name=""account"">
                    <filter>
                      <condition attribute=""bsd_mandatorysecondary"" operator=""not-null"" />
                      <condition attribute=""accountid"" operator=""eq"" value=""{en.Id}"" />
                    </filter>
                    <link-entity name=""bsd_mandatorysecondary"" from=""bsd_developeraccount"" to=""accountid"" alias=""nguoiUQ"">
                      <attribute name=""bsd_issuedby"" alias=""noiCongChung"" />
                      <attribute name=""bsd_powerofattorney"" alias=""soCongChung"" />
                      <link-entity name=""contact"" from=""contactid"" to=""bsd_contact"" alias=""contact"">
                        <attribute name=""fullname"" alias=""nguoiNhanUQ"" />
                      </link-entity>
                    </link-entity>
                  </entity>
                </fetch>";
            var result = service.RetrieveMultiple(new FetchExpression(fetchXml));
            tracingService.Trace("List NUQ: " + result.Entities.Count);
            if (result == null || result.Entities.Count <= 0) return null;
            return result.Entities[0];
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
