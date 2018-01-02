// ******************************************************************
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THE CODE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
// TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH
// THE CODE OR THE USE OR OTHER DEALINGS IN THE CODE.
// ******************************************************************

using BuildCast.DataModel;
using BuildCast.Helpers;
using BuildCast.Services.Navigation;
using BuildCast.ViewModels;
using System;
using System.Diagnostics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace BuildCast.Views
{
    public sealed partial class Favorites : Page, IPageWithViewModel<FavoritesViewModel>
    {
        private UIElement cachedSecondaryCommandChildPanel = null;
        private UIElement cachedSecondaryPlayIcon = null;

        public Favorites()
        {
            this.InitializeComponent();

            //lstFilter.SelectedItem = 0;
        }

        public FavoritesViewModel ViewModel { get; set; }

        public void UpdateBindings()
        {
            //Bindings?.Update();
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            Canvas.SetZIndex(this, 1);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            SetupMenuFlyout();
            Canvas.SetZIndex(this, 0);
        }

        private async void Favorites_Loaded(object sender, RoutedEventArgs e)
        {
            await ViewModel.LoadFavorites();
        }

        private void MenuFlyout_Opening(object sender, object e)
        {
            MenuFlyout senderAsMenuFlyout = sender as MenuFlyout;

            foreach (object menuFlyoutItem in senderAsMenuFlyout.Items)
            {
                if (menuFlyoutItem.GetType() == typeof(MenuFlyoutItem))
                {
                    // Associate the particular FeedItem with the menu flyout (so the MenuFlyoutItem knows which FeedItem to act upon)
                    ListViewItem itemContainer = senderAsMenuFlyout.Target as ListViewItem;

                    var data = favoriteListView.ItemFromContainer(itemContainer);

                    (menuFlyoutItem as MenuFlyoutItem).CommandParameter = data;
                }
            }
        }

        private void SetupMenuFlyout()
        {
            // Associate the menu with the item requesting it.
            MenuFlyout menu = new MenuFlyout();
            menu.Opening += MenuFlyout_Opening;

            // Add click handlers to the menu flyout items.
            MenuFlyoutItem item = new MenuFlyoutItem { Text = "Remove item", Icon = new SymbolIcon { Symbol = Symbol.Delete } };
            menu.Items.Add(item);
        }

        private void FavoriteListView_Tapped(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is EpisodeWithState episode)
            {
                ViewModel.NavigateToEpisodeAsync(episode.Episode);
            }
        }

        private void DownloadEpisode(Episode episode) => ViewModel.DownloadEpisode(episode);

        private void RemoveFavoritedEpisode(Episode episode) => ViewModel.RemoveFavoritedEpisode(episode);

        private void DeleteEpisode(Episode episode) => ViewModel.RemoveDownloadedEpisode(episode);

        private void RefreshList() => ViewModel.Refresh();

        private void ContainerItem_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            // Only show the hover buttons when the mouse or pen enters the item.
            if (e.Pointer.PointerDeviceType != Windows.Devices.Input.PointerDeviceType.Touch)
            {
                try
                {
                    var item = sender as ListViewItem;
                    var secondaryCommandPanel = item.GetVisualChildByName<StackPanel>("SecondaryCommandPanel");
                    var commandPanelChildHolder = secondaryCommandPanel.GetVisualChildByName<Grid>("ButtonHolder");

                    var commandPanelChild = commandPanelChildHolder.GetVisualChildByName<Button>("DownloadButton");

                    // If the episode is already downloaded, then show delete button instead
                    if ((item.Content as EpisodeWithState).Episode.IsDownloaded)
                    {
                        commandPanelChild = commandPanelChildHolder.GetVisualChildByName<Button>("DeleteButton");
                    }

                    var secondaryPlayIcon = item.GetVisualChildByName<Grid>("PlayIcon");

                    commandPanelChild.Visibility = Visibility.Visible;
                    secondaryPlayIcon.Visibility = Visibility.Visible;

                    cachedSecondaryCommandChildPanel = commandPanelChild;
                    cachedSecondaryPlayIcon = secondaryPlayIcon;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Catastrophic error: " + ex.Message);
                }
            }
        }

        private void ContainerItem_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType != Windows.Devices.Input.PointerDeviceType.Touch && cachedSecondaryCommandChildPanel != null)
            {
                cachedSecondaryCommandChildPanel.Visibility = Visibility.Collapsed;
                cachedSecondaryCommandChildPanel = null;

                cachedSecondaryPlayIcon.Visibility = Visibility.Collapsed;
                cachedSecondaryPlayIcon = null;
            }
        }

        private void SwipeDownload_Invoked(SwipeItem sender, SwipeItemInvokedEventArgs args)
        {
            if (args.SwipeControl.DataContext is EpisodeWithState target)
            {
                DownloadEpisode(target.Episode);
            }
        }

        private void SwipeUnfavorite_Invoked(SwipeItem sender, SwipeItemInvokedEventArgs args)
        {
            if (args.SwipeControl.DataContext is EpisodeWithState target)
            {
                RemoveFavoritedEpisode(target.Episode);
            }
        }

        private void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            RefreshList();
        }

        private void FavoriteListView_ChoosingItemContainer(ListViewBase sender, ChoosingItemContainerEventArgs args)
        {
            // Do we already have an ItemContainer? If so, we're done here.
            if (args.ItemContainer != null)
            {
                return;
            }

            ListViewItem containerItem = new ListViewItem();

            // Show hover buttons
            containerItem.PointerEntered += ContainerItem_PointerEntered;
            containerItem.PointerExited += ContainerItem_PointerExited;

            args.ItemContainer = containerItem;
        }

        private void UnfaveButton_Click(object sender, RoutedEventArgs e)
        {
            EpisodeWithState episodePointer = (EpisodeWithState)(sender as Button).DataContext;
            RemoveFavoritedEpisode(episodePointer.Episode);
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            EpisodeWithState episodePointer = (EpisodeWithState)(sender as Button).DataContext;
            DeleteEpisode(episodePointer.Episode);
        }

        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            EpisodeWithState episodePointer = (EpisodeWithState)(sender as Button).DataContext;
            DownloadEpisode(episodePointer.Episode);
        }
    }
}
