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

namespace BuildCast.Controls
{
    using System;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using BuildCast.Helpers;
    using BuildCast.Services;
    using BuildCast.ViewModels;
    using BuildCast.Views;
    using Windows.UI.Core;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;

    public sealed partial class CustomMTC : UserControl
    {
        public static readonly DependencyProperty RemoteSystemsProperty =
            DependencyProperty.Register(nameof(RemoteSystems), typeof(ObservableCollection<IRemoteSystemDescription>), typeof(CustomMTC), new PropertyMetadata(null));

        private static TimeSpan _currentTime;

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomMTC"/> class.
        /// </summary>
        public CustomMTC()
        {
            this.InitializeComponent();
        }

        public event EventHandler<IRemoteSystemDescription> RemoteSystemSelected;

        public event EventHandler RefreshRequested;

        public ObservableCollection<IRemoteSystemDescription> RemoteSystems
        {
            get { return (ObservableCollection<IRemoteSystemDescription>)GetValue(RemoteSystemsProperty); }
            set { SetValue(RemoteSystemsProperty, value); }
        }

        #region View Modes
        public static async Task LeaveCompactOverlayMode()
        {
            await ViewModeService.Instance.SwitchToNormalMode();
        }

        private void Fs_click(object sender, RoutedEventArgs e)
        {
            var navRoot = ((App)Application.Current).GetNavigationRoot();

            navRoot.ToggleFullscreen();
        }

        private async void Pip_click(object sender, RoutedEventArgs e)
        {
            var navRoot = ((App)Application.Current).GetNavigationRoot();

            navRoot.ExitFullScreen();

            var pipModes = await SettingsViewModel.GetCurrentMode();

            if (pipModes == PipModes.SingleView)
            {
                await ViewModeService.Instance.SwitchToCompactOverlay<PopupPlayer>(new PopupPlayer(pipModes), player => player.StartVideo());
            }
            else
            {
                await ViewModeService.Instance.CreateNewView<PopupPlayer>(() => { return new PopupPlayer(pipModes); }, player => player.StartVideo());
            }
        }
        #endregion

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            timelinectrl.SetElapsedTimeControl(elapsed);
            Window.Current.CoreWindow.PointerPressed += CoreWindow_PointerPressed;
            PlayerService.Current.PlayPauseChanged += Current_PlayPauseChanged;
            PlayerService.Current.SetTimeControls(timelinectrl);
            Current_PlayPauseChanged(this, PlayerService.Current.State);
        }

        // Playback
        #region Playback Logic

        private void Current_PlayPauseChanged(object sender, Windows.Media.Playback.MediaPlaybackState e)
        {
            if (e == Windows.Media.Playback.MediaPlaybackState.Playing)
            {
                // GlyphString = @"";
                playpause.Content = new FontIcon() { Glyph = @"" };
            }
            else
            {
                // GlyphString = @"";
                playpause.Content = new FontIcon() { Glyph = @"" };
            }
        }

        private void DoneSrubbing(object sender, EventArgs e)
        {
            Debug.WriteLine(timelinectrl.CurrentPercentage);
            _currentTime = timelinectrl.CurrentTime;
            PlayerService.Current.SetTime(timelinectrl.CurrentTime);
        }

        private void CoreWindow_PointerPressed(CoreWindow sender, PointerEventArgs args)
        {
            timelinectrl.NotifyPointerPressed(args);
        }

        private async void Playpause_click(object sender, RoutedEventArgs e)
        {
            await PlayerService.Current.TogglePlayPaused();
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            Window.Current.CoreWindow.PointerPressed -= CoreWindow_PointerPressed;
            PlayerService.Current.PlayPauseChanged -= Current_PlayPauseChanged;
        }

        private void Rwnd_click(object sender, RoutedEventArgs e)
        {
            PlayerService.Current.Rewind();
        }

        private void Ffwd_click(object sender, RoutedEventArgs e)
        {
            PlayerService.Current.FastForward();
        }

        #endregion

        #region Rome logic
        private void RomeItemClick(object sender, ItemClickEventArgs e)
        {
            romeProgress.IsIndeterminate = true;
            romeProgress.IsEnabled = true;
            romeProgress.Visibility = Visibility.Visible;
            this.sendtodevice.Flyout?.Hide();
            this.sendtodevice.IsEnabled = false;

            RemoteSystemSelected?.Invoke(this, e.ClickedItem as IRemoteSystemDescription);

            romeProgress.IsEnabled = false;
            romeProgress.IsIndeterminate = false;
            romeProgress.Visibility = Visibility.Collapsed;

            this.sendtodevice.IsEnabled = true;
        }

        private void Flyout_Opened(object sender, object e)
        {
            RefreshRequested?.Invoke(this, EventArgs.Empty);
        }
        #endregion
    }
}
