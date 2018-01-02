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
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using AudioVisualizer;
using BuildCast.Controls;
using BuildCast.DataModel;
using BuildCast.Helpers;
using BuildCast.Services;
using BuildCast.Services.Navigation;
using BuildCast.ViewModels;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.Text;
using Windows.UI;
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
        private SpectrumData _emptySpectrum = SpectrumData.CreateEmpty(2, 20, ScaleType.Linear, ScaleType.Linear, 27, 5500);
        private SpectrumData _previousSpectrum;
        private SpectrumData _previousPeakSpectrum;

        private TimeSpan _rmsRiseTime = TimeSpan.FromMilliseconds(50);
        private TimeSpan _rmsFallTime = TimeSpan.FromMilliseconds(50);
        private TimeSpan _peakRiseTime = TimeSpan.FromMilliseconds(100);
        private TimeSpan _peakFallTime = TimeSpan.FromMilliseconds(1000);
        private TimeSpan _frameDuration = TimeSpan.FromMilliseconds(16.7);

        private object _sizeLock = new object();
        //private float _visualizerWidth = 0.0f;
        //private float _visualizerHeight = 0.0f;

        private CanvasTextFormat _textFormat = new CanvasTextFormat();

        private Feed _currentFeed;
        private Episode _currentEpisode;

        private RelayCommand _addBookmarkCommand;
        private DispatcherTimer _timer;

        private IVisualizationSource _visualizationSource;

        public Player()
        {
            this.InitializeComponent();
            Instance = this;
        }

        public static Player Instance { get; set; }

        public static InkNoteData InkNoteData { get; set; }

        public PlayerViewModel ViewModel { get; set; }

        public void ExitFullscreen()
        {
            Grid.SetRow(videoPlayer, 1);
            Grid.SetRowSpan(videoPlayer, 1);
            UnhookMouseMove();
        }

        public void EnterFullscreen()
        {
            Grid.SetRow(videoPlayer, 0);
            Grid.SetRowSpan(videoPlayer, 3);
            HookMouseMove();
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            HandleIncomingConnectedNavigation(e);

            Canvas.SetZIndex(this, 0);
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

            var navRoot = ((App)Application.Current).GetNavigationRoot();

            navRoot.ExitFullScreen();
            Canvas.SetZIndex(this, 1);
        }

        private void Player_Loaded(object sender, RoutedEventArgs e)
        {
            // Put initial focus on mtc control
            mtc.Focus(FocusState.Programmatic);
            CreateVisualizer();
        }

        private void Player_Unloaded(object sender, RoutedEventArgs e)
        {
            PlayerService.Current.VisualizationSourceChanged -= Current_VisualizationSourceChanged;
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
            _timer.Interval = TimeSpan.FromSeconds(3);
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
            visualizer.Visibility = Visibility.Collapsed;
        }

        private void CoreWindow_PointerMoved(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.PointerEventArgs args)
        {
            playbackcontrolsholder.Visibility = Visibility.Visible;
            header.Visibility = Visibility.Visible;
            visualizer.Visibility = Visibility.Visible;
            _timer.Stop();
            _timer.Start();
        }

        private void HandleIncomingConnectedNavigation(NavigationEventArgs e)
        {
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

        private async void CreateVisualizer()
        {
            _textFormat.VerticalAlignment = CanvasVerticalAlignment.Center;
            _textFormat.HorizontalAlignment = CanvasHorizontalAlignment.Center;
            _textFormat.FontSize = 9;

            if (PlayerService.Current.VisualizationSource != null)
            {
                PlayerService.Current.VisualizationSourceChanged += Current_VisualizationSourceChanged;

                if (PlayerService.Current.VisualizationSource.Source != null)
                {
                    SetSource();
                }
            }
        }

        private void Current_VisualizationSourceChanged(object sender, IVisualizationSource e)
        {
            SetSource();
        }

        private void SetSource()
        {
            visualizer.Source = PlayerService.Current.VisualizationSource.Source;
            PlayerService.Current.VisualizationSource.Source.IsSuspended = false;
        }

        private void Visualizer_Draw(AudioVisualizer.IVisualizer sender, AudioVisualizer.VisualizerDrawEventArgs args)
        {
            var drawingSession = (CanvasDrawingSession)args.DrawingSession;

            var spectrum = args.Data != null ? args.Data.Spectrum.LogarithmicTransform(20, 27, 5500) : _emptySpectrum;

            _previousSpectrum = spectrum.ApplyRiseAndFall(_previousSpectrum, _rmsRiseTime, _rmsFallTime, _frameDuration);
            _previousPeakSpectrum = spectrum.ApplyRiseAndFall(_previousPeakSpectrum, _peakRiseTime, _peakFallTime, _frameDuration);

            float w = (float)args.ViewExtent.Width;
            float h = (float)args.ViewExtent.Height;

            // There are bugs in ConverToLogAmplitude. It is returning 0 if max is not 0 and min negative.
            // The heightScale is a workaround for this
            var s = _previousSpectrum.ConvertToDecibels(-50, 0);
            var p = _previousPeakSpectrum.ConvertToDecibels(-50, 0);
            DrawSpectrumSpline(p[0], drawingSession, Vector2.Zero, w, h, -0.02f, Color.FromArgb(0xff, 0x38, 0x38, 0x38));
            DrawSpectrumSpline(p[1], drawingSession, Vector2.Zero, w, h, -0.02f, Color.FromArgb(0xff, 0x38, 0x38, 0x38), true);
            DrawSpectrumSpline(s[0], drawingSession, Vector2.Zero, w, h, -0.02f, Color.FromArgb(0xff, 0x30, 0x30, 0x30));
            DrawSpectrumSpline(s[1], drawingSession, Vector2.Zero, w, h, -0.02f, Color.FromArgb(0xff, 0x30, 0x30, 0x30), true);
        }

        private void DrawSpectrumSpline(IReadOnlyList<float> data, CanvasDrawingSession session, Vector2 offset, float width, float height, float heightScale, Color color, bool rightToLeft = false)
        {
            int segmentCount = data.Count - 1;
            if (segmentCount <= 1 || width <= 0f)
            {
                return;
            }

            CanvasPathBuilder path = new CanvasPathBuilder(session);

            float segmentWidth = width / (float)segmentCount;

            Vector2 prevPosition = rightToLeft ? new Vector2(width + offset.X, data[0] * heightScale * height + offset.Y)
                                               : new Vector2(offset.X, data[0] * heightScale * height + offset.Y);

            if (rightToLeft)
            {
                path.BeginFigure(width + offset.X, height + offset.Y);
            }
            else
            {
                path.BeginFigure(offset.X, height + offset.Y);
            }

            path.AddLine(prevPosition);

            for (int i = 1; i < data.Count; i++)
            {
                Vector2 position = rightToLeft ? new Vector2(width - (float)i * segmentWidth + offset.X, data[i] * heightScale * height + offset.Y)
                                               : new Vector2((float)i * segmentWidth + offset.X, data[i] * heightScale * height + offset.Y);

                if (rightToLeft)
                {
                    Vector2 c1 = new Vector2(position.X + segmentWidth / 2.0f, prevPosition.Y);
                    Vector2 c2 = new Vector2(prevPosition.X - segmentWidth / 2.0f, position.Y);
                    path.AddCubicBezier(c1, c2, position);
                }
                else
                {
                    Vector2 c1 = new Vector2(position.X - segmentWidth / 2.0f, prevPosition.Y);
                    Vector2 c2 = new Vector2(prevPosition.X + segmentWidth / 2.0f, position.Y);
                    path.AddCubicBezier(c1, c2, position);
                }

                prevPosition = position;
            }

            if (rightToLeft)
            {
                path.AddLine(offset.X, height + offset.Y);
            }
            else
            {
                path.AddLine(width + offset.X, height + offset.Y);
            }

            path.EndFigure(CanvasFigureLoop.Closed);

            CanvasGeometry geometry = CanvasGeometry.CreatePath(path);
            session.FillGeometry(geometry, color);
        }
    }
}
