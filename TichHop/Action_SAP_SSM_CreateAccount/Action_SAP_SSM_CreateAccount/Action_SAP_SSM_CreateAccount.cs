using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Action_SAP_SSM_CreateAccount
{
    public class Action_SAP_SSM_CreateAccount : IPlugin
    {
        IOrganizationService service = null;
        IOrganizationServiceFactory factory = null;
        ITracingService tracingService = null;

        string id = string.Empty;
        Account responseActions = new Account();
        public void Execute(IServiceProvider serviceProvider)
        {
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            service = factory.CreateOrganizationService(context.UserId);

            if (context.InputParameters["input"] == null || string.IsNullOrWhiteSpace(context.InputParameters["input"].ToString())) throw new InvalidPluginExecutionException("Vui lòng nhập input");
            string input = context.InputParameters["input"].ToString();

            try
            {
                responseActions = JsonConvert.DeserializeObject<Account>(input);

                if (responseActions.isUpdate == "true")
                {
                    tracingService.Trace("Start Update");
                    InitUpdate();
                }
                else
                {
                    tracingService.Trace("Start Add");
                    InitAdd();
                }
                
                context.OutputParameters["accountid"] = id.ToString();
            }
            catch (Exception ex)
            {
                throw new InvalidPluginExecutionException(ex.Message);
            }
        }
        private void InitAdd()
        {
            if (string.IsNullOrWhiteSpace(responseActions.bsd_companycodesap)) throw new InvalidPluginExecutionException("Company code không được trống.");
            if (string.IsNullOrWhiteSpace(responseActions.bsd_name)) throw new InvalidPluginExecutionException("Tên khách hàng không được trống.");
            if (string.IsNullOrWhiteSpace(responseActions.bsd_address)) throw new InvalidPluginExecutionException("Địa chỉ liên hệ không được trống.");
            if (string.IsNullOrWhiteSpace(responseActions.bsd_permanentaddress1)) throw new InvalidPluginExecutionException("Địa chỉ thường trú không được trống.");

            tracingService.Trace("Check Double");
            bool isDouble = checkDuplicate(responseActions.bsd_companycodesap);
            if (isDouble) throw new InvalidPluginExecutionException("Chủ đầu tư " + responseActions.bsd_companycodesap + " đã có trên hệ thống SSM.");

            tracingService.Trace("Start add value");
            Entity enAccount = new Entity("account");
            enAccount["bsd_vatregistrationnumber"] = responseActions.bsd_vatregistrationnumber;
            enAccount["bsd_companycodesap"] = responseActions.bsd_companycodesap;
            enAccount["bsd_name"] = responseActions.bsd_name;
            enAccount["bsd_address"] = responseActions.bsd_address;
            enAccount["bsd_permanentaddress1"] = responseActions.bsd_permanentaddress1;
            enAccount["bsd_accounttype"] = new OptionSetValue(100000001);
            tracingService.Trace("Done add value");

            id = service.Create(enAccount).ToString();
        }
        private void InitUpdate()
        {
            if (string.IsNullOrWhiteSpace(responseActions.bsd_companycodesap)) throw new InvalidPluginExecutionException("Company code không được trống.");
            Entity enAccount = getAccount(responseActions.bsd_companycodesap);
            Entity enAccountUp = new Entity(enAccount.LogicalName, enAccount.Id);

            tracingService.Trace("Start add value");
            if (!string.IsNullOrWhiteSpace(responseActions.bsd_vatregistrationnumber))
            {
                enAccountUp["bsd_vatregistrationnumber"] = responseActions.bsd_vatregistrationnumber;
            }
            if (!string.IsNullOrWhiteSpace(responseActions.bsd_name))
            {
                enAccountUp["bsd_name"] = responseActions.bsd_name;
            }
            if (!string.IsNullOrWhiteSpace(responseActions.bsd_address))
            {
                enAccountUp["bsd_address"] = responseActions.bsd_address;
            }
            if (!string.IsNullOrWhiteSpace(responseActions.bsd_permanentaddress1))
            {
                enAccountUp["bsd_permanentaddress1"] = responseActions.bsd_permanentaddress1;
            }
            tracingService.Trace("Done add value");
            this.id = enAccount.Id.ToString();

            service.Update(enAccountUp);
        }
        private Entity getAccount(string companyCode)
        {
            tracingService.Trace("Start Account");
            var fetchXml = $@"<?xml version=""1.0"" encoding=""utf-16""?>
                <fetch>
                  <entity name=""account"">
                    <attribute name=""accountid"" />
                    <filter>
                      <condition attribute=""bsd_companycodesap"" operator=""eq"" value=""{companyCode}"" />
                    </filter>
                  </entity>
                </fetch>";
            var result = service.RetrieveMultiple(new FetchExpression(fetchXml));
            if (result == null || result.Entities.Count <= 0) throw new InvalidPluginExecutionException("Chủ đầu tư với mã " + companyCode + " chưa có trên hệ thống SSM.");
            tracingService.Trace("Done Account");
            return result.Entities[0];
        }
        private bool checkDuplicate(string companyCode)
        {
            var fetchXml = $@"<?xml version=""1.0"" encoding=""utf-16""?>
                <fetch>
                  <entity name=""account"">
                    <attribute name=""accountid"" />
                    <filter>
                      <condition attribute=""bsd_companycodesap"" operator=""eq"" value=""{companyCode}"" />
                    </filter>
                  </entity>
                </fetch>";
            var result = service.RetrieveMultiple(new FetchExpression(fetchXml));
            if (result == null || result.Entities.Count <= 0) return false;
            return true;
        }
    }
    public class Account
    {
        public string bsd_vatregistrationnumber { get; set; }
        public string bsd_companycodesap { get; set; }
        public string bsd_name { get; set; }
        public string bsd_address { get; set; }
        public string bsd_permanentaddress1 { get; set; }
        public string isUpdate { get; set; }
    }
}
