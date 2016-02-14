using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Template10.Mvvm;
using Template10.Services.NavigationService;
using Template10.Services.SettingsService;
using Windows.ApplicationModel;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace WhatsWrapper.ViewModels
{
    public class MainPageViewModel : ViewModelBase
    {
        private string WhatsAppUrl
        {
            get
            {
                return "web.whatsapp.com";
            }
        }

        private int notificationsCount { get; set; }

        public string Version
        {
            get
            {
                PackageVersion version = Package.Current.Id.Version;
                return string.Format("{0}.{1}.{2}", version.Major, version.Minor, version.Build);
            }
        }

        private bool showNotifications;

        public bool ShowNotifications
        {
            get
            {
                return showNotifications;
            }

            set
            {
                Set(ref showNotifications, value);
                SettingsService.Roaming.Write(nameof(ShowNotifications), showNotifications);
            }
        }

        public bool IsWindowInFocus { get; set; }

        private DelegateCommand reviewAppCommand;

        public DelegateCommand ReviewAppCommand
        {
            get
            {
                if (reviewAppCommand == null)
                {
                    reviewAppCommand = new DelegateCommand(async () =>
                    {
                        string storeLink = "ms-windows-store:REVIEW?PFN=" + Package.Current.Id.FamilyName;
                        await Windows.System.Launcher.LaunchUriAsync(new Uri(storeLink));
                    });
                }
                return reviewAppCommand;
            }
        }

        #region Page Events
        public override Task OnNavigatedToAsync(object parameter, NavigationMode mode, IDictionary<string, object> state)
        {
            Debug.WriteLine("navigated");
            ShowNotifications = SettingsService.Roaming.Read(nameof(ShowNotifications), true);

            Windows.UI.Xaml.Window.Current.Activated += (sender, eArgs) =>
            {
                switch (eArgs.WindowActivationState)
                {
                    case Windows.UI.Core.CoreWindowActivationState.CodeActivated:
                    case Windows.UI.Core.CoreWindowActivationState.PointerActivated:
                        IsWindowInFocus = true;
                        break;
                    case Windows.UI.Core.CoreWindowActivationState.Deactivated:
                        IsWindowInFocus = false;
                        break;
                }
            };

            return Task.CompletedTask;
        }
        #endregion

        #region Control Events
        public void WhatsAppWebView_PermissionRequested(WebView sender, WebViewPermissionRequestedEventArgs args)
        {
            if (args.PermissionRequest.PermissionType == WebViewPermissionType.Media
                && args.PermissionRequest.Uri.Host == WhatsAppUrl)
            {
                args.PermissionRequest.Allow();
            }
        }

        public void WhatsAppWebView_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            Debug.WriteLine("Loaded");
            WebView WhatsAppWebView = sender as WebView;
            WhatsAppWebView.Navigate(new System.Uri("http://" + WhatsAppUrl));
            WhatsAppWebView.RegisterPropertyChangedCallback(WebView.DocumentTitleProperty, OnDocumentTitleChanged);
        }

        private async void OnDocumentTitleChanged(DependencyObject sender, DependencyProperty dp)
        {
            WebView whatsAppWebView = sender as WebView;
            Debug.WriteLine("Doc title: " + whatsAppWebView.DocumentTitle);
            if (ShowNotifications)
            {
                await Notify(whatsAppWebView);
            }
        }

        #endregion

        #region Helper methods
        private async Task Notify(WebView whatsAppWebView)
        {
            if (whatsAppWebView.DocumentTitle.Trim().Length > 0)
            {
                string countStr = Regex.Replace(whatsAppWebView.DocumentTitle, "[^0-9]", "");
                int newChatCount = 0;
                int.TryParse(countStr, out newChatCount);
                if (newChatCount != notificationsCount && newChatCount > 0)
                {
                    notificationsCount = newChatCount;
                    string newChatMessage = string.Format("You have {0} new chats.", newChatCount);
                    string unreadMessage = string.Format("Total {0} unread messages.", await GetUnreadMessages(whatsAppWebView));
                    Debug.WriteLine("newChatMessage=>" + newChatMessage);
                    Debug.WriteLine("unreadMessage=>" + unreadMessage);
                    if (!IsWindowInFocus)
                    {
                        ShowToastNotification(newChatMessage, unreadMessage);
                    }
                    ShowTileNotification(newChatMessage, unreadMessage);
                }
            }
        }

        private async Task<string> GetUnreadMessages(WebView whatsAppWebView)
        {
            string[] args = { "(function(){var ele=document.getElementsByClassName('unread-count');var count=0;for(var i=0;i<ele.length;i++){count+=parseInt(ele[i].textContent);}return count.toString();})();" };
            string unreadNotificationsCount = await whatsAppWebView.InvokeScriptAsync("eval", args);
            return unreadNotificationsCount;
        }

        //private async Task<string> GetUserAvatar(WebView whatsAppWebView)
        //{
        //    string[] args = { "(function(){var ele=document.querySelector('header.pane-header div.icon-user-default img');try{return ele.getAttribute('src');}catch(err){return '';}})();" };
        //    string avatarUrl = await whatsAppWebView.InvokeScriptAsync("eval", args);
        //    return avatarUrl;
        //}

        private void ShowToastNotification(string mainMessage, string secondaryMessage)
        {
            XmlDocument notification = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText02);
            XmlNodeList notificationEle = notification.GetElementsByTagName("text");
            notificationEle[0].AppendChild(notification.CreateTextNode(mainMessage));
            notificationEle[1].AppendChild(notification.CreateTextNode(secondaryMessage));
            ToastNotification toast = new ToastNotification(notification);
            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }

        private void ShowTileNotification(string mainMessages, string secondaryMessage)
        {
            var tileXml = TileUpdateManager.GetTemplateContent(TileTemplateType.TileWide310x150Text01);
            var tileTextAttributes = tileXml.GetElementsByTagName("text");
            tileTextAttributes[0].AppendChild(tileXml.CreateTextNode(mainMessages));
            tileTextAttributes[1].AppendChild(tileXml.CreateTextNode(secondaryMessage));
            var tileNotification = new TileNotification(tileXml);
            tileNotification.ExpirationTime = DateTime.Now.AddMinutes(5);
            TileUpdateManager.CreateTileUpdaterForApplication().Clear();
            TileUpdateManager.CreateTileUpdaterForApplication().Update(tileNotification);
        }

        #endregion

    }
}

