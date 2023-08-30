using Action_SAP_RequestAPI.Configns;
using Action_SAP_RequestAPI.Models;
using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Action_SAP_RequestAPI
{
    public class Action_SAP_RequestAPI : IPlugin
    {
        IPluginExecutionContext context = null;
        ITracingService tracingService = null;

        string _cmdData = string.Empty;
        string _apiType = string.Empty;
        string _messId = string.Empty;
        string _key = string.Empty;

        Output output = new Output();
        public void Execute(IServiceProvider serviceProvider)
        {
            context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            Init();
        }
        private void Init()
        {
            tracingService.Trace("Start Input");
            if (context.InputParameters["cmddata"] == null || string.IsNullOrWhiteSpace(context.InputParameters["cmddata"].ToString())) throw new InvalidPluginExecutionException("CmdData trống.");
            if (context.InputParameters["apitype"] == null || string.IsNullOrWhiteSpace(context.InputParameters["apitype"].ToString())) throw new InvalidPluginExecutionException("ApiType trống.");
            if (context.InputParameters["key"] != null && !string.IsNullOrWhiteSpace(context.InputParameters["key"].ToString()))
            {
                this._key = context.InputParameters["key"].ToString();
            }
            _cmdData = context.InputParameters["cmddata"].ToString();
            _apiType = context.InputParameters["apitype"].ToString();
            tracingService.Trace("--End input");

            tracingService.Trace("Start add value");
            this._messId = Guid.NewGuid().ToString();
            RequestApi requestApi = new RequestApi();
            requestApi.ApiToken = context.InputParameters["apitoken"] == null || string.IsNullOrWhiteSpace(context.InputParameters["apitoken"].ToString()) ? Confign.apiToken : context.InputParameters["apitoken"].ToString();
            requestApi.ApiType = _apiType;
            requestApi.CmdData = _cmdData;
            requestApi.MessID = _messId;
            string _content = JsonConvert.SerializeObject(requestApi);
            tracingService.Trace("--End add value");

            using (var client = new HttpClient())
            {
                tracingService.Trace("Start Request");
                HttpContent fromBody = new StringContent(_content, Encoding.UTF8, "application/json");
                var byteArray = Encoding.ASCII.GetBytes(Confign.apiAuth);
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
                tracingService.Trace("Done khai bao client.");
                tracingService.Trace("Input: " + _content);

                var response = client.PostAsync(Confign.apiUrl, fromBody).Result;
                var content = response.Content.ReadAsStringAsync().Result;
                tracingService.Trace("--End Request");
                tracingService.Trace("Output: " + content);
                output = JsonConvert.DeserializeObject<Output>(content);

                CreateLog();
                if (output.MT_API_OUT.status != "S")
                {
                    throw new InvalidPluginExecutionException("SAP error: " + output.MT_API_OUT.message);
                }
                else
                {
                    string _output = JsonConvert.SerializeObject(output);
                    context.OutputParameters["output"] = _output;
                }
            }
        }
        private void CreateLog()
        {
            try
            {
                HttpClient client = new HttpClient();
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("client_id", Confign.clientId),
                    new KeyValuePair<string, string>("client_secret", Confign.clientSecret),
                    new KeyValuePair<string, string>("scope", $"{Confign.resource}/.default"),
                    new KeyValuePair<string, string>("grant_type", "client_credentials")
                });

                HttpResponseMessage tokenResponse = client.PostAsync(Confign.authorityUrl, content).Result;
                string token = "";

                if (tokenResponse.IsSuccessStatusCode)
                {
                    var tokenResult = tokenResponse.Content.ReadAsStringAsync().Result;
                    JObject tokenJson = JObject.Parse(tokenResult);
                    token = tokenJson.GetValue("access_token").ToString();
                }
                tracingService.Trace("Token: " + token);
                
                // Tạo JSON để tạo record mới
                string newRecordJson = addData();
                tracingService.Trace(newRecordJson);
                string entityName = "bsd_entitylogs";
                string apiPath = "/api/data/v9.2/";
                string createRecordUrl = $"{Confign.resource}{apiPath}{entityName}";

                // Gửi yêu cầu HTTP POST để tạo record
                HttpClient apiClient = new HttpClient();
                apiClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                StringContent apiContent = new StringContent(newRecordJson, Encoding.UTF8, "application/json");
                HttpResponseMessage response = apiClient.PostAsync(createRecordUrl, apiContent).Result;

                if (response.IsSuccessStatusCode)
                {
                }
                else
                {

                }
            }
            catch (InvalidPluginExecutionException ex)
            {

                throw new InvalidPluginExecutionException(ex.Message);
            }
        }
        private string addData()
        {
            tracingService.Trace("Start add data");
            IDictionary<string, object> data = new Dictionary<string, object>();
            data["bsd_messageid"] = this._messId;
            data["bsd_name"] = !string.IsNullOrWhiteSpace(this.output.MT_API_OUT.message) ? this.output.MT_API_OUT.message : null;
            data["bsd_chieutichhop"] = "IN";
            data["bsd_hethongtichhop"] = "SSM";
            data["bsd_manghiepvu"] = this._apiType;
            data["bsd_trangthaitichhop"] = this.output.MT_API_OUT.status;
            data["bsd_key1"] = this._key;
            data["bsd_key2"] = "";
            data["bsd_key3"] = "";
            data["bsd_key4"] = "";
            data["ownerid@odata.bind"] = "/systemusers(" + context.UserId + ")";
            data["bsd_requestdecode"] = convertBase64ToString(this._cmdData); 
            data["bsd_requestencode"] = this._cmdData;
            data["bsd_responsedecode"] = !string.IsNullOrWhiteSpace(this.output.MT_API_OUT.data) ? convertBase64ToString(this.output.MT_API_OUT.data) : null;
            data["bsd_responseencode"] = this.output.MT_API_OUT.data;
            tracingService.Trace("End add data");
            string _data = JsonConvert.SerializeObject(data);
            return _data;
        }
        private string convertBase64ToString(string base64)
        {
            var bytes = Convert.FromBase64String(base64);
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
