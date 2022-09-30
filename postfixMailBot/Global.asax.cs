using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.Security;
using System.Web.SessionState;
using NodaTime;
using postfixMailBot.Models;
using postfixMailBot.Utilities;
using S22.Imap;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace postfixMailBot
{
    public class MvcApplication : System.Web.HttpApplication
    {
        private static string _mailClient = "MailClient";
        public static ImapClient MailClient
        {
            get
            {
                try
                {
                    var obj = HttpContext.Current.Application[_mailClient];
                    if (obj == null)
                    {
                        HttpContext.Current.Application[_mailClient] = new ImapClient(NotificationSettings.PostfixServer, NotificationSettings.PostfixPort, NotificationSettings.PostfixUsername, NotificationSettings.PostfixPassword, AuthMethod.Auto, false);
                    }
                    return (ImapClient)HttpContext.Current.Application[_mailClient];
                }
                catch (Exception)
                {
                    return null;
                }
            }
            set
            {
                if (HttpContext.Current.Application != null)
                    HttpContext.Current.Application[_mailClient] = value;
            }
        }

        private static string _tokenKey = "TelegramAccessToken";

        private static string _sendNotificationToKey = "SendNotificationTo";
        public static string SendNotificationTo
        {
            get
            {
                string sendNotificationTo = ConfigurationManager.AppSettings[_sendNotificationToKey];
                return sendNotificationTo;
                //return sendNotificationTo.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            }
        }

        protected void Session_Start()
        {
        }

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            log4net.Config.XmlConfigurator.Configure();

            //notification settings
            MailClient = new ImapClient(NotificationSettings.PostfixServer, NotificationSettings.PostfixPort, NotificationSettings.PostfixUsername, NotificationSettings.PostfixPassword, AuthMethod.Auto, false);
            MailClient.NewMessage += new EventHandler<IdleMessageEventArgs>(OnNewMessage);
        }

        static void OnNewMessage(object sender, IdleMessageEventArgs e)
        {
            try
            {
                Log.Info("Message recieved at " + ConvertToTimeZoneFromUtc(DateTime.Now.ToUniversalTime()).ToString("dd.MM.yyyy HH:mm"));
                //Console.WriteLine("A new message has been received. Message has UID: " + e.MessageUID);
                // Fetch the new message's headers and print the subject line
                MailMessage m = e.Client.GetMessage(e.MessageUID, FetchOptions.HeadersOnly);
                var date = ConvertToTimeZoneFromUtc(m.Date().Value.ToUniversalTime());
                //On 25.01.2021 at 17:08 received a mail from nitin@nitinsawant.com
                string messageNotificationText = "On <b>" + m.Date().Value.ToString("dd.MM.yyyy") + "</b> at <b>" + m.Date().Value.ToString("HH:mm") + "</b> received a mail from <b>" + m.From.DisplayName + "</b>&lt;" + m.From.Address + "&gt;";

                Log.Info(messageNotificationText);
                string token = ConfigurationManager.AppSettings[_tokenKey];
                var botClient = new TelegramBotClient(token);
                var task = Task.Run(async () =>
                {
                    Message message = await botClient.SendTextMessageAsync(
                      chatId: new ChatId(SendNotificationTo),
                      text: messageNotificationText,
                      parseMode: ParseMode.Default
                    //disableNotification: true,
                    //replyMarkup: new InlineKeyboardMarkup(InlineKeyboardButton.WithUrl(
                    //  "Check sendMessage method",
                    //  "https://core.telegram.org/bots/api#sendmessage"
                    //))
                    );
                });
                task.Wait();

                //mark message as unread
                ImapClient c = e.Client;
                if(c==null)
                    c = new ImapClient(NotificationSettings.PostfixServer, NotificationSettings.PostfixPort, NotificationSettings.PostfixUsername, NotificationSettings.PostfixPassword, AuthMethod.Auto, false);
                c.RemoveMessageFlags(e.MessageUID, null, MessageFlag.Seen);
            }
            catch (Exception ex)
            {
                Log.Error("Unexpected error! " + Environment.NewLine + ex.Message, ex);
            }
        }

        public static DateTime ConvertToTimeZoneFromUtc(DateTime utcDateTime, string timezone = "Europe/Istanbul")
        {
            var easternTimeZone = DateTimeZoneProviders.Tzdb[timezone];
            return Instant.FromDateTimeUtc(utcDateTime)
                          .InZone(easternTimeZone)
                          .ToDateTimeUnspecified();
        }
    }
}
