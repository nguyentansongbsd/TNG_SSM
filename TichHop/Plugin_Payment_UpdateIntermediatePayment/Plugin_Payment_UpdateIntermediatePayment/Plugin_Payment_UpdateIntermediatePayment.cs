using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin_Payment_UpdateIntermediatePayment
{
    public class Plugin_Payment_UpdateIntermediatePayment : IPlugin
    {
        IPluginExecutionContext context = null;
        IOrganizationServiceFactory factory = null;
        IOrganizationService service = null;
        ITracingService tracingService = null;
        Entity target = null;
        Entity _enPaymentDetail = null;
        public void Execute(IServiceProvider serviceProvider)
        {
            context = serviceProvider.GetService(typeof(IPluginExecutionContext)) as IPluginExecutionContext;
            factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            service = (IOrganizationService)factory.CreateOrganizationService(context.UserId);
            tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            if (context.Depth > 3) return;
            if (context.MessageName == "Update")
            {
                target = (Entity)context.InputParameters["Target"];
                _enPaymentDetail = service.Retrieve(target.LogicalName,target.Id,new ColumnSet(new string[] { "bsd_paymentcode", "bsd_paymentactualtime", "bsd_purchaser", "bsd_units", "bsd_paymentdescription", "bsd_name", "bsd_paymentmode", "bsd_amountpay" , "statuscode" }));
                if (((OptionSetValue)_enPaymentDetail["statuscode"]).Value != 100000003) return; // active va tu choi
                UpdateIntermediatePayment();
            }
        }
        private void UpdateIntermediatePayment()
        {
            var fetchXml = $@"<?xml version=""1.0"" encoding=""utf-16""?>
                <fetch>
                  <entity name=""bsd_intermediatepayment"">
                    <attribute name=""statuscode"" />
                    <filter>
                      <condition attribute=""bsd_paymentcode"" operator=""eq"" value=""{_enPaymentDetail["bsd_paymentcode"].ToString()}"" />
                      <condition attribute=""bsd_customer"" operator=""eq"" value=""{((EntityReference)_enPaymentDetail["bsd_purchaser"]).Id}"" />
                      <condition attribute=""bsd_units"" operator=""eq"" value=""{((EntityReference)_enPaymentDetail["bsd_units"]).Id}""/>
                      <condition attribute=""statuscode"" operator=""eq"" value=""{100000001}""/>
                    </filter>
                  </entity>
                </fetch>";
            var result = service.RetrieveMultiple(new FetchExpression(fetchXml));
            if (result != null && result.Entities.Count > 0)
            {
                decimal tongTien = 0;
                var fetchXmlPayment = $@"<?xml version=""1.0"" encoding=""utf-16""?>
                    <fetch>
                      <entity name=""bsd_payment"">
                        <attribute name=""bsd_paymentcode"" />
                        <attribute name=""bsd_amountpay"" />
                        <attribute name=""bsd_paymentdescription"" />
                        <attribute name=""bsd_paymentactualtime"" />
                        <filter>
                          <condition attribute=""bsd_paymentcode"" operator=""eq"" value=""{_enPaymentDetail["bsd_paymentcode"].ToString()}"" />
                          <condition attribute=""bsd_purchaser"" operator=""eq"" value=""{((EntityReference)_enPaymentDetail["bsd_purchaser"]).Id}"" />
                          <condition attribute=""bsd_units"" operator=""eq"" value=""{((EntityReference)_enPaymentDetail["bsd_units"]).Id}"" />
                          <condition attribute=""statuscode"" operator=""eq"" value=""{((OptionSetValue)_enPaymentDetail["statuscode"]).Value}"" />
                        </filter>
                      </entity>
                    </fetch>";
                var resultPayment = service.RetrieveMultiple(new FetchExpression(fetchXmlPayment));
                if (resultPayment != null && resultPayment.Entities.Count > 0)
                {
                    tongTien = resultPayment.Entities.Sum(x=>((Money)x["bsd_amountpay"]).Value);
                    foreach (var itemEnPaymentDetail in resultPayment.Entities)
                    {
                        Entity enPaymentDetail = new Entity(itemEnPaymentDetail.LogicalName,itemEnPaymentDetail.Id);
                        enPaymentDetail["bsd_paymentcode"] = itemEnPaymentDetail["bsd_paymentcode"];
                        enPaymentDetail["bsd_paymentdescription"] = itemEnPaymentDetail["bsd_paymentdescription"];
                        enPaymentDetail["bsd_paymentactualtime"] = itemEnPaymentDetail["bsd_paymentactualtime"];
                        service.Update(enPaymentDetail);
                    }
                }
                
                Entity enIntermediatePayment = new Entity(result.Entities[0].LogicalName, result.Entities[0].Id);
                enIntermediatePayment["bsd_amountpay"] = new Money(tongTien);
                enIntermediatePayment["bsd_paymentcode"] = this._enPaymentDetail["bsd_paymentcode"].ToString();
                enIntermediatePayment["bsd_name"] = this._enPaymentDetail["bsd_paymentdescription"].ToString();
                enIntermediatePayment["bsd_paymentdate"] = this._enPaymentDetail["bsd_paymentactualtime"];

                service.Update(enIntermediatePayment);
            }
            else
            {
                throw new InvalidPluginExecutionException("Không có phiếu thu với mã: " + this._enPaymentDetail["bsd_paymentcode"]);
            }
        }
    }
}
