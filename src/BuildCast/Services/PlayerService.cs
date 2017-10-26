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
using System.Diagnostics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using AudioVisualizer;
using BuildCast.Controls;
using BuildCast.DataModel;
using BuildCast.Helpers;
using Microsoft.Toolkit.Uwp.Helpers;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Core;
using Windows.Media.Editing;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System.Display;
using Windows.UI.Composition;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace BuildCast.Services
{
    public class PlayerService : IPlayerService
    {
        private static PlayerService _current;

        private NowPlayingState _nowPlayingState;

        private MediaPlayer _mediaPlayer;
        private AudioVisualizer.PlaybackSource _source;

        private string _localFilename;
        private Uri _sourceUri;
        private TimeSpan _startTime;

        private DispatcherTimer _playerEventDebounceTimer;
        private CoreDispatcher _dispatcher;
        private object _syncLock = new object();

        private Timeline _timelinectrl;
        private MediaPlaybackState _nextEventtoFire;
        private MediaPlaybackState _lastEventFired;
        private StorageFile _currentFile;

        public event EventHandler<MediaPlaybackState> PlayPauseChanged;

        public event EventHandler MediaLoaded;

        public event EventHandler MediaLoading;

        public event EventHandler<IVisualizationSource> VisualizationSourceChanged;

        public static PlayerService Current
        {
            get
            {
                if (_current == null)
                {
                    _current = new PlayerService();
                }

                return _current;
            }
        }

        public CompositionBrush GetBrush(Compositor compositor)
        {
            var surface = _mediaPlayer.GetSurface(compositor);
            var surfaceBrush = compositor.CreateSurfaceBrush(surface.CompositionSurface);
            surfaceBrush.Stretch = CompositionStretch.Uniform;
            return surfaceBrush;
        }

        public async Task HandlePlayRequest(BuildCast.DataModel.InkNote i)
        {
            var shouldPlay = _nowPlayingState.HandlePlayRequest(i);
            await SetNewItem(new Uri(_nowPlayingState.CurrentEpisode.Key), _nowPlayingState.CurrentTime, shouldPlay, _nowPlayingState.CurrentEpisode.Id.ToString());
        }

        public async Task HandlePlayRequest(Episode e)
        {
            var shouldPlay = await _nowPlayingState.HandlePlayRequest(e);

            if (_nowPlayingState.CurrentEpisode != null && shouldPlay.Item1)
            {
                // if we need to switch to a new item, set new item and open it for playback
                await SetNewItem(new Uri(_nowPlayingState.CurrentEpisode.Key), _nowPlayingState.CurrentTime, shouldPlay.Item2, _nowPlayingState.CurrentEpisode.Id.ToString());
            }
            else if (_nowPlayingState.CurrentEpisode != null && !shouldPlay.Item1)
            {
                // if we don't need to switch (being asked to play what is already playing), just set the time - unless the time is already set.
                if (_startTime <= TimeSpan.Zero)
                {
                    SetTime(_nowPlayingState.CurrentTime);
                }

                if (shouldPlay.Item2)
                {
                    _mediaPlayer.Play();
                }
            }
        }

        #region Playback Commands
        public async Task Play(Episode ep, TimeSpan playTime)
        {
            await Microsoft.Toolkit.Uwp.Helpers.DispatcherHelper.AwaitableRunAsync(_dispatcher, () =>
            {
                if (_mediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.None)
                {
                    return SetNewItem(new Uri(ep.Key), playTime, true, string.Empty);
                }
                else
                {
                    SetTime(playTime);
                    return HandlePlayRequest(ep);
                }
            });
        }

        public void Pause()
        {
            var ignored = DispatcherHelper.ExecuteOnUIThreadAsync(() =>
            {
                _mediaPlayer.Pause();
            });
        }

        public async Task TogglePlayPaused()
        {
            if (NowPlaying.HasItem)
            {
                switch (_mediaPlayer.PlaybackSession.PlaybackState)
                {
                    case MediaPlaybackState.None:
                        await OpenAndPlayNewItem();
                        break;
                    case MediaPlaybackState.Playing:
                        _mediaPlayer.Pause();
                        break;
                    case MediaPlaybackState.Paused:
                        _mediaPlayer.Play();
                        break;
                }
            }
        }

        public void Rewind()
        {
            var ignored = DispatcherHelper.ExecuteOnUIThreadAsync(() => _mediaPlayer.PlaybackSession.Position = _mediaPlayer.PlaybackSession.Position - TimeSpan.FromSeconds(10));
        }

        public void FastForward()
        {
            var ignored = DispatcherHelper.ExecuteOnUIThreadAsync(() => _mediaPlayer.PlaybackSession.Position = _mediaPlayer.PlaybackSession.Position + TimeSpan.FromSeconds(30));
        }
        #endregion

        public async Task SetNewItem(Uri currentItem, TimeSpan currentTime, bool startPlayback, string localFilename)
        {
            var wasPlaying = _mediaPlayer?.PlaybackSession.PlaybackState == MediaPlaybackState.Playing;

            ClearPlayback();

            _startTime = currentTime;
            _sourceUri = currentItem;
            _localFilename = localFilename;

            if (startPlayback)
            {
                await OpenAndPlayNewItem();
            }
        }

        public void SetTime(TimeSpan currentTime)
        {
            if (_mediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing || _mediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Paused)
            {
                _mediaPlayer.PlaybackSession.Position = currentTime;
                _timelinectrl?.SetTime(currentTime, _mediaPlayer.PlaybackSession.NaturalDuration);
            }
            else
            {
                _startTime = currentTime;
            }
        }

        public void SetTimeControls(Timeline timeCtrl)
        {
            this._timelinectrl = timeCtrl;
        }

        public async Task<byte[]> GetBitmapForCurrentFrameFromLocalFile()
        {
            if (_currentFile != null)
            {
                return await GetBitmapBytesFromLocalFile(_mediaPlayer.PlaybackSession.Position);
            }

            return null;
        }

        #region Properties
        public bool IsPaused
        {
            get
            {
                return _mediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Paused;
            }
        }

        public bool IsPlaying
        {
            get
            {
                return this.IsPlayingInternal(_mediaPlayer.PlaybackSession.PlaybackState);
            }
        }

        public MediaPlaybackState State
        {
            get
            {
                return _mediaPlayer.PlaybackSession.PlaybackState;
            }
        }

        public bool IsOpening
        {
            get
            {
                return this.IsOpeningInternal(_mediaPlayer.PlaybackSession.PlaybackState);
            }
        }

        public bool IsClosed
        {
            get
            {
                return _mediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.None;
            }
        }

        public double CurrentTime
        {
            get
            {
                return _mediaPlayer.PlaybackSession.Position.TotalMilliseconds;
            }
        }

        public NowPlayingState NowPlaying { get => _nowPlayingState; }

        public PlaybackSource VisualizationSource { get => _source; set => _source = value; }
        #endregion

        private PlayerService()
        {
            _dispatcher = Window.Current.Dispatcher;
            InitializeMediaPlayer();

            _playerEventDebounceTimer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromMilliseconds(250)
            };
            _playerEventDebounceTimer.Tick += PlayerEventDebounceTimer_Tick;

            _nowPlayingState = new NowPlayingState();

            _nowPlayingState.LoadStateAsync().ContinueWith((a) =>
            {
                // TODO: handle error
                if (_nowPlayingState.CurrentEpisode != null)
                {
                    var setItemTask = SetNewItem(new Uri(_nowPlayingState.CurrentEpisode.Key), _nowPlayingState.CurrentTime, false, _nowPlayingState.CurrentEpisode.Id.ToString());
                }
            });
        }

        private void InitializeMediaPlayer()
        {
            _mediaPlayer = new MediaPlayer();
            VisualizationSource = new PlaybackSource(_mediaPlayer);
            VisualizationSource.SourceChanged += _source_SourceChanged;
            _mediaPlayer.PlaybackSession.PositionChanged += PlaybackSession_PositionChanged;

            _mediaPlayer.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;

            _mediaPlayer.MediaFailed += MediaPlayer_MediaFailed;

            _mediaPlayer.PlaybackSession.NaturalVideoSizeChanged += (sender, args) =>
            {
                sender.MediaPlayer.SetSurfaceSize(new Size(sender.NaturalVideoWidth, sender.NaturalVideoHeight));
            };
        }

        private void _source_SourceChanged(object sender, AudioVisualizer.IVisualizationSource args)
        {
            VisualizationSourceChanged?.Invoke(sender, args);
        }

        private async Task OpenAndPlayNewItem()
        {
            TypedEventHandler<MediaPlayer, object> openedHandler = null;

            OnMediaLoading();

            openedHandler = (s, e) =>
            {
                OnMediaLoaded();
                _mediaPlayer.MediaOpened -= openedHandler;
                if (_startTime > TimeSpan.Zero)
                {
                    _mediaPlayer.PlaybackSession.Position = _startTime;
                }
            };

            _mediaPlayer.MediaOpened += openedHandler;

            var result = await GetSourceForUri();

            _mediaPlayer.Source = result.source;
            _currentFile = result.storageFile;

            _mediaPlayer.Play();

            var disp = new DisplayRequest();
            disp.RequestActive();
        }

        private async Task<(MediaSource source, StorageFile storageFile)> GetSourceForUri()
        {
            MediaSource source = null;
            var currentFile = await BackgroundDownloadHelper.CheckLocalFileExistsFromUriHash(_sourceUri);

            if (currentFile != null)
            {
                source = MediaSource.CreateFromStorageFile(currentFile);
            }
            else
            {
                source = MediaSource.CreateFromUri(_sourceUri);
            }

            return (source, currentFile);
        }

        private async Task<byte[]> GetBitmapBytesFromLocalFile(TimeSpan span)
        {
            var thumbnail = await GetThumbnailAsync(_currentFile, span);
            InMemoryRandomAccessStream randomAccessStream = new InMemoryRandomAccessStream();
            await RandomAccessStream.CopyAsync(thumbnail, randomAccessStream);
            randomAccessStream.Seek(0);

            var bytes = new byte[randomAccessStream.Size];
            await randomAccessStream.ReadAsync(bytes.AsBuffer(), (uint)randomAccessStream.Size, InputStreamOptions.None);
            return bytes;
        }

        private async Task<IInputStream> GetThumbnailAsync(StorageFile file, TimeSpan span)
        {
            var mediaClip = await MediaClip.CreateFromFileAsync(file);
            var mediaComposition = new MediaComposition();
            mediaComposition.Clips.Add(mediaClip);
            return await mediaComposition.GetThumbnailAsync(
                span, 0, 0, VideoFramePrecision.NearestFrame);
        }

        private void ClearPlayback()
        {
            if (_mediaPlayer.PlaybackSession.PlaybackState != MediaPlaybackState.None)
            {
                _mediaPlayer.Source = null;
            }
        }

        private void PlaybackSession_PositionChanged(MediaPlaybackSession sender, object args)
        {
            _timelinectrl?.SetTime(_mediaPlayer.PlaybackSession.Position, _mediaPlayer.PlaybackSession.NaturalDuration);
            _nowPlayingState.CurrentTime = _mediaPlayer.PlaybackSession.Position;
        }

        private async void PlaybackSession_PlaybackStateChanged(MediaPlaybackSession sender, object args)
        {
            await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                lock (_syncLock)
                {
                    _nextEventtoFire = sender.PlaybackState;
                    if (_playerEventDebounceTimer.IsEnabled)
                    {
                        Debug.WriteLine($"Debounce");
                        _playerEventDebounceTimer.Stop();
                        _playerEventDebounceTimer.Start();
                    }
                    _playerEventDebounceTimer.Start();
                }
            });
        }

        private void MediaPlayer_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
        {
            Debug.WriteLine($"failed:{args.ErrorMessage}");
        }

        private async Task HandlePlayPauseChanged(MediaPlaybackState playOrPause)
        {
            if (playOrPause == MediaPlaybackState.Paused)
            {
                await _nowPlayingState.PeristEpisodePlaybackState();
            }
        }

        private void PlayerEventDebounceTimer_Tick(object sender, object e)
        {
            lock (_syncLock)
            {
                _playerEventDebounceTimer.Stop();
                CheckFireStateChanged();
            }
        }

        private void CheckFireStateChanged()
        {
            // Only fire the event if state has actually changed based on what was last fired
            var wasPlaying = IsPlayingInternal(_lastEventFired);

            var isPlaying = IsPlayingInternal(_nextEventtoFire);

            if (wasPlaying != isPlaying)
            {
                if (PlayPauseChanged != null)
                {
                    // Don't fire the event while the lock is held
                    var dispatcherTask = _dispatcher.RunIdleAsync(async (s) =>
                     {
                         var playOrPause = IsPlayingInternal(_nextEventtoFire) ? MediaPlaybackState.Playing : MediaPlaybackState.Paused;

                         Debug.WriteLine($"Fire either play or pause : {playOrPause}");
                         await OnPlayPauseChanged(playOrPause);
                     });
                }
            }

            _lastEventFired = _nextEventtoFire;
        }

        private async Task OnPlayPauseChanged(MediaPlaybackState playOrPause)
        {
            await HandlePlayPauseChanged(playOrPause);
            PlayPauseChanged(this, playOrPause);
        }

        /// <summary>
        /// Use by callers who care about playing vs paused like play pause button
        /// </summary>
        /// <param name="eventToTest"></param>
        /// <returns></returns>
        private bool IsPlayingInternal(MediaPlaybackState eventToTest)
        {
            return eventToTest == MediaPlaybackState.Buffering
             || eventToTest == MediaPlaybackState.Opening
             || eventToTest == MediaPlaybackState.Playing;
        }

        /// <summary>
        /// Used by callers who are showing loading UX
        /// </summary>
        /// <param name="eventToTest"></param>
        /// <returns></returns>
        private bool IsOpeningInternal(MediaPlaybackState eventToTest)
        {
            return eventToTest == MediaPlaybackState.Buffering
             || eventToTest == MediaPlaybackState.Opening;
        }

        private void OnMediaLoaded()
        {
            this.MediaLoaded?.Invoke(this, EventArgs.Empty);
        }

        private void OnMediaLoading()
        {
            this.MediaLoading?.Invoke(this, EventArgs.Empty);
        }
    }
}
