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

using BuildCast.Controls;
using BuildCast.Helpers;
using BuildCast.Services;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;

namespace BuildCast.Views
{
    public sealed partial class PopupPlayer : Page
    {
        private SpriteVisual _playerVisual;
        private PipModes _mode;

        public PopupPlayer(PipModes mode)
        {
            this.InitializeComponent();
            _playerVisual = Window.Current.Compositor.CreateSpriteVisual();
            _mode = mode;
            if (_mode == PipModes.MultiView)
            {
                btnexit.Visibility = Visibility.Collapsed;
            }

            ElementCompositionPreview.SetElementChildVisual(playerHolder, _playerVisual);
        }

        public void StartVideo()
        {
            _playerVisual.Brush = PlayerService.Current.GetBrush(Window.Current.Compositor);
        }

        private void PlayerHolder_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _playerVisual.SetSize(playerHolder);
        }

        private async void Btnexit_LeaveCompactMode(object sender, RoutedEventArgs e)
        {
            await CustomMTC.LeaveCompactOverlayMode();
        }
    }
}
