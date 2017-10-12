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
using System.Threading.Tasks;
using BuildCast.Controls;
using BuildCast.DataModel;
using BuildCast.Helpers;
using BuildCast.Services;
using BuildCast.Services.Navigation;
using BuildCast.ViewModels;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace BuildCast.Views
{
    public sealed partial class Player : Page, IPageWithViewModel<PlayerViewModel>, IFullscreenPage
    {
        private Feed _currentFeed;
        private Episode _currentEpisode;

        private RelayCommand _addBookmarkCommand;
        private DispatcherTimer _timer;

        public static Player Instance { get; set; }

        public static InkNoteData InkNoteData { get; set; }

        public PlayerViewModel ViewModel { get; set; }

        public Player()
        {
            this.InitializeComponent();
            ConfigureAnimations();
            Instance = this;
        }

        public void ExitFullscreen()
        {
            Grid.SetRow(videoPlayer, 1);
            Grid.SetRowSpan(videoPlayer, 1);
            UnhookMouseMove();
        }

        public void EnterFullscreen()
        {
            Grid.SetRow(videoPlayer, 0);
            Grid.SetRowSpan(videoPlayer, 4);
            HookMouseMove();
        }

        private void Player_Loaded(object sender, RoutedEventArgs e)
        {
            // Put initial focus on mtc control
            mtc.Focus(FocusState.Programmatic);
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            HandleIncomingConnectedNavigation(e);

            await HandlleIncomingPlaybackRequests(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            PlayerService.Current.SetTimeControls(null);

            base.OnNavigatedFrom(e);
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            UnhookMouseMove();
        }

        private void UnhookMouseMove()
        {
            Window.Current.CoreWindow.PointerMoved -= CoreWindow_PointerMoved;
            Window.Current.CoreWindow.KeyDown -= CoreWindow_KeyDown;
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Tick -= Timer_Tick;
                _timer = null;
            }

            playbackcontrolsholder.Visibility = Visibility.Visible;
            header.Visibility = Visibility.Visible;
        }

        private void HookMouseMove()
        {
            Window.Current.CoreWindow.PointerMoved += CoreWindow_PointerMoved;
            Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;
            _timer = new Windows.UI.Xaml.DispatcherTimer();
            _timer.Tick += Timer_Tick;
            _timer.Interval = TimeSpan.FromSeconds(5);
            _timer.Start();
        }

        private void CoreWindow_KeyDown(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.KeyEventArgs args)
        {
            if (args.VirtualKey == Windows.System.VirtualKey.Escape)
            {
                var navRoot = ((App)Application.Current).GetNavigationRoot();

                navRoot.ExitFullScreen();
            }
        }

        private void Timer_Tick(object sender, object e)
        {
            _timer.Stop();
            playbackcontrolsholder.Visibility = Visibility.Collapsed;
            header.Visibility = Visibility.Collapsed;
        }

        private void CoreWindow_PointerMoved(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.PointerEventArgs args)
        {
            playbackcontrolsholder.Visibility = Visibility.Visible;
            header.Visibility = Visibility.Visible;
            _timer.Stop();
            _timer.Start();
        }

        private void HandleIncomingConnectedNavigation(NavigationEventArgs e)
        {
        }

        private void ConfigureAnimations()
        {
            // TODO: collapse all this into a single helper method
            ElementCompositionPreview.SetIsTranslationEnabled(header, true);
            ElementCompositionPreview.SetImplicitShowAnimation(header,
                VisualHelpers.CreateAnimationGroup(
                VisualHelpers.CreateVerticalOffsetAnimationFrom(0.45, -50f),
                VisualHelpers.CreateOpacityAnimation(0.5)
                ));
            ElementCompositionPreview.SetImplicitHideAnimation(header, VisualHelpers.CreateOpacityAnimation(0.8, 0));

            ElementCompositionPreview.SetIsTranslationEnabled(playbackcontrolsholder, true);
            ElementCompositionPreview.SetImplicitShowAnimation(
                playbackcontrolsholder,
                VisualHelpers.CreateAnimationGroup(VisualHelpers.CreateVerticalOffsetAnimation(0.55, 100, 0),
                                                   VisualHelpers.CreateOpacityAnimation(0.8)));

            ElementCompositionPreview.SetImplicitHideAnimation(playbackcontrolsholder, VisualHelpers.CreateOpacityAnimation(0.8, 0));

            Canvas.SetZIndex(this, 1);
            ElementCompositionPreview.SetImplicitHideAnimation(this, VisualHelpers.CreateOpacityAnimation(0.8, 0));
        }

        // Buttons
        #region Button Handlers
        private async void Share_click(object sender, RoutedEventArgs e)
        {
            var nowPlaying = PlayerService.Current.NowPlaying;

            if (nowPlaying.HasItem)
            {
                var imageBytes = await PlayerService.Current.GetBitmapForCurrentFrameFromLocalFile();

                if (imageBytes == null)
                {
                    imageBytes = await videoPlayer.GetBitmapFromRenderTarget();
                }

                StartInking(nowPlaying, imageBytes);
            }
        }

        private void StartInking(NowPlayingState nowPlaying, byte[] imageBytes)
        {
            if (nowPlaying.HasItem && imageBytes != null)
            {
                BuildCast.DataModel.InkNote meme = new BuildCast.DataModel.InkNote(nowPlaying.CurrentEpisode.Key, PlayerService.Current.CurrentTime);

                InkNoteData = new InkNoteData();
                InkNoteData.ImageBytes = imageBytes;

                ViewModel.GoToInkNote(meme);
            }
        }

        private async void Bookmark_click(object sender, RoutedEventArgs e)
        {
            var nowPlaying = PlayerService.Current.NowPlaying;
            if (nowPlaying.HasItem)
            {
                bookmarkContent.Text = string.Empty;

                await BookmarkDialog.ShowAsync();
            }
        }

        private RelayCommand AddBookmarkCommand
        {
            get
            {
                if (_addBookmarkCommand == null)
                {
                    _addBookmarkCommand = new RelayCommand(
                        async () =>
                        {
                            var nowPlaying = PlayerService.Current.NowPlaying;
                            BuildCast.DataModel.InkNote meme = new BuildCast.DataModel.InkNote(nowPlaying.CurrentEpisode.Key, PlayerService.Current.CurrentTime);
                            meme.NoteText = bookmarkContent.Text;
                            using (LocalStorageContext lsc = new LocalStorageContext())
                            {
                                lsc.Memes.Add(meme);
                                await lsc.SaveChangesAsync();
                            }
                        });
                }

                return _addBookmarkCommand;
            }
        }

        #endregion

        // ViewState management
        #region viewstate
        private async Task HandlleIncomingPlaybackRequests(NavigationEventArgs e)
        {
            if (e.Parameter is BuildCast.DataModel.InkNote)
            {
                var paramItem = e.Parameter as BuildCast.DataModel.InkNote;
                await PlayerService.Current.HandlePlayRequest(paramItem);
            }
            else if (!(e.Parameter is string))
            {
                // Rome requests have already handled playback request.
                var paramItem = e.Parameter as Episode;
                await PlayerService.Current.HandlePlayRequest(paramItem);
            }

            // Update the UI
            var playbackState = PlayerService.Current.NowPlaying;
            if (playbackState.CurrentEpisode != null)
            {
                SetNowPlaying(playbackState.CurrentFeed, playbackState.CurrentEpisode, playbackState.CurrentTime);
            }
        }

        internal void SetNowPlaying(Feed feed, Episode episode, TimeSpan currenttime)
        {
            this._currentFeed = feed;
            this._currentEpisode = episode;

            if (feed != null)
            {
                podimage.Source = new BitmapImage(feed?.ImageUri);
            }

            Bindings.Update();

            videoPlayer.LoadPoster(episode?.ItemThumbnail);
        }

        public void UpdateBindings()
        {
            Bindings.Update();
        }
        #endregion

        private async void Mtc_RemoteSystemSelected(object sender, IRemoteSystemDescription eventArgs)
        {
            RemoteControl remote = new RemoteControl();
            IRemoteConnection remoteConnection = await ViewModel.CreateRemoteControl(eventArgs);
            if (remoteConnection != null)
            {
                remote.RemoteConnection = remoteConnection;
                RemoteControlPopup.Content = remote;

                remote.CloseClicked += (s, ev) => RemoteControlPopup.Hide();
                var result = await RemoteControlPopup.ShowAsync();
            }
            else
            {
                await UIHelpers.ShowContentAsync("Connection failed.");
            }
        }
    }
}
