using Action_SAP_SSM_CreateBankAccount.Models;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Action_SAP_SSM_CreateBankAccount
{
    public class Action_SAP_SSM_CreateBankAccount : IPlugin
    {
        IOrganizationService service = null;
        IOrganizationServiceFactory factory = null;
        ITracingService tracingService = null;

        string id = string.Empty;
        BankAccount responseActions = new BankAccount();
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
                tracingService.Trace("Input: " + input);
                responseActions = JsonConvert.DeserializeObject<BankAccount>(input);
                if ((!string.IsNullOrWhiteSpace(responseActions.isUpdate) && responseActions.isUpdate != "true") || !string.IsNullOrWhiteSpace(responseActions.isUnActive) && responseActions.isUnActive != "true") throw new InvalidPluginExecutionException("Vui lòng nhập đúng giá trị.");
                
                if (responseActions.isUpdate == "true")
                {
                    tracingService.Trace("Start Update");
                    InitUpdate();
                }
                else if (responseActions.isUnActive == "true")
                {
                    tracingService.Trace("Start Unactive");
                    InitUnActive();
                }
                else
                {
                    tracingService.Trace("Start Add");
                    InitAdd();
                }
                
                context.OutputParameters["bankaccountid"] = id;
            }
            catch (Exception ex)
            {
                throw new InvalidPluginExecutionException(ex.Message);
            }
        }
        private void InitAdd()
        {
            tracingService.Trace("Start check null");
            if (string.IsNullOrWhiteSpace(responseActions.bsd_name)) throw new InvalidPluginExecutionException("Số tài khoản không được trống.");
            if (string.IsNullOrWhiteSpace(responseActions.bsd_cashaccount)) throw new InvalidPluginExecutionException("Tài khoản hạch toán tiền không được trống.");
            if (string.IsNullOrWhiteSpace(responseActions.bsd_investor)) throw new InvalidPluginExecutionException("Chủ đầu tư không được trống.");
            if (string.IsNullOrWhiteSpace(responseActions.bsd_bank)) throw new InvalidPluginExecutionException("Ngân hàng không được trống.");
            tracingService.Trace("Done check null");

            tracingService.Trace("Check Double");
            bool isDouble = checkDuplicate(responseActions.bsd_name);
            if (isDouble) throw new InvalidPluginExecutionException("Số tài khoản " + responseActions.bsd_name + " đã có trên hệ thống SSM.");

            Entity enBankAccount = new Entity("bsd_applybankaccount");
            Entity enCustomer = getCustomer(responseActions.bsd_investor);
            Entity enBank = getBank(responseActions.bsd_bank);

            tracingService.Trace("Start add value");
            enBankAccount["bsd_name"] = responseActions.bsd_name;
            enBankAccount["bsd_cashaccount"] = responseActions.bsd_cashaccount;
            enBankAccount["bsd_investor"] = new EntityReference(enCustomer.LogicalName, enCustomer.Id);
            enBankAccount["bsd_bank"] = new EntityReference(enBank.LogicalName, enBank.Id);
            enBankAccount["bsd_bankbranch"] = responseActions.bsd_bankbranch;
            tracingService.Trace("Done add value");

            id = service.Create(enBankAccount).ToString();
        }
        private void InitUpdate()
        {
            tracingService.Trace(responseActions.bsd_name);
            if (string.IsNullOrWhiteSpace(responseActions.bsd_name)) throw new InvalidPluginExecutionException("Số tài khoản không được trống.");

            Entity enBankAccount = getBankAccount(responseActions.bsd_name);
            if (enBankAccount.Contains("statecode") && ((OptionSetValue)enBankAccount["statecode"]).Value == 1) throw new InvalidPluginExecutionException("Tài khoản ngân hàng đã vô hiệu hóa. Không thể cập nhật.");
            Entity enBankAccountUp = new Entity(enBankAccount.LogicalName, enBankAccount.Id);

            tracingService.Trace("Start add value");
            if (!string.IsNullOrWhiteSpace(responseActions.bsd_cashaccount))
            {
                enBankAccountUp["bsd_cashaccount"] = responseActions.bsd_cashaccount;
            }
            if (!string.IsNullOrWhiteSpace(responseActions.bsd_investor))
            {
                Entity enCustomer = getCustomer(responseActions.bsd_investor);
                enBankAccountUp["bsd_investor"] = new EntityReference(enCustomer.LogicalName, enCustomer.Id);
            }
            if (!string.IsNullOrWhiteSpace(responseActions.bsd_bank))
            {
                Entity enBank = getBank(responseActions.bsd_bank);
                enBankAccountUp["bsd_bank"] = new EntityReference(enBank.LogicalName, enBank.Id);
            }
            if (!string.IsNullOrWhiteSpace(responseActions.bsd_bankbranch))
            {
                enBankAccountUp["bsd_bankbranch"] = responseActions.bsd_bankbranch;
            }
            tracingService.Trace("Done add value");
            this.id = enBankAccount.Id.ToString();

            service.Update(enBankAccountUp);
        }
        private void InitUnActive()
        {
            tracingService.Trace(responseActions.bsd_name);
            if (string.IsNullOrWhiteSpace(responseActions.bsd_name)) throw new InvalidPluginExecutionException("Số tài khoản không được trống.");

            Entity enBankAccount = getBankAccount(responseActions.bsd_name);
            Entity enBankAccountUp = new Entity(enBankAccount.LogicalName, enBankAccount.Id);
            tracingService.Trace("Start add value");
            enBankAccountUp["statecode"] = new OptionSetValue(1);
            enBankAccountUp["statuscode"] = new OptionSetValue(2);
            tracingService.Trace("Done add value");
            this.id = enBankAccount.Id.ToString();

            service.Update(enBankAccountUp);
        }
        private Entity getCustomer(string companycode)
        {
            tracingService.Trace("Start customerId");
            var fetchXml = $@"<?xml version=""1.0"" encoding=""utf-16""?>
                <fetch top=""1"">
                  <entity name=""account"">
                    <attribute name=""accountid"" />
                    <filter>
                      <condition attribute=""bsd_companycodesap"" operator=""eq"" value=""{companycode}"" />
                    </filter>
                  </entity>
                </fetch>";
            var result = service.RetrieveMultiple(new FetchExpression(fetchXml));
            if (result == null || result.Entities.Count <= 0) throw new InvalidPluginExecutionException("Chủ đầu tư chưa có trên hệ thống SSM.");
            tracingService.Trace("Done customerId");
            return result.Entities[0];
        }
        private Entity getBank(string bank)
        {
            tracingService.Trace("Start Bank");
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
            if (result == null || result.Entities.Count <= 0) throw new InvalidPluginExecutionException("Ngân hàng "+ bank + " chưa có trên hệ thống SSM.");
            tracingService.Trace("Done Bank");
            return result.Entities[0];
        }
        private Entity getBankAccount(string bankAccount)
        {
            tracingService.Trace("Start Bank");
            var fetchXml = $@"<?xml version=""1.0"" encoding=""utf-16""?>
                <fetch top=""1"">
                  <entity name=""bsd_applybankaccount"">
                    <attribute name=""bsd_applybankaccountid"" />
                    <attribute name=""statecode"" />
                    <filter>
                      <condition attribute=""bsd_name"" operator=""eq"" value=""{bankAccount}"" />
                    </filter>
                  </entity>
                </fetch>";
            var result = service.RetrieveMultiple(new FetchExpression(fetchXml));
            if (result == null || result.Entities.Count <= 0) throw new InvalidPluginExecutionException("Tài khoản ngân hàng " + bankAccount + " chưa có trên hệ thống SSM.");
            tracingService.Trace("Done Bank");
            return result.Entities[0];
        }
        private bool checkDuplicate(string bankAccount)
        {
            var fetchXml = $@"<?xml version=""1.0"" encoding=""utf-16""?>
                <fetch top=""1"">
                  <entity name=""bsd_applybankaccount"">
                    <attribute name=""bsd_applybankaccountid"" />
                    <filter>
                      <condition attribute=""bsd_name"" operator=""eq"" value=""{bankAccount}"" />
                    </filter>
                  </entity>
                </fetch>";
            var result = service.RetrieveMultiple(new FetchExpression(fetchXml));
            if (result == null || result.Entities.Count <= 0) return false;
            return true;
        }
    }
}
