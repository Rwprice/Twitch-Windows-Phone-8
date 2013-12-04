﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using TwitchAPIHandler;
using TwitchAPIHandler.Objects;
using Windows.Storage;
using Windows.Storage.Streams;

namespace TwitchTV
{
    public partial class LoginPage : PhoneApplicationPage
    {
        public LoginPage()
        {
            InitializeComponent();
            this.WebBrowser.Loaded += WebBrowser_Loaded;
            this.WebBrowser.Navigating += WebBrowser_Navigating;
        }

        async void WebBrowser_Navigating(object sender, NavigatingEventArgs e)
        {
            if (e.Uri.Host == "localhost")
            {
                string token = e.Uri.AbsoluteUri.Substring(e.Uri.AbsoluteUri.IndexOf('=') + 1);
                token = token.Remove(token.IndexOf('&'));

                StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                StorageFile textFile = await localFolder.CreateFileAsync("token", CreationCollisionOption.ReplaceExisting);

                using (IRandomAccessStream textStream = await textFile.OpenAsync(FileAccessMode.ReadWrite))
                {
                    using (DataWriter textWriter = new DataWriter(textStream))
                    {
                        textWriter.WriteString(token);
                        await textWriter.StoreAsync();
                    }
                }

                App.ViewModel.token = token;

                await this.WebBrowser.ClearCookiesAsync();
                await this.WebBrowser.ClearInternetCacheAsync();

                NavigationService.GoBack();
            }
        }

        void WebBrowser_Loaded(object sender, RoutedEventArgs e)
        {
            this.WebBrowser.Navigate(new Uri("https://api.twitch.tv/kraken/oauth2/authorize?response_type=token&client_id=b4v9ttxqtldlobe5jswfqdhrmzp52hi&redirect_uri=http://localhost&scope=user_read chat_login"));
        }
    }
}