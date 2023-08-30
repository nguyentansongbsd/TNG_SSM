using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Action_CreateIntermediatePayment
{
    public class Action_CreateIntermediatePayment : IPlugin
    {
        IPluginExecutionContext context = null;
        IOrganizationServiceFactory factory = null;
        IOrganizationService service = null;
        ITracingService tracingService = null;

        List<Entity> listEnPay { get; set; }
        List<Entity> listEnPayGroup { get; set; }
        List<Entity> listNguonThanhToan { get; set; }
        Guid IntermediatePaymentId { get; set; } = new Guid();
        string _enIntermediatePayment = "bsd_intermediatepayment";
        string _enNguonThanhToan = "bsd_nguonthanhtoan";

        public void Execute(IServiceProvider serviceProvider)
        {
            context = serviceProvider.GetService(typeof(IPluginExecutionContext)) as IPluginExecutionContext;
            factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            service = (IOrganizationService)factory.CreateOrganizationService(context.UserId);
            tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            
            getListPaymentNeedSynch();
            if (listEnPay != null && listEnPay.Count > 0)
            {
                Init();
            }
        }
        private void Init()
        {
            foreach (var enPayGroup in listEnPayGroup)
            {
                decimal total = listEnPay.Where(x => x["bsd_paymentcode"].ToString() == enPayGroup["bsd_paymentcode"].ToString()).ToList().Sum(y => ((Money)y["bsd_amountpay"]).Value);
                Task.WaitAll(
                    createIntermediatePayment(enPayGroup, total)
                    );
                foreach (var enPay in listEnPay)
                {
                    if (enPayGroup["bsd_paymentcode"].ToString() == enPay["bsd_paymentcode"].ToString())
                    {
                        Task.WaitAll(
                            //updateNguonThanhToan(enPay),
                            updatePayment(enPay)
                            );
                    }
                }
            }
        }
        private async Task createIntermediatePayment(Entity enPayment, decimal total)
        {
            tracingService.Trace("Done Header");
            Entity enIntermediatePayment = new Entity(_enIntermediatePayment);
            enIntermediatePayment["bsd_paymentcode"] = enPayment["bsd_paymentcode"];
            enIntermediatePayment["bsd_name"] = enPayment["bsd_paymentdescription"];
            enIntermediatePayment["bsd_paymentdate"] = (DateTime)enPayment["bsd_paymentactualtime"];
            enIntermediatePayment["bsd_datcoc"] = enPayment.Contains("bsd_datcoc") ? new EntityReference(((EntityReference)enPayment["bsd_datcoc"]).LogicalName, ((EntityReference)enPayment["bsd_datcoc"]).Id) : null;
            enIntermediatePayment["bsd_reservation"] = enPayment.Contains("bsd_reservation") ? new EntityReference(((EntityReference)enPayment["bsd_reservation"]).LogicalName, ((EntityReference)enPayment["bsd_reservation"]).Id) : null;
            enIntermediatePayment["bsd_optionentry"] = enPayment.Contains("bsd_optionentry") ? new EntityReference(((EntityReference)enPayment["bsd_optionentry"]).LogicalName, ((EntityReference)enPayment["bsd_optionentry"]).Id) : null;
            enIntermediatePayment["bsd_paymentmode"] = enPayment.Contains("bsd_paymentmode") ? new EntityReference(((EntityReference)enPayment["bsd_paymentmode"]).LogicalName, ((EntityReference)enPayment["bsd_paymentmode"]).Id) : null;
            enIntermediatePayment["bsd_project"] = enPayment.Contains("bsd_project") ? new EntityReference(((EntityReference)enPayment["bsd_project"]).LogicalName, ((EntityReference)enPayment["bsd_project"]).Id) : null;
            enIntermediatePayment["bsd_units"] = enPayment.Contains("bsd_units") ? new EntityReference(((EntityReference)enPayment["bsd_units"]).LogicalName, ((EntityReference)enPayment["bsd_units"]).Id) : null;
            enIntermediatePayment["bsd_customer"] = enPayment.Contains("bsd_purchaser") ? new EntityReference(((EntityReference)enPayment["bsd_purchaser"]).LogicalName, ((EntityReference)enPayment["bsd_purchaser"]).Id) : null;
            enIntermediatePayment["statuscode"] = new OptionSetValue(1);
            enIntermediatePayment["bsd_fundstransferdate"] = enPayment.Contains("bsd_fundstransferdate") ? enPayment["bsd_fundstransferdate"] : null;
            enIntermediatePayment["bsd_amountpay"] = new Money(total);

            tracingService.Trace("Done Check");
            string contractCode = string.Empty;
            int typeopentry = 100000000;
            if (enPayment.Contains("bsd_datcoc"))
            {
                typeopentry = 100000002;
                contractCode = await getContractCode((EntityReference)enPayment["bsd_datcoc"], "bsd_mahethong");
            }
            else if (enPayment.Contains("bsd_reservation"))
            {
                typeopentry = 100000000;
                contractCode = await getContractCode((EntityReference)enPayment["bsd_reservation"], "bsd_mahethong");
            }
            else if (enPayment.Contains("bsd_optionentry"))
            {
                typeopentry = 100000001;
                contractCode = await getContractCode((EntityReference)enPayment["bsd_optionentry"], "bsd_mahethong");
            }
            enIntermediatePayment["bsd_contractcode"] = contractCode;
            enIntermediatePayment["bsd_typeoptionentry"] = new OptionSetValue(typeopentry);

            IntermediatePaymentId = service.Create(enIntermediatePayment);
            tracingService.Trace("Done tạo thanh toán trung gian");
            //await getListNguonThanhToan();
        }
        private async Task updatePayment(Entity en)
        {
            Entity enpayment = service.Retrieve(en.LogicalName, en.Id, new ColumnSet(new string[2] { "bsd_paymentcodesap", "bsd_issynchedsap" }));
            enpayment["bsd_phieuthu"] = new EntityReference(this._enIntermediatePayment,this.IntermediatePaymentId);
            service.Update(enpayment);
        }
        private async Task updateNguonThanhToan(Entity enPayment)
        {
            tracingService.Trace("Start update");
            int value = 100000000;
            switch (((OptionSetValue)enPayment["bsd_paymenttype"]).Value)
            {
                case 100000000: // Thanh Toán Theo Tiến Độ
                    {
                        value = 100000000; //Đợt thanh toán
                        break;
                    }
                case 100000001: // Thanh Toán Lãi
                    {
                        value = 100000002; // Lãi phạt chậm
                        break;
                    }
                case 100000002: // Thanh toán phí quản lý & phí bảo trì
                    {
                        value = 100000004; //Phí quản lý & bảo trì
                        break;
                    }
                case 100000003: // Thanh Toán Gói Hoàn Thiện
                    {
                        value = 100000003; //Gói hoàn thiện
                        break;
                    }
                case 100000004: // Phí Khác
                    {
                        value = 100000006; //Test 2
                        break;
                    }
                case 100000005: // Đặt cọc
                    {
                        value = 100000001; //Đặt cọc
                        break;
                    }
                case 100000006: // Thanh toán lãi đặt cọc
                    {
                        value = 100000005; // Test 1
                        break;
                    }
            }
            foreach (var nguonThanhToan in listNguonThanhToan)
            {
                if (((OptionSetValue)nguonThanhToan["bsd_loaithanhtoan"]).Value == value)
                {
                    Entity enNguonThanhToan = service.Retrieve(_enNguonThanhToan, (Guid)nguonThanhToan["bsd_nguonthanhtoanid"], new ColumnSet(new string[1] { "bsd_sotien" }));
                    decimal sotien = ((Money)enPayment["bsd_amountpay"]).Value;
                    enNguonThanhToan["bsd_sotien"] = new Money(sotien);
                    tracingService.Trace("Done add tham so");
                    service.Update(enNguonThanhToan);
                }
            }
        }
        private async Task getListNguonThanhToan()
        {
            listNguonThanhToan = new List<Entity>();
            var fetchXml = $@"<?xml version=""1.0"" encoding=""utf-16""?>
                <fetch>
                  <entity name=""bsd_nguonthanhtoan"">
                    <attribute name=""bsd_loaithanhtoan"" />
                    <attribute name=""bsd_nguonthanhtoanid"" />
                    <attribute name=""bsd_sotien"" />
                    <filter type=""and"">
                      <condition attribute=""bsd_thanhtoantrunggian"" operator=""eq"" value=""{IntermediatePaymentId}"" />
                    </filter>
                  </entity>
                </fetch>";
            var result = service.RetrieveMultiple(new FetchExpression(fetchXml));
            listNguonThanhToan = result.Entities.ToList();
            tracingService.Trace("Done get list nguon thanh toan");
        }
        private void getListPaymentNeedSynch()
        {
            string conditions = string.Empty;
            if ((bool)context.InputParameters["issyncform"] == true)
            {
                string id = context.InputParameters["idpayment"].ToString();
                conditions = $@"<condition attribute=""bsd_paymentid"" operator=""eq"" value=""{id}"" />";
            }
            listEnPay = new List<Entity>();
            listEnPayGroup = new List<Entity>();
            var fetchXml = $@"<?xml version=""1.0"" encoding=""utf-16""?>
                            <fetch>
                              <entity name=""bsd_payment"">
                                <filter>
                                  <condition attribute=""bsd_paymentcode"" operator=""not-null"" />
                                  <condition attribute=""bsd_issynchedsap"" operator=""eq"" value=""0"" />
                                  <condition attribute=""statecode"" operator=""eq"" value=""0"" />
                                  <condition attribute=""statuscode"" operator=""eq"" value=""1"" />
                                  <condition attribute=""ownerid"" operator=""eq"" value=""{context.UserId}"" />
                                  {conditions}
                                </filter>
                              </entity>
                            </fetch>";
            var resuft = service.RetrieveMultiple(new FetchExpression(fetchXml));
            if (resuft != null && resuft.Entities.Count > 0)
            {
                listEnPay = resuft.Entities.ToList();
                listEnPayGroup = resuft.Entities.GroupBy(x => new { g = x["bsd_paymentcode"].ToString() }).Select(y => y.First()).ToList();
            }
            else
                throw new InvalidPluginExecutionException("Không có dữ liệu để đồng bộ.");
        }
        private async Task<string> getContractCode(EntityReference entityRe, string attribute)
        {
            tracingService.Trace("Start get contract code: " + attribute);
            Entity en = service.Retrieve(entityRe.LogicalName, entityRe.Id, new ColumnSet(new string[1] { attribute }));
            if (entityRe.LogicalName == "bsd_datcoc" && !en.Contains(attribute))
            {
                Entity _en = service.Retrieve(entityRe.LogicalName, entityRe.Id, new ColumnSet(new string[1] { "bsd_mahethong" }));
                return _en["bsd_mahethong"].ToString();
            }
            else
                return en[attribute].ToString();
        }
    }
}
