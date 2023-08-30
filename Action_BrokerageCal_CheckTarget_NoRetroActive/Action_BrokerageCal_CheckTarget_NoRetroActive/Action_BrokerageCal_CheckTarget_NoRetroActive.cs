using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Action_BrokerageCal_CheckTarget_NoRetroActive
{
    public class Action_BrokerageCal_CheckTarget_NoRetroActive : IPlugin
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
            traceService.Trace("Action_BrokerageCal_CheckTarget_NoRetroActive");


            Entity enBrokerageCal = service.Retrieve(target.LogicalName, target.Id, new ColumnSet(true));
            Entity enAppendixContractBrokerage = service.Retrieve(((EntityReference)enBrokerageCal["bsd_appendixcontractbrokerage"]).LogicalName, ((EntityReference)enBrokerageCal["bsd_appendixcontractbrokerage"]).Id, new ColumnSet(true));
            Entity enBrokerageContract = service.Retrieve(((EntityReference)enBrokerageCal["bsd_brokeragecontract"]).LogicalName, ((EntityReference)enBrokerageCal["bsd_brokeragecontract"]).Id, new ColumnSet(true));
            Entity enDistributor = service.Retrieve(((EntityReference)enBrokerageContract["bsd_distributornpp"]).LogicalName, ((EntityReference)enBrokerageContract["bsd_distributornpp"]).Id, new ColumnSet(true));
            Entity enProject = service.Retrieve(((EntityReference)enBrokerageCal["bsd_project"]).LogicalName, ((EntityReference)enBrokerageCal["bsd_project"]).Id, new ColumnSet(true));
            //Entity enBrokerageFee = service.Retrieve(((EntityReference)enAppendixContractBrokerage["bsd_brokeragemethod"]).LogicalName, ((EntityReference)enAppendixContractBrokerage["bsd_brokeragemethod"]).Id, new ColumnSet(true));

            DateTime dateStart = RetrieveLocalTimeFromUTCTime((DateTime)enAppendixContractBrokerage["bsd_startdate"], service);
            DateTime dateCheck = new DateTime();
            dateCheck = checkDateNoworEnd(enAppendixContractBrokerage);

            getListByMonth(enProject, enBrokerageContract, enBrokerageCal, enAppendixContractBrokerage, enDistributor, dateStart, dateCheck, service);

            Entity enUpBrokerageCal = new Entity(enBrokerageCal.LogicalName, enBrokerageCal.Id);
            enUpBrokerageCal["statuscode"] = new OptionSetValue(100000001);
            service.Update(enUpBrokerageCal);

            //var fetchXmlTarget = $@"<?xml version=""1.0"" encoding=""utf-16""?>
            //<fetch>
            //  <entity name=""bsd_targetnpp"">               
            //  </entity>
            //</fetch>";
            //EntityCollection listTargetNPP = service.RetrieveMultiple(new FetchExpression(fetchXmlTarget));
            //throw new InvalidPluginExecutionException("Complete Tinh Chi Tieu " + listTargetNPP.Entities.Count);
        }

        private void createTarget(Entity enProject, Entity enBrokerageContract, Entity enAppendixContractBrokerage, Entity enDistributor, Entity enBrokerageCal, List<Entity> listFinalBaseContract, List<Entity> listFinalBrokerageFees, IOrganizationService service)
        {

            if (listFinalBaseContract.Count > 0 && listFinalBrokerageFees.Count > 0)
            {

                for (int i = 0; i < listFinalBaseContract.Count; i++)
                {
                    for (int j = i; j == i; j++)
                    {
                        Entity enBaseContract = service.Retrieve(listFinalBaseContract[j].LogicalName, listFinalBaseContract[j].Id, new ColumnSet(true));
                        Entity enBrokerageFees = service.Retrieve(listFinalBrokerageFees[j].LogicalName, listFinalBrokerageFees[j].Id, new ColumnSet(true));

                        DateTime dateNow = RetrieveLocalTimeFromUTCTime(DateTime.UtcNow, service);
                        int monthNow = dateNow.Month;
                        DateTime receiptDate = enBaseContract.Contains("bsd_receiptdate") ? RetrieveLocalTimeFromUTCTime((DateTime)enBaseContract["bsd_receiptdate"], service) : new DateTime();
                        int monthReceiptDate = receiptDate.Month;

                        EntityCollection checkCTContain = getListCT(enProject, enAppendixContractBrokerage, enBaseContract, monthReceiptDate, service);
                        if (checkCTContain.Entities.Count == 0)
                        {

                            decimal fees = enBrokerageFees.Contains("bsd_brokeragefee") ? (decimal)enBrokerageFees["bsd_brokeragefee"] : 0;

                            Entity enTargerNPP = new Entity("bsd_targetnpp");

                            enTargerNPP["bsd_name"] = "Chỉ tiêu sản phẩm " + enBaseContract["name"];

                            enTargerNPP["bsd_brokeragecontract"] = enBrokerageContract.ToEntityReference();
                            enTargerNPP["bsd_project"] = enProject.ToEntityReference();
                            enTargerNPP["bsd_distributors"] = enDistributor.ToEntityReference();
                            enTargerNPP["bsd_appendixcontractbrokerage"] = enAppendixContractBrokerage.ToEntityReference();
                            enTargerNPP["bsd_phaseslaunch"] = enBaseContract.Contains("bsd_phaseslaunchid") ? enBaseContract["bsd_phaseslaunchid"] : null;

                            enTargerNPP["bsd_brokeragecalculation"] = enBrokerageCal.ToEntityReference();
                            enTargerNPP["bsd_customer"] = enBaseContract.Contains("customerid") ? enBaseContract["customerid"] : null;
                            enTargerNPP["bsd_basecontract"] = enBaseContract.ToEntityReference();
                            enTargerNPP["bsd_receiptdate"] = enBaseContract.Contains("bsd_receiptdate") ? enBaseContract["bsd_receiptdate"] : null;


                            enTargerNPP["bsd_basecontractsigningdate"] = enBaseContract.Contains("bsd_signdate") ? enBaseContract["bsd_signdate"] : null;
                            enTargerNPP["bsd_installment1paiddate"] = enBaseContract.Contains("bsd_installment1paiddate") ? enBaseContract["bsd_installment1paiddate"] : null;
                            enTargerNPP["bsd_brokeragefees"] = enBrokerageFees.ToEntityReference();
                            enTargerNPP["bsd_percen"] = fees;
                            enTargerNPP["bsd_percentnow"] = new Decimal(0);
                            //enTargerNPP["bsd_percentdifferent"] = (decimal)enTargerNPP["bsd_percen"] - (decimal)enTargerNPP["bsd_percentnow"];

                            //Mapping Contract and Deposit
                            if (enBaseContract.Contains("bsd_datcoc"))
                            {
                                Entity enDeposit = service.Retrieve(((EntityReference)enBaseContract["bsd_datcoc"]).LogicalName, ((EntityReference)enBaseContract["bsd_datcoc"]).Id, new ColumnSet(true));
                                enTargerNPP["bsd_datcoc"] = enDeposit.ToEntityReference();
                            }

                            //Maping Contracts
                            EntityCollection listContract = getListContract(enBaseContract, service);
                            if (listContract.Entities.Count > 0)
                            {
                                Entity enContract = service.Retrieve(listContract.Entities[0].LogicalName, listContract.Entities[0].Id, new ColumnSet(true));
                                enTargerNPP["bsd_contracts"] = enContract.ToEntityReference();
                                enTargerNPP["bsd_signedcontractdate"] = enContract.Contains("bsd_signdate") ? enContract["bsd_signdate"] : null;
                            }

                            //Month
                            enTargerNPP["bsd_targetmonth"] = monthReceiptDate;
                            enTargerNPP["statuscode"] = new OptionSetValue(100000000);
                            service.Create(enTargerNPP);

                            str.AppendLine("Tao chỉ tiêu");

                            Entity enUpHDCS = new Entity(enBaseContract.LogicalName, enBaseContract.Id);
                            enUpHDCS["bsd_target"] = true;
                            service.Update(enUpHDCS);

                        }

                    }
                }

            }
            //throw new InvalidPluginExecutionException("Done Create");
        }




        private void getListFinal(Entity enProject, Entity enBrokerageContract, Entity enAppendixContractBrokerage, Entity enDistributor, Entity enBrokerageCal, List<Entity> listBaseContractTotal, List<Entity> listBrokerageFeesTotal, IOrganizationService service)
        {
            List<Entity> listFinalBaseContract = new List<Entity>();
            List<Entity> listFinalBrokerageFees = new List<Entity>();
            List<Entity> listFinalBaseContractNew = new List<Entity>();
            //throw new InvalidPluginExecutionException("List Final Function " + listBaseContractTotal.Count + listBrokerageFeesTotal.Count);
            if (listBaseContractTotal.Count > 0 && listBrokerageFeesTotal.Count > 0)
            {

                for (int i = 0; i < listBaseContractTotal.Count; i++)
                {
                    str.AppendLine("i:" + i);
                    for (int j = i; j == i; j++)
                    {
                        str.AppendLine("j:" + j);
                        Entity enBaseContract = service.Retrieve(listBaseContractTotal[j].LogicalName, listBaseContractTotal[j].Id, new ColumnSet(true));
                        str.AppendLine("ID Base " + enBaseContract.Id);
                        Entity enBrokerageFees = service.Retrieve(listBrokerageFeesTotal[j].LogicalName, listBrokerageFeesTotal[j].Id, new ColumnSet(true));
                        DateTime dateNow = RetrieveLocalTimeFromUTCTime(DateTime.UtcNow, service);
                        int monthNow = dateNow.Month;
                        DateTime receiptDate = enBaseContract.Contains("bsd_receiptdate") ? RetrieveLocalTimeFromUTCTime((DateTime)enBaseContract["bsd_receiptdate"], service) : new DateTime();
                        int monthReceiptDate = receiptDate.Month;
                        //throw new InvalidPluginExecutionException("Month " + monthReceiptDate);
                        List<Entity> listCheckCT = getBaseContractMonthBefore(enProject, enAppendixContractBrokerage, enBrokerageContract, monthReceiptDate, service);
                        //throw new InvalidPluginExecutionException("List check CT " + listCheckCT.Count);
                        str.AppendLine("Tháng " + monthReceiptDate);
                        if (listCheckCT.Count > 0)
                        {

                            listCheckCT.Add(enBaseContract);

                            foreach (var itemCheckCT in listCheckCT)
                            {
                                str.AppendLine("Final First " + listFinalBaseContractNew.Count);
                                str.AppendLine("ID " + itemCheckCT.Id);
                                str.AppendLine("Contain " + listFinalBaseContractNew.Any(item => item.Id == itemCheckCT.Id));
                                if (listFinalBaseContractNew.Any(item => item.Id == itemCheckCT.Id) == false)
                                    listFinalBaseContractNew.Add(itemCheckCT);
                            }

                        }

                        else
                        {
                            listFinalBaseContract.Add(enBaseContract);
                            listFinalBrokerageFees.Add(enBrokerageFees);
                        }
                    }
                }
                if (listFinalBaseContractNew.Count > 0)
                    getListFinalNew(enProject, enBrokerageContract, enAppendixContractBrokerage, enDistributor, enBrokerageCal, listFinalBaseContractNew, service);

                createTarget(enProject, enBrokerageContract, enAppendixContractBrokerage, enDistributor, enBrokerageCal, listFinalBaseContract, listFinalBrokerageFees, service);
            }
        }

        private void createTargetNew(Entity enProject, Entity enBrokerageContract, Entity enAppendixContractBrokerage, Entity enDistributor, Entity enBrokerageCal, List<Entity> listContractNew, List<Entity> listlBrokerageFeesNew, IOrganizationService service)
        {
            if (listContractNew.Count > 0 && listlBrokerageFeesNew.Count > 0)
            {
                for (int i = 0; i < listContractNew.Count; i++)
                {
                    str.AppendLine("i:" + i);
                    for (int j = i; j == i; j++)
                    {
                        str.AppendLine("j:" + j);
                        Entity enBaseContract = service.Retrieve(listContractNew[j].LogicalName, listContractNew[j].Id, new ColumnSet(true));
                        Entity enBrokerageFees = service.Retrieve(listlBrokerageFeesNew[j].LogicalName, listlBrokerageFeesNew[j].Id, new ColumnSet(true));
                        DateTime dateNow = RetrieveLocalTimeFromUTCTime(DateTime.UtcNow, service);
                        int monthNow = dateNow.Month;
                        DateTime receiptDate = enBaseContract.Contains("bsd_receiptdate") ? RetrieveLocalTimeFromUTCTime((DateTime)enBaseContract["bsd_receiptdate"], service) : new DateTime();
                        int monthReceiptDate = receiptDate.Month;

                        decimal fees = enBrokerageFees.Contains("bsd_brokeragefee") ? (decimal)enBrokerageFees["bsd_brokeragefee"] : 0;
                        var fetchXmlTargetCompare = $@"<?xml version=""1.0"" encoding=""utf-16""?>
                        <fetch>
                          <entity name=""bsd_targetnpp"">
                            <filter>
                              <condition attribute=""bsd_basecontract"" operator=""eq"" value=""{enBaseContract.Id}"" />
                              <condition attribute=""bsd_project"" operator=""eq"" value=""{enProject.Id}"" />
                              <condition attribute=""bsd_brokeragecontract"" operator=""eq"" value=""{enBrokerageContract.Id}"" />
                              <condition attribute=""bsd_appendixcontractbrokerage"" operator=""eq"" value=""{enAppendixContractBrokerage.Id}"" />
                              <condition attribute=""bsd_targetmonth"" operator=""eq"" value=""{monthReceiptDate}"" />                            
                            </filter>
                          </entity>
                        </fetch>";
                        EntityCollection listTargetCompare = service.RetrieveMultiple(new FetchExpression(fetchXmlTargetCompare));
                        str.AppendLine("ListTC " + listTargetCompare.Entities.Count);
                        if (listTargetCompare.Entities.Count > 0)
                        {
                            foreach (var itemListTargetCompare in listTargetCompare.Entities)
                            {
                                if (((EntityReference)itemListTargetCompare["bsd_brokeragefees"]).Id != enBrokerageFees.Id)
                                {
                                    Entity enTargerNPP = new Entity(itemListTargetCompare.LogicalName);
                                    itemListTargetCompare["bsd_targets"] = itemListTargetCompare.ToEntityReference();
                                    itemListTargetCompare["bsd_percentnow"] = itemListTargetCompare.Contains("bsd_percen") ? itemListTargetCompare["bsd_percen"] : null;
                                    itemListTargetCompare.Attributes.Remove("bsd_targetnppid");
                                    itemListTargetCompare.Attributes.Remove("bsd_brokeragefees");
                                    itemListTargetCompare.Attributes.Remove("bsd_percen");
                                    itemListTargetCompare.Attributes.Remove("bsd_name");
                                    itemListTargetCompare.Attributes.Remove("bsd_brokeragecalculation");
                                    itemListTargetCompare.Attributes.Remove("statuscode");
                                    itemListTargetCompare["bsd_name"] = "Chỉ tiêu bổ sung sản phẩm " + enBaseContract["name"];
                                    itemListTargetCompare["bsd_brokeragefees"] = enBrokerageFees.ToEntityReference();
                                    itemListTargetCompare["bsd_percen"] = fees;
                                    itemListTargetCompare["bsd_brokeragecalculation"] = enBrokerageCal.ToEntityReference();
                                    itemListTargetCompare["statuscode"] = new OptionSetValue(100000000);
                                    itemListTargetCompare.Id = Guid.NewGuid();
                                    enTargerNPP = itemListTargetCompare;
                                    service.Create(enTargerNPP);
                                }
                            }

                        }
                        else
                        {
                            Entity enTargerNPP = new Entity("bsd_targetnpp");
                            enTargerNPP["bsd_name"] = "Chỉ tiêu sản phẩm " + enBaseContract["name"];
                            enTargerNPP["bsd_brokeragecontract"] = enBrokerageContract.ToEntityReference();
                            enTargerNPP["bsd_project"] = enProject.ToEntityReference();
                            enTargerNPP["bsd_distributors"] = enDistributor.ToEntityReference();
                            enTargerNPP["bsd_appendixcontractbrokerage"] = enAppendixContractBrokerage.ToEntityReference();
                            enTargerNPP["bsd_phaseslaunch"] = enBaseContract.Contains("bsd_phaseslaunchid") ? enBaseContract["bsd_phaseslaunchid"] : null;
                            enTargerNPP["bsd_brokeragecalculation"] = enBrokerageCal.ToEntityReference();
                            enTargerNPP["bsd_customer"] = enBaseContract.Contains("customerid") ? enBaseContract["customerid"] : null;
                            enTargerNPP["bsd_basecontract"] = enBaseContract.ToEntityReference();
                            enTargerNPP["bsd_receiptdate"] = enBaseContract.Contains("bsd_receiptdate") ? enBaseContract["bsd_receiptdate"] : null;
                            enTargerNPP["bsd_basecontractsigningdate"] = enBaseContract.Contains("bsd_signdate") ? enBaseContract["bsd_signdate"] : null;
                            enTargerNPP["bsd_installment1paiddate"] = enBaseContract.Contains("bsd_installment1paiddate") ? enBaseContract["bsd_installment1paiddate"] : null;
                            enTargerNPP["bsd_brokeragefees"] = enBrokerageFees.ToEntityReference();
                            enTargerNPP["bsd_percen"] = fees;
                            enTargerNPP["bsd_percentnow"] = new Decimal(0);
                            //enTargerNPP["bsd_percentdifferent"] = (decimal)enTargerNPP["bsd_percen"] - (decimal)enTargerNPP["bsd_percentnow"];

                            //Mapping Contract and Deposit
                            if (enBaseContract.Contains("bsd_datcoc"))
                            {
                                Entity enDeposit = service.Retrieve(((EntityReference)enBaseContract["bsd_datcoc"]).LogicalName, ((EntityReference)enBaseContract["bsd_datcoc"]).Id, new ColumnSet(true));
                                enTargerNPP["bsd_datcoc"] = enDeposit.ToEntityReference();
                            }

                            //Maping Contracts
                            EntityCollection listContract = getListContract(enBaseContract, service);
                            if (listContract.Entities.Count > 0)
                            {
                                Entity enContract = service.Retrieve(listContract.Entities[0].LogicalName, listContract.Entities[0].Id, new ColumnSet(true));
                                enTargerNPP["bsd_contracts"] = enContract.ToEntityReference();
                                enTargerNPP["bsd_signedcontractdate"] = enContract.Contains("bsd_signdate") ? enContract["bsd_signdate"] : null;
                            }

                            //Month
                            enTargerNPP["bsd_targetmonth"] = monthReceiptDate;
                            enTargerNPP["statuscode"] = new OptionSetValue(100000000);
                            service.Create(enTargerNPP);
                            Entity enUpHDCS = new Entity(enBaseContract.LogicalName, enBaseContract.Id);
                            enUpHDCS["bsd_target"] = true;
                            service.Update(enUpHDCS);
                        }
                    }
                }

                //throw new InvalidPluginExecutionException(str.ToString());
            }
        }

        private void getListFinalNew(Entity enProject, Entity enBrokerageContract, Entity enAppendixContractBrokerage, Entity enDistributor, Entity enBrokerageCal, List<Entity> listFinalBaseContractNew, IOrganizationService service)
        {
            List<Entity> listContractNew = new List<Entity>();
            List<Entity> listlBrokerageFeesNew = new List<Entity>();

            

            EntityCollection listBrokerageFees = getBrokerageFees(enAppendixContractBrokerage, service);
            if (listBrokerageFees.Entities.Count > 0)
            {
                foreach (var itemBrokerageFees in listBrokerageFees.Entities)
                {
                    List<Entity> listContractConditionType = new List<Entity>();
                    List<Entity> listBrokerageConditionType = new List<Entity>();
                    //Điều kiện số lượng
                    var fetchXmlConditionQuantity = $@"<?xml version=""1.0"" encoding=""utf-16""?>
                            <fetch>
                              <entity name=""bsd_brokeragefeesdetail"">
                                <filter>
                                  <condition attribute=""bsd_conditioncalculate"" operator=""eq"" value=""100000000"" />
                                  <condition attribute=""statuscode"" operator=""eq"" value=""100000001"" />
                                </filter>
                                <link-entity name=""bsd_bsd_brokeragefees_bsd_brokeragefeesdeta"" from=""bsd_brokeragefeesdetailid"" to=""bsd_brokeragefeesdetailid"" intersect=""true"">
                                  <filter>
                                    <condition attribute=""bsd_brokeragefeesid"" operator=""eq"" value=""{itemBrokerageFees.Id}"" />
                                  </filter>
                                </link-entity>
                              </entity>
                            </fetch>";
                    EntityCollection listConditionQuantity = service.RetrieveMultiple(new FetchExpression(fetchXmlConditionQuantity));

                    //Điều kiện giá trị
                    var fetchXmlConditionAmount = $@"<?xml version=""1.0"" encoding=""utf-16""?>
                                <fetch>
                                  <entity name=""bsd_brokeragefeesdetail"">
                                    <filter>
                                      <condition attribute=""bsd_conditioncalculate"" operator=""eq"" value=""100000002"" />
                                      <condition attribute=""statuscode"" operator=""eq"" value=""100000001"" />
                                    </filter>
                                    <link-entity name=""bsd_bsd_brokeragefees_bsd_brokeragefeesdeta"" from=""bsd_brokeragefeesdetailid"" to=""bsd_brokeragefeesdetailid"" intersect=""true"">
                                      <filter>
                                        <condition attribute=""bsd_brokeragefeesid"" operator=""eq"" value=""{itemBrokerageFees.Id}"" />
                                      </filter>
                                    </link-entity>
                                  </entity>
                                </fetch>";
                    EntityCollection listConditionAmount = service.RetrieveMultiple(new FetchExpression(fetchXmlConditionAmount));

                    //Điều kiện hình thức sản phẩm
                    var fetchXmlConditionType = $@"<?xml version=""1.0"" encoding=""utf-16""?>
                                    <fetch>
                                      <entity name=""bsd_brokeragefeesdetail"">
                                        <filter>
                                          <condition attribute=""bsd_conditioncalculate"" operator=""eq"" value=""100000001"" />
                                          <condition attribute=""statuscode"" operator=""eq"" value=""100000001"" />
                                        </filter>
                                        <link-entity name=""bsd_bsd_brokeragefees_bsd_brokeragefeesdeta"" from=""bsd_brokeragefeesdetailid"" to=""bsd_brokeragefeesdetailid"" intersect=""true"">
                                          <filter>
                                            <condition attribute=""bsd_brokeragefeesid"" operator=""eq"" value=""{itemBrokerageFees.Id}"" />
                                          </filter>
                                        </link-entity>
                                      </entity>
                                    </fetch>";
                    EntityCollection listConditionType = service.RetrieveMultiple(new FetchExpression(fetchXmlConditionType));
                    str.AppendLine("List DK: ID " + itemBrokerageFees.Id);

                    //Kiểm tra điều kiện Hình thức sản phẩm
                    if (listConditionType.Entities.Count > 0)
                    {
                        //throw new InvalidPluginExecutionException("Điều kiện hình thức");
                        

                        foreach (var itemBaseContract in listFinalBaseContractNew)
                        {
                            int unitTypeBrokerage = listConditionType.Entities[0].Contains("bsd_unitstype") ? ((OptionSetValue)listConditionType.Entities[0]["bsd_unitstype"]).Value : 0;
                            int typeDetailBrokerage = listConditionType.Entities[0].Contains("bsd_typedetails") ? ((OptionSetValue)listConditionType.Entities[0]["bsd_typedetails"]).Value : 0;
                            str.AppendLine("Unit Type Brokerage " + unitTypeBrokerage);
                            if (unitTypeBrokerage == 100000001)
                            {
                                var baseContractTypeDetail = itemBaseContract.Contains("bsd_loaibangtinhgia") ? ((OptionSetValue)itemBaseContract["bsd_loaibangtinhgia"]).Value : 0;
                                if (baseContractTypeDetail == typeDetailBrokerage)
                                {
                                    //Kiem tra DK Gia tri
                                    if (listConditionAmount.Entities.Count > 0)
                                    {
                                        var amountFrom = ((Money)listConditionAmount.Entities[0]["bsd_amountfrom"]).Value;
                                        var amountTo = ((Money)listConditionAmount.Entities[0]["bsd_amountto"]).Value;
                                        var totalAmountDetail = ((Money)itemBaseContract["bsd_totalamountdetails"]).Value;
                                        if (totalAmountDetail >= amountFrom && totalAmountDetail <= amountTo)
                                        {
                                            if (listContractConditionType.Any(item => item.Id == itemBaseContract.Id) == false)
                                            {
                                                str.AppendLine("Vao dk Tap Tang " + itemBaseContract.Id);
                                                listContractConditionType.Add(itemBaseContract);
                                                listBrokerageConditionType.Add(itemBrokerageFees);
                                            }

                                        }
                                    }

                                    else
                                    {
                                        if (listContractConditionType.Any(item => item.Id == itemBaseContract.Id) == false)
                                        {
                                            listContractConditionType.Add(itemBaseContract);
                                            listBrokerageConditionType.Add(itemBrokerageFees);
                                        }
                                    }
                                }
                            }
                            else if (unitTypeBrokerage == 100000000)
                            {

                                Entity enUnit = service.Retrieve(((EntityReference)itemBaseContract["bsd_unitno"]).LogicalName, ((EntityReference)itemBaseContract["bsd_unitno"]).Id, new ColumnSet(true));
                                var baseContractTypeDetail = itemBaseContract.Contains("bsd_unittype") ? ((OptionSetValue)itemBaseContract["bsd_unittype"]).Value : 0;
                                //throw new InvalidPluginExecutionException("TypeDetail + BaseDetail " + typeDetailBrokerage + " " + baseContractTypeDetail);
                                if (baseContractTypeDetail == 100000001)
                                {
                                    int completePackageUnit = enUnit.Contains("bsd_completepackage") ? ((OptionSetValue)enUnit["bsd_completepackage"]).Value : 0;
                                    int ownershiptimeUnit = enUnit.Contains("bsd_ownershiptime1") ? ((OptionSetValue)enUnit["bsd_ownershiptime1"]).Value : 0;
                                    if (listConditionType.Entities[0].Contains("bsd_completepackage"))
                                    {
                                        int completePackageBrokerage = ((OptionSetValue)listConditionType.Entities[0]["bsd_completepackage"]).Value;
                                        if (listConditionType.Entities[0].Contains("bsd_ownershiptime"))
                                        {
                                            int ownershiptimeBrokerage = ((OptionSetValue)listConditionType.Entities[0]["bsd_ownershiptime"]).Value;
                                            if (completePackageUnit == completePackageBrokerage && ownershiptimeUnit == ownershiptimeBrokerage)
                                            {
                                                //Kiem tra DK Gia tri
                                                if (listConditionAmount.Entities.Count > 0)
                                                {
                                                    var amountFrom = ((Money)listConditionAmount.Entities[0]["bsd_amountfrom"]).Value;
                                                    var amountTo = ((Money)listConditionAmount.Entities[0]["bsd_amountto"]).Value;
                                                    var totalAmountDetail = ((Money)itemBaseContract["bsd_totalamountdetails"]).Value;
                                                    if (totalAmountDetail >= amountFrom && totalAmountDetail <= amountTo)
                                                    {
                                                        if (listContractConditionType.Any(item => item.Id == itemBaseContract.Id) == false)
                                                        {
                                                            
                                                            listContractConditionType.Add(itemBaseContract);
                                                            listBrokerageConditionType.Add(itemBrokerageFees);
                                                        }
                                                    }

                                                }

                                                else
                                                {
                                                    if (listContractConditionType.Any(item => item.Id == itemBaseContract.Id) == false)
                                                    {
                                                        listContractConditionType.Add(itemBaseContract);
                                                        listBrokerageConditionType.Add(itemBrokerageFees);
                                                    }
                                                }
                                            }
                                        }

                                        else if (completePackageUnit == completePackageBrokerage)
                                        {

                                            //Kiem tra DK Gia tri
                                            if (listConditionAmount.Entities.Count > 0)
                                            {
                                                var amountFrom = ((Money)listConditionAmount.Entities[0]["bsd_amountfrom"]).Value;
                                                var amountTo = ((Money)listConditionAmount.Entities[0]["bsd_amountto"]).Value;
                                                var totalAmountDetail = ((Money)itemBaseContract["bsd_totalamountdetails"]).Value;
                                                if (totalAmountDetail >= amountFrom && totalAmountDetail <= amountTo)
                                                {
                                                    if (listContractConditionType.Any(item => item.Id == itemBaseContract.Id) == false)
                                                    {
                                                        str.AppendLine("Vao dk Cao Tang " + itemBaseContract.Id);
                                                        listContractConditionType.Add(itemBaseContract);
                                                        listBrokerageConditionType.Add(itemBrokerageFees);
                                                    }
                                                }

                                            }

                                            else
                                            {
                                                if (listContractConditionType.Any(item => item.Id == itemBaseContract.Id) == false)
                                                {
                                                    listContractConditionType.Add(itemBaseContract);
                                                    listBrokerageConditionType.Add(itemBrokerageFees);
                                                }
                                            }

                                        }

                                    }
                                    else if (listConditionType.Entities[0].Contains("bsd_ownershiptime"))
                                    {
                                        int ownershiptimeBrokerage = ((OptionSetValue)listConditionType.Entities[0]["bsd_ownershiptime"]).Value;
                                        if (ownershiptimeUnit == ownershiptimeBrokerage)
                                        {
                                            //Kiem tra DK Gia tri
                                            if (listConditionAmount.Entities.Count > 0)
                                            {
                                                var amountFrom = ((Money)listConditionAmount.Entities[0]["bsd_amountfrom"]).Value;
                                                var amountTo = ((Money)listConditionAmount.Entities[0]["bsd_amountto"]).Value;
                                                var totalAmountDetail = ((Money)itemBaseContract["bsd_totalamountdetails"]).Value;
                                                if (totalAmountDetail >= amountFrom && totalAmountDetail <= amountTo)
                                                {


                                                    if (listContractConditionType.Any(item => item.Id == itemBaseContract.Id) == false)
                                                    {
                                                        listContractConditionType.Add(itemBaseContract);
                                                        listBrokerageConditionType.Add(itemBrokerageFees);
                                                    }

                                                }

                                            }

                                            else
                                            {
                                                if (listContractConditionType.Any(item => item.Id == itemBaseContract.Id) == false)
                                                {
                                                    listContractConditionType.Add(itemBaseContract);
                                                    listBrokerageConditionType.Add(itemBrokerageFees);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        //throw new InvalidPluginExecutionException(str.ToString());
                        //throw new InvalidPluginExecutionException("List Contract & Brokerage " + listContractConditionType.Count + listBrokerageConditionType.Count);
                        //Kiem tra DK So luong
                        if (listContractConditionType.Count > 0)
                        {
                            if (listConditionQuantity.Entities.Count > 0)
                            {
                                int quantityFrom = (int)listConditionQuantity.Entities[0]["bsd_quantityfrom"];
                                int quantityTo = (int)listConditionQuantity.Entities[0]["bsd_quantityto"];
                                if (listContractConditionType.Count >= quantityFrom && listContractConditionType.Count <= quantityTo)
                                {
                                    foreach (var itemContractConditionType in listContractConditionType)
                                    {
                                        if (listContractNew.Any(item => item.Id == itemContractConditionType.Id) == false)
                                        {
                                            listContractNew.Add(itemContractConditionType);
                                            listlBrokerageFeesNew.Add(itemBrokerageFees);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                foreach (var itemContractConditionType in listContractConditionType)
                                {
                                    if (listContractNew.Any(item => item.Id == itemContractConditionType.Id) == false)
                                    {
                                        listContractNew.Add(itemContractConditionType);
                                        listlBrokerageFeesNew.Add(itemBrokerageFees);
                                    }
                                }
                            }
                        }

                        //throw new InvalidPluginExecutionException(str.ToString());
                    }

                    //Kiem tra DK gia tri
                    else if (listConditionAmount.Entities.Count > 0)
                    {
                        var amountFrom = ((Money)listConditionAmount.Entities[0]["bsd_amountfrom"]).Value;
                        var amountTo = ((Money)listConditionAmount.Entities[0]["bsd_amountto"]).Value;
                        foreach (var itemBaseContract in listFinalBaseContractNew)
                        {
                            var totalAmountDetail = ((Money)itemBaseContract["bsd_totalamountdetails"]).Value;
                            if (totalAmountDetail >= amountFrom && totalAmountDetail <= amountTo)
                            {
                                if (listContractConditionType.Any(item => item.Id == itemBaseContract.Id) == false)
                                {
                                    listContractConditionType.Add(itemBaseContract);
                                    listBrokerageConditionType.Add(itemBrokerageFees);
                                }
                            }
                        }

                        //Kiem tra DK So luong
                        if (listContractConditionType.Count > 0)
                        {
                            if (listConditionQuantity.Entities.Count > 0)
                            {
                                int quantityFrom = (int)listConditionQuantity.Entities[0]["bsd_quantityfrom"];
                                int quantityTo = (int)listConditionQuantity.Entities[0]["bsd_quantityto"];
                                if (listContractConditionType.Count >= quantityFrom && listContractConditionType.Count <= quantityTo)
                                {
                                    foreach (var itemContractConditionType in listContractConditionType)
                                    {
                                        if (listContractNew.Any(item => item.Id == itemContractConditionType.Id) == false)
                                        {
                                            listContractNew.Add(itemContractConditionType);
                                            listlBrokerageFeesNew.Add(itemBrokerageFees);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                foreach (var itemContractConditionType in listContractConditionType)
                                {
                                    if (listContractNew.Any(item => item.Id == itemContractConditionType.Id) == false)
                                    {
                                        listContractNew.Add(itemContractConditionType);
                                        listlBrokerageFeesNew.Add(itemBrokerageFees);
                                    }
                                }
                            }
                        }
                    }

                    //Kiem tra DK so luong
                    else if (listConditionQuantity.Entities.Count > 0)
                    {
                        int quantityFrom = (int)listConditionQuantity.Entities[0]["bsd_quantityfrom"];
                        int quantityTo = (int)listConditionQuantity.Entities[0]["bsd_quantityto"];
                        if (listFinalBaseContractNew.Count >= quantityFrom && listFinalBaseContractNew.Count <= quantityTo)
                        {
                            foreach (var itemBaseContract in listFinalBaseContractNew)
                            {
                                if (listContractNew.Any(item => item.Id == itemBaseContract.Id) == false)
                                {
                                    listContractNew.Add(itemBaseContract);
                                    listlBrokerageFeesNew.Add(itemBrokerageFees);
                                }
                            }
                        }
                    }
                }

                createTargetNew(enProject, enBrokerageContract, enAppendixContractBrokerage, enDistributor, enBrokerageCal, listContractNew, listlBrokerageFeesNew, service);
            }
        }

        private void getListByMonth(Entity enProject, Entity enBrokerageContract, Entity enBrokerageCal, Entity enAppendixContractBrokerage, Entity enDistributor, DateTime dateStart, DateTime dateCheck, IOrganizationService service)
        {
            EntityCollection listBaseContract = getBaseContract(enProject, enDistributor, dateStart, dateCheck, service);
            EntityCollection listBrokerageFees = getBrokerageFees(enAppendixContractBrokerage, service);
            //foreach (var itemxport in listBaseContract.Entities)
            //{
            //    var name = (string)itemxport["name"];
            //    str.AppendLine("Name " + name);
            //}
            //throw new InvalidPluginExecutionException(str.ToString());
            //throw new InvalidPluginExecutionException("Count list mới " + listBaseContract.Entities.Count + listBrokerageFees.Entities.Count);
            if (listBaseContract.Entities.Count > 0)
            {
                if (listBrokerageFees.Entities.Count > 0)
                {
                    List<Entity> listBaseContractTotal = new List<Entity>();
                    List<Entity> listBrokerageFeesTotal = new List<Entity>();

                 
                    foreach (var itemHDCS in listBaseContract.Entities)
                    {
                        
                        DateTime depositeDate = RetrieveLocalTimeFromUTCTime((DateTime)itemHDCS["bsd_receiptdate"], service);
                        int monthDepositeDate = depositeDate.Month;
                        List<Entity> listHDCSbyMonth = new List<Entity>();
                        listHDCSbyMonth.Add(itemHDCS);
                        foreach (var itemHDCSbyMonth in listBaseContract.Entities)
                        {
                            DateTime depositeDatebyMonth = RetrieveLocalTimeFromUTCTime((DateTime)itemHDCSbyMonth["bsd_receiptdate"], service);
                            int monthDepositeDatebyMonth = depositeDatebyMonth.Month;
                            if (itemHDCSbyMonth.Id != itemHDCS.Id && monthDepositeDate == monthDepositeDatebyMonth)
                                listHDCSbyMonth.Add(itemHDCSbyMonth);

                        }
                        //throw new InvalidPluginExecutionException("List HDCS Month " + listHDCSbyMonth.Count + listBaseContractTotal.Count);
                        foreach (var itemBrokerageFees in listBrokerageFees.Entities)
                        {
                            List<Entity> listContractConditionType = new List<Entity>();
                            List<Entity> listBrokerageConditionType = new List<Entity>();
                            str.AppendLine("ID Brok First" + itemBrokerageFees.Id);
                            //Điều kiện số lượng
                            var fetchXmlConditionQuantity = $@"<?xml version=""1.0"" encoding=""utf-16""?>
                            <fetch>
                              <entity name=""bsd_brokeragefeesdetail"">
                                <filter>
                                  <condition attribute=""bsd_conditioncalculate"" operator=""eq"" value=""100000000"" />
                                  <condition attribute=""statuscode"" operator=""eq"" value=""100000001"" />
                                </filter>
                                <link-entity name=""bsd_bsd_brokeragefees_bsd_brokeragefeesdeta"" from=""bsd_brokeragefeesdetailid"" to=""bsd_brokeragefeesdetailid"" intersect=""true"">
                                  <filter>
                                    <condition attribute=""bsd_brokeragefeesid"" operator=""eq"" value=""{itemBrokerageFees.Id}"" />
                                  </filter>
                                </link-entity>
                              </entity>
                            </fetch>";
                            EntityCollection listConditionQuantity = service.RetrieveMultiple(new FetchExpression(fetchXmlConditionQuantity));

                            //Điều kiện giá trị
                            var fetchXmlConditionAmount = $@"<?xml version=""1.0"" encoding=""utf-16""?>
                                <fetch>
                                  <entity name=""bsd_brokeragefeesdetail"">
                                    <filter>
                                      <condition attribute=""bsd_conditioncalculate"" operator=""eq"" value=""100000002"" />
                                      <condition attribute=""statuscode"" operator=""eq"" value=""100000001"" />
                                    </filter>
                                    <link-entity name=""bsd_bsd_brokeragefees_bsd_brokeragefeesdeta"" from=""bsd_brokeragefeesdetailid"" to=""bsd_brokeragefeesdetailid"" intersect=""true"">
                                      <filter>
                                        <condition attribute=""bsd_brokeragefeesid"" operator=""eq"" value=""{itemBrokerageFees.Id}"" />
                                      </filter>
                                    </link-entity>
                                  </entity>
                                </fetch>";
                            EntityCollection listConditionAmount = service.RetrieveMultiple(new FetchExpression(fetchXmlConditionAmount));

                            //Điều kiện hình thức sản phẩm
                            var fetchXmlConditionType = $@"<?xml version=""1.0"" encoding=""utf-16""?>
                                    <fetch>
                                      <entity name=""bsd_brokeragefeesdetail"">
                                        <all-attributes />
                                        <filter>
                                          <condition attribute=""bsd_conditioncalculate"" operator=""eq"" value=""100000001"" />
                                          <condition attribute=""statuscode"" operator=""eq"" value=""100000001"" />
                                        </filter>
                                        <link-entity name=""bsd_bsd_brokeragefees_bsd_brokeragefeesdeta"" from=""bsd_brokeragefeesdetailid"" to=""bsd_brokeragefeesdetailid"" intersect=""true"">
                                          <filter>
                                            <condition attribute=""bsd_brokeragefeesid"" operator=""eq"" value=""{itemBrokerageFees.Id}"" />
                                          </filter>
                                        </link-entity>
                                      </entity>
                                    </fetch>";
                            EntityCollection listConditionType = service.RetrieveMultiple(new FetchExpression(fetchXmlConditionType));
                            //throw new InvalidPluginExecutionException("Count Condition " + listConditionQuantity.Entities.Count + listConditionAmount.Entities.Count + listConditionType.Entities.Count);

                            //Kiểm tra điều kiện Hình thức sản phẩm
                            if (listConditionType.Entities.Count > 0)
                            {
                                //throw new InvalidPluginExecutionException("List HDCS Month " + listHDCSbyMonth.Count);

                                //for (int i = 0; i < listHDCSbyMonth.Count; i++)
                                //{
                                //    var idtemp = listHDCSbyMonth[i].Id;
                                //    str.AppendLine("ID Contract " + listHDCSbyMonth[i].Id);
                                //}

                                #region code foreach
                                foreach (var itemBaseContract in listHDCSbyMonth)
                                {
                                    int unitTypeBrokerage = listConditionType.Entities[0].Contains("bsd_unitstype") ? ((OptionSetValue)listConditionType.Entities[0]["bsd_unitstype"]).Value : 0;
                                    int typeDetailBrokerage = listConditionType.Entities[0].Contains("bsd_typedetails") ? ((OptionSetValue)listConditionType.Entities[0]["bsd_typedetails"]).Value : 0;
                                    str.AppendLine("Type Brokerage " + unitTypeBrokerage);
                                    str.AppendLine("ID Contract " + itemBaseContract.Id);
                                    if (unitTypeBrokerage == 100000001)
                                    {
                                        var baseContractTypeDetail = itemBaseContract.Contains("bsd_loaibangtinhgia") ? ((OptionSetValue)itemBaseContract["bsd_loaibangtinhgia"]).Value : 0;

                                        if (baseContractTypeDetail == typeDetailBrokerage)
                                        {

                                            //Kiem tra DK Gia tri
                                            if (listConditionAmount.Entities.Count > 0)
                                            {
                                                var amountFrom = ((Money)listConditionAmount.Entities[0]["bsd_amountfrom"]).Value;
                                                var amountTo = ((Money)listConditionAmount.Entities[0]["bsd_amountto"]).Value;
                                                var totalAmountDetail = ((Money)itemBaseContract["bsd_totalamountdetails"]).Value;
                                                if (totalAmountDetail >= amountFrom && totalAmountDetail <= amountTo)
                                                {

                                                    if (listContractConditionType.Any(item => item.Id == itemBaseContract.Id) == false)
                                                    {
                                                        str.AppendLine("Vao dk Thap Tang " + itemBaseContract.Id);
                                                        listContractConditionType.Add(itemBaseContract);
                                                        listBrokerageConditionType.Add(itemBrokerageFees);
                                                    }

                                                }
                                            }

                                            else
                                            {
                                                if (listContractConditionType.Any(item => item.Id == itemBaseContract.Id) == false)
                                                {
                                                    listContractConditionType.Add(itemBaseContract);
                                                    listBrokerageConditionType.Add(itemBrokerageFees);
                                                }
                                            }
                                        }
                                    }
                                    else if (unitTypeBrokerage == 100000000)
                                    {

                                        Entity enUnit = service.Retrieve(((EntityReference)itemBaseContract["bsd_unitno"]).LogicalName, ((EntityReference)itemBaseContract["bsd_unitno"]).Id, new ColumnSet(true));
                                        var baseContractTypeDetail = itemBaseContract.Contains("bsd_unittype") ? ((OptionSetValue)itemBaseContract["bsd_unittype"]).Value : 0;
                                        //str.AppendLine("Unit Type" + baseContractTypeDetail);
                                        //throw new InvalidPluginExecutionException("TypeDetail + BaseDetail " + typeDetailBrokerage + " " + baseContractTypeDetail);
                                        if (baseContractTypeDetail == 100000001)
                                        {
                                            int completePackageUnit = enUnit.Contains("bsd_completepackage") ? ((OptionSetValue)enUnit["bsd_completepackage"]).Value : 0;
                                            int ownershiptimeUnit = enUnit.Contains("bsd_ownershiptime1") ? ((OptionSetValue)enUnit["bsd_ownershiptime1"]).Value : 0;

                                            //str.AppendLine("Unit Name" + (string)enUnit["name"]);
                                            if (listConditionType.Entities[0].Contains("bsd_completepackage"))
                                            {

                                                int completePackageBrokerage = ((OptionSetValue)listConditionType.Entities[0]["bsd_completepackage"]).Value;
                                                if (listConditionType.Entities[0].Contains("bsd_ownershiptime"))
                                                {
                                                    str.AppendLine("Contain Ownership");
                                                    int ownershiptimeBrokerage = ((OptionSetValue)listConditionType.Entities[0]["bsd_ownershiptime"]).Value;
                                                    if (completePackageUnit == completePackageBrokerage && ownershiptimeUnit == ownershiptimeBrokerage)
                                                    {
                                                        //Kiem tra DK Gia tri
                                                        if (listConditionAmount.Entities.Count > 0)
                                                        {
                                                            var amountFrom = ((Money)listConditionAmount.Entities[0]["bsd_amountfrom"]).Value;
                                                            var amountTo = ((Money)listConditionAmount.Entities[0]["bsd_amountto"]).Value;
                                                            var totalAmountDetail = ((Money)itemBaseContract["bsd_totalamountdetails"]).Value;
                                                            if (totalAmountDetail >= amountFrom && totalAmountDetail <= amountTo)
                                                            {
                                                                if (listContractConditionType.Any(item => item.Id == itemBaseContract.Id) == false)
                                                                {
                                                                    listContractConditionType.Add(itemBaseContract);
                                                                    listBrokerageConditionType.Add(itemBrokerageFees);
                                                                }
                                                            }

                                                        }

                                                        else
                                                        {
                                                            if (listContractConditionType.Any(item => item.Id == itemBaseContract.Id) == false)
                                                            {

                                                                listContractConditionType.Add(itemBaseContract);
                                                                listBrokerageConditionType.Add(itemBrokerageFees);
                                                            }
                                                        }
                                                    }
                                                }

                                                else if (completePackageUnit == completePackageBrokerage)
                                                {

                                                    //Kiem tra DK Gia tri
                                                    if (listConditionAmount.Entities.Count > 0)
                                                    {
                                                        var amountFrom = ((Money)listConditionAmount.Entities[0]["bsd_amountfrom"]).Value;
                                                        var amountTo = ((Money)listConditionAmount.Entities[0]["bsd_amountto"]).Value;
                                                        var totalAmountDetail = ((Money)itemBaseContract["bsd_totalamountdetails"]).Value;
                                                        if (totalAmountDetail >= amountFrom && totalAmountDetail <= amountTo)
                                                        {
                                                            if (listContractConditionType.Any(item => item.Id == itemBaseContract.Id) == false)
                                                            {
                                                                str.AppendLine("Vao dk Cao Tang " + itemBaseContract.Id);
                                                                listContractConditionType.Add(itemBaseContract);
                                                                listBrokerageConditionType.Add(itemBrokerageFees);
                                                            }
                                                        }

                                                    }

                                                    else
                                                    {
                                                        if (listContractConditionType.Any(item => item.Id == itemBaseContract.Id) == false)
                                                        {
                                                            listContractConditionType.Add(itemBaseContract);
                                                            listBrokerageConditionType.Add(itemBrokerageFees);
                                                        }
                                                    }

                                                }

                                            }
                                            else if (listConditionType.Entities[0].Contains("bsd_ownershiptime"))
                                            {
                                                int ownershiptimeBrokerage = ((OptionSetValue)listConditionType.Entities[0]["bsd_ownershiptime"]).Value;
                                                if (ownershiptimeUnit == ownershiptimeBrokerage)
                                                {
                                                    //Kiem tra DK Gia tri
                                                    if (listConditionAmount.Entities.Count > 0)
                                                    {
                                                        str.AppendLine("Thoa gia tri");
                                                        var amountFrom = ((Money)listConditionAmount.Entities[0]["bsd_amountfrom"]).Value;
                                                        var amountTo = ((Money)listConditionAmount.Entities[0]["bsd_amountto"]).Value;
                                                        var totalAmountDetail = ((Money)itemBaseContract["bsd_totalamountdetails"]).Value;
                                                        if (totalAmountDetail >= amountFrom && totalAmountDetail <= amountTo)
                                                        {


                                                            if (listContractConditionType.Any(item => item.Id == itemBaseContract.Id) == false)
                                                            {
                                                                listContractConditionType.Add(itemBaseContract);
                                                                listBrokerageConditionType.Add(itemBrokerageFees);
                                                            }

                                                        }

                                                    }

                                                    else
                                                    {
                                                        if (listContractConditionType.Any(item => item.Id == itemBaseContract.Id) == false)
                                                        {
                                                            listContractConditionType.Add(itemBaseContract);
                                                            listBrokerageConditionType.Add(itemBrokerageFees);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                #endregion

                                //str.AppendLine("Count contract condition Type" + listContractConditionType.Count);
                                //throw new InvalidPluginExecutionException("List Contract & Brokerage " + listContractConditionType.Count + listBrokerageConditionType.Count);
                                //Kiem tra DK So luong
                                if (listContractConditionType.Count > 0)
                                {
                                    if (listConditionQuantity.Entities.Count > 0)
                                    {
                                        
                                        int quantityFrom = (int)listConditionQuantity.Entities[0]["bsd_quantityfrom"];
                                        int quantityTo = (int)listConditionQuantity.Entities[0]["bsd_quantityto"];
                                        if (listContractConditionType.Count >= quantityFrom && listContractConditionType.Count <= quantityTo)
                                        {
                                            foreach (var itemContractConditionType in listContractConditionType)
                                            {
                                                if(listBaseContractTotal.Any(item => item.Id == itemContractConditionType.Id) == false)
                                                {
                                                    listBaseContractTotal.Add(itemContractConditionType);
                                                    listBrokerageFeesTotal.Add(itemBrokerageFees);
                                                }
                                            }
                                            
                                        }
                                    }
                                    else
                                    {
                                        foreach (var itemContractConditionType in listContractConditionType)
                                        {
                                            if (listBaseContractTotal.Any(item => item.Id == itemContractConditionType.Id) == false)
                                            {
                                                listBaseContractTotal.Add(itemContractConditionType);
                                                listBrokerageFeesTotal.Add(itemBrokerageFees);
                                            }
                                        }
                                    }
                                }

                                //throw new InvalidPluginExecutionException(str.ToString());
                            }

                            //Kiem tra DK gia tri
                            else if (listConditionAmount.Entities.Count > 0)
                            {
                                var amountFrom = ((Money)listConditionAmount.Entities[0]["bsd_amountfrom"]).Value;
                                var amountTo = ((Money)listConditionAmount.Entities[0]["bsd_amountto"]).Value;
                                foreach (var itemBaseContract in listHDCSbyMonth)
                                {
                                    var totalAmountDetail = ((Money)itemBaseContract["bsd_totalamountdetails"]).Value;
                                    if (totalAmountDetail >= amountFrom && totalAmountDetail <= amountTo)
                                    {
                                        if (listContractConditionType.Any(item => item.Id == itemBaseContract.Id) == false)
                                        {
                                            listContractConditionType.Add(itemBaseContract);
                                            listBrokerageConditionType.Add(itemBrokerageFees);
                                        }
                                    }
                                }

                                //Kiem tra DK So luong
                                if (listContractConditionType.Count > 0)
                                {
                                    if (listConditionQuantity.Entities.Count > 0)
                                    {
                                        int quantityFrom = (int)listConditionQuantity.Entities[0]["bsd_quantityfrom"];
                                        int quantityTo = (int)listConditionQuantity.Entities[0]["bsd_quantityto"];
                                        if (listContractConditionType.Count >= quantityFrom && listContractConditionType.Count <= quantityTo)
                                        {
                                            foreach (var itemContractConditionType in listContractConditionType)
                                            {
                                                if (listBaseContractTotal.Any(item => item.Id == itemContractConditionType.Id) == false)
                                                {
                                                    listBaseContractTotal.Add(itemContractConditionType);
                                                    listBrokerageFeesTotal.Add(itemBrokerageFees);
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        foreach (var itemContractConditionType in listContractConditionType)
                                        {
                                            if (listBaseContractTotal.Any(item => item.Id == itemContractConditionType.Id) == false)
                                            {
                                                listBaseContractTotal.Add(itemContractConditionType);
                                                listBrokerageFeesTotal.Add(itemBrokerageFees);
                                            }
                                        }
                                    }
                                }
                            }

                            //Kiem tra DK so luong
                            else if (listConditionQuantity.Entities.Count > 0)
                            {
                                int quantityFrom = (int)listConditionQuantity.Entities[0]["bsd_quantityfrom"];
                                int quantityTo = (int)listConditionQuantity.Entities[0]["bsd_quantityto"];
                                if (listHDCSbyMonth.Count >= quantityFrom && listHDCSbyMonth.Count <= quantityTo)
                                {
                                    foreach (var itemBaseContract in listHDCSbyMonth)
                                    {
                                        if (listBaseContractTotal.Any(item => item.Id == itemBaseContract.Id) == false)
                                        {
                                            listBaseContractTotal.Add(itemBaseContract);
                                            listBrokerageFeesTotal.Add(itemBrokerageFees);
                                        }
                                    }
                                }
                            }

                        }
                        
                        //throw new InvalidPluginExecutionException("Count list Total " + listBaseContractTotal.Count);

                    }
                    //throw new InvalidPluginExecutionException(str.ToString());
                    //throw new InvalidPluginExecutionException(str.ToString());
                    //throw new InvalidPluginExecutionException("List Base va Brokerage " + listBaseContractTotal.Count + listBrokerageFeesTotal.Count);
                    //foreach (var itemxport in listBrokerageFeesTotal)
                    //{
                    //    var id = itemxport.Id;
                    //    str.AppendLine("IDBrokTotal " + id);
                    //}
                    //throw new InvalidPluginExecutionException(str.ToString());
                    getListFinal(enProject, enBrokerageContract, enAppendixContractBrokerage, enDistributor, enBrokerageCal, listBaseContractTotal, listBrokerageFeesTotal, service);
                    //throw new InvalidPluginExecutionException(str.ToString());
                }
            }


        }

        private DateTime checkDateNoworEnd(Entity enAppendixContractBrokerage)
        {
            DateTime dateCheck = new DateTime();
            DateTime dateEnd = enAppendixContractBrokerage.Contains("bsd_enddate") ? RetrieveLocalTimeFromUTCTime((DateTime)enAppendixContractBrokerage["bsd_enddate"], service) : DateTime.UtcNow;
            DateTime dateNow = RetrieveLocalTimeFromUTCTime(DateTime.UtcNow, service);
            if (dateNow < dateEnd || !enAppendixContractBrokerage.Contains("bsd_enddate"))
                dateCheck = dateNow;
            else
                dateCheck = dateEnd;
            return dateCheck;
        }

        private EntityCollection getBaseContract(Entity enProject, Entity enDistributor, DateTime dateStart, DateTime dateCheck, IOrganizationService service)
        {

            var fetchXmlBaseContract = $@"<?xml version=""1.0"" encoding=""utf-16""?>
            <fetch>
              <entity name=""quote"">
                <filter>
                  <condition attribute=""bsd_projectid"" operator=""eq"" value=""{enProject.Id}"" />
                  <condition attribute=""bsd_distributor"" operator=""eq"" value=""{enDistributor.Id}"" />
                  <condition attribute=""bsd_receiptdate"" operator=""not-null"" />
                  <condition attribute=""bsd_signdate"" operator=""not-null"" />
                  <condition attribute=""bsd_installment1paiddate"" operator=""not-null"" />
                  <condition attribute=""bsd_receiptdate"" operator=""ge"" value=""{dateStart}"" />
                  <condition attribute=""bsd_receiptdate"" operator=""le"" value=""{dateCheck}"" />
                  <condition attribute=""bsd_signdate"" operator=""ge"" value=""{dateStart}"" />
                  <condition attribute=""bsd_signdate"" operator=""le"" value=""{dateCheck}"" />
                  <condition attribute=""bsd_installment1paiddate"" operator=""ge"" value=""{dateStart}"" />
                  <condition attribute=""bsd_installment1paiddate"" operator=""le"" value=""{dateCheck}"" />
                  <condition attribute=""bsd_target"" operator=""eq"" value=""0"" />
                  <condition attribute=""statuscode"" operator=""not-in"">
                    <value>100000010</value>
                    <value>100000012</value>
                  </condition>
                </filter>
                <order attribute=""bsd_receiptdate"" />
              </entity>
            </fetch>";
            EntityCollection listBaseContract = service.RetrieveMultiple(new FetchExpression(fetchXmlBaseContract));
            return listBaseContract;
        }

        private EntityCollection getListContract(Entity enBaseContract, IOrganizationService service)
        {
            var fetchXml = $@"<?xml version=""1.0"" encoding=""utf-16""?>
            <fetch>
              <entity name=""salesorder"">
                <filter>
                  <condition attribute=""quoteid"" operator=""eq"" value=""{enBaseContract.Id}"" />
                </filter>
              </entity>
            </fetch>";
            EntityCollection listHDMB = service.RetrieveMultiple(new FetchExpression(fetchXml));
            return listHDMB;
        }

        private List<Entity> getBaseContractMonthBefore(Entity enProject, Entity enAppendixContractBrokerage, Entity enBrokerageContract, int monthSignDate, IOrganizationService service)
        {
            List<Entity> listBaseContractMotnhBefore = new List<Entity>();
            var fetchXml = $@"<?xml version=""1.0"" encoding=""utf-16""?>
            <fetch>
              <entity name=""bsd_targetnpp"">
                <filter>
                  <condition attribute=""bsd_project"" operator=""eq"" value=""{enProject.Id}"" />
                  <condition attribute=""bsd_targetmonth"" operator=""eq"" value=""{monthSignDate}"" />
                  <condition attribute=""bsd_appendixcontractbrokerage"" operator=""eq"" value=""{enAppendixContractBrokerage.Id}"" />
                  <condition attribute=""bsd_brokeragecontract"" operator=""eq"" value=""{enBrokerageContract.Id}"" />
                </filter>
              </entity>
            </fetch>";
            EntityCollection listNPP = service.RetrieveMultiple(new FetchExpression(fetchXml));
            if (listNPP.Entities.Count > 0)
            {
                foreach (var itemNPP in listNPP.Entities)
                {
                    Entity enBaseContract = service.Retrieve(((EntityReference)itemNPP["bsd_basecontract"]).LogicalName, ((EntityReference)itemNPP["bsd_basecontract"]).Id, new ColumnSet(true));
                    listBaseContractMotnhBefore.Add(enBaseContract);
                }
            }
            return listBaseContractMotnhBefore;
        }

        private EntityCollection getListCT(Entity enProject, Entity enAppendixContractBrokerage, Entity enBaseContract, int targetMonth, IOrganizationService service)
        {
            var fetchXml = $@"<?xml version=""1.0"" encoding=""utf-16""?>
            <fetch>
              <entity name=""bsd_targetnpp"">
                <filter>
                  <condition attribute=""bsd_project"" operator=""eq"" value=""{enProject.Id}"" />
                  <condition attribute=""bsd_basecontract"" operator=""eq"" value=""{enBaseContract.Id}"" />
                  <condition attribute=""bsd_appendixcontractbrokerage"" operator=""eq"" value=""{enAppendixContractBrokerage.Id}"" />
                  <condition attribute=""bsd_targetmonth"" operator=""eq"" value=""{targetMonth}"" />
                </filter>
              </entity>
            </fetch>";
            EntityCollection listCT = service.RetrieveMultiple(new FetchExpression(fetchXml));

            return listCT;
        }


        private EntityCollection getBrokerageFees(Entity enAppendixContractBrokerage, IOrganizationService service)
        {
            var fetchXmlBrokerageFees = $@"<?xml version=""1.0"" encoding=""utf-16""?>
            <fetch>
              <entity name=""bsd_brokeragefees"">
                <filter>
                   <condition attribute=""statuscode"" operator=""eq"" value=""100000000"" />
                </filter>
                <link-entity name=""bsd_bsd_appendixcontractbrokerage_bsd_broke"" from=""bsd_brokeragefeesid"" to=""bsd_brokeragefeesid"" intersect=""true"">
                  <filter>
                    <condition attribute=""bsd_appendixcontractbrokerageid"" operator=""eq"" value=""{enAppendixContractBrokerage.Id}"" />
                  </filter>
                </link-entity>
              </entity>
            </fetch>";
            EntityCollection listBrokerageFees = service.RetrieveMultiple(new FetchExpression(fetchXmlBrokerageFees));
            return listBrokerageFees;
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
