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

namespace BuildCast.Views
{
    using System;
    using BuildCast.DataModel;
    using BuildCast.Helpers;
    using BuildCast.Services.Navigation;
    using Microsoft.Toolkit.Uwp.Helpers;
    using Windows.System.Profile;
    using Windows.UI.Core;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Navigation;

    public sealed partial class NavigationRoot : Page
    {
        private static NavigationRoot _instance;
        private INavigationService _navigationService;
        private bool hasLoadedPreviously;

        public NavigationRoot()
        {
            _instance = this;
            this.InitializeComponent();

            var nav = SystemNavigationManager.GetForCurrentView();

            nav.BackRequested += Nav_BackRequested;
        }

        public static NavigationRoot Instance
        {
            get
            {
                return _instance;
            }
        }

        public Frame AppFrame
        {
            get
            {
                return appNavFrame;
            }
        }

        public TitleBarHelper TitleHelper
        {
            get
            {
                return TitleBarHelper.Instance;
            }
        }

        public void InitializeNavigationService(INavigationService navigationService)
        {
            _navigationService = navigationService;
            // TODO: Hook into Navigation Events for loading screen
            _navigationService.Navigated += NavigationService_Navigated;
        }

        public void ToggleFullscreen()
        {
            ViewModeService.Instance.ToggleFullscreen();
        }

        public void ExitFullScreen()
        {
            ViewModeService.Instance.DoExitFullscreen();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is Episode)
            {
                AppFrame.Navigate(typeof(Player), e.Parameter);
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            ViewModeService.Instance.UnRegister();
        }

        private void Nav_BackRequested(object sender, BackRequestedEventArgs e)
        {
            var ignored = _navigationService.GoBackAsync();
            e.Handled = true;
        }

        private void NavigationService_Navigated(object sender, EventArgs e)
        {
            var ignored = DispatcherHelper.ExecuteOnUIThreadAsync(() =>
            {
                var nav = SystemNavigationManager.GetForCurrentView();
                nav.AppViewBackButtonVisibility = _navigationService.CanGoBack ? AppViewBackButtonVisibility.Visible : AppViewBackButtonVisibility.Collapsed;
            });
        }

        private void AppNavFrame_Navigated(object sender, NavigationEventArgs e)
        {
            switch (e.SourcePageType)
            {
                case Type c when e.SourcePageType == typeof(Home):
                    ((NavigationViewItem)navview.MenuItems[0]).IsSelected = true;
                    break;
                case Type c when e.SourcePageType == typeof(Player):
                    ((NavigationViewItem)navview.MenuItems[1]).IsSelected = true;
                    break;
                case Type c when e.SourcePageType == typeof(Favorites):
                    ((NavigationViewItem)navview.MenuItems[2]).IsSelected = true;
                    break;
                case Type c when e.SourcePageType == typeof(Notes):
                    ((NavigationViewItem)navview.MenuItems[3]).IsSelected = true;
                    break;
                case Type c when e.SourcePageType == typeof(Downloads):
                    ((NavigationViewItem)navview.MenuItems[4]).IsSelected = true;
                    break;
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // Only do an inital navigate the first time the page loads
            // when we switch out of compactoverloadmode this will fire but we don't want to navigate because
            // there is already a page loaded
            if (!hasLoadedPreviously)
            {
                _navigationService.NavigateToPodcastsAsync();
                hasLoadedPreviously = true;
            }

            ViewModeService.Instance.Register(navview, appNavFrame);

            if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Xbox")
            {
                ViewModeService.Instance.CollapseNavigationViewToBurger();
                TitleBarHelper.Instance.TitleVisibility = Visibility.Collapsed;
            }
        }

        private void Navview_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked)
            {
                _navigationService.NavigateToSettingsAsync();
                return;
            }

            switch (args.InvokedItem as string)
            {
                case "Browse videos":
                    _navigationService.NavigateToPodcastsAsync();
                    break;
                case "Now playing":
                    _navigationService.NavigateToNowPlayingAsync();
                    break;
                case "Favorites":
                    _navigationService.NavigateToFavoritesAsync();
                    break;
                case "Notes":
                    _navigationService.NavigateToNotesAsync();
                    break;
                case "Downloads":
                    _navigationService.NavigateToDownloadsAsync();
                    break;
            }
        }

        #region Binding Helpers
        public static string GetIcon(string kind)
        {
            switch (kind)
            {
                case "Phone":
                    return "\uE8EA";
                case "Xbox":
                    return "\uE7FC";
                default:
                    return "\uE770";
            }
        }

        public static string ShortDate(DateTime d)
        {
            return d.ToString("d");
        }
        #endregion
    }
}
