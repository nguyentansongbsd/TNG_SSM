using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Action_SettlementFeeCalculate
{
    public class Action_SettlementFeeCalculate : IPlugin
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
            traceService.Trace("Action_SettlementFeeCalculate");
            

            Entity enSettlementCal = service.Retrieve(target.LogicalName, target.Id, new ColumnSet(true));
            Entity enAppendixContractBrokerage = service.Retrieve(((EntityReference)enSettlementCal["bsd_appendixcontractbrokerage"]).LogicalName, ((EntityReference)enSettlementCal["bsd_appendixcontractbrokerage"]).Id, new ColumnSet(true));
            Entity enBrokerageContract = service.Retrieve(((EntityReference)enSettlementCal["bsd_brokeragecontract"]).LogicalName, ((EntityReference)enSettlementCal["bsd_brokeragecontract"]).Id, new ColumnSet(true));
            Entity enProject = service.Retrieve(((EntityReference)enSettlementCal["bsd_project"]).LogicalName, ((EntityReference)enSettlementCal["bsd_project"]).Id, new ColumnSet(true));
            int calculateBy = enAppendixContractBrokerage.Contains("bsd_brokeragefee") ? ((OptionSetValue)enAppendixContractBrokerage["bsd_brokeragefee"]).Value : 0;
            decimal brokerageFeeRetain = enAppendixContractBrokerage.Contains("bsd_brokeragefeesretain") ? (decimal)enAppendixContractBrokerage["bsd_brokeragefeesretain"] : 0;
            decimal totalAmountBrokerage = 0;
            decimal totalRepayment = 0;
            decimal totalSettlment = 0;

            List<Entity> listHDMBPromotion = new List<Entity>();
            List<Entity> listHDMBNoPromotion = new List<Entity>();
            getListContract(enProject, enAppendixContractBrokerage, enBrokerageContract, ref listHDMBPromotion, ref listHDMBNoPromotion, service);
            //Co CT Khuyen Mai
            if (listHDMBPromotion.Count > 0)
            {
                foreach (var itemHDMB in listHDMBPromotion)
                {
                    int statusContracts = itemHDMB.Contains("statuscode") ? ((OptionSetValue)itemHDMB["statuscode"]).Value : 0;
                    if (statusContracts != 100000008)
                    {
                        
                        var fetchXmlRepayment = $@"<?xml version=""1.0"" encoding=""utf-16""?>
                        <fetch>
                          <entity name=""bsd_repayment"">
                            <filter>
                              <condition attribute=""bsd_brokeragecontract"" operator=""eq"" value=""{enBrokerageContract.Id}"" />
                              <condition attribute=""bsd_appendixcontractbrokerage"" operator=""eq"" value=""{enAppendixContractBrokerage.Id}"" />
                              <condition attribute=""bsd_project"" operator=""eq"" value=""{enProject.Id}"" />
                              <condition attribute=""bsd_contracts"" operator=""eq"" value=""{itemHDMB.Id}"" />
                            </filter>
                            <order attribute=""createdon"" descending=""true"" />
                          </entity>
                        </fetch>";
                        EntityCollection listRepayment = service.RetrieveMultiple(new FetchExpression(fetchXmlRepayment));
                        if (listRepayment.Entities.Count == 1)
                        {
                            decimal percentTarget = 0;
                            Entity enContracts = service.Retrieve(itemHDMB.LogicalName, itemHDMB.Id, new ColumnSet(true));
                            Entity enBaseContracts = listRepayment.Entities[0].Contains("bsd_quote") ? service.Retrieve(((EntityReference)listRepayment.Entities[0]["bsd_quote"]).LogicalName, ((EntityReference)listRepayment.Entities[0]["bsd_quote"]).Id, new ColumnSet(true)) : new Entity();
                            Entity enUnit = service.Retrieve(((EntityReference)enContracts["bsd_unitnumber"]).LogicalName, ((EntityReference)enContracts["bsd_unitnumber"]).Id, new ColumnSet(true));
                            Entity enTarget = service.Retrieve(((EntityReference)listRepayment.Entities[0]["bsd_targetbrokeragefee"]).LogicalName, ((EntityReference)listRepayment.Entities[0]["bsd_targetbrokeragefee"]).Id, new ColumnSet(true));
                            percentTarget = enTarget.Contains("bsd_percen") ? (decimal)enTarget["bsd_percen"] : 0;
                            decimal repaymentValue = listRepayment.Entities[0].Contains("bsd_repaymentmoney") ? (decimal)((Money)listRepayment.Entities[0]["bsd_repaymentmoney"]).Value : 0;
                            if (calculateBy == 100000001)
                            {
                                decimal COMValue = 0;
                                decimal totalamountbrokeragefee = 0;
                                decimal settlementamount = 0;


                                decimal totalAmountDetail = enContracts.Contains("bsd_totalamountdetails") ? (decimal)((Money)enContracts["bsd_totalamountdetails"]).Value : 0;
                                decimal discount = enContracts.Contains("bsd_discount") ? (decimal)((Money)enContracts["bsd_discount"]).Value : 0;
                                decimal allotmentFee = enContracts.Contains("bsd_allotmentfee") ? (decimal)((Money)enContracts["bsd_allotmentfee"]).Value : 0;
                                decimal serviceFees = enContracts.Contains("serviceFees") ? (decimal)((Money)enContracts["serviceFees"]).Value : 0;

                                COMValue = ((totalAmountDetail - discount) / 1.1M) - allotmentFee - serviceFees;


                                repaymentValue = listRepayment.Entities[0].Contains("bsd_repaymentmoney") ? (decimal)((Money)listRepayment.Entities[0]["bsd_repaymentmoney"]).Value : 0;
                                totalamountbrokeragefee = COMValue * percentTarget / 100;

                                settlementamount = COMValue * brokerageFeeRetain / 100;
                                str.AppendLine("Total Detail " + totalAmountDetail);
                                str.AppendLine("Discount " + discount);
                                str.AppendLine("Retain " + brokerageFeeRetain);
                                str.AppendLine("COM " + COMValue);
                                str.AppendLine("1 " + totalamountbrokeragefee);
                                str.AppendLine("2 " + repaymentValue);
                                str.AppendLine("3 " + settlementamount);

                                Entity enSettlement = new Entity("bsd_settlement");
                                enSettlement["bsd_name"] = "Quyết toán sản phẩm " + enContracts["name"];
                                enSettlement["bsd_brokeragecontract"] = listRepayment.Entities[0].Contains("bsd_brokeragecontract") ? listRepayment.Entities[0]["bsd_brokeragecontract"] : null;
                                enSettlement["bsd_project"] = listRepayment.Entities[0].Contains("bsd_project") ? listRepayment.Entities[0]["bsd_project"] : null;
                                enSettlement["bsd_distributor"] = listRepayment.Entities[0].Contains("bsd_distributor") ? listRepayment.Entities[0]["bsd_distributor"] : null;
                                enSettlement["bsd_appendixcontractbrokerage"] = listRepayment.Entities[0].Contains("bsd_appendixcontractbrokerage") ? listRepayment.Entities[0]["bsd_appendixcontractbrokerage"] : null;
                                enSettlement["bsd_brokeragecalculate"] = listRepayment.Entities[0].Contains("bsd_brokeragecalculation") ? listRepayment.Entities[0]["bsd_brokeragecalculation"] : null;

                                enSettlement["bsd_repayment"] = listRepayment.Entities[0].ToEntityReference();
                                enSettlement["bsd_settlementcalculate"] = enSettlementCal.ToEntityReference();

                                enSettlement["bsd_customer"] = listRepayment.Entities[0].Contains("bsd_customer") ? listRepayment.Entities[0]["bsd_customer"] : null;
                                enSettlement["bsd_units"] = enUnit.ToEntityReference();

                                enSettlement["bsd_deposit"] = listRepayment.Entities[0].Contains("bsd_deposit") ? listRepayment.Entities[0]["bsd_deposit"] : null;
                                enSettlement["bsd_quote"] = listRepayment.Entities[0].Contains("bsd_quote") ? listRepayment.Entities[0]["bsd_quote"] : null;
                                enSettlement["bsd_contracts"] = listRepayment.Entities[0].Contains("bsd_contracts") ? listRepayment.Entities[0]["bsd_contracts"] : null;
                                enSettlement["bsd_depositdate"] = listRepayment.Entities[0].Contains("bsd_depositdate") ? listRepayment.Entities[0]["bsd_depositdate"] : null;
                                enSettlement["bsd_signeddatebasecontract"] = listRepayment.Entities[0].Contains("bsd_signeddatebasecontract") ? listRepayment.Entities[0]["bsd_signeddatebasecontract"] : null;
                                enSettlement["bsd_installment1paiddate"] = listRepayment.Entities[0].Contains("bsd_installment1paiddate") ? listRepayment.Entities[0]["bsd_installment1paiddate"] : null;
                                enSettlement["bsd_signeddatecontract"] = listRepayment.Entities[0].Contains("bsd_signeddatecontract") ? listRepayment.Entities[0]["bsd_signeddatecontract"] : null;

                                enSettlement["bsd_totalamountbrokeragefee"] = new Money(totalamountbrokeragefee);
                                enSettlement["bsd_repaymentamount"] = new Money(repaymentValue);
                                enSettlement["bsd_settlementamount"] = new Money(settlementamount);
                                enSettlement["statuscode"] = new OptionSetValue(100000001);

                                totalAmountBrokerage += totalamountbrokeragefee;
                                totalRepayment += repaymentValue;
                                totalSettlment += settlementamount;
                                service.Create(enSettlement);


                                //Update Status Tam Ung
                                Entity enUpRepayment = new Entity(listRepayment.Entities[0].LogicalName, listRepayment.Entities[0].Id);
                                enUpRepayment["statuscode"] = new OptionSetValue(100000001);
                                service.Update(enUpRepayment);

                                //Update Status Chi tieu
                                var fetchXmlListTarget = $@"<?xml version=""1.0"" encoding=""utf-16""?>
                                <fetch>
                                  <entity name=""bsd_targetnpp"">
                                    <filter>
                                      <condition attribute=""bsd_brokeragecontract"" operator=""eq"" value=""{enBrokerageContract.Id}"" />
                                      <condition attribute=""bsd_project"" operator=""eq"" value=""{enProject.Id}"" />
                                      <condition attribute=""bsd_appendixcontractbrokerage"" operator=""eq"" value=""{enAppendixContractBrokerage.Id}"" />
                                      <condition attribute=""bsd_basecontract"" operator=""eq"" value=""{enBaseContracts.Id}"" />
                                    </filter>
                                  </entity>
                                </fetch>";
                                EntityCollection listTarget = service.RetrieveMultiple(new FetchExpression(fetchXmlListTarget));
                                if (listTarget.Entities.Count > 0)
                                {
                                    foreach (var itemTarget in listTarget.Entities)
                                    {
                                        Entity enUpTarget = new Entity(itemTarget.LogicalName, itemTarget.Id);
                                        enUpTarget["statuscode"] = new OptionSetValue(100000002);
                                        service.Update(enUpTarget);
                                    }
                                }
                            }

                            else if (calculateBy == 100000000)
                            {
                                var unitType = enContracts.Contains("bsd_typeunit") ? ((OptionSetValue)enContracts["bsd_typeunit"]).Value : 0;

                                decimal COMValue = 0;
                                decimal totalamountbrokeragefee = 0;
                                decimal settlementamount = 0;

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

                                repaymentValue = listRepayment.Entities[0].Contains("bsd_repaymentmoney") ? (decimal)((Money)listRepayment.Entities[0]["bsd_repaymentmoney"]).Value : 0;
                                totalamountbrokeragefee = COMValue * percentTarget / 100;
                                settlementamount = COMValue * brokerageFeeRetain / 100;
                                str.AppendLine("Total Detail " + totalAmountDetail);
                                str.AppendLine("Discount " + discount);
                                str.AppendLine("Retain " + brokerageFeeRetain);
                                str.AppendLine("COM " + COMValue);
                                str.AppendLine("1 " + totalamountbrokeragefee);
                                str.AppendLine("2 " + repaymentValue);
                                str.AppendLine("3 " + settlementamount);

                                Entity enSettlement = new Entity("bsd_settlement");
                                enSettlement["bsd_name"] = "Quyết toán sản phẩm " + enContracts["name"];
                                enSettlement["bsd_brokeragecontract"] = listRepayment.Entities[0].Contains("bsd_brokeragecontract") ? listRepayment.Entities[0]["bsd_brokeragecontract"] : null;
                                enSettlement["bsd_project"] = listRepayment.Entities[0].Contains("bsd_project") ? listRepayment.Entities[0]["bsd_project"] : null;
                                enSettlement["bsd_distributor"] = listRepayment.Entities[0].Contains("bsd_distributor") ? listRepayment.Entities[0]["bsd_distributor"] : null;
                                enSettlement["bsd_appendixcontractbrokerage"] = listRepayment.Entities[0].Contains("bsd_appendixcontractbrokerage") ? listRepayment.Entities[0]["bsd_appendixcontractbrokerage"] : null;
                                enSettlement["bsd_brokeragecalculate"] = listRepayment.Entities[0].Contains("bsd_brokeragecalculation") ? listRepayment.Entities[0]["bsd_brokeragecalculation"] : null;

                                enSettlement["bsd_repayment"] = listRepayment.Entities[0].ToEntityReference();
                                enSettlement["bsd_settlementcalculate"] = enSettlementCal.ToEntityReference();

                                enSettlement["bsd_customer"] = listRepayment.Entities[0].Contains("bsd_customer") ? listRepayment.Entities[0]["bsd_customer"] : null;
                                enSettlement["bsd_units"] = enUnit.ToEntityReference();

                                enSettlement["bsd_deposit"] = listRepayment.Entities[0].Contains("bsd_deposit") ? listRepayment.Entities[0]["bsd_deposit"] : null;
                                enSettlement["bsd_quote"] = listRepayment.Entities[0].Contains("bsd_quote") ? listRepayment.Entities[0]["bsd_quote"] : null;
                                enSettlement["bsd_contracts"] = listRepayment.Entities[0].Contains("bsd_contracts") ? listRepayment.Entities[0]["bsd_contracts"] : null;
                                enSettlement["bsd_depositdate"] = listRepayment.Entities[0].Contains("bsd_depositdate") ? listRepayment.Entities[0]["bsd_depositdate"] : null;
                                enSettlement["bsd_signeddatebasecontract"] = listRepayment.Entities[0].Contains("bsd_signeddatebasecontract") ? listRepayment.Entities[0]["bsd_signeddatebasecontract"] : null;
                                enSettlement["bsd_installment1paiddate"] = listRepayment.Entities[0].Contains("bsd_installment1paiddate") ? listRepayment.Entities[0]["bsd_installment1paiddate"] : null;
                                enSettlement["bsd_signeddatecontract"] = listRepayment.Entities[0].Contains("bsd_signeddatecontract") ? listRepayment.Entities[0]["bsd_signeddatecontract"] : null;

                                enSettlement["bsd_totalamountbrokeragefee"] = new Money(totalamountbrokeragefee);
                                enSettlement["bsd_repaymentamount"] = new Money(repaymentValue);
                                enSettlement["bsd_settlementamount"] = new Money(settlementamount);
                                enSettlement["statuscode"] = new OptionSetValue(100000001);

                                totalAmountBrokerage += totalamountbrokeragefee;
                                totalRepayment += repaymentValue;
                                totalSettlment += settlementamount;
                                service.Create(enSettlement);

                                //Update Status Tam Ung
                                Entity enUpRepayment = new Entity(listRepayment.Entities[0].LogicalName, listRepayment.Entities[0].Id);
                                enUpRepayment["statuscode"] = new OptionSetValue(100000001);
                                service.Update(enUpRepayment);

                                //Update Status Chi tieu
                                var fetchXmlListTarget = $@"<?xml version=""1.0"" encoding=""utf-16""?>
                            <fetch>
                              <entity name=""bsd_targetnpp"">
                                <filter>
                                  <condition attribute=""bsd_brokeragecontract"" operator=""eq"" value=""{enBrokerageContract.Id}"" />
                                  <condition attribute=""bsd_project"" operator=""eq"" value=""{enProject.Id}"" />
                                  <condition attribute=""bsd_appendixcontractbrokerage"" operator=""eq"" value=""{enAppendixContractBrokerage.Id}"" />
                                  <condition attribute=""bsd_basecontract"" operator=""eq"" value=""{enBaseContracts.Id}"" />
                                </filter>
                              </entity>
                            </fetch>";
                                EntityCollection listTarget = service.RetrieveMultiple(new FetchExpression(fetchXmlListTarget));
                                if (listTarget.Entities.Count > 0)
                                {
                                    foreach (var itemTarget in listTarget.Entities)
                                    {
                                        Entity enUpTarget = new Entity(itemTarget.LogicalName, itemTarget.Id);
                                        enUpTarget["statuscode"] = new OptionSetValue(100000002);
                                        service.Update(enUpTarget);
                                    }
                                }
                            }
                        }

                        else if (listRepayment.Entities.Count > 1)
                        {
                            decimal percentTarget = 0;
                            Entity enContracts = service.Retrieve(itemHDMB.LogicalName, itemHDMB.Id, new ColumnSet(true));
                            Entity enBaseContracts = listRepayment.Entities[0].Contains("bsd_quote") ? service.Retrieve(((EntityReference)listRepayment.Entities[0]["bsd_quote"]).LogicalName, ((EntityReference)listRepayment.Entities[0]["bsd_quote"]).Id, new ColumnSet(true)) : new Entity();
                            Entity enUnit = service.Retrieve(((EntityReference)enContracts["bsd_unitnumber"]).LogicalName, ((EntityReference)enContracts["bsd_unitnumber"]).Id, new ColumnSet(true));
                            Entity enTarget = service.Retrieve(((EntityReference)listRepayment.Entities[0]["bsd_targetbrokeragefee"]).LogicalName, ((EntityReference)listRepayment.Entities[0]["bsd_targetbrokeragefee"]).Id, new ColumnSet(true));
                            percentTarget = enTarget.Contains("bsd_percen") ? (decimal)enTarget["bsd_percen"] : 0;
                            decimal repaymentValue = listRepayment.Entities[0].Contains("bsd_repaymentmoney") ? (decimal)((Money)listRepayment.Entities[0]["bsd_repaymentmoney"]).Value : 0;

                            if (calculateBy == 100000001)
                            {
                                decimal COMValue = 0;
                                decimal totalamountbrokeragefee = 0;
                                decimal settlementamount = 0;


                                decimal totalAmountDetail = enContracts.Contains("bsd_totalamountdetails") ? (decimal)((Money)enContracts["bsd_totalamountdetails"]).Value : 0;
                                decimal discount = enContracts.Contains("bsd_discount") ? (decimal)((Money)enContracts["bsd_discount"]).Value : 0;
                                decimal allotmentFee = enContracts.Contains("bsd_allotmentfee") ? (decimal)((Money)enContracts["bsd_allotmentfee"]).Value : 0;
                                decimal serviceFees = enContracts.Contains("serviceFees") ? (decimal)((Money)enContracts["serviceFees"]).Value : 0;

                                COMValue = ((totalAmountDetail - discount) / 1.1M) - allotmentFee - serviceFees;

                                totalamountbrokeragefee = COMValue * percentTarget / 100;

                                settlementamount = COMValue * brokerageFeeRetain / 100;
                                str.AppendLine("Total Detail " + totalAmountDetail);
                                str.AppendLine("Discount " + discount);
                                str.AppendLine("Retain " + brokerageFeeRetain);
                                str.AppendLine("COM " + COMValue);
                                str.AppendLine("1 " + totalamountbrokeragefee);
                                str.AppendLine("2 " + repaymentValue);
                                str.AppendLine("3 " + settlementamount);

                                Entity enSettlement = new Entity("bsd_settlement");
                                enSettlement["bsd_name"] = "Quyết toán sản phẩm " + enContracts["name"];
                                enSettlement["bsd_brokeragecontract"] = listRepayment.Entities[0].Contains("bsd_brokeragecontract") ? listRepayment.Entities[0]["bsd_brokeragecontract"] : null;
                                enSettlement["bsd_project"] = listRepayment.Entities[0].Contains("bsd_project") ? listRepayment.Entities[0]["bsd_project"] : null;
                                enSettlement["bsd_distributor"] = listRepayment.Entities[0].Contains("bsd_distributor") ? listRepayment.Entities[0]["bsd_distributor"] : null;
                                enSettlement["bsd_appendixcontractbrokerage"] = listRepayment.Entities[0].Contains("bsd_appendixcontractbrokerage") ? listRepayment.Entities[0]["bsd_appendixcontractbrokerage"] : null;
                                enSettlement["bsd_brokeragecalculate"] = listRepayment.Entities[0].Contains("bsd_brokeragecalculation") ? listRepayment.Entities[0]["bsd_brokeragecalculation"] : null;

                                enSettlement["bsd_repayment"] = listRepayment.Entities[0].ToEntityReference();
                                enSettlement["bsd_settlementcalculate"] = enSettlementCal.ToEntityReference();

                                enSettlement["bsd_customer"] = listRepayment.Entities[0].Contains("bsd_customer") ? listRepayment.Entities[0]["bsd_customer"] : null;
                                enSettlement["bsd_units"] = enUnit.ToEntityReference();

                                enSettlement["bsd_deposit"] = listRepayment.Entities[0].Contains("bsd_deposit") ? listRepayment.Entities[0]["bsd_deposit"] : null;
                                enSettlement["bsd_quote"] = listRepayment.Entities[0].Contains("bsd_quote") ? listRepayment.Entities[0]["bsd_quote"] : null;
                                enSettlement["bsd_contracts"] = listRepayment.Entities[0].Contains("bsd_contracts") ? listRepayment.Entities[0]["bsd_contracts"] : null;
                                enSettlement["bsd_depositdate"] = listRepayment.Entities[0].Contains("bsd_depositdate") ? listRepayment.Entities[0]["bsd_depositdate"] : null;
                                enSettlement["bsd_signeddatebasecontract"] = listRepayment.Entities[0].Contains("bsd_signeddatebasecontract") ? listRepayment.Entities[0]["bsd_signeddatebasecontract"] : null;
                                enSettlement["bsd_installment1paiddate"] = listRepayment.Entities[0].Contains("bsd_installment1paiddate") ? listRepayment.Entities[0]["bsd_installment1paiddate"] : null;
                                enSettlement["bsd_signeddatecontract"] = listRepayment.Entities[0].Contains("bsd_signeddatecontract") ? listRepayment.Entities[0]["bsd_signeddatecontract"] : null;

                                enSettlement["bsd_totalamountbrokeragefee"] = new Money(totalamountbrokeragefee);
                                enSettlement["bsd_repaymentamount"] = new Money(repaymentValue);
                                enSettlement["bsd_settlementamount"] = new Money(settlementamount);
                                enSettlement["statuscode"] = new OptionSetValue(100000001);

                                totalAmountBrokerage += totalamountbrokeragefee;
                                totalRepayment += repaymentValue;
                                totalSettlment += settlementamount;
                                service.Create(enSettlement);

                                //Update Status Tam Ung
                                foreach (var itemRepayment in listRepayment.Entities)
                                {
                                    Entity enUpRepayment = new Entity(itemRepayment.LogicalName, itemRepayment.Id);
                                    enUpRepayment["statuscode"] = new OptionSetValue(100000001);
                                    service.Update(enUpRepayment);
                                }

                                //Update Status Chi tieu
                                var fetchXmlListTarget = $@"<?xml version=""1.0"" encoding=""utf-16""?>
                            <fetch>
                              <entity name=""bsd_targetnpp"">
                                <filter>
                                  <condition attribute=""bsd_brokeragecontract"" operator=""eq"" value=""{enBrokerageContract.Id}"" />
                                  <condition attribute=""bsd_project"" operator=""eq"" value=""{enProject.Id}"" />
                                  <condition attribute=""bsd_appendixcontractbrokerage"" operator=""eq"" value=""{enAppendixContractBrokerage.Id}"" />
                                  <condition attribute=""bsd_basecontract"" operator=""eq"" value=""{enBaseContracts.Id}"" />
                                </filter>
                              </entity>
                            </fetch>";
                                EntityCollection listTarget = service.RetrieveMultiple(new FetchExpression(fetchXmlListTarget));
                                if (listTarget.Entities.Count > 0)
                                {
                                    foreach (var itemTarget in listTarget.Entities)
                                    {
                                        Entity enUpTarget = new Entity(itemTarget.LogicalName, itemTarget.Id);
                                        enUpTarget["statuscode"] = new OptionSetValue(100000002);
                                        service.Update(enUpTarget);
                                    }
                                }

                            }

                            else if (calculateBy == 100000000)
                            {
                                var unitType = enContracts.Contains("bsd_typeunit") ? ((OptionSetValue)enContracts["bsd_typeunit"]).Value : 0;

                                decimal COMValue = 0;
                                decimal totalamountbrokeragefee = 0;
                                decimal settlementamount = 0;

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

                                totalamountbrokeragefee = COMValue * percentTarget / 100;
                                settlementamount = COMValue * brokerageFeeRetain / 100;
                                str.AppendLine("Total Detail " + totalAmountDetail);
                                str.AppendLine("Discount " + discount);
                                str.AppendLine("Retain " + brokerageFeeRetain);
                                str.AppendLine("COM " + COMValue);
                                str.AppendLine("1 " + totalamountbrokeragefee);
                                str.AppendLine("2 " + repaymentValue);
                                str.AppendLine("3 " + settlementamount);

                                Entity enSettlement = new Entity("bsd_settlement");
                                enSettlement["bsd_name"] = "Quyết toán sản phẩm " + enContracts["name"];
                                enSettlement["bsd_brokeragecontract"] = listRepayment.Entities[0].Contains("bsd_brokeragecontract") ? listRepayment.Entities[0]["bsd_brokeragecontract"] : null;
                                enSettlement["bsd_project"] = listRepayment.Entities[0].Contains("bsd_project") ? listRepayment.Entities[0]["bsd_project"] : null;
                                enSettlement["bsd_distributor"] = listRepayment.Entities[0].Contains("bsd_distributor") ? listRepayment.Entities[0]["bsd_distributor"] : null;
                                enSettlement["bsd_appendixcontractbrokerage"] = listRepayment.Entities[0].Contains("bsd_appendixcontractbrokerage") ? listRepayment.Entities[0]["bsd_appendixcontractbrokerage"] : null;
                                enSettlement["bsd_brokeragecalculate"] = listRepayment.Entities[0].Contains("bsd_brokeragecalculation") ? listRepayment.Entities[0]["bsd_brokeragecalculation"] : null;

                                enSettlement["bsd_repayment"] = listRepayment.Entities[0].ToEntityReference();
                                enSettlement["bsd_settlementcalculate"] = enSettlementCal.ToEntityReference();

                                enSettlement["bsd_customer"] = listRepayment.Entities[0].Contains("bsd_customer") ? listRepayment.Entities[0]["bsd_customer"] : null;
                                enSettlement["bsd_units"] = enUnit.ToEntityReference();

                                enSettlement["bsd_deposit"] = listRepayment.Entities[0].Contains("bsd_deposit") ? listRepayment.Entities[0]["bsd_deposit"] : null;
                                enSettlement["bsd_quote"] = listRepayment.Entities[0].Contains("bsd_quote") ? listRepayment.Entities[0]["bsd_quote"] : null;
                                enSettlement["bsd_contracts"] = listRepayment.Entities[0].Contains("bsd_contracts") ? listRepayment.Entities[0]["bsd_contracts"] : null;
                                enSettlement["bsd_depositdate"] = listRepayment.Entities[0].Contains("bsd_depositdate") ? listRepayment.Entities[0]["bsd_depositdate"] : null;
                                enSettlement["bsd_signeddatebasecontract"] = listRepayment.Entities[0].Contains("bsd_signeddatebasecontract") ? listRepayment.Entities[0]["bsd_signeddatebasecontract"] : null;
                                enSettlement["bsd_installment1paiddate"] = listRepayment.Entities[0].Contains("bsd_installment1paiddate") ? listRepayment.Entities[0]["bsd_installment1paiddate"] : null;
                                enSettlement["bsd_signeddatecontract"] = listRepayment.Entities[0].Contains("bsd_signeddatecontract") ? listRepayment.Entities[0]["bsd_signeddatecontract"] : null;

                                enSettlement["bsd_totalamountbrokeragefee"] = new Money(totalamountbrokeragefee);
                                enSettlement["bsd_repaymentamount"] = new Money(repaymentValue);
                                enSettlement["bsd_settlementamount"] = new Money(settlementamount);
                                enSettlement["statuscode"] = new OptionSetValue(100000001);

                                totalAmountBrokerage += totalamountbrokeragefee;
                                totalRepayment += repaymentValue;
                                totalSettlment += settlementamount;
                                service.Create(enSettlement);

                                //Update Status Tam Ung
                                foreach (var itemRepayment in listRepayment.Entities)
                                {
                                    Entity enUpRepayment = new Entity(itemRepayment.LogicalName, itemRepayment.Id);
                                    enUpRepayment["statuscode"] = new OptionSetValue(100000001);
                                    service.Update(enUpRepayment);
                                }

                                //Update Status Chi tieu
                                var fetchXmlListTarget = $@"<?xml version=""1.0"" encoding=""utf-16""?>
                            <fetch>
                              <entity name=""bsd_targetnpp"">
                                <filter>
                                  <condition attribute=""bsd_brokeragecontract"" operator=""eq"" value=""{enBrokerageContract.Id}"" />
                                  <condition attribute=""bsd_project"" operator=""eq"" value=""{enProject.Id}"" />
                                  <condition attribute=""bsd_appendixcontractbrokerage"" operator=""eq"" value=""{enAppendixContractBrokerage.Id}"" />
                                  <condition attribute=""bsd_basecontract"" operator=""eq"" value=""{enBaseContracts.Id}"" />
                                </filter>
                              </entity>
                            </fetch>";
                                EntityCollection listTarget = service.RetrieveMultiple(new FetchExpression(fetchXmlListTarget));
                                if (listTarget.Entities.Count > 0)
                                {
                                    foreach (var itemTarget in listTarget.Entities)
                                    {
                                        Entity enUpTarget = new Entity(itemTarget.LogicalName, itemTarget.Id);
                                        enUpTarget["statuscode"] = new OptionSetValue(100000002);
                                        service.Update(enUpTarget);
                                    }
                                }

                            }
                        }
                    }

                }
            }

            //Khong Co CT Khuyen Mai
            if (listHDMBNoPromotion.Count > 0)
            {
                foreach (var itemHDMB in listHDMBNoPromotion)
                {
                    int statusContracts = itemHDMB.Contains("statuscode") ? ((OptionSetValue)itemHDMB["statuscode"]).Value : 0;

                    if (statusContracts != 100000008)
                    {
                        var fetchXmlRepayment = $@"<?xml version=""1.0"" encoding=""utf-16""?>
                        <fetch>
                          <entity name=""bsd_repayment"">
                            <filter>
                              <condition attribute=""bsd_brokeragecontract"" operator=""eq"" value=""{enBrokerageContract.Id}"" />
                              <condition attribute=""bsd_appendixcontractbrokerage"" operator=""eq"" value=""{enAppendixContractBrokerage.Id}"" />
                              <condition attribute=""bsd_project"" operator=""eq"" value=""{enProject.Id}"" />
                              <condition attribute=""bsd_contracts"" operator=""eq"" value=""{itemHDMB.Id}"" />
                            </filter>
                            <order attribute=""createdon"" descending=""true"" />
                          </entity>
                        </fetch>";
                        EntityCollection listRepayment = service.RetrieveMultiple(new FetchExpression(fetchXmlRepayment));
                        if (listRepayment.Entities.Count == 1)
                        {
                            decimal percentTarget = 0;
                            Entity enContracts = service.Retrieve(itemHDMB.LogicalName, itemHDMB.Id, new ColumnSet(true));
                            Entity enBaseContracts = listRepayment.Entities[0].Contains("bsd_quote") ? service.Retrieve(((EntityReference)listRepayment.Entities[0]["bsd_quote"]).LogicalName, ((EntityReference)listRepayment.Entities[0]["bsd_quote"]).Id, new ColumnSet(true)) : new Entity();
                            Entity enUnit = service.Retrieve(((EntityReference)enContracts["bsd_unitnumber"]).LogicalName, ((EntityReference)enContracts["bsd_unitnumber"]).Id, new ColumnSet(true));
                            Entity enTarget = service.Retrieve(((EntityReference)listRepayment.Entities[0]["bsd_targetbrokeragefee"]).LogicalName, ((EntityReference)listRepayment.Entities[0]["bsd_targetbrokeragefee"]).Id, new ColumnSet(true));
                            percentTarget = enTarget.Contains("bsd_percen") ? (decimal)enTarget["bsd_percen"] : 0;
                            decimal repaymentValue = listRepayment.Entities[0].Contains("bsd_repaymentmoney") ? (decimal)((Money)listRepayment.Entities[0]["bsd_repaymentmoney"]).Value : 0;
                            if (calculateBy == 100000001)
                            {
                                decimal COMValue = 0;
                                decimal totalamountbrokeragefee = 0;
                                decimal settlementamount = 0;


                                decimal totalAmountDetail = enContracts.Contains("bsd_totalamountdetails") ? (decimal)((Money)enContracts["bsd_totalamountdetails"]).Value : 0;
                                decimal discount = enContracts.Contains("bsd_discount") ? (decimal)((Money)enContracts["bsd_discount"]).Value : 0;
                                decimal allotmentFee = enContracts.Contains("bsd_allotmentfee") ? (decimal)((Money)enContracts["bsd_allotmentfee"]).Value : 0;
                                decimal serviceFees = enContracts.Contains("serviceFees") ? (decimal)((Money)enContracts["serviceFees"]).Value : 0;

                                COMValue = ((totalAmountDetail - discount) / 1.1M) - allotmentFee - serviceFees;


                                repaymentValue = listRepayment.Entities[0].Contains("bsd_repaymentmoney") ? (decimal)((Money)listRepayment.Entities[0]["bsd_repaymentmoney"]).Value : 0;
                                totalamountbrokeragefee = COMValue * percentTarget / 100;

                                settlementamount = COMValue * brokerageFeeRetain / 100;
                                str.AppendLine("Total Detail " + totalAmountDetail);
                                str.AppendLine("Discount " + discount);
                                str.AppendLine("Retain " + brokerageFeeRetain);
                                str.AppendLine("COM " + COMValue);
                                str.AppendLine("1 " + totalamountbrokeragefee);
                                str.AppendLine("2 " + repaymentValue);
                                str.AppendLine("3 " + settlementamount);

                                Entity enSettlement = new Entity("bsd_settlement");
                                enSettlement["bsd_name"] = "Quyết toán sản phẩm " + enContracts["name"];
                                enSettlement["bsd_brokeragecontract"] = listRepayment.Entities[0].Contains("bsd_brokeragecontract") ? listRepayment.Entities[0]["bsd_brokeragecontract"] : null;
                                enSettlement["bsd_project"] = listRepayment.Entities[0].Contains("bsd_project") ? listRepayment.Entities[0]["bsd_project"] : null;
                                enSettlement["bsd_distributor"] = listRepayment.Entities[0].Contains("bsd_distributor") ? listRepayment.Entities[0]["bsd_distributor"] : null;
                                enSettlement["bsd_appendixcontractbrokerage"] = listRepayment.Entities[0].Contains("bsd_appendixcontractbrokerage") ? listRepayment.Entities[0]["bsd_appendixcontractbrokerage"] : null;
                                enSettlement["bsd_brokeragecalculate"] = listRepayment.Entities[0].Contains("bsd_brokeragecalculation") ? listRepayment.Entities[0]["bsd_brokeragecalculation"] : null;

                                enSettlement["bsd_repayment"] = listRepayment.Entities[0].ToEntityReference();
                                enSettlement["bsd_settlementcalculate"] = enSettlementCal.ToEntityReference();

                                enSettlement["bsd_customer"] = listRepayment.Entities[0].Contains("bsd_customer") ? listRepayment.Entities[0]["bsd_customer"] : null;
                                enSettlement["bsd_units"] = enUnit.ToEntityReference();

                                enSettlement["bsd_deposit"] = listRepayment.Entities[0].Contains("bsd_deposit") ? listRepayment.Entities[0]["bsd_deposit"] : null;
                                enSettlement["bsd_quote"] = listRepayment.Entities[0].Contains("bsd_quote") ? listRepayment.Entities[0]["bsd_quote"] : null;
                                enSettlement["bsd_contracts"] = listRepayment.Entities[0].Contains("bsd_contracts") ? listRepayment.Entities[0]["bsd_contracts"] : null;
                                enSettlement["bsd_depositdate"] = listRepayment.Entities[0].Contains("bsd_depositdate") ? listRepayment.Entities[0]["bsd_depositdate"] : null;
                                enSettlement["bsd_signeddatebasecontract"] = listRepayment.Entities[0].Contains("bsd_signeddatebasecontract") ? listRepayment.Entities[0]["bsd_signeddatebasecontract"] : null;
                                enSettlement["bsd_installment1paiddate"] = listRepayment.Entities[0].Contains("bsd_installment1paiddate") ? listRepayment.Entities[0]["bsd_installment1paiddate"] : null;
                                enSettlement["bsd_signeddatecontract"] = listRepayment.Entities[0].Contains("bsd_signeddatecontract") ? listRepayment.Entities[0]["bsd_signeddatecontract"] : null;

                                enSettlement["bsd_totalamountbrokeragefee"] = new Money(totalamountbrokeragefee);
                                enSettlement["bsd_repaymentamount"] = new Money(repaymentValue);
                                enSettlement["bsd_settlementamount"] = new Money(settlementamount);
                                enSettlement["statuscode"] = new OptionSetValue(100000001);

                                totalAmountBrokerage += totalamountbrokeragefee;
                                totalRepayment += repaymentValue;
                                totalSettlment += settlementamount;
                                service.Create(enSettlement);

                                //Update Status Tam Ung
                                Entity enUpRepayment = new Entity(listRepayment.Entities[0].LogicalName, listRepayment.Entities[0].Id);
                                enUpRepayment["statuscode"] = new OptionSetValue(100000001);
                                service.Update(enUpRepayment);

                                //Update Status Chi tieu
                                var fetchXmlListTarget = $@"<?xml version=""1.0"" encoding=""utf-16""?>
                            <fetch>
                              <entity name=""bsd_targetnpp"">
                                <filter>
                                  <condition attribute=""bsd_brokeragecontract"" operator=""eq"" value=""{enBrokerageContract.Id}"" />
                                  <condition attribute=""bsd_project"" operator=""eq"" value=""{enProject.Id}"" />
                                  <condition attribute=""bsd_appendixcontractbrokerage"" operator=""eq"" value=""{enAppendixContractBrokerage.Id}"" />
                                  <condition attribute=""bsd_basecontract"" operator=""eq"" value=""{enBaseContracts.Id}"" />
                                </filter>
                              </entity>
                            </fetch>";
                                EntityCollection listTarget = service.RetrieveMultiple(new FetchExpression(fetchXmlListTarget));
                                if (listTarget.Entities.Count > 0)
                                {
                                    foreach (var itemTarget in listTarget.Entities)
                                    {
                                        Entity enUpTarget = new Entity(itemTarget.LogicalName, itemTarget.Id);
                                        enUpTarget["statuscode"] = new OptionSetValue(100000002);
                                        service.Update(enUpTarget);
                                    }
                                }
                            }

                            else if (calculateBy == 100000000)
                            {
                                var unitType = enContracts.Contains("bsd_typeunit") ? ((OptionSetValue)enContracts["bsd_typeunit"]).Value : 0;

                                decimal COMValue = 0;
                                decimal totalamountbrokeragefee = 0;
                                decimal settlementamount = 0;

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

                                repaymentValue = listRepayment.Entities[0].Contains("bsd_repaymentmoney") ? (decimal)((Money)listRepayment.Entities[0]["bsd_repaymentmoney"]).Value : 0;
                                totalamountbrokeragefee = COMValue * percentTarget / 100;
                                settlementamount = COMValue * brokerageFeeRetain / 100;
                                str.AppendLine("Total Detail " + totalAmountDetail);
                                str.AppendLine("Discount " + discount);
                                str.AppendLine("Retain " + brokerageFeeRetain);
                                str.AppendLine("COM " + COMValue);
                                str.AppendLine("1 " + totalamountbrokeragefee);
                                str.AppendLine("2 " + repaymentValue);
                                str.AppendLine("3 " + settlementamount);

                                Entity enSettlement = new Entity("bsd_settlement");
                                enSettlement["bsd_name"] = "Quyết toán sản phẩm " + enContracts["name"];
                                enSettlement["bsd_brokeragecontract"] = listRepayment.Entities[0].Contains("bsd_brokeragecontract") ? listRepayment.Entities[0]["bsd_brokeragecontract"] : null;
                                enSettlement["bsd_project"] = listRepayment.Entities[0].Contains("bsd_project") ? listRepayment.Entities[0]["bsd_project"] : null;
                                enSettlement["bsd_distributor"] = listRepayment.Entities[0].Contains("bsd_distributor") ? listRepayment.Entities[0]["bsd_distributor"] : null;
                                enSettlement["bsd_appendixcontractbrokerage"] = listRepayment.Entities[0].Contains("bsd_appendixcontractbrokerage") ? listRepayment.Entities[0]["bsd_appendixcontractbrokerage"] : null;
                                enSettlement["bsd_brokeragecalculate"] = listRepayment.Entities[0].Contains("bsd_brokeragecalculation") ? listRepayment.Entities[0]["bsd_brokeragecalculation"] : null;

                                enSettlement["bsd_repayment"] = listRepayment.Entities[0].ToEntityReference();
                                enSettlement["bsd_settlementcalculate"] = enSettlementCal.ToEntityReference();

                                enSettlement["bsd_customer"] = listRepayment.Entities[0].Contains("bsd_customer") ? listRepayment.Entities[0]["bsd_customer"] : null;
                                enSettlement["bsd_units"] = enUnit.ToEntityReference();

                                enSettlement["bsd_deposit"] = listRepayment.Entities[0].Contains("bsd_deposit") ? listRepayment.Entities[0]["bsd_deposit"] : null;
                                enSettlement["bsd_quote"] = listRepayment.Entities[0].Contains("bsd_quote") ? listRepayment.Entities[0]["bsd_quote"] : null;
                                enSettlement["bsd_contracts"] = listRepayment.Entities[0].Contains("bsd_contracts") ? listRepayment.Entities[0]["bsd_contracts"] : null;
                                enSettlement["bsd_depositdate"] = listRepayment.Entities[0].Contains("bsd_depositdate") ? listRepayment.Entities[0]["bsd_depositdate"] : null;
                                enSettlement["bsd_signeddatebasecontract"] = listRepayment.Entities[0].Contains("bsd_signeddatebasecontract") ? listRepayment.Entities[0]["bsd_signeddatebasecontract"] : null;
                                enSettlement["bsd_installment1paiddate"] = listRepayment.Entities[0].Contains("bsd_installment1paiddate") ? listRepayment.Entities[0]["bsd_installment1paiddate"] : null;
                                enSettlement["bsd_signeddatecontract"] = listRepayment.Entities[0].Contains("bsd_signeddatecontract") ? listRepayment.Entities[0]["bsd_signeddatecontract"] : null;

                                enSettlement["bsd_totalamountbrokeragefee"] = new Money(totalamountbrokeragefee);
                                enSettlement["bsd_repaymentamount"] = new Money(repaymentValue);
                                enSettlement["bsd_settlementamount"] = new Money(settlementamount);
                                enSettlement["statuscode"] = new OptionSetValue(100000001);

                                totalAmountBrokerage += totalamountbrokeragefee;
                                totalRepayment += repaymentValue;
                                totalSettlment += settlementamount;
                                service.Create(enSettlement);

                                //Update Status Tam Ung
                                Entity enUpRepayment = new Entity(listRepayment.Entities[0].LogicalName, listRepayment.Entities[0].Id);
                                enUpRepayment["statuscode"] = new OptionSetValue(100000001);
                                service.Update(enUpRepayment);

                                //Update Status Chi tieu
                                var fetchXmlListTarget = $@"<?xml version=""1.0"" encoding=""utf-16""?>
                            <fetch>
                              <entity name=""bsd_targetnpp"">
                                <filter>
                                  <condition attribute=""bsd_brokeragecontract"" operator=""eq"" value=""{enBrokerageContract.Id}"" />
                                  <condition attribute=""bsd_project"" operator=""eq"" value=""{enProject.Id}"" />
                                  <condition attribute=""bsd_appendixcontractbrokerage"" operator=""eq"" value=""{enAppendixContractBrokerage.Id}"" />
                                  <condition attribute=""bsd_basecontract"" operator=""eq"" value=""{enBaseContracts.Id}"" />
                                </filter>
                              </entity>
                            </fetch>";
                                EntityCollection listTarget = service.RetrieveMultiple(new FetchExpression(fetchXmlListTarget));
                                if (listTarget.Entities.Count > 0)
                                {
                                    foreach (var itemTarget in listTarget.Entities)
                                    {
                                        Entity enUpTarget = new Entity(itemTarget.LogicalName, itemTarget.Id);
                                        enUpTarget["statuscode"] = new OptionSetValue(100000002);
                                        service.Update(enUpTarget);
                                    }
                                }
                            }
                        }

                        else if (listRepayment.Entities.Count > 1)
                        {
                            decimal percentTarget = 0;
                            Entity enContracts = service.Retrieve(itemHDMB.LogicalName, itemHDMB.Id, new ColumnSet(true));
                            Entity enBaseContracts = listRepayment.Entities[0].Contains("bsd_quote") ? service.Retrieve(((EntityReference)listRepayment.Entities[0]["bsd_quote"]).LogicalName, ((EntityReference)listRepayment.Entities[0]["bsd_quote"]).Id, new ColumnSet(true)) : new Entity();
                            Entity enUnit = service.Retrieve(((EntityReference)enContracts["bsd_unitnumber"]).LogicalName, ((EntityReference)enContracts["bsd_unitnumber"]).Id, new ColumnSet(true));
                            Entity enTarget = service.Retrieve(((EntityReference)listRepayment.Entities[0]["bsd_targetbrokeragefee"]).LogicalName, ((EntityReference)listRepayment.Entities[0]["bsd_targetbrokeragefee"]).Id, new ColumnSet(true));
                            percentTarget = enTarget.Contains("bsd_percen") ? (decimal)enTarget["bsd_percen"] : 0;
                            decimal repaymentValue = listRepayment.Entities[0].Contains("bsd_repaymentmoney") ? (decimal)((Money)listRepayment.Entities[0]["bsd_repaymentmoney"]).Value : 0;

                            if (calculateBy == 100000001)
                            {
                                decimal COMValue = 0;
                                decimal totalamountbrokeragefee = 0;
                                decimal settlementamount = 0;


                                decimal totalAmountDetail = enContracts.Contains("bsd_totalamountdetails") ? (decimal)((Money)enContracts["bsd_totalamountdetails"]).Value : 0;
                                decimal discount = enContracts.Contains("bsd_discount") ? (decimal)((Money)enContracts["bsd_discount"]).Value : 0;
                                decimal allotmentFee = enContracts.Contains("bsd_allotmentfee") ? (decimal)((Money)enContracts["bsd_allotmentfee"]).Value : 0;
                                decimal serviceFees = enContracts.Contains("serviceFees") ? (decimal)((Money)enContracts["serviceFees"]).Value : 0;

                                COMValue = ((totalAmountDetail - discount) / 1.1M) - allotmentFee - serviceFees;

                                totalamountbrokeragefee = COMValue * percentTarget / 100;

                                settlementamount = COMValue * brokerageFeeRetain / 100;
                                str.AppendLine("Total Detail " + totalAmountDetail);
                                str.AppendLine("Discount " + discount);
                                str.AppendLine("Retain " + brokerageFeeRetain);
                                str.AppendLine("COM " + COMValue);
                                str.AppendLine("1 " + totalamountbrokeragefee);
                                str.AppendLine("2 " + repaymentValue);
                                str.AppendLine("3 " + settlementamount);

                                Entity enSettlement = new Entity("bsd_settlement");
                                enSettlement["bsd_name"] = "Quyết toán sản phẩm " + enContracts["name"];
                                enSettlement["bsd_brokeragecontract"] = listRepayment.Entities[0].Contains("bsd_brokeragecontract") ? listRepayment.Entities[0]["bsd_brokeragecontract"] : null;
                                enSettlement["bsd_project"] = listRepayment.Entities[0].Contains("bsd_project") ? listRepayment.Entities[0]["bsd_project"] : null;
                                enSettlement["bsd_distributor"] = listRepayment.Entities[0].Contains("bsd_distributor") ? listRepayment.Entities[0]["bsd_distributor"] : null;
                                enSettlement["bsd_appendixcontractbrokerage"] = listRepayment.Entities[0].Contains("bsd_appendixcontractbrokerage") ? listRepayment.Entities[0]["bsd_appendixcontractbrokerage"] : null;
                                enSettlement["bsd_brokeragecalculate"] = listRepayment.Entities[0].Contains("bsd_brokeragecalculation") ? listRepayment.Entities[0]["bsd_brokeragecalculation"] : null;

                                enSettlement["bsd_repayment"] = listRepayment.Entities[0].ToEntityReference();
                                enSettlement["bsd_settlementcalculate"] = enSettlementCal.ToEntityReference();

                                enSettlement["bsd_customer"] = listRepayment.Entities[0].Contains("bsd_customer") ? listRepayment.Entities[0]["bsd_customer"] : null;
                                enSettlement["bsd_units"] = enUnit.ToEntityReference();

                                enSettlement["bsd_deposit"] = listRepayment.Entities[0].Contains("bsd_deposit") ? listRepayment.Entities[0]["bsd_deposit"] : null;
                                enSettlement["bsd_quote"] = listRepayment.Entities[0].Contains("bsd_quote") ? listRepayment.Entities[0]["bsd_quote"] : null;
                                enSettlement["bsd_contracts"] = listRepayment.Entities[0].Contains("bsd_contracts") ? listRepayment.Entities[0]["bsd_contracts"] : null;
                                enSettlement["bsd_depositdate"] = listRepayment.Entities[0].Contains("bsd_depositdate") ? listRepayment.Entities[0]["bsd_depositdate"] : null;
                                enSettlement["bsd_signeddatebasecontract"] = listRepayment.Entities[0].Contains("bsd_signeddatebasecontract") ? listRepayment.Entities[0]["bsd_signeddatebasecontract"] : null;
                                enSettlement["bsd_installment1paiddate"] = listRepayment.Entities[0].Contains("bsd_installment1paiddate") ? listRepayment.Entities[0]["bsd_installment1paiddate"] : null;
                                enSettlement["bsd_signeddatecontract"] = listRepayment.Entities[0].Contains("bsd_signeddatecontract") ? listRepayment.Entities[0]["bsd_signeddatecontract"] : null;

                                enSettlement["bsd_totalamountbrokeragefee"] = new Money(totalamountbrokeragefee);
                                enSettlement["bsd_repaymentamount"] = new Money(repaymentValue);
                                enSettlement["bsd_settlementamount"] = new Money(settlementamount);
                                enSettlement["statuscode"] = new OptionSetValue(100000001);

                                totalAmountBrokerage += totalamountbrokeragefee;
                                totalRepayment += repaymentValue;
                                totalSettlment += settlementamount;
                                service.Create(enSettlement);

                                //Update Status Tam Ung
                                foreach (var itemRepayment in listRepayment.Entities)
                                {
                                    Entity enUpRepayment = new Entity(itemRepayment.LogicalName, itemRepayment.Id);
                                    enUpRepayment["statuscode"] = new OptionSetValue(100000001);
                                    service.Update(enUpRepayment);
                                }

                                //Update Status Chi tieu
                                var fetchXmlListTarget = $@"<?xml version=""1.0"" encoding=""utf-16""?>
                            <fetch>
                              <entity name=""bsd_targetnpp"">
                                <filter>
                                  <condition attribute=""bsd_brokeragecontract"" operator=""eq"" value=""{enBrokerageContract.Id}"" />
                                  <condition attribute=""bsd_project"" operator=""eq"" value=""{enProject.Id}"" />
                                  <condition attribute=""bsd_appendixcontractbrokerage"" operator=""eq"" value=""{enAppendixContractBrokerage.Id}"" />
                                  <condition attribute=""bsd_basecontract"" operator=""eq"" value=""{enBaseContracts.Id}"" />
                                </filter>
                              </entity>
                            </fetch>";
                                EntityCollection listTarget = service.RetrieveMultiple(new FetchExpression(fetchXmlListTarget));
                                if (listTarget.Entities.Count > 0)
                                {
                                    foreach (var itemTarget in listTarget.Entities)
                                    {
                                        Entity enUpTarget = new Entity(itemTarget.LogicalName, itemTarget.Id);
                                        enUpTarget["statuscode"] = new OptionSetValue(100000002);
                                        service.Update(enUpTarget);
                                    }
                                }
                            }

                            else if (calculateBy == 100000000)
                            {
                                var unitType = enContracts.Contains("bsd_typeunit") ? ((OptionSetValue)enContracts["bsd_typeunit"]).Value : 0;

                                decimal COMValue = 0;
                                decimal totalamountbrokeragefee = 0;
                                decimal settlementamount = 0;

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

                                totalamountbrokeragefee = COMValue * percentTarget / 100;
                                settlementamount = COMValue * brokerageFeeRetain / 100;
                                str.AppendLine("Total Detail " + totalAmountDetail);
                                str.AppendLine("Discount " + discount);
                                str.AppendLine("Retain " + brokerageFeeRetain);
                                str.AppendLine("COM " + COMValue);
                                str.AppendLine("1 " + totalamountbrokeragefee);
                                str.AppendLine("2 " + repaymentValue);
                                str.AppendLine("3 " + settlementamount);

                                Entity enSettlement = new Entity("bsd_settlement");
                                enSettlement["bsd_name"] = "Quyết toán sản phẩm " + enContracts["name"];
                                enSettlement["bsd_brokeragecontract"] = listRepayment.Entities[0].Contains("bsd_brokeragecontract") ? listRepayment.Entities[0]["bsd_brokeragecontract"] : null;
                                enSettlement["bsd_project"] = listRepayment.Entities[0].Contains("bsd_project") ? listRepayment.Entities[0]["bsd_project"] : null;
                                enSettlement["bsd_distributor"] = listRepayment.Entities[0].Contains("bsd_distributor") ? listRepayment.Entities[0]["bsd_distributor"] : null;
                                enSettlement["bsd_appendixcontractbrokerage"] = listRepayment.Entities[0].Contains("bsd_appendixcontractbrokerage") ? listRepayment.Entities[0]["bsd_appendixcontractbrokerage"] : null;
                                enSettlement["bsd_brokeragecalculate"] = listRepayment.Entities[0].Contains("bsd_brokeragecalculation") ? listRepayment.Entities[0]["bsd_brokeragecalculation"] : null;

                                enSettlement["bsd_repayment"] = listRepayment.Entities[0].ToEntityReference();
                                enSettlement["bsd_settlementcalculate"] = enSettlementCal.ToEntityReference();

                                enSettlement["bsd_customer"] = listRepayment.Entities[0].Contains("bsd_customer") ? listRepayment.Entities[0]["bsd_customer"] : null;
                                enSettlement["bsd_units"] = enUnit.ToEntityReference();

                                enSettlement["bsd_deposit"] = listRepayment.Entities[0].Contains("bsd_deposit") ? listRepayment.Entities[0]["bsd_deposit"] : null;
                                enSettlement["bsd_quote"] = listRepayment.Entities[0].Contains("bsd_quote") ? listRepayment.Entities[0]["bsd_quote"] : null;
                                enSettlement["bsd_contracts"] = listRepayment.Entities[0].Contains("bsd_contracts") ? listRepayment.Entities[0]["bsd_contracts"] : null;
                                enSettlement["bsd_depositdate"] = listRepayment.Entities[0].Contains("bsd_depositdate") ? listRepayment.Entities[0]["bsd_depositdate"] : null;
                                enSettlement["bsd_signeddatebasecontract"] = listRepayment.Entities[0].Contains("bsd_signeddatebasecontract") ? listRepayment.Entities[0]["bsd_signeddatebasecontract"] : null;
                                enSettlement["bsd_installment1paiddate"] = listRepayment.Entities[0].Contains("bsd_installment1paiddate") ? listRepayment.Entities[0]["bsd_installment1paiddate"] : null;
                                enSettlement["bsd_signeddatecontract"] = listRepayment.Entities[0].Contains("bsd_signeddatecontract") ? listRepayment.Entities[0]["bsd_signeddatecontract"] : null;

                                enSettlement["bsd_totalamountbrokeragefee"] = new Money(totalamountbrokeragefee);
                                enSettlement["bsd_repaymentamount"] = new Money(repaymentValue);
                                enSettlement["bsd_settlementamount"] = new Money(settlementamount);
                                enSettlement["statuscode"] = new OptionSetValue(100000001);

                                totalAmountBrokerage += totalamountbrokeragefee;
                                totalRepayment += repaymentValue;
                                totalSettlment += settlementamount;
                                service.Create(enSettlement);

                                //Update Status Tam Ung
                                foreach (var itemRepayment in listRepayment.Entities)
                                {
                                    Entity enUpRepayment = new Entity(itemRepayment.LogicalName, itemRepayment.Id);
                                    enUpRepayment["statuscode"] = new OptionSetValue(100000001);
                                    service.Update(enUpRepayment);
                                }

                                //Update Status Chi tieu
                                var fetchXmlListTarget = $@"<?xml version=""1.0"" encoding=""utf-16""?>
                            <fetch>
                              <entity name=""bsd_targetnpp"">
                                <filter>
                                  <condition attribute=""bsd_brokeragecontract"" operator=""eq"" value=""{enBrokerageContract.Id}"" />
                                  <condition attribute=""bsd_project"" operator=""eq"" value=""{enProject.Id}"" />
                                  <condition attribute=""bsd_appendixcontractbrokerage"" operator=""eq"" value=""{enAppendixContractBrokerage.Id}"" />
                                  <condition attribute=""bsd_basecontract"" operator=""eq"" value=""{enBaseContracts.Id}"" />
                                </filter>
                              </entity>
                            </fetch>";
                                EntityCollection listTarget = service.RetrieveMultiple(new FetchExpression(fetchXmlListTarget));
                                if (listTarget.Entities.Count > 0)
                                {
                                    foreach (var itemTarget in listTarget.Entities)
                                    {
                                        Entity enUpTarget = new Entity(itemTarget.LogicalName, itemTarget.Id);
                                        enUpTarget["statuscode"] = new OptionSetValue(100000002);
                                        service.Update(enUpTarget);
                                    }
                                }
                            }
                        }
                    }

                    
                }
            }


           


            Entity enUpSettlementCal = new Entity(enSettlementCal.LogicalName, enSettlementCal.Id);
            enUpSettlementCal["bsd_totalamountbrokeragefee"] = new Money(totalAmountBrokerage);
            enUpSettlementCal["bsd_totalrepayment"] = new Money(totalRepayment);
            enUpSettlementCal["bsd_revenuesettlement"] = new Money(totalSettlment);
            enUpSettlementCal["statuscode"] = new OptionSetValue(100000000);
            service.Update(enUpSettlementCal);
            //throw new InvalidPluginExecutionException("Debug Quyet Toan Final");

            //var fetchXmlQT = $@"<?xml version=""1.0"" encoding=""utf-16""?>
            //<fetch>
            //  <entity name=""bsd_settlement"" />
            //</fetch>";
            //EntityCollection listQT = service.RetrieveMultiple(new FetchExpression(fetchXmlQT));
            //if (listQT.Entities.Count > 0)
            //{
            //    foreach (var itemQT in listQT.Entities)
            //    {

            //        var name = (string)itemQT["bsd_name"];
            //        str.AppendLine("Name " + name);
            //    }
            //}
            //throw new InvalidPluginExecutionException(str.ToString());
            //throw new InvalidPluginExecutionException("Complete QT22 " + listQT.Entities.Count);
        }

        private void getListContract(Entity enProject, Entity enAppendixContractBrokerage, Entity enBrokerageContract, ref List<Entity> listHDMBPromotion, ref List<Entity> listHDMBNoPromotion, IOrganizationService service)
        {
            listHDMBPromotion = new List<Entity>();
            listHDMBNoPromotion = new List<Entity>();
            List<Entity> listHDMBTemp = new List<Entity>();
            var fetchXmlRepayment = $@"<?xml version=""1.0"" encoding=""utf-16""?>
            <fetch>
              <entity name=""bsd_repayment"">
                <filter>
                  <condition attribute=""bsd_brokeragecontract"" operator=""eq"" value=""{enBrokerageContract.Id}"" />
                  <condition attribute=""bsd_appendixcontractbrokerage"" operator=""eq"" value=""{enAppendixContractBrokerage.Id}"" />
                  <condition attribute=""bsd_project"" operator=""eq"" value=""{enProject.Id}"" />
                  <condition attribute=""bsd_contracts"" operator=""not-null"" />
                  <condition attribute=""statuscode"" operator=""eq"" value=""100000002"" />
                </filter>              
              </entity>
            </fetch>";
            EntityCollection listRepayment = service.RetrieveMultiple(new FetchExpression(fetchXmlRepayment));
            
            if (listRepayment.Entities.Count > 0)
            {
                foreach (var itemRepayment in listRepayment.Entities)
                {
                    Entity enHDMB = service.Retrieve(((EntityReference)itemRepayment["bsd_contracts"]).LogicalName, ((EntityReference)itemRepayment["bsd_contracts"]).Id, new ColumnSet(true));
                    if (listHDMBTemp.Any(item => item.Id == enHDMB.Id) == false)
                    {
                        listHDMBTemp.Add(enHDMB);
                    }
                }

            }
            
            if (listHDMBTemp.Count > 0)
            {
                foreach (var itemHDMBTemp in listHDMBTemp)
                {
                    bool checkSignDate = itemHDMBTemp.Contains("bsd_signdate");
                    bool checkBocTham = itemHDMBTemp.Contains("bsd_luckydrawprogram") ? (bool)itemHDMBTemp["bsd_luckydrawprogram"] : false;
                    bool checkPhanBo = itemHDMBTemp.Contains("bsd_distributionofluckydrawprogram") ? (bool)itemHDMBTemp["bsd_distributionofluckydrawprogram"] : false;
                    if (checkSignDate == true && checkBocTham == false)
                        listHDMBNoPromotion.Add(itemHDMBTemp);
                    else if (checkSignDate == true && checkBocTham == true)
                    {
                        if (checkPhanBo == true)
                            listHDMBPromotion.Add(itemHDMBTemp);
                    }
                }
            }
            

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
