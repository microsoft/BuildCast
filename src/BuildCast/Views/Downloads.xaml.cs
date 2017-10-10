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
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace BuildCast.Views
{
    public sealed partial class Downloads : Page, IPageWithViewModel<DownloadsViewModel>
    {
        public DownloadsViewModel ViewModel { get; set; }

        public Downloads()
        {
            this.InitializeComponent();

            ConfigureAnimations();
        }

        public void UpdateBindings()
        {
            // Bindings?.Update();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.NavigationMode == NavigationMode.Back)
            {
                if (ConnectedAnimationService.GetForCurrentView().GetAnimation("FeedItemImage") != null)
                {
                    ConnectedAnimationService.GetForCurrentView().GetAnimation("FeedItemImage").Cancel();
                }
            }

            SetupMenuFlyout();
        }

        private void ConfigureAnimations()
        {
            ElementCompositionPreview.SetIsTranslationEnabled(title, true);
            ElementCompositionPreview.SetImplicitShowAnimation(title,
                VisualHelpers.CreateAnimationGroup(
                VisualHelpers.CreateVerticalOffsetAnimationFrom(0.45, -50f),
                VisualHelpers.CreateOpacityAnimation(0.5)));

            Canvas.SetZIndex(this, 1);
            ElementCompositionPreview.SetImplicitHideAnimation(this, VisualHelpers.CreateOpacityAnimation(0.4, 0));
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
    }
}
