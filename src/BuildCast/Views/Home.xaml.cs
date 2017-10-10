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
using System.Linq;
using System.Numerics;
using BuildCast.DataModel;
using BuildCast.Helpers;
using BuildCast.Services;
using BuildCast.Services.Navigation;
using BuildCast.ViewModels;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace BuildCast.Views
{
    public sealed partial class Home : Page, IPageWithViewModel<HomeViewModel>
    {
        private static int _persistedItemIndex = -1;

        public Home()
        {
            this.InitializeComponent();

            HomeFeedGrid.ItemsSource = FeedStore.AllFeeds;

            ConfigureAnimations();
        }

        public HomeViewModel ViewModel { get; set; }

        public ElementTheme HomeTheme
        {
            get
            {
                return ThemeSelectorService.GetHomeTheme();
            }
        }

        public Style HomeBackground
        {
            get
            {
                return ThemeSelectorService.GetHomeBackground();
            }
        }

        public string ParallaxImage
        {
            get
            {
                return ThemeSelectorService.GetHomeImageSource();
            }
        }

        public string LogoSource
        {
            get
            {
                return ThemeSelectorService.GetLogoSource();
            }
        }

        public void UpdateBindings()
        {
            Bindings.Update();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.NavigationMode == NavigationMode.Back)
            {
                if (_persistedItemIndex >= 0)
                {
                    HomeFeedGrid.Loaded += async (a, s) =>
                    {
                    var item = HomeFeedGrid.Items[_persistedItemIndex];
                    if (item != null)
                        {
                            HomeFeedGrid.ScrollIntoView(item);
                            var connectedAnimation = ConnectedAnimationService
                            .GetForCurrentView()
                            .GetAnimation("podimageback");

                            if (connectedAnimation != null)
                            {
                                await HomeFeedGrid.TryStartConnectedAnimationAsync(
                                    connectedAnimation,
                                    item,
                                    "Image");
                            }
                        }
                    };
                }
            }
            else
            {
                _persistedItemIndex = -1;
            }

            ConfigureComposition();
        }

        private void ConfigureComposition()
        {
            this.Logo.EnableLayoutImplicitAnimations(TimeSpan.FromMilliseconds(100));
            this.Search.EnableLayoutImplicitAnimations(TimeSpan.FromMilliseconds(100));
        }

        private void ConfigureAnimations()
        {
            ElementCompositionPreview.SetIsTranslationEnabled(paraimage, true);
            ElementCompositionPreview.SetImplicitShowAnimation(paraimage, VisualHelpers.CreateVerticalOffsetAnimationFrom(0.55, -150));
            ElementCompositionPreview.SetImplicitHideAnimation(paraimage, VisualHelpers.CreateVerticalOffsetAnimationTo(0.55, -150));
            ElementCompositionPreview.SetImplicitHideAnimation(paraimage, VisualHelpers.CreateOpacityAnimation(0.4, 0));

            Canvas.SetZIndex(this, 1);
            ElementCompositionPreview.SetImplicitHideAnimation(this, VisualHelpers.CreateOpacityAnimation(0.4, 0));
        }

        #region staggering
        private void HomeFeedGrid_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            args.ItemContainer.Loaded += ItemContainer_Loaded;
        }

        private void ItemContainer_Loaded(object sender, RoutedEventArgs e)
        {
            var itemsPanel = (ItemsStackPanel)this.HomeFeedGrid.ItemsPanelRoot;
            var itemContainer = (GridViewItem)sender;

            var itemIndex = this.HomeFeedGrid.IndexFromContainer(itemContainer);

            var relativeIndex = itemIndex - itemsPanel.FirstVisibleIndex;

            var uc = itemContainer.ContentTemplateRoot as Grid;

            if (itemIndex != _persistedItemIndex && itemIndex >= 0 && itemIndex >= itemsPanel.FirstVisibleIndex && itemIndex <= itemsPanel.LastVisibleIndex)
            {
                var itemVisual = ElementCompositionPreview.GetElementVisual(uc);
                ElementCompositionPreview.SetIsTranslationEnabled(uc, true);

                var easingFunction = Window.Current.Compositor.CreateCubicBezierEasingFunction(new Vector2(0.1f, 0.9f), new Vector2(0.2f, 1f));

                // Create KeyFrameAnimations
                var offsetAnimation = Window.Current.Compositor.CreateScalarKeyFrameAnimation();
                offsetAnimation.InsertKeyFrame(0f, 100);
                offsetAnimation.InsertKeyFrame(1f, 0, easingFunction);
                offsetAnimation.Target = "Translation.X";
                offsetAnimation.DelayBehavior = AnimationDelayBehavior.SetInitialValueBeforeDelay;
                offsetAnimation.Duration = TimeSpan.FromMilliseconds(700);
                offsetAnimation.DelayTime = TimeSpan.FromMilliseconds(relativeIndex * 100);

                var fadeAnimation = Window.Current.Compositor.CreateScalarKeyFrameAnimation();
                fadeAnimation.InsertExpressionKeyFrame(0f, "0");
                fadeAnimation.InsertExpressionKeyFrame(1f, "1");
                fadeAnimation.DelayBehavior = AnimationDelayBehavior.SetInitialValueBeforeDelay;
                fadeAnimation.Duration = TimeSpan.FromMilliseconds(700);
                fadeAnimation.DelayTime = TimeSpan.FromMilliseconds(relativeIndex * 100);

                // Start animations
                itemVisual.StartAnimation("Translation.X", offsetAnimation);
                itemVisual.StartAnimation("Opacity", fadeAnimation);
            }
            else
            {
                Debug.WriteLine("Skipping");
            }

            itemContainer.Loaded -= this.ItemContainer_Loaded;
        }

#endregion

        private void HomeFeedGrid_ItemClick(object sender, ItemClickEventArgs e)
        {
            var selectedFeed = e.ClickedItem as Feed;
            _persistedItemIndex = HomeFeedGrid.Items.IndexOf(e.ClickedItem);
            HomeFeedGrid.PrepareConnectedAnimation("PodcastImageBorder", e.ClickedItem, "Image");
            ViewModel.NavigateToFeed(selectedFeed);
        }

        private void AutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                using (var context = new LocalStorageContext())
                {
                    var results = context.EpisodeCache.Where(t => t.Title.ToLower().Contains(sender.Text.ToLower()));
                    sender.ItemsSource = results.ToList();
                }
            }
        }

        private void AutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            ViewModel.NavigateToEpisode(args.SelectedItem as Episode);
        }
    }
}
