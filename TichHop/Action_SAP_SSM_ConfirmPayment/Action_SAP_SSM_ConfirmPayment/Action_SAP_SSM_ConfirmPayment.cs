using Action_SAP_SSM_ConfirmPayment.Models;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.ServiceModel;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Action_SAP_SSM_ConfirmPayment
{
    public class Action_SAP_SSM_ConfirmPayment : IPlugin
    {
        IPluginExecutionContext context = null;
        IOrganizationService service = null;
        IOrganizationServiceFactory factory = null;
        ITracingService tracingService = null;
        IOrganizationServiceFactory serviceFactory = null;

        CmdData cmdData = new CmdData();
        Header header = new Header();
        RequestApi body = new RequestApi();
        RequestData requestData = new RequestData();

        Entity enPhieuThu = null;
        List<Entity> listChiTietPhieuThu = new List<Entity>(); 

        string paymentCode = string.Empty;
        string resultpaymentCode = string.Empty;
        string isConfirm = string.Empty; // 1:confirm, 0: reject
        bool isResend = false;
        string paymentCodeSAP = string.Empty;
        string reasonReject = string.Empty;
        string message = string.Empty;
        string _customerCode = string.Empty;
        string _unitCode = string.Empty;
        string _trasactionCode = string.Empty;
        string _cashaccount = string.Empty;
        string _companyCode = string.Empty;
        string _projectCode = string.Empty;

        public void Execute(IServiceProvider serviceProvider)
        {
            context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            service = factory.CreateOrganizationService(context.UserId);

            if (context.InputParameters["paymentcode"] == null ||string.IsNullOrWhiteSpace(context.InputParameters["paymentcode"].ToString())) throw new InvalidPluginExecutionException("Mã phiếu thu trống.");
            if (context.InputParameters["isresend"] == null && (context.InputParameters["isconfirm"] == null || string.IsNullOrWhiteSpace(context.InputParameters["isconfirm"].ToString()))) throw new InvalidPluginExecutionException("Thiếu thông tin dữ liệu.");
            //if ((bool)context.InputParameters["isresend"] == false && context.InputParameters["isconfirm"] != null) throw new InvalidPluginExecutionException("Vui lòng truyền đúng tham số.");
            if (context.InputParameters["isresend"] != null && (bool)context.InputParameters["isresend"] == true)
            {
                if (context.InputParameters["paymentcodesap"] == null || string.IsNullOrWhiteSpace(context.InputParameters["paymentcodesap"].ToString())) throw new InvalidPluginExecutionException("Mã phiếu thu SAP trống.");
                paymentCodeSAP = context.InputParameters["paymentcodesap"].ToString();
                isResend = (bool)context.InputParameters["isresend"];
            }
            if (context.InputParameters["isconfirm"] != null && !string.IsNullOrWhiteSpace(context.InputParameters["isconfirm"].ToString()))
            {
                isConfirm = context.InputParameters["isconfirm"].ToString();
            }
            paymentCode = context.InputParameters["paymentcode"].ToString();
            
            Init();
        }
        private void Init()
        {
            if(this.isConfirm == "1" && this.isResend == false) // Confirm
            {
                ConfirmPayment();
            }
            else if(this.isConfirm == "0" && this.isResend == false) // Reject
            {
                if (context.InputParameters["reason"] == null || string.IsNullOrWhiteSpace(context.InputParameters["reason"].ToString())) throw new InvalidPluginExecutionException("Vui lòng nhập lý do.");
                reasonReject = context.InputParameters["reason"].ToString();

                RejectPayment();
            }
            else if (this.isResend == true)
            {
                updatePaymentCodeSAP();
            }
            else
            {
                this.message = "Vui lòng nhập 1 hoặc 0. Để xác nhận/từ chối phiếu thu.";
            }
            
            context.OutputParameters["message"] = message;
        }
        private void ConfirmPayment()
        {
            tracingService.Trace("Start confirm");
            requestData.DATA = new List<DataPhieuThu>();
            cmdData.item = new List<Item>();
            bool isAsync = false;

            getPhieuThu();
            Task.WaitAll(
                getChiTietPhieuThu()
                );
            if (this.enPhieuThu == null || this.listChiTietPhieuThu.Count <= 0) return;
            Task.WaitAll(
                    getProject(this.enPhieuThu),
                    getCustomerCode(this.enPhieuThu),
                    getUnitCode(this.enPhieuThu),
                    getTransactionCode(this.enPhieuThu),
                    getTKTien(this.enPhieuThu)
                    );
            if (string.IsNullOrWhiteSpace(this._cashaccount)) throw new InvalidPluginExecutionException("Giao dịch chưa có tài khoản ngân hàng");
            List<DataPhieuThu> dataPhieuThus = new List<DataPhieuThu>();
            for (int i = 0; i < listChiTietPhieuThu.Count; i++)
            {
                tracingService.Trace("Loai PT: " + ((OptionSetValue)listChiTietPhieuThu[i]["bsd_paymenttype"]).Value + "--- " + listChiTietPhieuThu[i].Id);
                OrganizationRequest request = new OrganizationRequest("bsd_Action_ConfirmPayment");
                request["Target"] = new EntityReference("bsd_payment", listChiTietPhieuThu[i].Id);
                OrganizationResponse response = this.service.Execute(request);

                tracingService.Trace("Xong xác nhận thanh toán");
                if (response.Results.Contains("id") && response.Results != null)
                {
                    tracingService.Trace("vao lay đơt");
                    List<string> ids = response.Results["id"].ToString().Split(';').ToList();
                    if (ids.Count <= 1)  // id rỗng cắt chuỗi thì trong list tồn tại 1 giá trị
                    {
                        DataPhieuThu _dataPhieuThu = addDataPhieuThu(listChiTietPhieuThu[i], null, false, ((Money)listChiTietPhieuThu[i]["bsd_amountpay"]).Value);
                        dataPhieuThus.Add(_dataPhieuThu);
                    }
                    else
                    {
                        var fetchXml = $@"<?xml version=""1.0"" encoding=""utf-16""?>
                        <fetch aggregate=""true"">
                          <entity name=""bsd_phieutinhlai"">
                            <attribute name=""bsd_name"" alias=""dot"" groupby=""true"" />
                            <attribute name=""bsd_sotienthanhtoan"" alias=""sotien"" aggregate=""sum"" />
                            <filter>
                              <condition attribute=""bsd_payment"" operator=""eq"" value=""{listChiTietPhieuThu[i].Id}""/>
                            </filter>
                            <link-entity name=""bsd_paymentschemedetail"" from=""bsd_paymentschemedetailid"" to=""bsd_installment"" alias=""dottt"">
                              <attribute name=""bsd_ordernumber"" alias=""stt"" groupby=""true"" />
                              <attribute name=""bsd_lastinstallment"" alias=""lastinstallment"" groupby=""true"" />
                            </link-entity>
                          </entity>
                        </fetch>";
                        var result = service.RetrieveMultiple(new FetchExpression(fetchXml));
                        if (result != null && result.Entities.Count > 0)
                        {
                            foreach (var item in result.Entities)
                            {
                                DataPhieuThu _dataPhieuThuDoTran = addDataPhieuThu(listChiTietPhieuThu[i], (int)((AliasedValue)item["stt"]).Value, (bool)((AliasedValue)item["lastinstallment"]).Value, ((Money)((AliasedValue)item["sotien"]).Value).Value);
                                dataPhieuThus.Add(_dataPhieuThuDoTran);
                            }
                        }
                        tracingService.Trace("Id đợt thanh toan đổ tràn: " + response.Results["id"]);
                    }
                }
                
                tracingService.Trace("Loai PT: " + ((OptionSetValue)listChiTietPhieuThu[i]["bsd_paymenttype"]).Value + "--- " + listChiTietPhieuThu[i].Id);
                tracingService.Trace("End response");
            }

            string sapCode = string.Empty;
            if (dataPhieuThus.Count > 0)
            {
                requestData.DATA = dataPhieuThus;
                string _content = JsonConvert.SerializeObject(requestData);
                string crmDataBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(_content));
                tracingService.Trace("Content: " + _content);
                tracingService.Trace("Base64: " + crmDataBase64);
                tracingService.Trace("Done body");
                
                Output _outPut = ApiRequest("301", crmDataBase64 ,dataPhieuThus[0].CHUNGTU_SSM);
                if (_outPut != null && _outPut.MT_API_OUT.status == "S")
                {
                    sapCode = "- SAP Code: " + resultpaymentCode;
                }
            }
            
            tracingService.Trace("End confirm" + isAsync);
            Task.WaitAll(
                        updatePayment(this.enPhieuThu),
                        updateChiTietPhieuThu()
                        );
            this.message = "Xác nhận thành công " + sapCode;
        }
        private void RejectPayment()
        {
            getPhieuThu();
            Task.WaitAll(
                getChiTietPhieuThu()
                );

            Task.WaitAll(
                updateReasonPayment(),
                updateReasonPaymentDetail()
                );

            this.message = "Từ chối thành công " + this.paymentCode;
        }
        private void updatePaymentCodeSAP()
        {
            this.resultpaymentCode = this.paymentCodeSAP;
            getPhieuThu();
            Task.WaitAll(
                getChiTietPhieuThu()
                );
            Task.WaitAll(
                updatePayment(this.enPhieuThu,true),
                updateChiTietPhieuThu()
                );
            this.message = "Thành công ";
        }

        private DataPhieuThu addDataPhieuThu(Entity enPaymentDetail,int? dotThanhToan, bool lastInstallment, decimal soTien)
        {
            tracingService.Trace("Start add data phieu thu");
            DataPhieuThu dataPhieuThu = new DataPhieuThu();
            dataPhieuThu.ID_TYPE = "PT01";
            dataPhieuThu.LOAI_THU_TIEN = getContractType(enPaymentDetail);
            dataPhieuThu.CHUNGTU_SSM = enPaymentDetail.Contains("bsd_paymentcode") ? enPaymentDetail["bsd_paymentcode"].ToString() : null;
            dataPhieuThu.CONG_TY = this._companyCode;
            dataPhieuThu.DU_AN = this._projectCode;
            dataPhieuThu.NGAY_PHIEU = enPaymentDetail.Contains("bsd_paymentactualtime") ? ((DateTime)enPaymentDetail["bsd_paymentactualtime"]).ToLocalTime().ToString("yyyy-MM-dd").Replace("-","") : null;
            dataPhieuThu.TK_TGNH = this._cashaccount;
            tracingService.Trace("1");
            dataPhieuThu.ND_PHIEU = enPaymentDetail.Contains("bsd_paymentdescription") ? enPaymentDetail["bsd_paymentdescription"].ToString() : null;
            dataPhieuThu.ND_THU_TIEN = enPaymentDetail.Contains("bsd_name") ? enPaymentDetail["bsd_name"].ToString() : null;
            dataPhieuThu.MA_HOP_DONG = this._trasactionCode;
            dataPhieuThu.MA_SAN_PHAM = this._unitCode;
            dataPhieuThu.MA_KHACH_HANG = this._customerCode;
            tracingService.Trace("2");
            dataPhieuThu.DOT_THANHTOAN = dotThanhToan.HasValue ? "H" + dotThanhToan.Value.ToString("D3") : null;
            dataPhieuThu.DOT_THANHTOAN_E = lastInstallment == true ? "X" : null;
            tracingService.Trace("3");
            dataPhieuThu.SO_TIEN = Math.Round(soTien).ToString();
            dataPhieuThu.LOAI_TIEN = "VND";
            tracingService.Trace("4");
            tracingService.Trace(dataPhieuThu.DOT_THANHTOAN);
            tracingService.Trace("End add data phieu thu");
            return dataPhieuThu;
        }
        
        private Output ApiRequest(string apiType, string cmdData,string key)
        {
            OrganizationRequest request = new OrganizationRequest("bsd_Action_SAP_RequestAPI");
            request["apitype"] = apiType;
            request["cmddata"] = cmdData;
            request["key"] = key;
            OrganizationResponse response = service.Execute(request);

            if (response.Results.Contains("output"))
            {
                string result = response.Results["output"].ToString();
                Output output = JsonConvert.DeserializeObject<Output>(result);
                if (output.MT_API_OUT.status == "S")
                {
                    string pattern = @"\d+";
                    MatchCollection matches = Regex.Matches(output.MT_API_OUT.message, pattern);
                    var numbers = matches.Cast<Match>().Select(match => match.Value);
                    resultpaymentCode = string.Join("/", numbers);
                    return output;
                }
                else return null;
            }
            else return null;
        }
        private void getPhieuThu()
        {
            tracingService.Trace("Start get phieu thu");
            var fetchXml = $@"<?xml version=""1.0"" encoding=""utf-16""?>
                <fetch>
                  <entity name=""bsd_intermediatepayment"">
                    <filter>
                      <condition attribute=""bsd_paymentcode"" operator=""eq"" value=""{paymentCode}"" />
                    </filter>
                  </entity>
                </fetch>";
            var result = service.RetrieveMultiple(new FetchExpression(fetchXml));
            if (result == null || result.Entities.Count <= 0)
            {
                throw new InvalidPluginExecutionException("Không tìm thấy phiếu thu với mã: " + paymentCode);
            }
            else if(result.Entities[0].Contains("statuscode") && ((OptionSetValue)result.Entities[0]["statuscode"]).Value == 100000001)
            {
                throw new InvalidPluginExecutionException("Phiếu thu " + paymentCode + " đã được từ chối.");
            }
            else if ((bool)context.InputParameters["isresend"] == false && result.Entities[0].Contains("statuscode") && ((OptionSetValue)result.Entities[0]["statuscode"]).Value == 100000000)
            {
                throw new InvalidPluginExecutionException("Phiếu thu " + paymentCode + " đã được xác nhận.");
            }
            else
            {
                this.enPhieuThu = result.Entities[0];
                tracingService.Trace("End get phieu thu");
            }
        }
        private async Task getChiTietPhieuThu()
        {
            tracingService.Trace("Start get chi tiet phieu thu");
            var fetchXml = $@"<?xml version=""1.0"" encoding=""utf-16""?>
                <fetch>
                  <entity name=""bsd_payment"">
                    <filter>
                      <condition attribute=""bsd_paymentcode"" operator=""eq"" value=""{paymentCode}"" />
                    </filter>
                  </entity>
                </fetch>";
            var result = service.RetrieveMultiple(new FetchExpression(fetchXml));
            if (result == null || result.Entities.Count <= 0)
            {
                message = "Không tìm thấy phiếu thu với mã: " + paymentCode;
                return;
            }
            this.listChiTietPhieuThu = result.Entities.ToList();
            tracingService.Trace("End get chi tiet phieu thu");
        }
        private async Task getProject(Entity enPayment)
        {
            tracingService.Trace("Start du an");
            if (!enPayment.Contains("bsd_project")) throw new InvalidPluginExecutionException("Không có thông tin dự án.");
            Entity _enProject = service.Retrieve(((EntityReference)enPayment["bsd_project"]).LogicalName, ((EntityReference)enPayment["bsd_project"]).Id, new ColumnSet(new string[2] { "bsd_projectcode", "bsd_investor" }));
            this._projectCode = _enProject.Contains("bsd_projectcode") ? _enProject["bsd_projectcode"].ToString() : null;
            if (!_enProject.Contains("bsd_investor")) throw new InvalidPluginExecutionException("Không có thông tin chủ đầu tư.");
            Entity _enCompany = service.Retrieve(((EntityReference)_enProject["bsd_investor"]).LogicalName, ((EntityReference)_enProject["bsd_investor"]).Id, new ColumnSet(new string[1] { "bsd_companycodesap" }));
            this._companyCode = _enCompany.Contains("bsd_companycodesap") ? _enCompany["bsd_companycodesap"].ToString() : "1101";
            tracingService.Trace("End du an");
        }
        private async Task getCustomerCode(Entity enPayment)
        {
            tracingService.Trace("Start get customer");
            Entity _enCustomer = service.Retrieve(((EntityReference)enPayment["bsd_customer"]).LogicalName, ((EntityReference)enPayment["bsd_customer"]).Id, new ColumnSet(new string[1] { "bsd_customercodesap" }));
            this._customerCode = _enCustomer.Contains("bsd_customercodesap") ? _enCustomer["bsd_customercodesap"].ToString() : null;
            tracingService.Trace("End get customer: " + _customerCode);
        }
        private async Task getUnitCode(Entity enPayment)
        {
            tracingService.Trace("Start get unit code");
            Entity enUnit = service.Retrieve(((EntityReference)enPayment["bsd_units"]).LogicalName, ((EntityReference)enPayment["bsd_units"]).Id, new ColumnSet(new string[1] { "bsd_unitcodesap" }));
            this._unitCode = enUnit.Contains("bsd_unitcodesap") ? enUnit["bsd_unitcodesap"].ToString() : null;
            tracingService.Trace("End get unit code");
        }
        private async Task getTransactionCode(Entity enPayment)
        {
            tracingService.Trace("Start get giao dich");
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
            tracingService.Trace("End get giao dich");
        }
        private async Task updatePayment(Entity en,bool? _isResend = false)
        {
            Entity enpayment = new Entity(en.LogicalName, en.Id); 
            enpayment["bsd_paymentcodesap"] = resultpaymentCode;
            if (_isResend == false)
            {
                enpayment["statuscode"] = new OptionSetValue(100000000);
            }
            service.Update(enpayment);
        }
        private async Task updateChiTietPhieuThu()
        {
            foreach (var item in this.listChiTietPhieuThu)
            {
                Entity enpayment = new Entity(item.LogicalName, item.Id);
                enpayment["bsd_paymentcodesap"] = resultpaymentCode;
                service.Update(enpayment);
            }
        }
        private Entity getPaymentSchemedetail(string id)
        {
            var enPaymentSchemeDetail = service.Retrieve("bsd_paymentschemedetail", Guid.Parse(id), new ColumnSet(new string[] { "bsd_ordernumber", "bsd_lastinstallment" }));
            if (!enPaymentSchemeDetail.Contains("bsd_paymentschemedetailid")) throw new InvalidPluginExecutionException("Không tìm thấy đợt thanh toán");
            return enPaymentSchemeDetail;
        }
        private async Task getTKTien(Entity en)
        {
            tracingService.Trace("Start TK Tien");
            EntityReference enrGiaoDich = en.Contains("bsd_datcoc") ? (EntityReference)en["bsd_datcoc"] : en.Contains("bsd_reservation") ? (EntityReference)en["bsd_reservation"] : en.Contains("bsd_optionentry") ? (EntityReference)en["bsd_optionentry"]: null;
            
            Entity enGiaoDich = service.Retrieve(enrGiaoDich.LogicalName, enrGiaoDich.Id, new ColumnSet(new string[1] { "bsd_taikoannganhangduan" }));
            if (!enGiaoDich.Contains("bsd_taikoannganhangduan"))
            {
                tracingService.Trace("Khong co tai khoan ngan hang");
                this._cashaccount = null;
                return;
            }
            EntityReference enrBankAccount = enGiaoDich.Contains("bsd_taikoannganhangduan") ? (EntityReference)enGiaoDich["bsd_taikoannganhangduan"] : null;
            Entity enBankAccount = service.Retrieve(enrBankAccount.LogicalName, enrBankAccount.Id, new ColumnSet(new string[1] { "bsd_cashaccount" }));
            this._cashaccount = enBankAccount.Contains("bsd_cashaccount") ? enBankAccount["bsd_cashaccount"].ToString() : null;
        }
        private async Task updateReasonPayment()
        {
            Entity enPayment = new Entity(this.enPhieuThu.LogicalName,this.enPhieuThu.Id);
            enPayment["statuscode"] = new OptionSetValue(100000001);
            enPayment["bsd_reasonreject"] = this.reasonReject;

            service.Update(enPayment);
        }
        private async Task updateReasonPaymentDetail()
        {
            foreach (var chiTietPhieuThu in this.listChiTietPhieuThu)
            {
                Entity enChiTietPhieuThu = new Entity(chiTietPhieuThu.LogicalName, chiTietPhieuThu.Id);
                enChiTietPhieuThu["bsd_reasonreject"] = this.reasonReject;
                enChiTietPhieuThu["statuscode"] = new OptionSetValue(100000003); // tu choi

                service.Update(enChiTietPhieuThu);
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
        
    }
}
