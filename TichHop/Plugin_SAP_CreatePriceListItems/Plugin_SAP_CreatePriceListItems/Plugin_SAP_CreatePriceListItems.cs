using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json;
using Plugin_SAP_CreatePriceListItems.Configns;
using Plugin_SAP_CreatePriceListItems.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Plugin_SAP_CreatePriceListItems
{
    public class Plugin_SAP_CreatePriceListItems : IPlugin
    {
        IPluginExecutionContext context = null;
        IOrganizationServiceFactory factory = null;
        IOrganizationService service = null;
        ITracingService tracingService = null;
        Entity target = null;

        CmdData cmdData = new CmdData();
        ZDETAIL zDETAIL1 = new ZDETAIL();
        ZDETAIL zDETAIL2 = new ZDETAIL();
        RequestApi body = new RequestApi();
        public void Execute(IServiceProvider serviceProvider)
        {
            context = serviceProvider.GetService(typeof(IPluginExecutionContext)) as IPluginExecutionContext;
            factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            service = (IOrganizationService)factory.CreateOrganizationService(context.UserId);
            tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            if (context.Depth > 4) return;
            target = (Entity)context.InputParameters["Target"];
            Entity en = service.Retrieve(target.LogicalName, target.Id, new ColumnSet(true));

            Entity enPriceList = service.Retrieve(((EntityReference)en["bsd_pricelist"]).LogicalName, ((EntityReference)en["bsd_pricelist"]).Id,new ColumnSet(new string[4] { "bsd_pricelistcode", "name", "begindate", "enddate" }));

            body.ApiToken = Confign.apiToken;
            cmdData.MATNR = enPriceList.Contains("bsd_pricelistcode") ? enPriceList["bsd_pricelistcode"].ToString() : null ;
            cmdData.MAKTX = enPriceList.Contains("name") ? enPriceList["name"].ToString() : null;
            cmdData.KSCHL = "ZC11";
            cmdData.VKORG = "1101";
            cmdData.ZWERKS = "1001";
            cmdData.ZDETAIL = new List<ZDETAIL>();

            zDETAIL1.MATNR = enPriceList.Contains("bsd_pricelistcode") ? enPriceList["bsd_pricelistcode"].ToString() : null;
            zDETAIL1.ID_BGLS = "123456789";
            zDETAIL1.MAKTX = en.Contains("bsd_name") ? en["bsd_name"].ToString() : null;
            zDETAIL1.KBETR = "5000";
            zDETAIL1.DATAB = enPriceList.Contains("begindate") ? ((DateTime)enPriceList["begindate"]).ToString("yyyy.MM.dd").Replace(".","") : "20220101";
            zDETAIL1.DATBI = enPriceList.Contains("enddate") ? ((DateTime)enPriceList["enddate"]).ToString("yyyy.MM.dd").Replace(".", "") : "20220101";
            zDETAIL1.LTX01 = "ABC123";
            cmdData.ZDETAIL.Add(zDETAIL1);

            zDETAIL2.MATNR = enPriceList.Contains("bsd_pricelistcode") ? enPriceList["bsd_pricelistcode"].ToString() : null;
            zDETAIL2.ID_BGLS = "123457";
            zDETAIL2.MAKTX = en.Contains("bsd_name") ? en["bsd_name"].ToString() : null;
            zDETAIL2.KBETR = "6000";
            zDETAIL2.DATAB = enPriceList.Contains("begindate") ? ((DateTime)enPriceList["begindate"]).ToString("yyyy.MM.dd").Replace(".", "") : "20220101";
            zDETAIL2.DATBI = enPriceList.Contains("enddate") ? ((DateTime)enPriceList["enddate"]).ToString("yyyy.MM.dd").Replace(".", "") : "20220601";
            zDETAIL2.LTX01 = "ABC123456";
            cmdData.ZDETAIL.Add(zDETAIL2);


            if (context.MessageName == "Create")
            {
                string _content = JsonConvert.SerializeObject(cmdData);
                string crmDataBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(_content));
                body.ApiType = "601";
                body.CmdData = crmDataBase64;
                tracingService.Trace("Content: " + _content);
                tracingService.Trace("Base64: " + crmDataBase64);

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
                    Entity enPriceListItem = service.Retrieve(target.LogicalName, target.Id, new ColumnSet(new string[1] { "bsd_pricelistcodesap" }));
                    enPriceListItem["bsd_pricelistcodesap"] = output.MT_API_OUT.message;
                    service.Update(enPriceListItem);
                }
            }
        }
    }
}
