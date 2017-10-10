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

namespace BuildCast.Helpers
{
    using System;
    using System.Threading.Tasks;
    using BuildCast.Services;
    using Windows.ApplicationModel.Core;
    using Windows.UI.Core;
    using Windows.UI.ViewManagement;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Media;

    public class ViewModeService
    {
        // Compact mode
        private static readonly ViewModeService _instance = new ViewModeService();
        private UIElement _previousViewContent;

        // Fullscreen
        private double oldCompactThreshold;
        private double oldExpandedThreshold;
        private Brush oldBackgroundBrush;
        private FrameworkElement _navHeader;
        private FrameworkElement _menuButton;
        private NavigationView _navigationView;
        private Frame _appNavFrame;
        private bool inFullScreen;

        public static ViewModeService Instance => _instance;

        public async Task CreateNewView<T>(Func<T> newViewObjectFactory, Action<T> loadAction)
            where T : UIElement
        {
            CoreApplicationView newView = CoreApplication.CreateNewView();
            var newViewId = 0;
            await newView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        var newViewContent = newViewObjectFactory();
                        Window.Current.Content = newViewContent;
                        Window.Current.Activate();
                        newViewId = ApplicationView.GetForCurrentView().Id;

                        loadAction(newViewContent);
                    });
            var viewShown = await ApplicationViewSwitcher.TryShowAsViewModeAsync(newViewId, ApplicationViewMode.CompactOverlay);
        }

        public async Task<bool> SwitchToCompactOverlay<T>(T newView, Action<T> loadAction)
            where T : UIElement
        {
            _previousViewContent = Window.Current.Content;
            bool modeSwitched = await ApplicationView.GetForCurrentView().TryEnterViewModeAsync(ApplicationViewMode.CompactOverlay);
            if (modeSwitched)
            {
                Window.Current.Content = newView;
                loadAction(newView);
            }

            return modeSwitched;
        }

        public async Task<bool> SwitchToNormalMode()
        {
            bool modeSwitched = await ApplicationView.GetForCurrentView().TryEnterViewModeAsync(ApplicationViewMode.Default);
            if (modeSwitched)
            {
                Window.Current.Content = _previousViewContent;
                _previousViewContent = null;
            }

            return modeSwitched;
        }

        public void Register(NavigationView navview, Frame appNavFrame)
        {
            _navigationView = navview;
            _appNavFrame = appNavFrame;
        }

        public void UnRegister()
        {
            _navigationView = null;
            _appNavFrame = null;
        }

        public void DoEnterFullscreen()
        {
            var view = ApplicationView.GetForCurrentView();
            inFullScreen = view.TryEnterFullScreenMode();
            if (inFullScreen)
            {
                // Adjust navigationview to accomodate fullscreen
                if (_navigationView != null)
                {
                    oldBackgroundBrush = _navigationView.Background;

                    CollapseNavigationViewToBurger();

                    EnsureElements(_navigationView);

                    // Hide navigationview menu while in fullscreen
                    _menuButton.Visibility = Visibility.Collapsed;
                }

                // Hide titlebar
                TitleBarHelper.Instance.GoFullscreen();

                // If current page is intersted in fullscreen, tell it
                if (_appNavFrame != null && _appNavFrame.Content is IFullscreenPage)
                {
                    ((IFullscreenPage)_appNavFrame.Content).EnterFullscreen();
                }
            }
        }

        public void CollapseNavigationViewToBurger()
        {
            EnsureElements(_navigationView);

            oldCompactThreshold = _navigationView.CompactModeThresholdWidth;
            oldExpandedThreshold = _navigationView.ExpandedModeThresholdWidth;

            // Force navview to collapse to it's least wide mode
            _navigationView.CompactModeThresholdWidth = 10000;
            _navigationView.ExpandedModeThresholdWidth = 10000;

            // Collapse navigationview header while in fullscreen
            _navHeader.Visibility = Visibility.Collapsed;
        }

        public void RestoreNavigationViewDefault()
        {
            _navigationView.CompactModeThresholdWidth = oldCompactThreshold;
            _navigationView.ExpandedModeThresholdWidth = oldExpandedThreshold;
            _navHeader.Visibility = Visibility.Visible;
        }

        public void DoExitFullscreen()
        {
            if (inFullScreen)
            {
                var view = ApplicationView.GetForCurrentView();
                view.ExitFullScreenMode();

                TitleBarHelper.Instance.ExitFullscreen();

                if (_navigationView != null)
                {
                    EnsureElements(_navigationView);
                    RestoreNavigationViewDefault();

                    _navigationView.Background = oldBackgroundBrush;

                    _menuButton.Visibility = Visibility.Visible;
                }

                if (_appNavFrame != null && _appNavFrame.Content is IFullscreenPage)
                {
                    ((IFullscreenPage)_appNavFrame.Content).ExitFullscreen();
                }

                inFullScreen = false;
            }
        }

        public void ToggleFullscreen()
        {
            var view = ApplicationView.GetForCurrentView();
            if (view.IsFullScreenMode)
            {
                ViewModeService.Instance.DoExitFullscreen();
            }
            else
            {
                ViewModeService.Instance.DoEnterFullscreen();
            }
        }

        private void EnsureElements(NavigationView navview)
        {
            if (_navHeader == null)
            {
                _navHeader = VisualHelpers.GetVisualChildByName<FrameworkElement>(navview, "HeaderContent");
            }

            if (_menuButton == null)
            {
                _menuButton = VisualHelpers.GetVisualChildByName<FrameworkElement>(navview, "TogglePaneButton");
            }
        }
    }
}