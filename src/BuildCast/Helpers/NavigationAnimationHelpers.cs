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
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Media.Animation;

namespace BuildCast.Helpers
{
    public static class NavigationAnimationHelpers
    {
        public static bool NavigateWithFadeOutgoing(this Frame frame, object parameter, Type destination)
        {
            ImplicitHideFrameContent(frame);

            return frame.Navigate(destination, parameter);
        }

        private static void ImplicitHideFrameContent(Frame frame)
        {
            if (frame.Content != null)
            {
                SetImplicitHide(frame.Content as UIElement);
            }
        }

        private static void SetImplicitHide(UIElement thisPtr)
        {
            ElementCompositionPreview.SetImplicitHideAnimation(thisPtr, VisualHelpers.CreateOpacityAnimation(0.4, 0));
            Canvas.SetTop(thisPtr, 1);
        }
    }
}
