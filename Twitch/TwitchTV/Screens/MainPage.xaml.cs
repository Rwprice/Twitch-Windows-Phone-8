﻿using LiveTileTaskAgent;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Net.NetworkInformation;
using Microsoft.Phone.Scheduler;
using Microsoft.Phone.Shell;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Navigation;
using TwitchAPIHandler.Objects;
using TwitchTV.ViewModels;

namespace TwitchTV
{
    public partial class MainPage : PhoneApplicationPage
    {
        private int _topStreamsPageNumber = 0;
        private int _topStreamsOffsetKnob = 1;
        TopStreamsViewModel _topStreamsViewModel;

        private int _topGamesPageNumber = 0;
        private int _topGamesOffsetKnob = 1;
        TopGamesViewModel _topGamesViewModel;

        private int _followedPageNumber = 0;
        private int _followedOffsetKnob = 1;
        FollowedStreamsViewModel _followedViewModel;

        FeaturedStreamsViewModel _featuredViewModel;

        public bool isNetwork { get; set; }
        private bool alreadyLoadedFromToken = false;
        private DateTime lastUpdate { get; set; }
        private static string liveTileTaskName = "LiveTileTask";

        public MainPage()
        {
            isNetwork = NetworkInterface.GetIsNetworkAvailable();
            
            if (!isNetwork)
            {
                MessageBox.Show("You are not connected to a network. Twitchy is unavailable");
            }

            InitializeComponent();

            this.FrontPageAd.ErrorOccurred += FrontPageAd_ErrorOccurred;

            App.ViewModel.LoadSettings();

            _topStreamsViewModel = new TopStreamsViewModel();
            _topGamesViewModel = new TopGamesViewModel();
            _followedViewModel = new FollowedStreamsViewModel();
            _featuredViewModel = new FeaturedStreamsViewModel();

            TopStreamsList.ItemRealized += topStreamsList_ItemRealized;
            TopGamesList.ItemRealized += topGamesList_ItemRealized;
            FollowedStreamsList.ItemRealized += followedStreamsList_ItemRealized;

            this.Loaded += new RoutedEventHandler(MainPage_Loaded);
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                App.ViewModel.curTopGame = null;

                TopStreamsList.ItemsSource = _topStreamsViewModel.StreamList;
                TopGamesList.ItemsSource = _topGamesViewModel.GamesList;
                FollowedStreamsList.ItemsSource = _followedViewModel.StreamList;
                FeaturedStreams.ItemsSource = _featuredViewModel.StreamList;

                var progressIndicator = SystemTray.ProgressIndicator;
                if (progressIndicator != null)
                {
                    return;
                }

                progressIndicator = new ProgressIndicator();

                SystemTray.SetProgressIndicator(this, progressIndicator);

                Binding binding = new Binding("IsLoading") { Source = _topStreamsViewModel };
                BindingOperations.SetBinding(
                    progressIndicator, ProgressIndicator.IsVisibleProperty, binding);

                binding = new Binding("IsLoading") { Source = _topStreamsViewModel };
                BindingOperations.SetBinding(
                    progressIndicator, ProgressIndicator.IsIndeterminateProperty, binding);

                progressIndicator.Text = "Loading...";
            }

            catch (Exception ex)
            {
                MessageBox.Show("Can't load front page data. Try again later", "Well, this is embarrassing...", MessageBoxButton.OK);
                Debug.WriteLine(ex.Message);
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            TryRefresh();
            base.OnNavigatedTo(e);
        }

        private void followedStreamsList_ItemRealized(object sender, ItemRealizationEventArgs e)
        {
            if (App.ViewModel.user != null)
            {
                if (!_followedViewModel.IsLoading && FollowedStreamsList.ItemsSource != null && FollowedStreamsList.ItemsSource.Count >= _followedOffsetKnob)
                {
                    if (e.ItemKind == LongListSelectorItemKind.Item)
                    {
                        if ((e.Container.Content as Stream).Equals(FollowedStreamsList.ItemsSource[FollowedStreamsList.ItemsSource.Count - _followedOffsetKnob]))
                        {
                            Debug.WriteLine("Searching for Followed Page {0}", _followedPageNumber);
                            _followedViewModel.LoadPage(App.ViewModel.user.Oauth, _followedPageNumber++);
                        }
                    }
                }
            }
        }        

