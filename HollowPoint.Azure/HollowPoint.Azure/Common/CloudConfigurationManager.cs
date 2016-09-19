using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HollowPoint.Azure.Common
{

    public class CloudConfigurationManager
    {
        internal static string GetSetting(string v)
        {
            switch (v)
            {
                case "StorageConnectionString":
                    return "Add your own Storage Connection String";

                default:
                    return "";
            }


        }

        internal static string AzureSearchApiKey()
        {
            return "Add your own Azure Search Api Key";
        }

        internal static string AzureSearchServiceName()
        {
            return "Add your own Azure Search Service Name";
        }
     

        internal static int DefaultKeyPaddingLength()
        {
            return 8;
        }

        internal static char DefaultKeyPaddingChar()
        {
            return '0';
        }
    }

}
