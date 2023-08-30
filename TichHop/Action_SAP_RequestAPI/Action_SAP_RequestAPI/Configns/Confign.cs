using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Action_SAP_RequestAPI.Configns
{
    public class Confign
    {
        //public const string apiUrl = "https://sapwebdisp-uat.tnginter.com/RESTAdapter/POST_DATA_600"; // UAT
        //public const string apiToken = "005056BB71D81EEE8FEF098A9CC24EB5";// UAT

        public const string apiUrl = "https://sapwebdisp-uat.tnginter.com/RESTAdapter/POST_DATA"; // DEV
        public const string apiToken = "005056BBA7F11EDE8BD21562FA7B5CC5"; // DEV
        public const string apiAuth = "SSM:TNG@1234";

        public const string resource = "https://tngredev.crm5.dynamics.com"; // DEV
        //public const string resource = "https://ssmuat.crm5.dynamics.com"; // UAT
        public const string clientId = "64ac2865-0a5c-4f64-829d-c41b850b0893";
        public const string clientSecret = "4jb8Q~P3o5aDEiWoaFDYtgHFFzw2_.4GxvIs.dn0";
        public const string authorityUrl = "https://login.microsoftonline.com/2081a403-c773-427a-86a4-543f0983d921/oauth2/v2.0/token";
    }
}