        private void topGamesList_ItemRealized(object sender, ItemRealizationEventArgs e)
        {
            if (!_topGamesViewModel.IsLoading && TopGamesList.ItemsSource != null && TopGamesList.ItemsSource.Count >= _topGamesOffsetKnob)
            {
                if (e.ItemKind == LongListSelectorItemKind.Item)
                {
                    if ((e.Container.Content as TopGame).Equals(TopGamesList.ItemsSource[TopGamesList.ItemsSource.Count - _topGamesOffsetKnob]))
                    {
                        Debug.WriteLine("Searching for Top Games Page {0}", _topGamesPageNumber);
                        _topGamesViewModel.LoadPage(_topGamesPageNumber++);
                    }
                }
            }
        }

        private void topStreamsList_ItemRealized(object sender, ItemRealizationEventArgs e)
        {
            if (!_topStreamsViewModel.IsLoading && TopStreamsList.ItemsSource != null && TopStreamsList.ItemsSource.Count >= _topStreamsOffsetKnob)
            {
                if (e.ItemKind == LongListSelectorItemKind.Item)
                {
                    if ((e.Container.Content as Stream).Equals(TopStreamsList.ItemsSource[TopStreamsList.ItemsSource.Count - _topStreamsOffsetKnob]))
                    {
                        Debug.WriteLine("Searching for Top Streams Page {0}", _topStreamsPageNumber);
                        _topStreamsViewModel.LoadPage(_topStreamsPageNumber++);
                    }
                }
            }
        }

        void FrontPageAd_ErrorOccurred(object sender, Microsoft.Advertising.AdErrorEventArgs e)
        {
            Debug.WriteLine(e.Error.Message);
        }

        private void SettingTapped(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if((((TextBlock)sender).Text) != "Logout")
                NavigationService.Navigate(new Uri("/Screens/" + (((TextBlock)sender).Text) + "Page.xaml", UriKind.RelativeOrAbsolute));

            else if ((((TextBlock)sender).Text) == "Logout")
            {
                User.LogoutUser();

                App.ViewModel.user = null;
                App.ViewModel.LiveTilesEnabled = false;

                MessageBox.Show("User has been logged out!");

                if (this.FollowedStreamsList.ItemsSource.Count > 0)
                {
                    _followedViewModel.ClearList();
                    _followedPageNumber = 0;
                    _followedOffsetKnob = 1;
                }

                alreadyLoadedFromToken = false;

                this.Account.Text = "Login";
            }
        }

        private void StreamsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (((Stream)((LongListSelector)sender).SelectedItem) != null)
            {
                App.ViewModel.stream = ((Stream)((LongListSelector)sender).SelectedItem);
                ((LongListSelector)sender).SelectedItem = null;
                NavigationService.Navigate(new Uri("/Screens/PlayerPage.xaml", UriKind.RelativeOrAbsolute));
            }
        }

