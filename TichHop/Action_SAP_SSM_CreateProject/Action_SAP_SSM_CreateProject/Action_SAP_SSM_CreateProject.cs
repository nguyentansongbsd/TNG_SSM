using Action_SAP_SSM_CreateProject.Models;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Action_SAP_SSM_CreateProject
{
    public class Action_SAP_SSM_CreateProject : IPlugin
    {
        IOrganizationService service = null;
        IOrganizationServiceFactory factory = null;
        ITracingService tracingService = null;

        string id = string.Empty;
        Project responseActions = new Project();
        public void Execute(IServiceProvider serviceProvider)
        {
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            service = factory.CreateOrganizationService(context.UserId);

            if (context.InputParameters["input"] == null || string.IsNullOrWhiteSpace(context.InputParameters["input"].ToString())) throw new InvalidPluginExecutionException("Vui lòng nhập input");
            string input = context.InputParameters["input"].ToString();

            try
            {
                responseActions = JsonConvert.DeserializeObject<Project>(input);

                if (responseActions.isUpdate == "true")
                {
                    tracingService.Trace("Start Update");
                    InitUpdate();
                }
                else
                {
                    tracingService.Trace("Start Add");
                    InitAdd();
                }

                context.OutputParameters["projectid"] = id.ToString();
            }
            catch (Exception ex)
            {
                throw new InvalidPluginExecutionException(ex.Message);
            }
        }
        private void InitAdd()
        {
            tracingService.Trace("Start check null");
            if (string.IsNullOrWhiteSpace(responseActions.bsd_projectcode)) throw new InvalidPluginExecutionException("Mã dự án không được trống.");
            if (string.IsNullOrWhiteSpace(responseActions.bsd_investor)) throw new InvalidPluginExecutionException("Chủ đầu tư không được trống.");
            if (string.IsNullOrWhiteSpace(responseActions.bsd_name)) throw new InvalidPluginExecutionException("Tên dự án không được trống.");
            if (string.IsNullOrWhiteSpace(responseActions.bsd_address)) throw new InvalidPluginExecutionException("Địa chỉ không được trống.");
            tracingService.Trace("Done check null");

            tracingService.Trace("Check Double");
            bool isDouble = checkDuplicate(responseActions.bsd_projectcode);
            if (isDouble) throw new InvalidPluginExecutionException("Dự án " + responseActions.bsd_projectcode + " đã có trên hệ thống SSM.");

            tracingService.Trace("Start add value");
            Entity enProject = new Entity("bsd_project");
            Entity enInvestor = getInvestor(responseActions.bsd_investor);

            enProject["bsd_projectcode"] = responseActions.bsd_projectcode;
            enProject["bsd_name"] = responseActions.bsd_name;
            enProject["bsd_investor"] = new EntityReference(enInvestor.LogicalName, enInvestor.Id);
            enProject["bsd_address"] = responseActions.bsd_address;
            enProject["bsd_projectformsap"] = true;
            tracingService.Trace("Done add value");

            this.id = service.Create(enProject).ToString();
        }
        private void InitUpdate()
        {
            if (string.IsNullOrWhiteSpace(responseActions.bsd_projectcode)) throw new InvalidPluginExecutionException("Mã dự án không được trống.");
            Entity enProject = getProject(responseActions.bsd_projectcode);
            Entity enProjectUp = new Entity(enProject.LogicalName, enProject.Id);

            tracingService.Trace("Start add value");
            if (!string.IsNullOrWhiteSpace(responseActions.bsd_name))
            {
                enProjectUp["bsd_name"] = responseActions.bsd_name;
            }
            if (!string.IsNullOrWhiteSpace(responseActions.bsd_investor))
            {
                Entity enInvestor = getInvestor(responseActions.bsd_investor);
                enProjectUp["bsd_investor"] = new EntityReference(enInvestor.LogicalName, enInvestor.Id);
            }
            if (!string.IsNullOrWhiteSpace(responseActions.bsd_address))
            {
                enProjectUp["bsd_address"] = responseActions.bsd_address;
            }
            tracingService.Trace("Done add value");
            this.id = enProject.Id.ToString();

            service.Update(enProjectUp);
        }
        private Entity getInvestor(string investorId)
        {
            tracingService.Trace("Start Investor");
            var fetchXml = $@"<?xml version=""1.0"" encoding=""utf-16""?>
                <fetch top=""1"">
                  <entity name=""account"">
                    <attribute name=""accountid"" />
                    <filter>
                      <condition attribute=""bsd_companycodesap"" operator=""eq"" value=""{investorId}"" />
                    </filter>
                  </entity>
                </fetch>";
            var result = service.RetrieveMultiple(new FetchExpression(fetchXml));
            if (result == null || result.Entities.Count <= 0) throw new InvalidPluginExecutionException("Chủ đầu tư "+ investorId + " chưa có trên hệ thống SSM.");
            tracingService.Trace("Done Investor");
            return result.Entities[0];
        }
        private Entity getProject(string projectCode)
        {
            tracingService.Trace("Start Project");
            var fetchXml = $@"<?xml version=""1.0"" encoding=""utf-16""?>
                <fetch >
                  <entity name=""bsd_project"">
                    <attribute name=""bsd_projectid"" />
                    <filter>
                      <condition attribute=""bsd_projectcode"" operator=""eq"" value=""{projectCode}"" />
                    </filter>
                  </entity>
                </fetch>";
            var result = service.RetrieveMultiple(new FetchExpression(fetchXml));
            if (result == null || result.Entities.Count <= 0) throw new InvalidPluginExecutionException("Dự án " + projectCode + " chưa có trên hệ thống SSM.");
            tracingService.Trace("Done Project");
            return result.Entities[0];
        }
        private bool checkDuplicate(string projectCode)
        {
            var fetchXml = $@"<?xml version=""1.0"" encoding=""utf-16""?>
                <fetch >
                  <entity name=""bsd_project"">
                    <attribute name=""bsd_projectid"" />
                    <filter>
                      <condition attribute=""bsd_projectcode"" operator=""eq"" value=""{projectCode}"" />
                    </filter>
                  </entity>
                </fetch>";
            var result = service.RetrieveMultiple(new FetchExpression(fetchXml));
            if (result == null || result.Entities.Count <= 0) return false;
            return true;
        }
    }
}
