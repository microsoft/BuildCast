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
using System.Numerics;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace BuildCast.Helpers
{
    public class BufferingVisualManager
    {
        private Compositor _compositor;
        private PointLight _pointLight;
        private AmbientLight _ambientLight;

        private TextBlock _bufferingMessage;

        private Panel _textHolder;
        private FrameworkElement _lightingTarget;

        public bool IsBuffering { get; private set; }

        public async Task StartBuffering(Panel textHolder, FrameworkElement lightingTarget, bool lightingdisabled)
        {
            if (!this.IsBuffering)
            {
                this.IsBuffering = true;
                this._textHolder = textHolder;
                if (!lightingdisabled)
                {
                    this._lightingTarget = lightingTarget;
                    this.ConfigureLighting(this._lightingTarget);
                }

                this._bufferingMessage = await this.ConfigureText(this._textHolder);
            }
        }

        public void StopBuffering(bool lightingdisabled)
        {
            if (this.IsBuffering)
            {
                CompositionScopedBatch batch = null;

                if (!lightingdisabled)
                {
                    batch = this.RemoveLighting();
                }
                else
                {
                    batch = this._compositor.CreateScopedBatch(CompositionBatchTypes.Animation);
                }

                this.RemoveText(batch, this._bufferingMessage);
                if (batch != null)
                {
                    batch.Completed += (o, e) =>
                    {
                        this.IsBuffering = false;
                    };

                    batch.End();
                }
            }
        }

        public void SetCompositor(Compositor compositor)
        {
            this._compositor = compositor;
        }

        private async Task<TextBlock> ConfigureText(Panel target)
        {
            TextBlock tb = new TextBlock();
            tb.FontSize = 30;
            tb.Text = "Loading..";
            tb.HorizontalAlignment = HorizontalAlignment.Center;
            tb.VerticalAlignment = VerticalAlignment.Center;
            tb.SetValue(Grid.RowProperty, 2);
            target.Children.Add(tb);
            var visual = tb.GetVisual();
            var batch = visual.ApplyImplicitAnimation(TimeSpan.FromSeconds(2));

            Func<Task> delega = null;

            delega = async () =>
             {
                 if (batch.IsEnded)
                 {
                     visual.Opacity = 1.0f;
                 }
                 else
                 {
                     await Window.Current.Dispatcher.RunIdleAsync((s) => { delega(); });
                 }
             };

            await Window.Current.Dispatcher.RunIdleAsync((s) => { delega(); });
            return tb;
        }

        private void RemoveText(CompositionScopedBatch batch, TextBlock textElement)
        {
            if (textElement != null)
            {
                var visual = textElement.GetVisual();
                visual.Opacity = 0;
            }

            if (batch != null)
            {
                batch.Completed += (o, e) =>
                {
                    this._textHolder.Children.Remove(this._bufferingMessage);
                    this._bufferingMessage = null;
                };
            }
        }

        private void ConfigureLighting(FrameworkElement element)
        {
            if (this._compositor != null)
            {
                // get interop visual for element
                var text = element.GetVisual();

                this._ambientLight = this._compositor.CreateAmbientLight();
                this._ambientLight.Color = Colors.White;
                this._ambientLight.Targets.Add(text);

                this._pointLight = this._compositor.CreatePointLight();
                this._pointLight.Color = Colors.White;
                this._pointLight.CoordinateSpace = text;
                this._pointLight.Targets.Add(text);

                // starts out to the left; vertically centered; light's z-offset is related to fontsize
                this._pointLight.Offset = new Vector3(0, (float)element.ActualHeight / 2, 480);

                // simple offset.X animation that runs forever
                var animation = this._compositor.CreateScalarKeyFrameAnimation();
                animation.InsertKeyFrame(1, (float)element.ActualWidth);
                animation.Duration = TimeSpan.FromMilliseconds(800);
                animation.IterationBehavior = AnimationIterationBehavior.Forever;
                animation.Direction = AnimationDirection.Alternate;

                var animation2 = this._compositor.CreateColorKeyFrameAnimation();
                animation2.Duration = TimeSpan.FromSeconds(2);
                animation2.InsertKeyFrame(1, Color.FromArgb(0xFF, 0x50, 0x50, 0x50));
                this._ambientLight.StartAnimation(nameof(AmbientLight.Color), animation2);

                this._pointLight.StartAnimation("Offset.X", animation);
            }
        }

        private CompositionScopedBatch RemoveLighting()
        {
            CompositionScopedBatch returnBatch = null;
            if (this._compositor != null)
            {
                returnBatch = this._compositor.CreateScopedBatch(CompositionBatchTypes.Animation);
                var animation2 = this._compositor.CreateColorKeyFrameAnimation();
                animation2.Duration = TimeSpan.FromSeconds(2);
                animation2.InsertKeyFrame(1, Colors.White);
                this._ambientLight.StartAnimation("Color", animation2);

                var animation = this._compositor.CreateScalarKeyFrameAnimation();
                animation.Duration = TimeSpan.FromSeconds(2);
                animation.InsertKeyFrame(1, 40.0f);
                this._pointLight.StartAnimation("LinearAttenuation", animation);

                returnBatch.Completed += (o, e) =>
                {
                    if (this._pointLight != null)
                    {
                        this._pointLight.Dispose();
                        this._pointLight = null;
                    }

                    if (this._ambientLight != null)
                    {
                        this._ambientLight.Dispose();
                        this._ambientLight = null;
                    }
                };
            }

            return returnBatch;
        }
    }
}
