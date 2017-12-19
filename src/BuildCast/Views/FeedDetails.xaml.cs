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

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using BuildCast.DataModel;
using BuildCast.Helpers;
using BuildCast.Services.Navigation;
using BuildCast.ViewModels;
using Windows.System.Profile;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace BuildCast.Views
{
    public sealed partial class FeedDetails : Page, IPageWithViewModel<FeedDetailsViewModel>
    {
        private UIElement cachedSecondaryCommandPanel = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="FeedDetails"/> class.
        /// </summary>
        public FeedDetails()
        {
            this.InitializeComponent();

            // Customize sizing for Xbox
            if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Xbox")
            {
                podimage.Width = 208;
                podimage.Height = 208;
                TopBorder.Height = 306;
            }

            Loaded += FeedDetails_Loaded;
        }

        public FeedDetailsViewModel ViewModel { get; set; }

        public void UpdateBindings()
        {
            if (ViewModel.PersistedEpisode != null)
            {
                feeditems.Loaded += (s, ev) => feeditems.ScrollIntoView(ViewModel.PersistedEpisode, ScrollIntoViewAlignment.Leading);
            }

            Bindings.Update();
        }

        private void FeedDetails_Loaded(object sender, RoutedEventArgs e)
        {
            btnrefresh.Focus(FocusState.Programmatic);

            Loaded -= FeedDetails_Loaded;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            Canvas.SetZIndex(this, 0);
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            Canvas.SetZIndex(this, 1);
        }

        private void Feeditems_ChoosingItemContainer(ListViewBase sender, ChoosingItemContainerEventArgs args)
        {
            // Do we already have an ItemContainer? If so, we're done here.
            if (args.ItemContainer != null)
            {
                return;
            }

            // Otherwise, we need to make a new container. Wire up the events on the container, and set it as the ItemContainer.
            ListViewItem containerItem = new ListViewItem();

            // Wire up stagger animations on items
            containerItem.Loaded += ContainerItem_Loaded;

            // Show hover buttons
            containerItem.PointerEntered += ContainerItem_PointerEntered;
            containerItem.PointerExited += ContainerItem_PointerExited;

            // Listen for key events.
            containerItem.KeyDown += ContainerItem_KeyDown;

            args.ItemContainer = containerItem;
        }

        #region stagger animation on list items

        private void ContainerItem_Loaded(object sender, RoutedEventArgs e)
        {
            var itemsPanel = (ItemsStackPanel)feeditems.ItemsPanelRoot;
            var itemContainer = (ListViewItem)sender;

            var itemIndex = feeditems.IndexFromContainer(itemContainer);

            var relativeIndex = itemIndex - itemsPanel.FirstVisibleIndex;

            Grid uc;
            if (itemContainer.ContentTemplateRoot as SwipeControl != null)
            {
                uc = (itemContainer.ContentTemplateRoot as SwipeControl).Content as Grid;
            }
            else
            {
                uc = itemContainer.ContentTemplateRoot as Grid;
            }

            if (itemContainer.Content != ViewModel.PersistedEpisode && itemIndex >= 0 && itemIndex >= itemsPanel.FirstVisibleIndex && itemIndex <= itemsPanel.LastVisibleIndex)
            {
                var itemVisual = ElementCompositionPreview.GetElementVisual(uc);
                ElementCompositionPreview.SetIsTranslationEnabled(uc, true);

                var staggerDelay = TimeSpan.FromMilliseconds(relativeIndex * 100);

                var offsetAnimation = VisualHelpers.CreateHorizontalOffsetAnimation(0.7, 150, staggerDelay.TotalSeconds);
                itemVisual.StartAnimation("Translation.X", offsetAnimation);

                var opacityAnimation = VisualHelpers.CreateOpacityAnimation(0.5);
                opacityAnimation.DelayBehavior = Windows.UI.Composition.AnimationDelayBehavior.SetInitialValueBeforeDelay;
                opacityAnimation.DelayTime = staggerDelay;
                itemVisual.StartAnimation("Opacity", opacityAnimation);
            }

            itemContainer.Loaded -= this.ContainerItem_Loaded;
        }

        #endregion
        private void ContainerItem_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            // Only show the hover buttons when the mouse or pen enters the item.
            if (e.Pointer.PointerDeviceType != Windows.Devices.Input.PointerDeviceType.Touch)
            {
                try
                {
                    var item = sender as ListViewItem;
                    var secondaryCommandPanel = item.GetVisualChildByName<StackPanel>("SecondaryCommandPanel");
                    secondaryCommandPanel.Visibility = Visibility.Visible;
                    cachedSecondaryCommandPanel = secondaryCommandPanel;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Catastrophic error: " + ex.Message);
                }
            }
        }

        private void ContainerItem_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType != Windows.Devices.Input.PointerDeviceType.Touch && cachedSecondaryCommandPanel != null)
            {
                cachedSecondaryCommandPanel.Visibility = Visibility.Collapsed;
                cachedSecondaryCommandPanel = null;
            }
        }

        private void ContainerItem_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            Episode episode = feeditems.SelectedItem as Episode;

            // Do not try to perform an action if an item isn't selected.
            if (episode == null)
            {
                return;
            }

            if (e.Key == Windows.System.VirtualKey.P)
            {
                ViewModel.PlayEpisode(episode);
            }

            if (e.Key == Windows.System.VirtualKey.F)
            {
                FavoriteEpisode(episode);
            }

            if (e.Key == Windows.System.VirtualKey.D)
            {
                DownloadEpisode(episode);
            }
        }

        private void FavoriteEpisode(Episode episode) => ViewModel.FavoriteEpisode(episode);

        private void PlayEpisode(Episode episode) => ViewModel.PlayEpisode(episode);

        private void DownloadEpisode(Episode episode) => ViewModel.DownloadEpisode(episode);

        private void MenuFlyout_Opening(object sender, object e)
        {
            MenuFlyout senderAsMenuFlyout = sender as MenuFlyout;

            foreach (object menuFlyoutItem in senderAsMenuFlyout.Items)
            {
                if (menuFlyoutItem.GetType() == typeof(MenuFlyoutItem))
                {
                    // Associate the particular FeedItem with the menu flyout (so the MenuFlyoutItem knows which FeedItem to act upon)
                    ListViewItem itemContainer = senderAsMenuFlyout.Target as ListViewItem;

                    Episode feedItem = feeditems.ItemFromContainer(itemContainer) as Episode;

                    (menuFlyoutItem as MenuFlyoutItem).CommandParameter = feedItem;
                }
            }
        }

        private void Feeditems_ItemClick(object sender, ItemClickEventArgs e)
        {
            Episode detailsItem = e.ClickedItem as Episode;

            ViewModel.GoToEpisodeDetails(detailsItem);
        }

        private void PlayMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            PlayEpisode((sender as MenuFlyoutItem).CommandParameter as Episode);
        }

        private void PlayIconButton_Click(object sender, RoutedEventArgs e)
        {
            PlayEpisode((sender as Button).CommandParameter as Episode);
        }

        private void DownloadFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            DownloadEpisode((sender as MenuFlyoutItem).CommandParameter as Episode);
        }

        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            DownloadEpisode((sender as Button).CommandParameter as Episode);
        }

        private void FavoriteFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            FavoriteEpisode((sender as MenuFlyoutItem).CommandParameter as Episode);
        }

        private void FavoriteButton_Click(object sender, RoutedEventArgs e)
        {
            FavoriteEpisode((sender as Button).CommandParameter as Episode);
        }

        // Don't do anything but close the swipe container if a non-favorite swipe action occurs.
        private void SwipeItem_Invoked(SwipeItem sender, SwipeItemInvokedEventArgs args)
        {
            if (args.SwipeControl.DataContext is Episode target)
            {
                DownloadEpisode(target);
            }
        }

        // Favorite the particular item, and then close the container.
        private void FavoriteSwipeItem_Invoked(SwipeItem sender, SwipeItemInvokedEventArgs args)
        {
            if (args.SwipeControl.DataContext is Episode episode)
            {
                FavoriteEpisode(episode);
            }
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadMoreData();
        }

        private async void Button_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            await this.ViewModel.RemoveTopThree();

            await UIHelpers.ShowContentAsync("Removing top 3 items");
        }

        private async Task LoadMoreData()
        {
            this.feeditems.ItemContainerTransitions = new TransitionCollection
            {
                new AddDeleteThemeTransition(),
            };

            var newEpisodeCount = await ViewModel.RefreshData();
            RefreshStatusTextBlock.Text = (newEpisodeCount == 0) ? "Already up-to-date." : $"Found {newEpisodeCount} new episodes.";
        }

        private void Podimage_ImageOpened(object sender, RoutedEventArgs e)
        {

        }
    }
}
