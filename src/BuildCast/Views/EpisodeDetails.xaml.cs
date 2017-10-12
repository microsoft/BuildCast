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
using BuildCast.Helpers;
using BuildCast.Services;
using BuildCast.Services.Navigation;
using BuildCast.ViewModels;
using Microsoft.Toolkit.Uwp.Helpers;
using Windows.System.Profile;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace BuildCast.Views
{
    public sealed partial class EpisodeDetails : Page, IPageWithViewModel<EpisodeDetailsViewModel>
    {
        public EpisodeDetails()
        {
            this.InitializeComponent();
            this.ConfigureAnimations();

            // Custom Image sizing for Xbox
            if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Xbox")
            {
                feedItemImage.Height = 200;
                TopBorderRow.Height = new GridLength(300);
            }
        }

        public EpisodeDetailsViewModel ViewModel { get; set; }

        public void UpdateBindings()
        {
            Bindings.Update();
            descriptionweb.DOMContentLoaded += Descriptionweb_DOMContentLoaded;
            descriptionweb.NavigateToString(ViewModel.CurrentEpisode.Description);
           // feedItemImage.Opacity = 0;

            ViewModel.DownloadError += ViewModel_DownloadError;
        }

        private void ViewModel_DownloadError(object sender, EventArgs e)
        {
            var dispatcherTask = DispatcherHelper.ExecuteOnUIThreadAsync(() =>
            {
                var md = new Windows.UI.Popups.MessageDialog("Error in download");
                var showTask = md.ShowAsync();
            });
        }

        private void EpisodeDetails_Loaded(object sender, RoutedEventArgs e)
        {
            // Give focus to play button upon page load
            playepisode.Focus(FocusState.Programmatic);
        }

        private void ConfigureAnimations()
        {
            // TODO: collapse all this into a single helper method
            ElementCompositionPreview.SetIsTranslationEnabled(TopBorder, true);
            ElementCompositionPreview.SetImplicitShowAnimation(TopBorder, VisualHelpers.CreateVerticalOffsetAnimationFrom(0.45, -450f));
            ElementCompositionPreview.SetImplicitHideAnimation(TopBorder, VisualHelpers.CreateVerticalOffsetAnimationTo(0.45, -30));

            // ListContent:
            var listContentShowAnimations = VisualHelpers.CreateVerticalOffsetAnimation(0.45, 50, 0.2);
            var listContentOpacityAnimations = VisualHelpers.CreateOpacityAnimation(.8);

            ElementCompositionPreview.SetIsTranslationEnabled(ListContent, true);
            ElementCompositionPreview.SetImplicitShowAnimation(
                ListContent,
                VisualHelpers.CreateAnimationGroup(listContentShowAnimations, listContentOpacityAnimations));

            ElementCompositionPreview.SetImplicitHideAnimation(ListContent, VisualHelpers.CreateVerticalOffsetAnimationTo(0.4, 50));

            // Set Z index to force this page to the top during the hide animation
            Canvas.SetZIndex(this, 1);
            ElementCompositionPreview.SetImplicitHideAnimation(this, VisualHelpers.CreateOpacityAnimation(0.4, 0));
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            if (ViewModel != null)
            {
                ViewModel.DownloadError -= ViewModel_DownloadError;
            }
        }

        private async void Descriptionweb_DOMContentLoaded(WebView sender, WebViewDOMContentLoadedEventArgs args)
        {
            string webForegroundColor = string.Empty;

            webForegroundColor = ThemeSelectorService.GetSystemControlForegroundColorForThemeHex();

            try
            {
                await descriptionweb.InvokeScriptAsync("eval", new string[] { $"document.body.style.color='{webForegroundColor}'; document.body.style.fontSize='16px'; document.body.style.fontFamily='Segoe UI'; document.getElementsByTagName('img')[0].style.display = 'none';" });
            }
            catch (Exception)
            {
            }
        }

        private void FeedItemImage_ImageOpened(object sender, RoutedEventArgs e)
        {
        }
    }
}
