using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace postfixMailBot.Models
{
    public class NotificationSettings
    {
        private static string _server = "PostfixServer";
        private static string _port = "PostfixPort";
        private static string _username = "PostfixUsername";
        private static string _password = "PostfixPassword";
        private static string _notificationTo = "SendNotificationTo";
        public static string PostfixServer
        {
            get
            {
                return ConfigurationManager.AppSettings[_server];
            }
        }
        public static int PostfixPort
        {
            get
            {
                object val = ConfigurationManager.AppSettings[_port];
                if (val == null)
                {
                    return 143;
                }
                return Convert.ToInt32(val);
            }
        }
        public static string PostfixUsername
        {
            get
            {
                return ConfigurationManager.AppSettings[_username];
            }
        }
        public static string PostfixPassword
        {
            get
            {
                return ConfigurationManager.AppSettings[_password];
            }
        }
        public static string SendNotificationTo
        {
            get
            {
                return ConfigurationManager.AppSettings[_notificationTo];
            }
        }

    }
}