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
    using System.Numerics;
    using System.Threading;
    using BuildCast.Helpers;
    using Microsoft.Graphics.Canvas;
    using Microsoft.Graphics.Canvas.Text;
    using Microsoft.Graphics.Canvas.UI.Composition;
    using Windows.Foundation;
    using Windows.UI;
    using Windows.UI.Composition;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Hosting;
    using Windows.UI.Xaml.Media;
    using Windows.UI.Xaml.Shapes;

    public class TimeControl : Control, IDisposable
    {
        private object drawlock = new object();
        private ContainerVisual rootVisual;
        private CanvasSwapChain swapChain;
        private SpriteVisual swapChainVisual;
        private CanvasDevice canvasDevice;

        private Rectangle hostElement;

        private CancellationTokenSource drawLoopCancellationTokenSource;
        private int drawCount;

        private TimeSpan currentTime;

        private Color foregroundColor;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeControl"/> class.
        /// </summary>
        public TimeControl()
        {
            drawLoopCancellationTokenSource = new CancellationTokenSource();
            this.DefaultStyleKey = typeof(TimeControl);
        }

        public bool Scrubbing { get; set; }

        public TimeSpan Duration
        {
            get; set;
        }

        public bool ShowAsPercentage { get; set; }

        public TimeSpan CurrentTime
        {
            get
            {
                return currentTime;
            }

            set
            {
                currentTime = value;
                if (swapChain != null && Duration > TimeSpan.Zero)
                {
                    DrawSwapChain(swapChain);
                    swapChain.Present();
                }
            }
        }

        public double CurrentPercentage
        {
            get
            {
                return 0;
            }

            set
            {
                if (Duration > TimeSpan.MinValue)
                {
                    CurrentTime = TimeSpan.FromMilliseconds(value * Duration.TotalMilliseconds);
                }
            }
        }

        public void Dispose()
        {
            drawLoopCancellationTokenSource?.Cancel();
            swapChain?.Dispose();
        }

        public void SetDevice(CanvasDevice device)
        {
            drawLoopCancellationTokenSource?.Cancel();

            var displayInfo = Windows.Graphics.Display.DisplayInformation.GetForCurrentView();

            this.swapChain = new CanvasSwapChain(
                device,
                (int)this.ActualWidth,
                (int)this.ActualHeight,
                displayInfo.LogicalDpi,
                Windows.Graphics.DirectX.DirectXPixelFormat.B8G8R8A8UIntNormalized,
                2,
                CanvasAlphaMode.Premultiplied);

            swapChainVisual.Brush = Window.Current.Compositor.CreateSurfaceBrush(CanvasComposition.CreateCompositionSurfaceForSwapChain(Window.Current.Compositor, swapChain));

            drawLoopCancellationTokenSource = new CancellationTokenSource();
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            hostElement = GetTemplateChild("DrawHolder") as Rectangle;
            hostElement.SizeChanged += ElementCompositionVisual_SizeChanged;
            rootVisual = Window.Current.Compositor.CreateContainerVisual();
            swapChainVisual = Window.Current.Compositor.CreateSpriteVisual();
            swapChainVisual.Brush = Window.Current.Compositor.CreateColorBrush(Colors.Pink);
            rootVisual.Children.InsertAtTop(swapChainVisual);
            ElementCompositionPreview.SetElementChildVisual(hostElement, rootVisual);

            foregroundColor = ((SolidColorBrush)Foreground).Color;
        }

        private void ElementCompositionVisual_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            rootVisual.SetSize(hostElement);
            swapChainVisual.SetSize(hostElement);

            if (canvasDevice != null)
            {
                Dispose();
                canvasDevice = null;
            }

            if (canvasDevice == null)
            {
                canvasDevice = CanvasDevice.GetSharedDevice();
                SetDevice(canvasDevice);
            }

            DrawSwapChain(swapChain);
            swapChain.Present();
        }

        private void DrawSwapChain(CanvasSwapChain sc)
        {
            var result = Monitor.TryEnter(drawlock);
            if (result)
            {
                try
                {
                    ++drawCount;

                    using (var ds = sc.CreateDrawingSession(Colors.Transparent))
                    {
                        var size = sc.Size.ToVector2();

                        if (ShowAsPercentage)
                        {
                            ds.DrawText($"{CalcPercentage()}%", new Rect(0, 0, this.swapChain.Size.Width, this.swapChain.Size.Height), foregroundColor, new CanvasTextFormat()
                            {
                                FontSize = 17,
                                VerticalAlignment = CanvasVerticalAlignment.Bottom,
                                HorizontalAlignment = CanvasHorizontalAlignment.Center,
                            });
                        }
                        else
                        {
                            ds.DrawText($"{this.CurrentTime.ToString(@"hh\:mm\:ss")}", new Rect(0, 0, this.swapChain.Size.Width, this.swapChain.Size.Height), foregroundColor, new CanvasTextFormat()
                            {
                                FontSize = 15,
                                VerticalAlignment = CanvasVerticalAlignment.Center,
                                HorizontalAlignment = CanvasHorizontalAlignment.Center,
                            });
                        }
                    }
                }
                finally
                {
                    Monitor.Exit(drawlock);
                }
            }
        }

        private double CalcPercentage()
        {
            return Math.Round((CurrentTime.TotalMilliseconds / Duration.TotalMilliseconds) * 100);
        }
    }
}