        private void TopGamesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (((TopGame)((LongListSelector)sender).SelectedItem) != null)
            {
                App.ViewModel.curTopGame = ((TopGame)((LongListSelector)sender).SelectedItem);
                ((LongListSelector)sender).SelectedItem = null;
                NavigationService.Navigate(new Uri("/Screens/TopGamePage.xaml", UriKind.RelativeOrAbsolute));
            }
        }

        private void SearchButton_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Screens/SearchPage.xaml", UriKind.RelativeOrAbsolute));
        }

        private void RefreshButton_Click(object sender, EventArgs e)
        {
            Refresh();
        }

        private async void Refresh()
        {
            if (isNetwork)
            {
                lastUpdate = DateTime.MinValue;

                _topStreamsViewModel.ClearList();
                _topStreamsPageNumber = 0;
                _topStreamsOffsetKnob = 1;
                _topStreamsViewModel.LoadPage(_topStreamsPageNumber++);

                _topGamesViewModel.ClearList();
                _topGamesPageNumber = 0;
                _topGamesOffsetKnob = 1;
                _topGamesViewModel.LoadPage(_topGamesPageNumber++);

                _followedViewModel.ClearList();
                _followedPageNumber = 0;
                _followedOffsetKnob = 1;

                _featuredViewModel.ClearList();
                _featuredViewModel.LoadPage();

                if (App.ViewModel.user == null)
                {
                    var user = await User.TryLoadUser();
                    if (user != null)
                    {
                        App.ViewModel.user = user;
                        this.Account.Text = "Logout";
                    }
                }

                else
                    this.Account.Text = "Logout";

                if (App.ViewModel.user != null)
                {
                    _followedViewModel.LoadPage(App.ViewModel.user.Oauth, _followedPageNumber++);
                    alreadyLoadedFromToken = true;
                }
            }

            lastUpdate = DateTime.Now;

            StartPeriodicAgent(liveTileTaskName);
        }

        private void TryRefresh()
        {
            if (lastUpdate == null || (!alreadyLoadedFromToken && App.ViewModel.user != null) || lastUpdate.AddMinutes(2) <= DateTime.Now)
            {
                Refresh();
            }
        }

        #region Context Menu

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            Stream stream = (Stream)(sender as MenuItem).DataContext;
            App.ViewModel.stream = stream;
            NavigationService.Navigate(new Uri("/Screens/PlayerPage.xaml", UriKind.RelativeOrAbsolute));
        }

        private async void Follow_Click(object sender, RoutedEventArgs e)
        {
            Stream stream = (Stream)(sender as MenuItem).DataContext;

            if (((string)((MenuItem)(sender)).Header) == "Unfollow")
            {
                await User.UnfollowStream(stream.channel.name, App.ViewModel.user);
                ((MenuItem)(sender)).Header = "Follow";
            }

            else
            {
                await User.FollowStream(stream.channel.name, App.ViewModel.user);
                ((MenuItem)(sender)).Header = "Unfollow";
            }

            _followedViewModel.ClearList();
            _followedPageNumber = 0;
            _followedOffsetKnob = 1;

            if (App.ViewModel.user != null)
            {
                _followedViewModel.LoadPage(App.ViewModel.user.Oauth, _followedPageNumber++);
                alreadyLoadedFromToken = true;
            }
        }

        private async void Follow_Loaded(object sender, RoutedEventArgs e)
        {
            if (App.ViewModel.user != null)
            {
                var menuItem = sender as MenuItem;
                var contextMenu = menuItem.Parent as ContextMenu;
                Stream stream = (Stream)contextMenu.DataContext;
                bool isFollowedTask = await User.IsStreamFollowed(stream.channel.name, App.ViewModel.user);

                menuItem.IsEnabled = true;

                if (isFollowedTask)
                    menuItem.Header = "Unfollow";
            }
        }

        private void ContextMenu_Unloaded(object sender, RoutedEventArgs e)
        {
            ContextMenu conmen = (sender as ContextMenu);
            conmen.ClearValue(FrameworkElement.DataContextProperty);
        }

        #region Live Tiles
        private void Pin_to_Start_Click(object sender, RoutedEventArgs e)
        {
            Stream stream = (Stream)(sender as MenuItem).DataContext;
            ShellTile tile;

            if ((sender as MenuItem).Header.ToString() == "Pin to Start")
            {
                tile = LiveTileHelper.FindTile(stream.channel.name);

                if (tile == null)
                {
                    Uri uri;
                    if (stream.channel.logoUri != "")
                        uri = new Uri(stream.channel.logoUri);
                    else
                        uri = new Uri("/Assets/noProfPic.png", UriKind.Relative);

                    StandardTileData tileData = new StandardTileData
                    {
                        BackgroundImage = uri,
                        Title = stream.channel.display_name
                    };

                    LiveTileHelper.SaveTileImages(stream.channel.name, uri);
                    string tileUri = string.Concat("/Screens/PlayerPage.xaml?", stream.channel.name);
                    ShellTile.Create(new Uri(tileUri, UriKind.Relative), tileData);
                }
            }

            else
            {
                tile = LiveTileHelper.FindTile(stream.channel.name);
                if (tile != null)
                {
                    tile.Delete();
                    LiveTileHelper.DeleteImage(stream.channel.name);
                }
            }
        }

        private void Pin_to_Start_Loaded(object sender, RoutedEventArgs e)
        {
            Stream stream = (Stream)(sender as MenuItem).DataContext;
            ShellTile tile = LiveTileHelper.FindTile(stream.channel.name);

            if (tile == null)
                (sender as MenuItem).Header = "Pin to Start";
            else
                (sender as MenuItem).Header = "Unpin from Start";
        }

        private void StartPeriodicAgent(string name)
        {
            try
            {
                if (ScheduledActionService.Find(name) != null)
                {
                    //if the agent exists, remove and then add it to ensure
                    //the agent's schedule is updated to avoid expiration
                    RemoveAgent(name);
                }

                PeriodicTask periodicTask = new PeriodicTask(name);
                periodicTask.Description = (App.ViewModel.user != null && App.ViewModel.LiveTilesEnabled) ? App.ViewModel.user.Oauth : "No OAuth to use";
                ScheduledActionService.Add(periodicTask);

                #if DEBUG
                ScheduledActionService.LaunchForTest(periodicTask.Name, TimeSpan.FromSeconds(60));
                #endif
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        private void RemoveAgent(string name)
        {
            try
            {
                ScheduledActionService.Remove(name);
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception);
            }
        }
        #endregion
        #endregion
    }
}