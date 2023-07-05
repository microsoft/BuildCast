﻿// ******************************************************************
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
using System.ComponentModel;
using Windows.ApplicationModel.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace BuildCast.Helpers
{
    public class TitleBarHelper : INotifyPropertyChanged
    {
        private static TitleBarHelper _instance = new TitleBarHelper();
        private static CoreApplicationViewTitleBar _coreTitleBar;
        private Thickness _titlePosition;
        private Visibility _titleVisibility;
        private int _extraPadding;

        /// <summary>
        /// Initializes a new instance of the <see cref="TitleBarHelper"/> class.
        /// </summary>
        public TitleBarHelper()
        {
            _coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            _coreTitleBar.LayoutMetricsChanged += CoreTitleBar_LayoutMetricsChanged;
            _titlePosition = CalculateTilebarOffset(_coreTitleBar.SystemOverlayLeftInset, _coreTitleBar.Height);
            _titleVisibility = Visibility.Visible;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public static TitleBarHelper Instance
        {
            get
            {
                return _instance;
            }
        }

        public CoreApplicationViewTitleBar TitleBar
        {
            get
            {
                return _coreTitleBar;
            }
        }

        public Thickness TitlePosition
        {
            get
            {
                return _titlePosition;
            }

            set
            {
                if (value.Left != _titlePosition.Left || value.Top != _titlePosition.Top)
                {
                    _titlePosition = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TitlePosition)));
                }
            }
        }

        public Visibility TitleVisibility
        {
            get
            {
                return _titleVisibility;
            }

            set
            {
                _titleVisibility = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TitleVisibility)));
            }
        }

        public void ExitFullscreen()
        {
            TitleVisibility = Visibility.Visible;
        }

        public void GoFullscreen()
        {
            TitleVisibility = Visibility.Collapsed;
        }

        private void CoreTitleBar_LayoutMetricsChanged(CoreApplicationViewTitleBar sender, object args)
        {
            TitlePosition = CalculateTilebarOffset(_coreTitleBar.SystemOverlayLeftInset, _coreTitleBar.Height);
        }

        private Thickness CalculateTilebarOffset(double leftPosition, double height)
        {
            // top position should be 6 pixels for a 32 pixel high titlebar hence scale by actual height
            var correctHeight = height / 32 * 6;

            return new Thickness(leftPosition + 12 + _extraPadding, correctHeight, 0, 0);
        }

        internal void NavDisplayModeChanged(NavigationView sender, NavigationViewDisplayModeChangedEventArgs args)
        {
            switch (args.DisplayMode)
            {
                case NavigationViewDisplayMode.Compact:
                    _extraPadding = 50;
                    break;
                default:
                    _extraPadding = 0;
                    break;
            }

            TitlePosition = CalculateTilebarOffset(_coreTitleBar.SystemOverlayLeftInset, _coreTitleBar.Height);
        }
    }
}
