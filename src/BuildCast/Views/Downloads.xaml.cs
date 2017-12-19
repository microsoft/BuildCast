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
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace BuildCast.Views
{
    public sealed partial class Downloads : Page, IPageWithViewModel<DownloadsViewModel>
    {
        private UIElement cachedSecondaryPlayIcon = null;
        private UIElement cachedDeleteButtonIcon = null;

        public DownloadsViewModel ViewModel { get; set; }

        public Downloads()
        {
            this.InitializeComponent();
        }

        public void UpdateBindings()
        {
            // Bindings?.Update();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            SetupMenuFlyout();
            Canvas.SetZIndex(this, 0);
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            Canvas.SetZIndex(this, 1);
        }

        private async void Downloads_Loaded(object sender, RoutedEventArgs e)
        {
            await ViewModel.LoadDownloads();
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

                    var feedItem = downloadListView.ItemFromContainer(itemContainer) as EpisodeWithState;

                    (menuFlyoutItem as MenuFlyoutItem).CommandParameter = feedItem.Episode;
                }
            }
        }

        private void DownloadListView_Tapped(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is EpisodeWithState)
            {
                ViewModel.NavigateToEpisode((e.ClickedItem as EpisodeWithState).Episode);
                return;
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

        private void Grid_PointerEntered(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            Grid originGrid = (Grid)sender;

            Grid hoverGrid = (Grid)originGrid.Children[originGrid.Children.Count - 1];
            hoverGrid.Visibility = Visibility.Visible;
        }

        private void Grid_PointerExited(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            Grid originGrid = (Grid)sender;

            Grid hoverGrid = (Grid)originGrid.Children[originGrid.Children.Count - 1];
            hoverGrid.Visibility = Visibility.Collapsed;
        }

        private void DeleteDownload(Episode episode) => ViewModel.RemoveDownloadedEpisode(episode);

        private void RefreshList() => ViewModel.ReloadDownloadList();

        private void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            EpisodeWithState episodePointer = (EpisodeWithState)(sender as AppBarButton).DataContext;
            DeleteDownload(episodePointer.Episode);
        }

        private void swipeDelete_Invoked(SwipeItem sender, SwipeItemInvokedEventArgs args)
        {
            if (args.SwipeControl.DataContext is EpisodeWithState target)
            {
                if (target.Episode != null)
                {
                    DeleteDownload(target.Episode);
                }
            }
        }

        private void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            DeleteDownload((sender as MenuFlyoutItem).CommandParameter as Episode);
            RefreshList();
        }

        private void ContainerItem_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            // Only show the hover buttons when the mouse or pen enters the item.
            if (e.Pointer.PointerDeviceType != Windows.Devices.Input.PointerDeviceType.Touch)
            {
                try
                {
                    var item = sender as ListViewItem;
                    var secondaryPlayIcon = item.GetVisualChildByName<Grid>("PlayIcon");

                    var secondaryCommandPanel = item.GetVisualChildByName<Grid>("SecondaryCommandPanel");
                    var deleteIconButton = secondaryCommandPanel.GetVisualChildByName<Button>("DeleteButton");

                    secondaryPlayIcon.Visibility = Visibility.Visible;
                    deleteIconButton.Visibility = Visibility.Visible;

                    cachedSecondaryPlayIcon = secondaryPlayIcon;
                    cachedDeleteButtonIcon = deleteIconButton;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Catastrophic error: " + ex.Message);
                }
            }
        }

        private void ContainerItem_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType != Windows.Devices.Input.PointerDeviceType.Touch && cachedSecondaryPlayIcon != null)
            {
                cachedDeleteButtonIcon.Visibility = Visibility.Collapsed;
                cachedDeleteButtonIcon = null;

                cachedSecondaryPlayIcon.Visibility = Visibility.Collapsed;
                cachedSecondaryPlayIcon = null;
            }
        }

        private void DownloadListView_ChoosingItemContainer(ListViewBase sender, ChoosingItemContainerEventArgs args)
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
    }
}
