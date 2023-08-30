using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Action_SAP_CreateAccount
{
    public class Action_SAP_CreateAccount : IPlugin
    {
        IOrganizationService service = null;
        IOrganizationServiceFactory factory = null;
        ITracingService tracingService = null;
        public void Execute(IServiceProvider serviceProvider)
        {
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            service = factory.CreateOrganizationService(context.UserId);

            string input = context.InputParameters["input"].ToString();

            try
            {
                Account responseActions = JsonConvert.DeserializeObject<Account>(input);
                DateTime? firstdate = null;
                DateTime? issuedon = null;
                DateTime? authorizationtime = null;
                if (!string.IsNullOrWhiteSpace(responseActions.bsd_firstdate))
                {
                    firstdate = DateTime.Parse(responseActions.bsd_firstdate);
                }
                if (!string.IsNullOrWhiteSpace(responseActions.bsd_issuedon))
                {
                    issuedon = DateTime.Parse(responseActions.bsd_issuedon);
                }
                if (!string.IsNullOrWhiteSpace(responseActions.bsd_authorizationtime))
                {
                    authorizationtime = DateTime.Parse(responseActions.bsd_authorizationtime);
                }
                
                Entity enAccount = new Entity("account"); 
                enAccount["bsd_customercodesap"] = responseActions.bsd_customercodesap;
                enAccount["bsd_companycodesap"] = responseActions.bsd_companycodesap;
                enAccount["bsd_name"] = responseActions.bsd_name;
                enAccount["bsd_groupcustomer"] = new EntityReference("bsd_customergroup", Guid.Parse(responseActions.bsd_groupcustomer));
                enAccount["bsd_operationscope"] = new OptionSetValue(int.Parse(responseActions.bsd_operationscope));
                enAccount["bsd_registrationcode"] = responseActions.bsd_registrationcode;
                enAccount["bsd_firstdate"] = firstdate;
                enAccount["bsd_issuedon"] = issuedon;
                enAccount["bsd_placeofissue"] = responseActions.bsd_placeofissue;
                enAccount["telephone1"] = responseActions.telephone1;
                enAccount["emailaddress1"] = responseActions.emailaddress1;
                enAccount["primarycontactid"] = new EntityReference("contact",Guid.Parse(responseActions.primarycontactid));
                enAccount["bsd_authorizationtime"] = authorizationtime;
                enAccount["bsd_nation"] = new EntityReference("bsd_country", Guid.Parse(responseActions.bsd_nation));
                enAccount["bsd_province"] = new EntityReference("new_province", Guid.Parse(responseActions.bsd_province));
                enAccount["bsd_district"] = new EntityReference("new_district", Guid.Parse(responseActions.bsd_district));
                enAccount["bsd_ward2"] = new EntityReference("bsd_ward", Guid.Parse(responseActions.bsd_ward2));
                enAccount["bsd_housenumberstreet"] = responseActions.bsd_housenumberstreet;
                enAccount["bsd_address"] = responseActions.bsd_address;
                enAccount["bsd_permanentnation"] = new EntityReference("bsd_country", Guid.Parse(responseActions.bsd_permanentnation));
                enAccount["bsd_permanentprovince"] = new EntityReference("new_province", Guid.Parse(responseActions.bsd_permanentprovince));
                enAccount["bsd_permanentdistrict"] = new EntityReference("new_district", Guid.Parse(responseActions.bsd_permanentdistrict)); 
                enAccount["bsd_permanentward"] = new EntityReference("bsd_ward", Guid.Parse(responseActions.bsd_permanentward)); 
                enAccount["bsd_permanenthousenumberstreetwardvn"] = responseActions.bsd_permanenthousenumberstreetwardvn;
                enAccount["bsd_permanentaddress1"] = responseActions.bsd_permanentaddress1;

                var id = service.Create(enAccount);

                context.OutputParameters["accountid"] = id.ToString();
            }
            catch (Exception ex)
            {
                throw new InvalidPluginExecutionException(ex.Message);
            }
        }
    }
    public class Account
    {
        public string bsd_customercodesap { get; set; }
        public string bsd_companycodesap { get; set; }
        public string bsd_name { get; set; }
        public string bsd_groupcustomer { get; set; }
        public string bsd_operationscope { get; set; }
        public string bsd_registrationcode { get; set; }
        public string bsd_firstdate { get; set; }
        public string bsd_issuedon { get; set; }
        public string bsd_placeofissue { get; set; }
        public string telephone1 { get; set; }
        public string emailaddress1 { get; set; }
        public string primarycontactid { get; set; }
        public string bsd_authorizationtime { get; set; }
        public string bsd_nation { get; set; }
        public string bsd_province { get; set; }
        public string bsd_district { get; set; }
        public string bsd_ward2 { get; set; }
        public string bsd_housenumberstreet { get; set; }
        public string bsd_address { get; set; }
        public string bsd_permanentnation { get; set; }
        public string bsd_permanentprovince { get; set; }
        public string bsd_permanentdistrict { get; set; }
        public string bsd_permanentward { get; set; }
        public string bsd_permanenthousenumberstreetwardvn { get; set; }
        public string bsd_permanentaddress1 { get; set; }
    }
}
