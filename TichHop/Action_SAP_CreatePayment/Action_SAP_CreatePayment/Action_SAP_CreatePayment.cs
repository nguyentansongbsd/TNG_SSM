using Action_SAP_CreatePayment.Models;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Action_SAP_CreatePayment
{
    public class Action_SAP_CreatePayment : IPlugin
    {
        IPluginExecutionContext context = null;
        IOrganizationServiceFactory factory = null;
        IOrganizationService service = null;
        ITracingService tracingService = null;

        RequestData data = new RequestData();
        CmdData cmdData = new CmdData();
        Header header = new Header();

        string paymentCode { get; set; }
        List<Entity> listEnPayGroup { get; set; }
        List<Entity> listNguonThanhToan { get; set; }
        List<string> arrId = new List<string>();
        List<Entity> listEnPay = new List<Entity>();
        Guid IntermediatePaymentId { get; set; } = new Guid();
        string _enIntermediatePayment = "bsd_intermediatepayment";
        string _enNguonThanhToan = "bsd_nguonthanhtoan";
        string _customerCode = string.Empty;
        string _unitCode = string.Empty;
        string _trasactionCode = string.Empty;
        string _companyCode = string.Empty;
        string _projectCode = string.Empty;
        bool isSyncFromEnPayment = false;

        public void Execute(IServiceProvider serviceProvider)
        {
            context = serviceProvider.GetService(typeof(IPluginExecutionContext)) as IPluginExecutionContext;
            factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            service = (IOrganizationService)factory.CreateOrganizationService(context.UserId);
            tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            Init();
        }
        private void Init()
        {
            tracingService.Trace("Start Init");
            this.data.DATA = new List<DataPhieuThu>();
            if (context.InputParameters["ids"] != null && !string.IsNullOrWhiteSpace(context.InputParameters["ids"].ToString()))
            {
                string ids = context.InputParameters["ids"].ToString(); // List id can dong bo
                List<string> _arrId = ids.Split(',').ToList();
                arrId.AddRange(_arrId);
                tracingService.Trace("Done add id");
            }
            isSyncFromEnPayment = (bool)context.InputParameters["issyncfromenpayment"];
            if (isSyncFromEnPayment == false) // =false la nhan dong bo tu view cua entity thanh toan trung gian (intermediatepayment)
            {
                syncForIntermediatePayment(arrId);
            }
            else// nhan dong bo tu form cua man hinh thanh toan (Payment)
            {
                syncForPayment();
            }
            
            string _content = JsonConvert.SerializeObject(this.data);
            string crmDataBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(_content));
            tracingService.Trace("Content: " + _content);
            tracingService.Trace("Base64: " + crmDataBase64);
            tracingService.Trace("Done body");

            Output output = ApiRequest("300", crmDataBase64);
            if (output != null && output.MT_API_OUT.status != "S")
            {
                throw new InvalidPluginExecutionException("Error result data: " + output.MT_API_OUT.message);
            }
            else if (output == null)
            {
                throw new InvalidPluginExecutionException("Output null.");
            }
            else
            {
                if (isSyncFromEnPayment == false) // =false la nhan dong bo tu view cua entity thanh toan trung gian (intermediatepayment)
                {
                    foreach (var id in arrId)
                    {
                        updateIntermediatePayment(id);
                    }
                }
                else // nhan dong bo tu form cua man hinh thanh toan (Payment) va id la cua record entity Payment 
                {
                    foreach (var itemPayment in this.listEnPay)
                    {
                        Entity enIntermediatePayment = new Entity(((EntityReference)itemPayment["bsd_phieuthu"]).LogicalName, ((EntityReference)itemPayment["bsd_phieuthu"]).Id);
                        enIntermediatePayment["bsd_issynced"] = true;
                        enIntermediatePayment["statuscode"] = new OptionSetValue(100000002); // da dong bo sap
                        service.Update(enIntermediatePayment);

                        Entity enPaymentDetailUp = new Entity(itemPayment.LogicalName, itemPayment.Id);
                        enPaymentDetailUp["bsd_issynchedsap"] = true;
                        enPaymentDetailUp["statuscode"] = new OptionSetValue(100000005); // 100000005 == da dong bo sap
                        service.Update(enPaymentDetailUp);
                    }
                }
            }
        }
        private async Task getProject(Entity enPayment)
        {
            if (!enPayment.Contains("bsd_project")) throw new InvalidPluginExecutionException("Không có thông tin dự án.");
            Entity _enProject = service.Retrieve(((EntityReference)enPayment["bsd_project"]).LogicalName, ((EntityReference)enPayment["bsd_project"]).Id, new ColumnSet(new string[2] { "bsd_projectcode", "bsd_investor" }));
            this._projectCode = _enProject.Contains("bsd_projectcode") ? _enProject["bsd_projectcode"].ToString() : null;
            if (!_enProject.Contains("bsd_investor")) throw new InvalidPluginExecutionException("Không có thông tin chủ đầu tư.");
            Entity _enCompany = service.Retrieve(((EntityReference)_enProject["bsd_investor"]).LogicalName, ((EntityReference)_enProject["bsd_investor"]).Id, new ColumnSet(new string[1] { "bsd_companycodesap" }));
            this._companyCode = _enCompany.Contains("bsd_companycodesap") ? _enCompany["bsd_companycodesap"].ToString() : "1101";
        }
        private async Task getCustomerCode(Entity enPayment)
        {
            if (!enPayment.Contains("bsd_purchaser")) throw new InvalidPluginExecutionException("Không có thông tin khách hàng.");
            Entity _enCustomer = service.Retrieve(((EntityReference)enPayment["bsd_purchaser"]).LogicalName, ((EntityReference)enPayment["bsd_purchaser"]).Id, new ColumnSet(new string[1] { "bsd_customercodesap" }));
            this._customerCode = _enCustomer.Contains("bsd_customercodesap") ? _enCustomer["bsd_customercodesap"].ToString() : null;
        }
        private async Task getUnitCode(Entity enPayment)
        {
            if (!enPayment.Contains("bsd_units")) throw new InvalidPluginExecutionException("Không có thông tin sản phẩm.");
            Entity enUnit = service.Retrieve(((EntityReference)enPayment["bsd_units"]).LogicalName, ((EntityReference)enPayment["bsd_units"]).Id, new ColumnSet(new string[1] { "bsd_unitcodesap" }));
            this._unitCode = enUnit.Contains("bsd_unitcodesap") ? enUnit["bsd_unitcodesap"].ToString() : null;
        }
        private async Task getTransactionCode(Entity enPayment)
        {
            string entityReference = string.Empty;
            string atribute = string.Empty;
            if (enPayment.Contains("bsd_datcoc"))
            {
                entityReference = "bsd_datcoc";
                atribute = "bsd_datcoccodesap";
            }
            else if (enPayment.Contains("bsd_reservation"))
            {
                entityReference = "bsd_reservation";
                atribute = "bsd_contractcodesap";
            }
            else
            {
                entityReference = "bsd_optionentry";
                atribute = "bsd_contractcodesap";
            }
            Entity en = service.Retrieve(((EntityReference)enPayment[entityReference]).LogicalName, ((EntityReference)enPayment[entityReference]).Id, new ColumnSet(new string[1] { atribute }));
            this._trasactionCode = en.Contains(atribute) ? en[atribute].ToString() : null;
        }
        private void getListPaymentDetailNeedSynch(string paymentCode = null)
        {
            tracingService.Trace("Start get list payment");
            string conditionPaymentCode = !string.IsNullOrWhiteSpace(paymentCode) ? $@"<condition attribute=""bsd_paymentcode"" operator=""eq"" value=""{paymentCode}"" />" : null;

            string conditionIsSyncSAP = this.isSyncFromEnPayment == true && this.arrId.Count <= 0 ? @"<condition attribute=""bsd_issynchedsap"" operator=""eq"" value=""0"" />" : null;
            string conditionStatusCode = this.isSyncFromEnPayment == true && this.arrId.Count <= 0 ? @"<condition attribute=""statuscode"" operator=""eq"" value=""1"" />" : null;
            
            var fetchXml = $@"<?xml version=""1.0"" encoding=""utf-16""?>
                            <fetch>
                              <entity name=""bsd_payment"">
                                <filter>
                                  <condition attribute=""bsd_paymentcode"" operator=""not-null"" />
                                  <condition attribute=""statecode"" operator=""eq"" value=""0"" />
                                  <condition attribute=""ownerid"" operator=""eq"" value=""{context.UserId}"" />
                                  {conditionStatusCode}
                                  {conditionIsSyncSAP}
                                  {conditionPaymentCode}
                                </filter>
                              </entity>
                            </fetch>";
            var resuft = service.RetrieveMultiple(new FetchExpression(fetchXml));
            if (resuft != null && resuft.Entities.Count > 0)
            {
                listEnPay.AddRange(resuft.Entities);
                tracingService.Trace("End get list payment");
            }
            else
                throw new InvalidPluginExecutionException("Không có mã phiếu thu chi tiết " + paymentCode);
        }

        private void syncForIntermediatePayment(List<string> arrId)
        {
            foreach (string id in arrId)
            {
                tracingService.Trace("Start for");
                Entity _enCurrentIntermediatePayment = service.Retrieve("bsd_intermediatepayment", Guid.Parse(id), new ColumnSet(new string[] { "bsd_paymentcode", "statuscode" }));
                if (_enCurrentIntermediatePayment.Contains("bsd_paymentcode") && (((OptionSetValue)_enCurrentIntermediatePayment["statuscode"]).Value == 1 || ((OptionSetValue)_enCurrentIntermediatePayment["statuscode"]).Value == 100000001))
                {
                    getListPaymentDetailNeedSynch(_enCurrentIntermediatePayment["bsd_paymentcode"].ToString());

                    foreach (var item in this.listEnPay)
                    {
                        DataPhieuThu dataPhieuThu = addData(item);
                        this.data.DATA.Add(dataPhieuThu);
                    }
                    tracingService.Trace("End for");
                }
            }
        }
        private void syncForPayment()
        {
            if (this.arrId.Count > 0)
            {
                tracingService.Trace("Start find payment");
                List<string> listPaymentCode = new List<string>();
                foreach (var paymentId in this.arrId)
                {
                    Entity enPayment = service.Retrieve("bsd_payment",Guid.Parse(paymentId),new ColumnSet(new string[] { "bsd_paymentcode" }));
                    if (enPayment.Contains("bsd_paymentcode")) listPaymentCode.Add(enPayment["bsd_paymentcode"].ToString());
                }
                listPaymentCode = listPaymentCode.GroupBy(x=>x).Select(y=>y.First()).ToList();
                foreach (var itemPaymentCode in listPaymentCode)
                {
                    getListPaymentDetailNeedSynch(itemPaymentCode);
                }
                tracingService.Trace("End find payment");
            }
            else
            {
                getListPaymentDetailNeedSynch();
            }
            
            foreach (var item in this.listEnPay)
            {
                DataPhieuThu dataPhieuThu = addData(item);
                this.data.DATA.Add(dataPhieuThu);
            }
            tracingService.Trace("End for");
        }
        private DataPhieuThu addData(Entity item)
        {
            DataPhieuThu dataPhieuThu = new DataPhieuThu();
            Task.WaitAll(
                getProject(item),
                getCustomerCode(item),
                getUnitCode(item),
                getTransactionCode(item)
                );
            tracingService.Trace("Start add value");
            dataPhieuThu.ID_TYPE = "PT01";
            dataPhieuThu.LOAI_THU_TIEN = getContractType(item);
            dataPhieuThu.CONG_TY = _companyCode;
            dataPhieuThu.DU_AN = _projectCode;
            dataPhieuThu.CHUNGTU_SSM = item.Contains("bsd_paymentcode") ? item["bsd_paymentcode"].ToString() : null;
            dataPhieuThu.NGAY_PHIEU = item.Contains("bsd_paymentactualtime") ? ((DateTime)item["bsd_paymentactualtime"]).ToString("yyyy-MM-dd").Replace("-", "") : null;
            dataPhieuThu.ND_PHIEU = item.Contains("bsd_paymentdescription") ? item["bsd_paymentdescription"].ToString() : null;
            dataPhieuThu.ND_THU_TIEN = item.Contains("bsd_name") ? item["bsd_name"].ToString() : null;
            dataPhieuThu.MA_SAN_PHAM = _unitCode;
            dataPhieuThu.MA_KHACH_HANG = _customerCode;
            dataPhieuThu.MA_HOP_DONG = _trasactionCode;
            dataPhieuThu.LOAI_THANH_TOAN = item.Contains("bsd_paymenttype") ? item.FormattedValues["bsd_paymenttype"].ToString() : null;
            dataPhieuThu.LOAI_HOP_DONG = item.Contains("bsd_typeoptionentry") ? item.FormattedValues["bsd_typeoptionentry"].ToString() : null;
            dataPhieuThu.SO_TIEN = item.Contains("bsd_amountpay") ? Math.Round(((Money)item["bsd_amountpay"]).Value).ToString() : null;
            dataPhieuThu.LOAI_TIEN = "VND";
            tracingService.Trace("End add value");
            return dataPhieuThu;
        }
        private void updateIntermediatePayment(string id)
        {
            Entity enpayment = new Entity(_enIntermediatePayment, Guid.Parse(id));
            enpayment["bsd_issynced"] = true;
            enpayment["statuscode"] = new OptionSetValue(100000002);
            service.Update(enpayment);
            updatePaymentDetail(id);
        }
        private void updatePaymentDetail(string paymentId)
        {
            Entity enPayment = service.Retrieve(_enIntermediatePayment,Guid.Parse(paymentId),new ColumnSet(new string[] { "bsd_paymentcode" }));
            if (!enPayment.Contains("bsd_paymentcode")) return;
            var fetchXml = $@"<?xml version=""1.0"" encoding=""utf-16""?>
                <fetch>
                  <entity name=""bsd_payment"">
                    <attribute name=""statuscode"" />
                    <filter>
                      <condition attribute=""bsd_paymentcode"" operator=""eq"" value=""{enPayment["bsd_paymentcode"]}"" />
                    </filter>
                  </entity>
                </fetch>";
            var result = service.RetrieveMultiple(new FetchExpression(fetchXml));
            foreach (var item in result.Entities)
            {
                Entity enPaymentDetail = new Entity(item.LogicalName, item.Id);
                enPaymentDetail["statuscode"] = new OptionSetValue(100000005); // 100000005 == da dong bo sap
                service.Update(enPaymentDetail);
            }
        }
        private string getContractType(Entity enPaymentDetail)
        {
            var fetchXml = $@"<?xml version=""1.0"" encoding=""utf-16""?>
                <fetch>
                  <entity name=""bsd_paymenttype"">
                    <attribute name=""bsd_paymenttypecodesap"" />
                    <filter>
                      <condition attribute=""bsd_paymenttypecode"" operator=""eq"" value=""{((OptionSetValue)enPaymentDetail["bsd_paymenttype"]).Value}"" />
                    </filter>
                  </entity>
                </fetch>";
            var result = service.RetrieveMultiple(new FetchExpression(fetchXml));
            if (result == null || result.Entities.Count <= 0) return null;
            string paymentType = result.Entities[0].Contains("bsd_paymenttypecodesap") ? result.Entities[0]["bsd_paymenttypecodesap"].ToString() : null;
            return paymentType;
        }
        private Output ApiRequest(string apiType,string cmdData)
        {
            OrganizationRequest request = new OrganizationRequest("bsd_Action_SAP_RequestAPI");
            request["apitype"] = apiType;
            request["cmddata"] = cmdData;
            OrganizationResponse response = service.Execute(request);
            
            if (response.Results.Contains("output"))
            {
                string result = response.Results["output"].ToString();
                return JsonConvert.DeserializeObject<Output>(result);
            }
            return null;
        }
    }
}
