using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json;
using Plugin_SAP_CreatePayment.Configns;
using Plugin_SAP_CreatePayment.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Plugin_SAP_CreatePayment
{
    public class Plugin_SAP_CreatePayment : IPlugin
    {
        IPluginExecutionContext context = null;
        IOrganizationServiceFactory factory = null;
        IOrganizationService service = null;
        ITracingService tracingService = null;
        Entity target = null;
        public void Execute(IServiceProvider serviceProvider)
        {
            context = serviceProvider.GetService(typeof(IPluginExecutionContext)) as IPluginExecutionContext;
            factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            service = (IOrganizationService)factory.CreateOrganizationService(context.UserId);
            tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            if (context.Depth > 4) return;
            target = (Entity)context.InputParameters["Target"];
            Entity en = service.Retrieve(target.LogicalName, target.Id, new ColumnSet(true));

            CmdData cmdData = new CmdData();
            Header header = new Header();
            Item item1 = new Item();
            Item item2 = new Item();

            header.bldat = en.Contains("bsd_paymentactualtime") ? ((DateTime)en["bsd_paymentactualtime"]).ToString("yyyy/MM/dd").Replace("/", "") : null;
            header.budat = en.Contains("bsd_fundstransferdate") ? ((DateTime)en["bsd_fundstransferdate"]).ToString("yyyy/MM/dd").Replace("/", "") : null;
            header.blart = "SA";
            header.bukrs = "1101";
            header.waers = "VND";
            header.bktxt = en.Contains("bsd_name") ? en["bsd_name"].ToString() : null;
            header.api_type = "01";
            header.xblnr = en.Contains("bsd_paymentnumber") ? en["bsd_paymentnumber"].ToString() : null;
            tracingService.Trace("Done Header");

            item1.NEWBS = "40";
            item1.NEWKO = "1121011000";
            item1.CF_ID = "01";
            item1.PRCTR = "D0001";
            item1.ZPCHD = "1100000863";
            item1.ZMASP = "600002609";
            item1.ZDTTN = "H001";
            item1.ZDTTC = "";
            item1.ZVACODE = "xxx";
            item1.WRBTR = en.Contains("bsd_amountpay") ? ((Money)en["bsd_amountpay"]).Value.ToString() : null ;
            item1.SGTXT = "Items 2";
            item1.ZEILE = "";
            tracingService.Trace("Done item1");

            item2.NEWBS = "11";
            item2.NEWKO = "1000000165";
            item2.HKONT = "1311100000";
            item2.PRCTR = "D0001";
            item2.ZPCHD = "1100000863";
            item2.ZMASP = "600002609";
            item2.ZDTTN = "H001";
            item2.ZDTTC = "";
            item2.ZVACODE = "xxx";
            item2.WRBTR = en.Contains("bsd_amountpay") ? ((Money)en["bsd_amountpay"]).Value.ToString() : null;
            item2.ZTERM = "T002";
            item2.ZFBDT = "20221104";
            item2.SGTXT = "Items 2";
            item2.ZEILE = "ssssstxx";
            tracingService.Trace("Done item2");

            if (context.MessageName == "Create")
            {
                cmdData.header = header;
                cmdData.item = new List<Item>();
                cmdData.item.Add(item1);
                cmdData.item.Add(item2);

                string _content = JsonConvert.SerializeObject(cmdData);
                string crmDataBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(_content));
                //throw new InvalidPluginExecutionException(crmDataBase64);
                RequestApi body = new RequestApi();
                body.ApiToken = Confign.apiToken;
                body.ApiType = "600";
                body.CmdData = crmDataBase64;
                tracingService.Trace("Done body");

                ApiRequest(body);
            }
        }

        private void ApiRequest(RequestApi body)
        {
            using (var client = new HttpClient())
            {
                string objContent = JsonConvert.SerializeObject(body);
                HttpContent fromBody = new StringContent(objContent, Encoding.UTF8, "application/json");
                var byteArray = Encoding.ASCII.GetBytes(Confign.apiAuth);
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                var response = client.PostAsync(Confign.apiUrl, fromBody).Result;
                var content = response.Content.ReadAsStringAsync().Result;
                Output output = JsonConvert.DeserializeObject<Output>(content);
                tracingService.Trace("Done POST");
                if (output.MT_API_OUT.status != "S")
                {
                    throw new InvalidPluginExecutionException("Error result data: " + output.MT_API_OUT.message);
                }
                else
                {
                    tracingService.Trace(target.LogicalName);
                    Entity enpayment = service.Retrieve(target.LogicalName, target.Id, new ColumnSet(new string[1] { "bsd_paymentcodesap" }));
                    string paymentcode = output.MT_API_OUT.message.Split(':')[1].Replace(" ", "");
                    enpayment["bsd_paymentcodesap"] = paymentcode;
                    service.Update(enpayment);
                }
            }
        }
    }
}
