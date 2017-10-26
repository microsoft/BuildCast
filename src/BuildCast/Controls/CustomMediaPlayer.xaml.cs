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
    using System.Runtime.InteropServices.WindowsRuntime;
    using System.Threading.Tasks;
    using BuildCast.Helpers;
    using BuildCast.Services;
    using Windows.Graphics.Imaging;
    using Windows.Storage.Streams;
    using Windows.UI;
    using Windows.UI.Composition;
    using Windows.UI.Core;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Hosting;
    using Windows.UI.Xaml.Media;
    using Windows.UI.Xaml.Media.Imaging;

    public sealed partial class CustomMediaPlayer : UserControl
    {
        private Compositor _compositor;
        private ContainerVisual _container;
        private SpriteVisual _tintVisual;
        private SpriteVisual _posterVisual;
        private SpriteVisual _videoVisual;
        private LoadedImageSurface _surface;
        private Uri _currentPosterUri;
        private CoreDispatcher _dispatcher;
        private BufferingVisualManager _bvm;

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomMediaPlayer"/> class.
        /// </summary>
        public CustomMediaPlayer()
        {
            this.InitializeComponent();
            InitializeComposition();
            ConfigureAnimations();

            // Capture dispatcher from current window on the UI thread
            _dispatcher = Window.Current.Dispatcher;

            _bvm = new BufferingVisualManager();
            _bvm.SetCompositor(Window.Current.Compositor);
        }

        /// <summary>
        /// Causes placdeholder image to be loaded and displayed
        /// </summary>
        /// <param name="uri">Uri for placeholder image</param>
        public void LoadPoster(string uri)
        {
            if (!string.IsNullOrEmpty(uri))
            {
                LoadPlaceholderImage(new System.Uri(uri));
            }
        }

        /// <summary>
        /// Get bitmap from current frame of video using RenderTarget method.
        /// Only used if video is not already downloaded.
        /// </summary>
        /// <returns>Byte array representing bitmap</returns>
        public async Task<byte[]> GetBitmapFromRenderTarget()
        {
            var tweet = new RenderTargetBitmap();
            await tweet.RenderAsync(HostElement);

            var pixels = await tweet.GetPixelsAsync();

            InMemoryRandomAccessStream randomAccessStream = new InMemoryRandomAccessStream();

            var be = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, randomAccessStream);

            be.SetPixelData(
                BitmapPixelFormat.Bgra8,
                BitmapAlphaMode.Ignore,
                (uint)tweet.PixelWidth,
                (uint)tweet.PixelHeight,
                92.0,
                92.0,
                pixels.ToArray());

            await be.FlushAsync();

            var bytes = new byte[randomAccessStream.Size];
            await randomAccessStream.ReadAsync(bytes.AsBuffer(), (uint)randomAccessStream.Size, InputStreamOptions.None);
            return bytes;
        }

        private void HostElement_SizeChanged(object sender, Windows.UI.Xaml.SizeChangedEventArgs e)
        {
            _container.SetSize(HostElement);
        }

        private void InitializeComposition()
        {
            _compositor = Window.Current.Compositor;
            _container = Window.Current.Compositor.CreateContainerVisual();
            ElementCompositionPreview.SetElementChildVisual(HostElement, _container);

            _tintVisual = _compositor.CreateSpriteVisual();
            _tintVisual.Brush = _compositor.CreateColorBrush(Colors.Black);
            _tintVisual.Opacity = 0.6f;
            _tintVisual.RelativeSizeAdjustment = new System.Numerics.Vector2(1.0f);
            _container.Children.InsertAtBottom(_tintVisual);

            _posterVisual = _compositor.CreateSpriteVisual();
            _posterVisual.Brush = _compositor.CreateColorBrush(Colors.Black);
            _posterVisual.RelativeSizeAdjustment = new System.Numerics.Vector2(1.0f);
            _posterVisual.Opacity = 0;
            _posterVisual.FadeVisual(2);

            _videoVisual = _compositor.CreateSpriteVisual();
            _videoVisual.RelativeSizeAdjustment = new System.Numerics.Vector2(1.0f);
            if (!PlayerService.Current.IsPlaying && !PlayerService.Current.IsPaused)
            {
                _videoVisual.Opacity = 0;
            }

            _videoVisual.FadeVisual(2);
            _videoVisual.Brush = PlayerService.Current.GetBrush(Window.Current.Compositor);
            _container.Children.InsertAtTop(_videoVisual);
            _container.Children.InsertAtTop(_posterVisual);
        }

        private void ConfigureAnimations()
        {
            // TODO: collapse all this into a single helper method
            ElementCompositionPreview.SetIsTranslationEnabled(HostElement, true);
            ElementCompositionPreview.SetImplicitShowAnimation(HostElement,
                VisualHelpers.CreateAnimationGroup(
                VisualHelpers.CreateVerticalOffsetAnimationFrom(0.45, -50f),
                VisualHelpers.CreateOpacityAnimation(0.5)
                ));
            ElementCompositionPreview.SetImplicitHideAnimation(HostElement, VisualHelpers.CreateOpacityAnimation(0.8, 0));
        }

        private void LoadPlaceholderImage(Uri load)
        {
            _currentPosterUri = load;
            Action setSurface = () =>
            {
                _posterVisual.Brush = Window.Current.Compositor.CreateSurfaceBrush(_surface);
                if (PlayerService.Current.IsOpening || PlayerService.Current.IsClosed)
                {
                    _posterVisual.Opacity = 1.0f;
                }
            };

            if (_surface == null)
            {
                _surface = LoadedImageSurface.StartLoadFromUri(load);
                _surface.LoadCompleted += (o, l) =>
                {
                    setSurface();
                };
            }
            else
            {
                setSurface();
            }
        }

        private async Task SetLoadingProgress()
        {
            pgrloading.IsIndeterminate = true;
            pgrloading.Visibility = Visibility.Visible;
            await _bvm.StartBuffering(HostElement, HostElement, false);
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            PlayerService.Current.MediaLoaded += Current_MediaLoaded;
            PlayerService.Current.MediaLoading += Current_MediaLoading;
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            PlayerService.Current.MediaLoaded -= Current_MediaLoaded;
            PlayerService.Current.MediaLoading -= Current_MediaLoading;
        }

        private async void Current_MediaLoading(object sender, EventArgs e)
        {
            await SetLoadingProgress();
        }

        private async void Current_MediaLoaded(object sender, EventArgs e)
        {
            _posterVisual.Opacity = 0.0f;
            _videoVisual.Opacity = 1.0f;

            await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                _bvm.StopBuffering(false);
                pgrloading.IsIndeterminate = false;
                pgrloading.Visibility = Visibility.Collapsed;
            });
        }
    }
}