using Action_SAP_SSM_CreateBank.Models;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Action_SAP_SSM_CreateBank
{
    public class Action_SAP_SSM_CreateBank : IPlugin
    {
        IOrganizationService service = null;
        IOrganizationServiceFactory factory = null;
        ITracingService tracingService = null;

        string id = string.Empty;
        Bank responseActions = new Bank();
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
                responseActions = JsonConvert.DeserializeObject<Bank>(input);
                if (responseActions.isUpdate == "true")
                {
                    tracingService.Trace("Update");
                    InitUpdate();
                }
                else
                {
                    tracingService.Trace("Add");
                    InitAdd();
                }
                
                context.OutputParameters["bankid"] = id;
            }
            catch (Exception ex)
            {
                throw new InvalidPluginExecutionException(ex.Message);
            }
        }
        private void InitAdd()
        {
            tracingService.Trace("Start check null");
            if (string.IsNullOrWhiteSpace(responseActions.bsd_abbreviation)) throw new InvalidPluginExecutionException("Tên viết tắt không được trống.");
            if (string.IsNullOrWhiteSpace(responseActions.bsd_name)) throw new InvalidPluginExecutionException("Tên ngân hàng không được trống.");
            if (string.IsNullOrWhiteSpace(responseActions.bsd_address)) throw new InvalidPluginExecutionException("Địa chỉ không được trống.");
            tracingService.Trace("Done check null");

            tracingService.Trace("Check Double");
            bool isDouble = checkDuplicate(responseActions.bsd_abbreviation);
            if (isDouble) throw new InvalidPluginExecutionException(responseActions.bsd_abbreviation + " đã có trên hệ thống SSM.");

            tracingService.Trace("Start add value");
            Entity enBank = new Entity("bsd_bank");
            enBank["bsd_abbreviation"] = responseActions.bsd_abbreviation;
            enBank["bsd_name"] = responseActions.bsd_name;
            enBank["bsd_othername"] = responseActions.bsd_othername;
            enBank["bsd_typeofbank"] = new OptionSetValue(1);
            enBank["bsd_taxcode"] = responseActions.bsd_taxcode;
            enBank["bsd_chartercapital"] = new Money(responseActions.bsd_chartercapital);
            enBank["bsd_swiftcode"] = responseActions.bsd_swiftcode;
            enBank["bsd_refcode"] = responseActions.bsd_refcode;
            enBank["bsd_phone"] = responseActions.bsd_phone;
            enBank["bsd_email"] = responseActions.bsd_email;
            enBank["bsd_fax"] = responseActions.bsd_fax;
            enBank["bsd_website"] = responseActions.bsd_website;
            enBank["bsd_address"] = responseActions.bsd_address;
            enBank["bsd_addressother"] = responseActions.bsd_addressother;
            tracingService.Trace("Done add value");

            this.id = service.Create(enBank).ToString();
        }
        private void InitUpdate()
        {
            if (string.IsNullOrWhiteSpace(responseActions.bsd_abbreviation)) throw new InvalidPluginExecutionException("Số tài khoản không được trống.");

            Entity enBank = getBank(responseActions.bsd_abbreviation);
            Entity enBankUp = new Entity(enBank.LogicalName, enBank.Id);
            tracingService.Trace("Start add value");

            if (!string.IsNullOrWhiteSpace(responseActions.bsd_name))
            {
                enBankUp["bsd_name"] = responseActions.bsd_name;
            }
            if (!string.IsNullOrWhiteSpace(responseActions.bsd_othername))
            {
                enBankUp["bsd_othername"] = responseActions.bsd_othername;
            }
            if (!string.IsNullOrWhiteSpace(responseActions.bsd_taxcode))
            {
                enBankUp["bsd_taxcode"] = responseActions.bsd_taxcode;
            }
            if (((Money)enBank["bsd_chartercapital"]).Value != responseActions.bsd_chartercapital)
            {
                enBankUp["bsd_chartercapital"] = new Money(responseActions.bsd_chartercapital);
            }
            if (!string.IsNullOrWhiteSpace(responseActions.bsd_swiftcode))
            {
                enBankUp["bsd_swiftcode"] = responseActions.bsd_swiftcode;
            }
            if (!string.IsNullOrWhiteSpace(responseActions.bsd_refcode))
            {
                enBankUp["bsd_refcode"] = responseActions.bsd_refcode;
            }
            if (!string.IsNullOrWhiteSpace(responseActions.bsd_phone))
            {
                enBankUp["bsd_phone"] = responseActions.bsd_phone;
            }
            if (!string.IsNullOrWhiteSpace(responseActions.bsd_email))
            {
                enBankUp["bsd_email"] = responseActions.bsd_email;
            }
            if (!string.IsNullOrWhiteSpace(responseActions.bsd_fax))
            {
                enBankUp["bsd_fax"] = responseActions.bsd_fax;
            }
            if (!string.IsNullOrWhiteSpace(responseActions.bsd_website))
            {
                enBankUp["bsd_website"] = responseActions.bsd_website;
            }
            if (!string.IsNullOrWhiteSpace(responseActions.bsd_address))
            {
                enBankUp["bsd_address"] = responseActions.bsd_address;
            }
            if (!string.IsNullOrWhiteSpace(responseActions.bsd_addressother))
            {
                enBankUp["bsd_addressother"] = responseActions.bsd_addressother;
            }
            tracingService.Trace("Done add value");
            this.id = enBank.Id.ToString();

            service.Update(enBankUp);
        }
        private bool checkDuplicate(string bank)
        {
            var fetchXml = $@"<?xml version=""1.0"" encoding=""utf-16""?>
                <fetch top=""1"">
                  <entity name=""bsd_bank"">
                    <attribute name=""bsd_bankid"" />
                    <filter>
                      <condition attribute=""bsd_abbreviation"" operator=""eq"" value=""{bank}"" />
                    </filter>
                  </entity>
                </fetch>";
            var result = service.RetrieveMultiple(new FetchExpression(fetchXml));
            if (result == null || result.Entities.Count <= 0) return false;
            return true;
        }
        private Entity getBank(string bank)
        {
            var fetchXml = $@"<?xml version=""1.0"" encoding=""utf-16""?>
                <fetch top=""1"">
                  <entity name=""bsd_bank"">
                    <attribute name=""bsd_bankid"" />
                    <attribute name=""bsd_chartercapital"" />
                    <filter>
                      <condition attribute=""bsd_abbreviation"" operator=""eq"" value=""{bank}"" />
                    </filter>
                  </entity>
                </fetch>";
            var result = service.RetrieveMultiple(new FetchExpression(fetchXml));
            if (result == null || result.Entities.Count <= 0) throw new InvalidPluginExecutionException("Ngân hàng " + bank + " chưa có trên hệ thống SSM.");
            tracingService.Trace("Done Bank");
            return result.Entities[0];
        }
    }
}
