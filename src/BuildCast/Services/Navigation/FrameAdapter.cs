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
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace BuildCast.Services.Navigation
{
    public class FrameAdapter : IFrameAdapter
    {
        private Frame _internalFrame;

        public event NavigatedEventHandler Navigated { add => _internalFrame.Navigated += value; remove => _internalFrame.Navigated -= value; }

        public event NavigatingCancelEventHandler Navigating { add => _internalFrame.Navigating += value; remove => _internalFrame.Navigating -= value; }

        public event NavigationFailedEventHandler NavigationFailed { add => _internalFrame.NavigationFailed += value; remove => _internalFrame.NavigationFailed -= value; }

        public event NavigationStoppedEventHandler NavigationStopped { add => _internalFrame.NavigationStopped += value; remove => _internalFrame.NavigationStopped -= value; }

        public FrameAdapter(Frame internalFrame)
        {
            _internalFrame = internalFrame;
        }

        public bool IsNavigating { get; private set; }

        public bool CanGoBack => _internalFrame.CanGoBack;

        public bool CanGoForward => _internalFrame.CanGoForward;

        public object Content => _internalFrame.Content;

        public void GoForward()
        {
            _internalFrame.GoForward();
        }

        public void GoBack() => _internalFrame.GoBack();

        public string GetNavigationState() => _internalFrame.GetNavigationState();

        public void SetNavigationState(string navigationState) => _internalFrame.SetNavigationState(navigationState);

        public bool Navigate(Type sourcePageType, object parameter)
        {
            return _internalFrame.NavigateWithFadeOutgoing(parameter, sourcePageType);
        }
    }
}
