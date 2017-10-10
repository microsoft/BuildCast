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
    public sealed partial class Favorites : Page, IPageWithViewModel<FavoritesViewModel>
    {
        public Favorites()
        {
            this.InitializeComponent();

            ConfigureAnimations();

            //lstFilter.SelectedItem = 0;
        }

        public FavoritesViewModel ViewModel { get; set; }

        public void UpdateBindings()
        {
            //Bindings?.Update();
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
                VisualHelpers.CreateOpacityAnimation(0.5)
                ));

            // favorites listview
            ElementCompositionPreview.SetIsTranslationEnabled(favoriteListView, true);
            ElementCompositionPreview.SetImplicitShowAnimation(
                favoriteListView,
                VisualHelpers.CreateAnimationGroup(
                    VisualHelpers.CreateVerticalOffsetAnimation(0.55, 50, 0),
                    VisualHelpers.CreateOpacityAnimation(0.5)));

            ElementCompositionPreview.SetImplicitHideAnimation(favoriteListView, VisualHelpers.CreateVerticalOffsetAnimationTo(0.4, 50));
            ElementCompositionPreview.SetImplicitHideAnimation(favoriteListView, VisualHelpers.CreateOpacityAnimation(0.4, 0));

            Canvas.SetZIndex(this, 1);
            ElementCompositionPreview.SetImplicitHideAnimation(this, VisualHelpers.CreateOpacityAnimation(0.4, 0));
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

        private void DownloadEpisode(Episode episode) => ViewModel.DownloadEpisode(episode);

        private void RemoveFavoritedEpisode(Episode episode) => ViewModel.RemoveFavoritedEpisode(episode);

        private void RefreshList() => ViewModel.Refresh();

        private void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            EpisodeWithState episodePointer = (EpisodeWithState)(sender as AppBarButton).DataContext;
            DownloadEpisode(episodePointer.Episode);
        }

        private void AppBarButton_Click_1(object sender, RoutedEventArgs e)
        {
            EpisodeWithState episodePointer = (EpisodeWithState)(sender as AppBarButton).DataContext;
            RemoveFavoritedEpisode(episodePointer.Episode);
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
    }
}
