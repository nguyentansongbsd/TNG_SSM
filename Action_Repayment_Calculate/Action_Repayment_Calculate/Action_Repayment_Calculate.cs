using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Action_Repayment_Calculate
{
    public class Action_Repayment_Calculate : IPlugin
    {
        IOrganizationService service = null;
        IOrganizationServiceFactory factory = null;
        StringBuilder str = new StringBuilder();
        public void Execute(IServiceProvider serviceProvider)
        {
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            EntityReference target = (EntityReference)context.InputParameters["Target"];
            factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            service = factory.CreateOrganizationService(context.UserId);
            ITracingService traceService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            traceService.Trace("Action_Repayment_Calculate");


            Entity enRepaymentCal = service.Retrieve(target.LogicalName, target.Id, new ColumnSet(true));
            Entity enAppendixContractBrokerage = service.Retrieve(((EntityReference)enRepaymentCal["bsd_appendixcontractbrokerage"]).LogicalName, ((EntityReference)enRepaymentCal["bsd_appendixcontractbrokerage"]).Id, new ColumnSet(true));
            Entity enBrokerageContract = service.Retrieve(((EntityReference)enRepaymentCal["bsd_brokeragecontract"]).LogicalName, ((EntityReference)enRepaymentCal["bsd_brokeragecontract"]).Id, new ColumnSet(true));
            Entity enProject = service.Retrieve(((EntityReference)enRepaymentCal["bsd_project"]).LogicalName, ((EntityReference)enRepaymentCal["bsd_project"]).Id, new ColumnSet(true));
            int calculateBy = enAppendixContractBrokerage.Contains("bsd_brokeragefee") ? ((OptionSetValue)enAppendixContractBrokerage["bsd_brokeragefee"]).Value : 0;
            decimal brokerageFeeRetain = enAppendixContractBrokerage.Contains("bsd_brokeragefeesretain") ? (decimal)enAppendixContractBrokerage["bsd_brokeragefeesretain"] : 0;
            decimal totalAmountBrokerage = 0;
            decimal totalRepayment = 0;

            EntityCollection listTarget = getTarget(enProject, enAppendixContractBrokerage, enBrokerageContract, service);
            //throw new InvalidPluginExecutionException("Record Chỉ Tiêu " + listTarget.Entities.Count);
            if (listTarget.Entities.Count > 0)
            {
                foreach (var itemTarget in listTarget.Entities)
                {
                    Entity enBaseContract = service.Retrieve(((EntityReference)itemTarget["bsd_basecontract"]).LogicalName, ((EntityReference)itemTarget["bsd_basecontract"]).Id, new ColumnSet(true));
                    Entity enUnit = service.Retrieve(((EntityReference)enBaseContract["bsd_unitno"]).LogicalName, ((EntityReference)enBaseContract["bsd_unitno"]).Id, new ColumnSet(true));
                    decimal percent = itemTarget.Contains("bsd_percen") ? (decimal)itemTarget["bsd_percen"] : 0;
                    decimal percentNow = itemTarget.Contains("bsd_percentnow") ? (decimal)itemTarget["bsd_percentnow"] : 0;
                    decimal percentDifferent = itemTarget.Contains("bsd_percentdifferent") ? (decimal)itemTarget["bsd_percentdifferent"]  : 0;
                    EntityCollection listContract = getListHDMB(enUnit, service);
                    if (listContract.Entities.Count > 0)
                    {

                        Entity enContracts = service.Retrieve(listContract.Entities[0].LogicalName, listContract.Entities[0].Id, new ColumnSet(true));
                        int statusContracts = enContracts.Contains("statuscode") ? ((OptionSetValue)enContracts["statuscode"]).Value : 0;
                        if(statusContracts != 100000008)
                        {
                            if (calculateBy == 100000001)
                            {
                                decimal COMValue = 0;
                                decimal brokerageFeeValue = 0;
                                decimal brokerageFeeHold = 0;
                                decimal repaymentValue = 0;

                                decimal totalAmountDetail = enContracts.Contains("bsd_totalamountdetails") ? (decimal)((Money)enContracts["bsd_totalamountdetails"]).Value : 0;
                                decimal discount = enContracts.Contains("bsd_discount") ? (decimal)((Money)enContracts["bsd_discount"]).Value : 0;
                                decimal allotmentFee = enContracts.Contains("bsd_allotmentfee") ? (decimal)((Money)enContracts["bsd_allotmentfee"]).Value : 0;
                                decimal serviceFees = enContracts.Contains("serviceFees") ? (decimal)((Money)enContracts["serviceFees"]).Value : 0;
                                COMValue = ((totalAmountDetail - discount) / 1.1M) - allotmentFee - serviceFees;

                                brokerageFeeValue = percent / 100 * COMValue;
                                brokerageFeeHold = brokerageFeeRetain / 100 * COMValue;
                                repaymentValue = ((percent - brokerageFeeRetain) / 100) * COMValue;

                                //Tạo record Giao dich tạm ứng
                                if (percent == percentDifferent)
                                {
                                    Entity enRepayment = new Entity("bsd_repayment");
                                    enRepayment["bsd_name"] = "Tạm ứng sản phẩm " + enContracts["name"];
                                    enRepayment["bsd_brokeragecontract"] = itemTarget.Contains("bsd_brokeragecontract") ? itemTarget["bsd_brokeragecontract"] : null;
                                    enRepayment["bsd_project"] = itemTarget.Contains("bsd_project") ? itemTarget["bsd_project"] : null;
                                    enRepayment["bsd_distributor"] = itemTarget.Contains("bsd_distributors") ? itemTarget["bsd_distributors"] : null;
                                    enRepayment["bsd_appendixcontractbrokerage"] = itemTarget.Contains("bsd_appendixcontractbrokerage") ? itemTarget["bsd_appendixcontractbrokerage"] : null;
                                    enRepayment["bsd_brokeragecalculate"] = itemTarget.Contains("bsd_brokeragecalculation") ? itemTarget["bsd_brokeragecalculation"] : null;
                                    enRepayment["bsd_repaymentcalculate"] = enRepaymentCal.ToEntityReference();
                                    enRepayment["bsd_targetbrokeragefee"] = itemTarget.ToEntityReference();

                                    enRepayment["bsd_customer"] = itemTarget.Contains("bsd_customer") ? itemTarget["bsd_customer"] : null;
                                    enRepayment["bsd_units"] = enUnit.ToEntityReference();

                                    enRepayment["bsd_quote"] = itemTarget.Contains("bsd_basecontract") ? itemTarget["bsd_basecontract"] : null;
                                    enRepayment["bsd_deposit"] = itemTarget.Contains("bsd_datcoc") ? itemTarget["bsd_datcoc"] : null;
                                    enRepayment["bsd_contracts"] = itemTarget.Contains("bsd_contracts") ? itemTarget["bsd_contracts"] : null;
                                    enRepayment["bsd_depositdate"] = itemTarget.Contains("bsd_receiptdate") ? itemTarget["bsd_receiptdate"] : null;
                                    enRepayment["bsd_signeddatecontract"] = itemTarget.Contains("bsd_signedcontractdate") ? itemTarget["bsd_signedcontractdate"] : null;
                                    enRepayment["bsd_signeddatebasecontract"] = itemTarget.Contains("bsd_basecontractsigningdate") ? itemTarget["bsd_basecontractsigningdate"] : null;
                                    enRepayment["bsd_installment1paiddate"] = itemTarget.Contains("bsd_installment1paiddate") ? itemTarget["bsd_installment1paiddate"] : null;
                                    enRepayment["bsd_brokeragefees"] = itemTarget.Contains("bsd_brokeragefees") ? itemTarget["bsd_brokeragefees"] : null;

                                    enRepayment["bsd_totalamount"] = new Money(brokerageFeeValue);
                                    enRepayment["bsd_settlementamount"] = new Money(brokerageFeeHold);
                                    enRepayment["bsd_repaymentmoney"] = new Money(repaymentValue);
                                    enRepayment["bsd_comvalue"] = new Money(COMValue);
                                    enRepayment["statuscode"] = new OptionSetValue(100000002);
                                    totalAmountBrokerage += brokerageFeeValue;
                                    totalRepayment += repaymentValue;
                                    service.Create(enRepayment);
                                }
                                else
                                {
                                    decimal repaymentAdd = 0;
                                    decimal repaymentOld = 0;
                                    var fetchXmlRepaymentOld = $@"<?xml version=""1.0"" encoding=""utf-16""?>
                                <fetch>
                                  <entity name=""bsd_repayment"">
                                    <filter>
                                      <condition attribute=""bsd_brokeragecontract"" operator=""eq"" value=""{enBrokerageContract.Id}"" />
                                      <condition attribute=""bsd_project"" operator=""eq"" value=""{enProject.Id}"" />
                                      <condition attribute=""bsd_appendixcontractbrokerage"" operator=""eq"" value=""{enAppendixContractBrokerage.Id}"" />
                                      <condition attribute=""bsd_quote"" operator=""eq"" value=""{enBaseContract.Id}"" />
                                    </filter>
                                    <order attribute=""createdon"" descending=""true"" />
                                  </entity>
                                </fetch>";
                                    EntityCollection listRepaymentOld = service.RetrieveMultiple(new FetchExpression(fetchXmlRepaymentOld));
                                    if (listRepaymentOld.Entities.Count > 0)
                                    {
                                        repaymentOld = listRepaymentOld.Entities[0].Contains("bsd_repaymentmoney") ? (decimal)((Money)listRepaymentOld.Entities[0]["bsd_repaymentmoney"]).Value : 0;
                                    }

                                    repaymentAdd = brokerageFeeValue - brokerageFeeHold - repaymentOld;

                                    Entity enRepayment = new Entity("bsd_repayment");
                                    enRepayment["bsd_name"] = "Tạm ứng bổ sung sản phẩm " + enContracts["name"];
                                    enRepayment["bsd_brokeragecontract"] = itemTarget.Contains("bsd_brokeragecontract") ? itemTarget["bsd_brokeragecontract"] : null;
                                    enRepayment["bsd_project"] = itemTarget.Contains("bsd_project") ? itemTarget["bsd_project"] : null;
                                    enRepayment["bsd_distributor"] = itemTarget.Contains("bsd_distributors") ? itemTarget["bsd_distributors"] : null;
                                    enRepayment["bsd_appendixcontractbrokerage"] = itemTarget.Contains("bsd_appendixcontractbrokerage") ? itemTarget["bsd_appendixcontractbrokerage"] : null;
                                    enRepayment["bsd_brokeragecalculate"] = itemTarget.Contains("bsd_brokeragecalculation") ? itemTarget["bsd_brokeragecalculation"] : null;
                                    enRepayment["bsd_repaymentcalculate"] = enRepaymentCal.ToEntityReference();
                                    enRepayment["bsd_targetbrokeragefee"] = itemTarget.ToEntityReference();

                                    enRepayment["bsd_customer"] = itemTarget.Contains("bsd_customer") ? itemTarget["bsd_customer"] : null;
                                    enRepayment["bsd_units"] = enUnit.ToEntityReference();

                                    enRepayment["bsd_quote"] = itemTarget.Contains("bsd_basecontract") ? itemTarget["bsd_basecontract"] : null;
                                    enRepayment["bsd_deposit"] = itemTarget.Contains("bsd_datcoc") ? itemTarget["bsd_datcoc"] : null;
                                    enRepayment["bsd_contracts"] = itemTarget.Contains("bsd_contracts") ? itemTarget["bsd_contracts"] : null;
                                    enRepayment["bsd_depositdate"] = itemTarget.Contains("bsd_receiptdate") ? itemTarget["bsd_receiptdate"] : null;
                                    enRepayment["bsd_signeddatecontract"] = itemTarget.Contains("bsd_signedcontractdate") ? itemTarget["bsd_signedcontractdate"] : null;
                                    enRepayment["bsd_signeddatebasecontract"] = itemTarget.Contains("bsd_basecontractsigningdate") ? itemTarget["bsd_basecontractsigningdate"] : null;
                                    enRepayment["bsd_installment1paiddate"] = itemTarget.Contains("bsd_installment1paiddate") ? itemTarget["bsd_installment1paiddate"] : null;
                                    enRepayment["bsd_brokeragefees"] = itemTarget.Contains("bsd_brokeragefees") ? itemTarget["bsd_brokeragefees"] : null;

                                    enRepayment["bsd_totalamount"] = new Money(brokerageFeeValue);
                                    enRepayment["bsd_settlementamount"] = new Money(brokerageFeeHold);
                                    enRepayment["bsd_repaymentmoney"] = new Money(repaymentAdd);
                                    enRepayment["bsd_comvalue"] = new Money(COMValue);
                                    enRepayment["statuscode"] = new OptionSetValue(100000002);
                                    totalAmountBrokerage += brokerageFeeValue;
                                    totalRepayment += repaymentAdd;
                                    service.Create(enRepayment);
                                }

                            }
                            else if (calculateBy == 100000000)
                            {
                                decimal COMValue = 0;
                                decimal brokerageFeeValue = 0;
                                decimal brokerageFeeHold = 0;
                                decimal repaymentValue = 0;
                                var unitType = enContracts.Contains("bsd_typeunit") ? ((OptionSetValue)enContracts["bsd_typeunit"]).Value : 0;
                                decimal totalAmountDetail = 0;
                                decimal discount = 0;
                                decimal allotmentFee = enContracts.Contains("bsd_allotmentfee") ? (decimal)((Money)enContracts["bsd_allotmentfee"]).Value : 0;
                                decimal serviceFees = enContracts.Contains("serviceFees") ? (decimal)((Money)enContracts["serviceFees"]).Value : 0;

                                if (unitType == 100000001)
                                {
                                    totalAmountDetail = enContracts.Contains("bsd_totalamountdetails") ? (decimal)((Money)enContracts["bsd_totalamountdetails"]).Value : 0;
                                    discount = enContracts.Contains("bsd_discount") ? (decimal)((Money)enContracts["bsd_discount"]).Value : 0;
                                    COMValue = ((totalAmountDetail - discount) / 1.1M) - allotmentFee - serviceFees;
                                }
                                else
                                {
                                    decimal unitsquare = enUnit.Contains("bsd_areasqm") ? (decimal)enUnit["bsd_areasqm"] : 0;
                                    decimal unitPrice = enUnit.Contains("bsd_landvalueofunit") ? (decimal)((Money)enUnit["bsd_landvalueofunit"]).Value : 0;
                                    discount = enContracts.Contains("bsd_discountland") ? (decimal)((Money)enContracts["bsd_discountland"]).Value : 0;
                                    COMValue = ((unitPrice * unitsquare - discount) / 1.1M) - allotmentFee - serviceFees;
                                }

                                brokerageFeeValue = percent / 100 * COMValue;
                                brokerageFeeHold = brokerageFeeRetain / 100 * COMValue;
                                repaymentValue = ((percent - brokerageFeeRetain) / 100) * COMValue;

                                if (percent == percentDifferent)
                                {
                                    Entity enRepayment = new Entity("bsd_repayment");
                                    enRepayment["bsd_name"] = "Tạm ứng sản phẩm " + enContracts["name"];
                                    enRepayment["bsd_brokeragecontract"] = itemTarget.Contains("bsd_brokeragecontract") ? itemTarget["bsd_brokeragecontract"] : null;
                                    enRepayment["bsd_project"] = itemTarget.Contains("bsd_project") ? itemTarget["bsd_project"] : null;
                                    enRepayment["bsd_distributor"] = itemTarget.Contains("bsd_distributors") ? itemTarget["bsd_distributors"] : null;
                                    enRepayment["bsd_appendixcontractbrokerage"] = itemTarget.Contains("bsd_appendixcontractbrokerage") ? itemTarget["bsd_appendixcontractbrokerage"] : null;
                                    enRepayment["bsd_brokeragecalculate"] = itemTarget.Contains("bsd_brokeragecalculation") ? itemTarget["bsd_brokeragecalculation"] : null;
                                    enRepayment["bsd_repaymentcalculate"] = enRepaymentCal.ToEntityReference();
                                    enRepayment["bsd_targetbrokeragefee"] = itemTarget.ToEntityReference();

                                    enRepayment["bsd_customer"] = itemTarget.Contains("bsd_customer") ? itemTarget["bsd_customer"] : null;
                                    enRepayment["bsd_units"] = enUnit.ToEntityReference();

                                    enRepayment["bsd_quote"] = itemTarget.Contains("bsd_basecontract") ? itemTarget["bsd_basecontract"] : null;
                                    enRepayment["bsd_deposit"] = itemTarget.Contains("bsd_datcoc") ? itemTarget["bsd_datcoc"] : null;
                                    enRepayment["bsd_contracts"] = itemTarget.Contains("bsd_contracts") ? itemTarget["bsd_contracts"] : null;
                                    enRepayment["bsd_depositdate"] = itemTarget.Contains("bsd_receiptdate") ? itemTarget["bsd_receiptdate"] : null;
                                    enRepayment["bsd_signeddatecontract"] = itemTarget.Contains("bsd_signedcontractdate") ? itemTarget["bsd_signedcontractdate"] : null;
                                    enRepayment["bsd_signeddatebasecontract"] = itemTarget.Contains("bsd_basecontractsigningdate") ? itemTarget["bsd_basecontractsigningdate"] : null;
                                    enRepayment["bsd_installment1paiddate"] = itemTarget.Contains("bsd_installment1paiddate") ? itemTarget["bsd_installment1paiddate"] : null;
                                    enRepayment["bsd_brokeragefees"] = itemTarget.Contains("bsd_brokeragefees") ? itemTarget["bsd_brokeragefees"] : null;

                                    enRepayment["bsd_totalamount"] = new Money(brokerageFeeValue);
                                    enRepayment["bsd_settlementamount"] = new Money(brokerageFeeHold);
                                    enRepayment["bsd_repaymentmoney"] = new Money(repaymentValue);
                                    enRepayment["bsd_comvalue"] = new Money(COMValue);
                                    enRepayment["statuscode"] = new OptionSetValue(100000002);
                                    totalAmountBrokerage += brokerageFeeValue;
                                    totalRepayment += repaymentValue;
                                    service.Create(enRepayment);
                                }
                                else
                                {
                                    decimal repaymentAdd = 0;
                                    decimal repaymentOld = 0;
                                    var fetchXmlRepaymentOld = $@"<?xml version=""1.0"" encoding=""utf-16""?>
                                <fetch>
                                  <entity name=""bsd_repayment"">
                                    <filter>
                                      <condition attribute=""bsd_brokeragecontract"" operator=""eq"" value=""{enBrokerageContract.Id}"" />
                                      <condition attribute=""bsd_project"" operator=""eq"" value=""{enProject.Id}"" />
                                      <condition attribute=""bsd_appendixcontractbrokerage"" operator=""eq"" value=""{enAppendixContractBrokerage.Id}"" />
                                      <condition attribute=""bsd_quote"" operator=""eq"" value=""{enBaseContract.Id}"" />
                                    </filter>
                                    <order attribute=""createdon"" descending=""true"" />
                                  </entity>
                                </fetch>";
                                    EntityCollection listRepaymentOld = service.RetrieveMultiple(new FetchExpression(fetchXmlRepaymentOld));
                                    if (listRepaymentOld.Entities.Count > 0)
                                    {
                                        repaymentOld = listRepaymentOld.Entities[0].Contains("bsd_repaymentmoney") ? (decimal)((Money)listRepaymentOld.Entities[0]["bsd_repaymentmoney"]).Value : 0;
                                    }

                                    repaymentAdd = brokerageFeeValue - brokerageFeeHold - repaymentOld;

                                    Entity enRepayment = new Entity("bsd_repayment");
                                    enRepayment["bsd_name"] = "Tạm ứng bổ sung sản phẩm " + enContracts["name"];
                                    enRepayment["bsd_brokeragecontract"] = itemTarget.Contains("bsd_brokeragecontract") ? itemTarget["bsd_brokeragecontract"] : null;
                                    enRepayment["bsd_project"] = itemTarget.Contains("bsd_project") ? itemTarget["bsd_project"] : null;
                                    enRepayment["bsd_distributor"] = itemTarget.Contains("bsd_distributors") ? itemTarget["bsd_distributors"] : null;
                                    enRepayment["bsd_appendixcontractbrokerage"] = itemTarget.Contains("bsd_appendixcontractbrokerage") ? itemTarget["bsd_appendixcontractbrokerage"] : null;
                                    enRepayment["bsd_brokeragecalculate"] = itemTarget.Contains("bsd_brokeragecalculation") ? itemTarget["bsd_brokeragecalculation"] : null;
                                    enRepayment["bsd_repaymentcalculate"] = enRepaymentCal.ToEntityReference();
                                    enRepayment["bsd_targetbrokeragefee"] = itemTarget.ToEntityReference();

                                    enRepayment["bsd_customer"] = itemTarget.Contains("bsd_customer") ? itemTarget["bsd_customer"] : null;
                                    enRepayment["bsd_units"] = enUnit.ToEntityReference();

                                    enRepayment["bsd_quote"] = itemTarget.Contains("bsd_basecontract") ? itemTarget["bsd_basecontract"] : null;
                                    enRepayment["bsd_deposit"] = itemTarget.Contains("bsd_datcoc") ? itemTarget["bsd_datcoc"] : null;
                                    enRepayment["bsd_contracts"] = itemTarget.Contains("bsd_contracts") ? itemTarget["bsd_contracts"] : null;
                                    enRepayment["bsd_depositdate"] = itemTarget.Contains("bsd_receiptdate") ? itemTarget["bsd_receiptdate"] : null;
                                    enRepayment["bsd_signeddatecontract"] = itemTarget.Contains("bsd_signedcontractdate") ? itemTarget["bsd_signedcontractdate"] : null;
                                    enRepayment["bsd_signeddatebasecontract"] = itemTarget.Contains("bsd_basecontractsigningdate") ? itemTarget["bsd_basecontractsigningdate"] : null;
                                    enRepayment["bsd_installment1paiddate"] = itemTarget.Contains("bsd_installment1paiddate") ? itemTarget["bsd_installment1paiddate"] : null;
                                    enRepayment["bsd_brokeragefees"] = itemTarget.Contains("bsd_brokeragefees") ? itemTarget["bsd_brokeragefees"] : null;

                                    enRepayment["bsd_totalamount"] = new Money(brokerageFeeValue);
                                    enRepayment["bsd_settlementamount"] = new Money(brokerageFeeHold);
                                    enRepayment["bsd_repaymentmoney"] = new Money(repaymentAdd);
                                    enRepayment["bsd_comvalue"] = new Money(COMValue);
                                    enRepayment["statuscode"] = new OptionSetValue(100000002);
                                    totalAmountBrokerage += brokerageFeeValue;
                                    totalRepayment += repaymentAdd;
                                    service.Create(enRepayment);
                                }
                            }
                        }
                        

                    }
                    else
                    {
                        int statusBaseContract = enBaseContract.Contains("statuscode") ? ((OptionSetValue)enBaseContract["statuscode"]).Value : 0;
                        if(statusBaseContract != 100000010)
                        {
                            if (calculateBy == 100000001)
                            {
                                decimal COMValue = 0;
                                decimal brokerageFeeValue = 0;
                                decimal brokerageFeeHold = 0;
                                decimal repaymentValue = 0;

                                decimal totalAmountDetail = enBaseContract.Contains("bsd_totalamountdetails") ? (decimal)((Money)enBaseContract["bsd_totalamountdetails"]).Value : 0;
                                decimal discount = enBaseContract.Contains("bsd_discount") ? (decimal)((Money)enBaseContract["bsd_discount"]).Value : 0;
                                decimal allotmentFee = enBaseContract.Contains("bsd_allotmentfee") ? (decimal)((Money)enBaseContract["bsd_allotmentfee"]).Value : 0;
                                decimal serviceFees = enBaseContract.Contains("serviceFees") ? (decimal)((Money)enBaseContract["serviceFees"]).Value : 0;
                                COMValue = ((totalAmountDetail - discount) / 1.1M) - allotmentFee - serviceFees;

                                brokerageFeeValue = percent / 100 * COMValue;
                                brokerageFeeHold = brokerageFeeRetain / 100 * COMValue;
                                repaymentValue = ((percent - brokerageFeeRetain) / 100) * COMValue;

                                //Tạo record Giao dich tạm ứng
                                if (percent == percentDifferent)
                                {
                                    Entity enRepayment = new Entity("bsd_repayment");
                                    enRepayment["bsd_name"] = "Tạm ứng sản phẩm " + enBaseContract["name"];
                                    enRepayment["bsd_brokeragecontract"] = itemTarget.Contains("bsd_brokeragecontract") ? itemTarget["bsd_brokeragecontract"] : null;
                                    enRepayment["bsd_project"] = itemTarget.Contains("bsd_project") ? itemTarget["bsd_project"] : null;
                                    enRepayment["bsd_distributor"] = itemTarget.Contains("bsd_distributors") ? itemTarget["bsd_distributors"] : null;
                                    enRepayment["bsd_appendixcontractbrokerage"] = itemTarget.Contains("bsd_appendixcontractbrokerage") ? itemTarget["bsd_appendixcontractbrokerage"] : null;
                                    enRepayment["bsd_brokeragecalculate"] = itemTarget.Contains("bsd_brokeragecalculation") ? itemTarget["bsd_brokeragecalculation"] : null;
                                    enRepayment["bsd_repaymentcalculate"] = enRepaymentCal.ToEntityReference();
                                    enRepayment["bsd_targetbrokeragefee"] = itemTarget.ToEntityReference();

                                    enRepayment["bsd_customer"] = itemTarget.Contains("bsd_customer") ? itemTarget["bsd_customer"] : null;
                                    enRepayment["bsd_units"] = enUnit.ToEntityReference();

                                    enRepayment["bsd_quote"] = itemTarget.Contains("bsd_basecontract") ? itemTarget["bsd_basecontract"] : null;
                                    enRepayment["bsd_deposit"] = itemTarget.Contains("bsd_datcoc") ? itemTarget["bsd_datcoc"] : null;
                                    enRepayment["bsd_contracts"] = itemTarget.Contains("bsd_contracts") ? itemTarget["bsd_contracts"] : null;
                                    enRepayment["bsd_depositdate"] = itemTarget.Contains("bsd_receiptdate") ? itemTarget["bsd_receiptdate"] : null;
                                    enRepayment["bsd_signeddatecontract"] = itemTarget.Contains("bsd_signedcontractdate") ? itemTarget["bsd_signedcontractdate"] : null;
                                    enRepayment["bsd_signeddatebasecontract"] = itemTarget.Contains("bsd_basecontractsigningdate") ? itemTarget["bsd_basecontractsigningdate"] : null;
                                    enRepayment["bsd_installment1paiddate"] = itemTarget.Contains("bsd_installment1paiddate") ? itemTarget["bsd_installment1paiddate"] : null;
                                    enRepayment["bsd_brokeragefees"] = itemTarget.Contains("bsd_brokeragefees") ? itemTarget["bsd_brokeragefees"] : null;

                                    enRepayment["bsd_totalamount"] = new Money(brokerageFeeValue);
                                    enRepayment["bsd_settlementamount"] = new Money(brokerageFeeHold);
                                    enRepayment["bsd_repaymentmoney"] = new Money(repaymentValue);
                                    enRepayment["bsd_comvalue"] = new Money(COMValue);
                                    enRepayment["statuscode"] = new OptionSetValue(100000002);
                                    totalAmountBrokerage += brokerageFeeValue;
                                    totalRepayment += repaymentValue;
                                    service.Create(enRepayment);
                                }
                                else
                                {
                                    decimal repaymentAdd = 0;
                                    decimal repaymentOld = 0;
                                    var fetchXmlRepaymentOld = $@"<?xml version=""1.0"" encoding=""utf-16""?>
                                <fetch>
                                  <entity name=""bsd_repayment"">
                                    <filter>
                                      <condition attribute=""bsd_brokeragecontract"" operator=""eq"" value=""{enBrokerageContract.Id}"" />
                                      <condition attribute=""bsd_project"" operator=""eq"" value=""{enProject.Id}"" />
                                      <condition attribute=""bsd_appendixcontractbrokerage"" operator=""eq"" value=""{enAppendixContractBrokerage.Id}"" />
                                      <condition attribute=""bsd_quote"" operator=""eq"" value=""{enBaseContract.Id}"" />
                                    </filter>
                                    <order attribute=""createdon"" descending=""true"" />
                                  </entity>
                                </fetch>";
                                    EntityCollection listRepaymentOld = service.RetrieveMultiple(new FetchExpression(fetchXmlRepaymentOld));
                                    if (listRepaymentOld.Entities.Count > 0)
                                    {
                                        repaymentOld = listRepaymentOld.Entities[0].Contains("bsd_repaymentmoney") ? (decimal)((Money)listRepaymentOld.Entities[0]["bsd_repaymentmoney"]).Value : 0;
                                    }

                                    repaymentAdd = brokerageFeeValue - brokerageFeeHold - repaymentOld;

                                    Entity enRepayment = new Entity("bsd_repayment");
                                    enRepayment["bsd_name"] = "Tạm ứng bổ sung sản phẩm " + enBaseContract["name"];
                                    enRepayment["bsd_brokeragecontract"] = itemTarget.Contains("bsd_brokeragecontract") ? itemTarget["bsd_brokeragecontract"] : null;
                                    enRepayment["bsd_project"] = itemTarget.Contains("bsd_project") ? itemTarget["bsd_project"] : null;
                                    enRepayment["bsd_distributor"] = itemTarget.Contains("bsd_distributors") ? itemTarget["bsd_distributors"] : null;
                                    enRepayment["bsd_appendixcontractbrokerage"] = itemTarget.Contains("bsd_appendixcontractbrokerage") ? itemTarget["bsd_appendixcontractbrokerage"] : null;
                                    enRepayment["bsd_brokeragecalculate"] = itemTarget.Contains("bsd_brokeragecalculation") ? itemTarget["bsd_brokeragecalculation"] : null;
                                    enRepayment["bsd_repaymentcalculate"] = enRepaymentCal.ToEntityReference();
                                    enRepayment["bsd_targetbrokeragefee"] = itemTarget.ToEntityReference();

                                    enRepayment["bsd_customer"] = itemTarget.Contains("bsd_customer") ? itemTarget["bsd_customer"] : null;
                                    enRepayment["bsd_units"] = enUnit.ToEntityReference();

                                    enRepayment["bsd_quote"] = itemTarget.Contains("bsd_basecontract") ? itemTarget["bsd_basecontract"] : null;
                                    enRepayment["bsd_deposit"] = itemTarget.Contains("bsd_datcoc") ? itemTarget["bsd_datcoc"] : null;
                                    enRepayment["bsd_contracts"] = itemTarget.Contains("bsd_contracts") ? itemTarget["bsd_contracts"] : null;
                                    enRepayment["bsd_depositdate"] = itemTarget.Contains("bsd_receiptdate") ? itemTarget["bsd_receiptdate"] : null;
                                    enRepayment["bsd_signeddatecontract"] = itemTarget.Contains("bsd_signedcontractdate") ? itemTarget["bsd_signedcontractdate"] : null;
                                    enRepayment["bsd_signeddatebasecontract"] = itemTarget.Contains("bsd_basecontractsigningdate") ? itemTarget["bsd_basecontractsigningdate"] : null;
                                    enRepayment["bsd_installment1paiddate"] = itemTarget.Contains("bsd_installment1paiddate") ? itemTarget["bsd_installment1paiddate"] : null;
                                    enRepayment["bsd_brokeragefees"] = itemTarget.Contains("bsd_brokeragefees") ? itemTarget["bsd_brokeragefees"] : null;

                                    enRepayment["bsd_totalamount"] = new Money(brokerageFeeValue);
                                    enRepayment["bsd_settlementamount"] = new Money(brokerageFeeHold);

                                    enRepayment["bsd_repaymentmoney"] = new Money(repaymentAdd);
                                    enRepayment["bsd_comvalue"] = new Money(COMValue);
                                    enRepayment["statuscode"] = new OptionSetValue(100000002);
                                    totalAmountBrokerage += brokerageFeeValue;
                                    totalRepayment += repaymentAdd;
                                    service.Create(enRepayment);
                                }

                            }
                            else if (calculateBy == 100000000)
                            {
                                decimal COMValue = 0;
                                decimal brokerageFeeValue = 0;
                                decimal brokerageFeeHold = 0;
                                decimal repaymentValue = 0;
                                var unitType = enBaseContract.Contains("bsd_unittype") ? ((OptionSetValue)enBaseContract["bsd_unittype"]).Value : 0;
                                decimal totalAmountDetail = 0;
                                decimal discount = 0;
                                decimal allotmentFee = enBaseContract.Contains("bsd_allotmentfee") ? (decimal)((Money)enBaseContract["bsd_allotmentfee"]).Value : 0;
                                decimal serviceFees = enBaseContract.Contains("serviceFees") ? (decimal)((Money)enBaseContract["serviceFees"]).Value : 0;

                                if (unitType == 100000001)
                                {
                                    totalAmountDetail = enBaseContract.Contains("bsd_totalamountdetails") ? (decimal)((Money)enBaseContract["bsd_totalamountdetails"]).Value : 0;
                                    discount = enBaseContract.Contains("bsd_discount") ? (decimal)((Money)enBaseContract["bsd_discount"]).Value : 0;
                                    COMValue = ((totalAmountDetail - discount) / 1.1M) - allotmentFee - serviceFees;
                                }
                                else
                                {
                                    decimal unitsquare = enUnit.Contains("bsd_areasqm") ? (decimal)enUnit["bsd_areasqm"] : 0;
                                    decimal unitPrice = enUnit.Contains("bsd_landvalueofunit") ? (decimal)((Money)enUnit["bsd_landvalueofunit"]).Value : 0;
                                    discount = enBaseContract.Contains("bsd_discountlandamount") ? (decimal)((Money)enBaseContract["bsd_discountlandamount"]).Value : 0;
                                    COMValue = ((unitPrice * unitsquare - discount) / 1.1M) - allotmentFee - serviceFees;
                                }

                                brokerageFeeValue = percent / 100 * COMValue;
                                brokerageFeeHold = brokerageFeeRetain / 100 * COMValue;
                                repaymentValue = ((percent - brokerageFeeRetain) / 100) * COMValue;

                                if (percent == percentDifferent)
                                {
                                    Entity enRepayment = new Entity("bsd_repayment");
                                    enRepayment["bsd_name"] = "Tạm ứng sản phẩm " + enBaseContract["name"];
                                    enRepayment["bsd_brokeragecontract"] = itemTarget.Contains("bsd_brokeragecontract") ? itemTarget["bsd_brokeragecontract"] : null;
                                    enRepayment["bsd_project"] = itemTarget.Contains("bsd_project") ? itemTarget["bsd_project"] : null;
                                    enRepayment["bsd_distributor"] = itemTarget.Contains("bsd_distributors") ? itemTarget["bsd_distributors"] : null;
                                    enRepayment["bsd_appendixcontractbrokerage"] = itemTarget.Contains("bsd_appendixcontractbrokerage") ? itemTarget["bsd_appendixcontractbrokerage"] : null;
                                    enRepayment["bsd_brokeragecalculate"] = itemTarget.Contains("bsd_brokeragecalculation") ? itemTarget["bsd_brokeragecalculation"] : null;
                                    enRepayment["bsd_repaymentcalculate"] = enRepaymentCal.ToEntityReference();
                                    enRepayment["bsd_targetbrokeragefee"] = itemTarget.ToEntityReference();

                                    enRepayment["bsd_customer"] = itemTarget.Contains("bsd_customer") ? itemTarget["bsd_customer"] : null;
                                    enRepayment["bsd_units"] = enUnit.ToEntityReference();

                                    enRepayment["bsd_quote"] = itemTarget.Contains("bsd_basecontract") ? itemTarget["bsd_basecontract"] : null;
                                    enRepayment["bsd_deposit"] = itemTarget.Contains("bsd_datcoc") ? itemTarget["bsd_datcoc"] : null;
                                    enRepayment["bsd_contracts"] = itemTarget.Contains("bsd_contracts") ? itemTarget["bsd_contracts"] : null;
                                    enRepayment["bsd_depositdate"] = itemTarget.Contains("bsd_receiptdate") ? itemTarget["bsd_receiptdate"] : null;
                                    enRepayment["bsd_signeddatecontract"] = itemTarget.Contains("bsd_signedcontractdate") ? itemTarget["bsd_signedcontractdate"] : null;
                                    enRepayment["bsd_signeddatebasecontract"] = itemTarget.Contains("bsd_basecontractsigningdate") ? itemTarget["bsd_basecontractsigningdate"] : null;
                                    enRepayment["bsd_installment1paiddate"] = itemTarget.Contains("bsd_installment1paiddate") ? itemTarget["bsd_installment1paiddate"] : null;
                                    enRepayment["bsd_brokeragefees"] = itemTarget.Contains("bsd_brokeragefees") ? itemTarget["bsd_brokeragefees"] : null;

                                    enRepayment["bsd_totalamount"] = new Money(brokerageFeeValue);
                                    enRepayment["bsd_settlementamount"] = new Money(brokerageFeeHold);
                                    enRepayment["bsd_repaymentmoney"] = new Money(repaymentValue);
                                    enRepayment["bsd_comvalue"] = new Money(COMValue);
                                    enRepayment["statuscode"] = new OptionSetValue(100000002);
                                    totalAmountBrokerage += brokerageFeeValue;
                                    totalRepayment += repaymentValue;
                                    service.Create(enRepayment);
                                }
                                else
                                {
                                    decimal repaymentAdd = 0;
                                    decimal repaymentOld = 0;
                                    var fetchXmlRepaymentOld = $@"<?xml version=""1.0"" encoding=""utf-16""?>
                                <fetch>
                                  <entity name=""bsd_repayment"">
                                    <filter>
                                      <condition attribute=""bsd_brokeragecontract"" operator=""eq"" value=""{enBrokerageContract.Id}"" />
                                      <condition attribute=""bsd_project"" operator=""eq"" value=""{enProject.Id}"" />
                                      <condition attribute=""bsd_appendixcontractbrokerage"" operator=""eq"" value=""{enAppendixContractBrokerage.Id}"" />
                                      <condition attribute=""bsd_quote"" operator=""eq"" value=""{enBaseContract.Id}"" />
                                    </filter>
                                    <order attribute=""createdon"" descending=""true"" />
                                  </entity>
                                </fetch>";
                                    EntityCollection listRepaymentOld = service.RetrieveMultiple(new FetchExpression(fetchXmlRepaymentOld));
                                    if (listRepaymentOld.Entities.Count > 0)
                                    {
                                        repaymentOld = listRepaymentOld.Entities[0].Contains("bsd_repaymentmoney") ? (decimal)((Money)listRepaymentOld.Entities[0]["bsd_repaymentmoney"]).Value : 0;
                                    }

                                    repaymentAdd = brokerageFeeValue - brokerageFeeHold - repaymentOld;

                                    Entity enRepayment = new Entity("bsd_repayment");
                                    enRepayment["bsd_name"] = "Tạm ứng bổ sung sản phẩm " + enBaseContract["name"];
                                    enRepayment["bsd_brokeragecontract"] = itemTarget.Contains("bsd_brokeragecontract") ? itemTarget["bsd_brokeragecontract"] : null;
                                    enRepayment["bsd_project"] = itemTarget.Contains("bsd_project") ? itemTarget["bsd_project"] : null;
                                    enRepayment["bsd_distributor"] = itemTarget.Contains("bsd_distributors") ? itemTarget["bsd_distributors"] : null;
                                    enRepayment["bsd_appendixcontractbrokerage"] = itemTarget.Contains("bsd_appendixcontractbrokerage") ? itemTarget["bsd_appendixcontractbrokerage"] : null;
                                    enRepayment["bsd_brokeragecalculate"] = itemTarget.Contains("bsd_brokeragecalculation") ? itemTarget["bsd_brokeragecalculation"] : null;
                                    enRepayment["bsd_repaymentcalculate"] = enRepaymentCal.ToEntityReference();
                                    enRepayment["bsd_targetbrokeragefee"] = itemTarget.ToEntityReference();

                                    enRepayment["bsd_customer"] = itemTarget.Contains("bsd_customer") ? itemTarget["bsd_customer"] : null;
                                    enRepayment["bsd_units"] = enUnit.ToEntityReference();

                                    enRepayment["bsd_quote"] = itemTarget.Contains("bsd_basecontract") ? itemTarget["bsd_basecontract"] : null;
                                    enRepayment["bsd_deposit"] = itemTarget.Contains("bsd_datcoc") ? itemTarget["bsd_datcoc"] : null;
                                    enRepayment["bsd_contracts"] = itemTarget.Contains("bsd_contracts") ? itemTarget["bsd_contracts"] : null;
                                    enRepayment["bsd_depositdate"] = itemTarget.Contains("bsd_receiptdate") ? itemTarget["bsd_receiptdate"] : null;
                                    enRepayment["bsd_signeddatecontract"] = itemTarget.Contains("bsd_signedcontractdate") ? itemTarget["bsd_signedcontractdate"] : null;
                                    enRepayment["bsd_signeddatebasecontract"] = itemTarget.Contains("bsd_basecontractsigningdate") ? itemTarget["bsd_basecontractsigningdate"] : null;
                                    enRepayment["bsd_installment1paiddate"] = itemTarget.Contains("bsd_installment1paiddate") ? itemTarget["bsd_installment1paiddate"] : null;
                                    enRepayment["bsd_brokeragefees"] = itemTarget.Contains("bsd_brokeragefees") ? itemTarget["bsd_brokeragefees"] : null;

                                    enRepayment["bsd_totalamount"] = new Money(brokerageFeeValue);
                                    enRepayment["bsd_settlementamount"] = new Money(brokerageFeeHold);
                                    enRepayment["bsd_repaymentmoney"] = new Money(repaymentAdd);
                                    enRepayment["bsd_comvalue"] = new Money(COMValue);
                                    enRepayment["statuscode"] = new OptionSetValue(100000002);
                                    totalAmountBrokerage += brokerageFeeValue;
                                    totalRepayment += repaymentAdd;
                                    service.Create(enRepayment);
                                }
                            }
                        }
                        
                    }
                    Entity enUpTarget = new Entity(itemTarget.LogicalName, itemTarget.Id);
                    enUpTarget["statuscode"] = new OptionSetValue(100000001);
                    service.Update(enUpTarget);
                }

                Entity enUpRepaymentCal = new Entity(enRepaymentCal.LogicalName, enRepaymentCal.Id);
                enUpRepaymentCal["bsd_totalamountbrokeragefee"] = new Money(totalAmountBrokerage);
                enUpRepaymentCal["bsd_totalrepayment"] = new Money(totalRepayment);
                enUpRepaymentCal["statuscode"] = new OptionSetValue(100000000);
                service.Update(enUpRepaymentCal);
            }
            //var fetchXmlRepayment = $@"<?xml version=""1.0"" encoding=""utf-16""?>
            //<fetch>
            //  <entity name=""bsd_repayment"" />
            //</fetch>";
            //EntityCollection listRepayment = service.RetrieveMultiple(new FetchExpression(fetchXmlRepayment));
        }

        private EntityCollection getTarget(Entity enProject, Entity enAppendixContractBrokerage, Entity enBrokerageContract, IOrganizationService service)
        {

            var fetchXml = $@"<?xml version=""1.0"" encoding=""utf-16""?>
            <fetch>
              <entity name=""bsd_targetnpp"">
                <filter>
                  <condition attribute=""bsd_project"" operator=""eq"" value=""{enProject.Id}"" />
                  <condition attribute=""bsd_brokeragecontract"" operator=""eq"" value=""{enBrokerageContract.Id}"" />
                  <condition attribute=""bsd_appendixcontractbrokerage"" operator=""eq"" value=""{enAppendixContractBrokerage.Id}"" />
                  <condition attribute=""statuscode"" operator=""eq"" value=""100000000"" />
                </filter>
              </entity>
            </fetch>";
            EntityCollection listTarget = service.RetrieveMultiple(new FetchExpression(fetchXml));
            
            return listTarget;
        }

        private EntityCollection getListHDMB(Entity enUnit, IOrganizationService service)
        {
            var fetchXml = $@"<?xml version=""1.0"" encoding=""utf-16""?>
            <fetch>
              <entity name=""salesorder"">
                <filter>
                  <condition attribute=""bsd_unitnumber"" operator=""eq"" value=""{enUnit.Id}"" />
                  <condition attribute=""statuscode"" operator=""not-in"">
                    <value>100000008</value>
                  </condition>
                </filter>
              </entity>
            </fetch>";
            EntityCollection listHDMB = service.RetrieveMultiple(new FetchExpression(fetchXml));
            return listHDMB;
        }
        private DateTime RetrieveLocalTimeFromUTCTime(DateTime utcTime, IOrganizationService service)
        {
            int? timeZoneCode = RetrieveCurrentUsersSettings(service);
            if (!timeZoneCode.HasValue)
                throw new InvalidPluginExecutionException("Can't find time zone code");
            var request = new LocalTimeFromUtcTimeRequest
            {
                TimeZoneCode = timeZoneCode.Value,
                UtcTime = utcTime.ToUniversalTime()
            };
            var response = (LocalTimeFromUtcTimeResponse)service.Execute(request);

            return response.LocalTime;
            //var utcTime = utcTime.ToString("MM/dd/yyyy HH:mm:ss");
            //var localDateOnly = response.LocalTime.ToString("dd-MM-yyyy");
        }
        private int? RetrieveCurrentUsersSettings(IOrganizationService service)
        {
            var currentUserSettings = service.RetrieveMultiple(
            new QueryExpression("usersettings")
            {
                ColumnSet = new ColumnSet("localeid", "timezonecode"),
                Criteria = new FilterExpression
                {
                    Conditions = { new ConditionExpression("systemuserid", ConditionOperator.EqualUserId) }
                }
            }).Entities[0].ToEntity<Entity>();

            return (int?)currentUserSettings.Attributes["timezonecode"];
        }
    }
}
