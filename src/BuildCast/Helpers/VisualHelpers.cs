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
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Media;

namespace BuildCast.Helpers
{
    public static class VisualHelpers
    {
        public static Visual GetVisual(this UIElement element)
        {
            return ElementCompositionPreview.GetElementVisual(element);
        }

        public static CompositionCommitBatch ApplyImplicitAnimation(this Visual target, TimeSpan duration)
        {
            var myBatch = target.Compositor.GetCommitBatch(CompositionBatchTypes.Animation);
            target.Opacity = 0.0f;
            ImplicitAnimationCollection implicitAnimationCollection = target.Compositor.CreateImplicitAnimationCollection();

            implicitAnimationCollection[nameof(Visual.Opacity)] = CreateOpacityAnimation(target.Compositor, duration);
            target.ImplicitAnimations = implicitAnimationCollection;
            return myBatch;
        }

        public static KeyFrameAnimation CreateOpacityAnimation(Compositor compositor, TimeSpan duration)
        {
            ScalarKeyFrameAnimation kf = compositor.CreateScalarKeyFrameAnimation();
            kf.InsertExpressionKeyFrame(1.0f, "this.FinalValue");
            kf.Duration = duration;
            kf.Target = "Opacity";
            return kf;
        }

        public static void SetSize(this Visual v, FrameworkElement element)
        {
            v.Size = new System.Numerics.Vector2((float)element.ActualWidth, (float)element.ActualHeight);
        }

        public static void FadeVisual(this Visual v, double seconds)
        {
            var fadeAnimation = CreateImplicitFadeAnimation(seconds);
            v.ImplicitAnimations = Window.Current.Compositor.CreateImplicitAnimationCollection();
            v.ImplicitAnimations.Add(nameof(Visual.Opacity), fadeAnimation);
        }

        // TODO: kill this function
        public static ScalarKeyFrameAnimation CreateOpacityAnimation(double seconds)
        {
            var animation = Window.Current.Compositor.CreateScalarKeyFrameAnimation();
            animation.Target = "Opacity";
            animation.Duration = TimeSpan.FromSeconds(seconds);
            animation.InsertKeyFrame(0, 0);
            animation.InsertKeyFrame(0.25f, 0);
            animation.InsertKeyFrame(1, 1);
            return animation;
        }

        public static ICompositionAnimationBase CreateOpacityAnimation(double seconds, float finalvalue)
        {
            var animation = Window.Current.Compositor.CreateScalarKeyFrameAnimation();
            animation.Target = nameof(Visual.Opacity);
            animation.Duration = TimeSpan.FromSeconds(seconds);
            animation.InsertKeyFrame(1, finalvalue);
            return animation;
        }

        public static ICompositionAnimationBase CreateAnimationGroup(CompositionAnimation listContentShowAnimations, ScalarKeyFrameAnimation listContentOpacityAnimations)
        {
            var group = Window.Current.Compositor.CreateAnimationGroup();
            group.Add(listContentShowAnimations);
            group.Add(listContentOpacityAnimations);
            return group;
        }

        public static void EnableLayoutImplicitAnimations(this UIElement element, TimeSpan t)
        {
            Compositor compositor;
            var result = element.GetVisual();
            compositor = result.Compositor;

            var elementImplicitAnimation = compositor.CreateImplicitAnimationCollection();
            elementImplicitAnimation[nameof(Visual.Offset)] = CreateOffsetAnimation(compositor, t);

            result.ImplicitAnimations = elementImplicitAnimation;
        }

        private static CompositionAnimation CreateImplicitFadeAnimation(double seconds)
        {
            var animation = Window.Current.Compositor.CreateScalarKeyFrameAnimation();
            animation.InsertExpressionKeyFrame(1.0f, "this.FinalValue");
            animation.Target = nameof(Visual.Opacity);
            animation.Duration = TimeSpan.FromSeconds(seconds);
            return animation;
        }

        private static KeyFrameAnimation CreateOffsetAnimation(Compositor compositor, TimeSpan duration)
        {
            Vector3KeyFrameAnimation kf = compositor.CreateVector3KeyFrameAnimation();
            kf.InsertExpressionKeyFrame(1.0f, "this.FinalValue");
            kf.Duration = duration;
            kf.Target = "Offset";
            return kf;
        }

        public static CompositionAnimation CreateHorizontalOffsetAnimation(double seconds, float offset, double delaySeconds, bool from)
        {
            var animation = Window.Current.Compositor.CreateScalarKeyFrameAnimation();
            if (delaySeconds != 0.0)
            {
                animation.DelayBehavior = AnimationDelayBehavior.SetInitialValueBeforeDelay;
                animation.DelayTime = TimeSpan.FromSeconds(delaySeconds);
            }

            animation.Duration = TimeSpan.FromSeconds(seconds);
            animation.Target = "Translation.X";
            if (from)
            {
                animation.InsertKeyFrame(0, offset);
                animation.InsertKeyFrame(1, 0);
            }
            else
            {
                animation.InsertKeyFrame(1, offset);
            }

            return animation;
        }

        public static CompositionAnimation CreateHorizontalOffsetAnimation(double seconds, float offset, double delaySeconds)
        {
            return CreateHorizontalOffsetAnimation(seconds, offset, delaySeconds, true);
        }

        public static CompositionAnimation CreateVerticalOffsetAnimation(double seconds, float offset, double delaySeconds, bool from)
        {
            var animation = Window.Current.Compositor.CreateScalarKeyFrameAnimation();
            if (delaySeconds != 0.0)
            {
                animation.DelayBehavior = AnimationDelayBehavior.SetInitialValueBeforeDelay;
                animation.DelayTime = TimeSpan.FromSeconds(delaySeconds);
            }

            animation.Duration = TimeSpan.FromSeconds(seconds);
            animation.Target = "Translation.Y";
            if (from)
            {
                animation.InsertKeyFrame(0, offset);
                animation.InsertKeyFrame(1, 0);
            }
            else
            {
                animation.InsertKeyFrame(1, offset);
            }

            return animation;
        }

        public static CompositionAnimation CreateVerticalOffsetAnimation(double seconds, float offset, double delaySeconds)
        {
            return CreateVerticalOffsetAnimation(seconds, offset, delaySeconds, true);
        }

        public static CompositionAnimation CreateVerticalOffsetAnimationFrom(double seconds, float offset)
        {
            return CreateVerticalOffsetAnimation(seconds, offset, 0.0f);
        }

        public static CompositionAnimation CreateVerticalOffsetAnimationTo(double seconds, float offset)
        {
            return CreateVerticalOffsetAnimation(seconds, offset, 0.0f, false);
        }

        public static T GetVisualChildByName<T>(this FrameworkElement root, string name)
            where T : FrameworkElement
        {
            var chil = VisualTreeHelper.GetChild(root, 0);
            FrameworkElement child = null;

            int count = VisualTreeHelper.GetChildrenCount(root);

            for (int i = 0; i < count && child == null; i++)
            {
                var current = (FrameworkElement)VisualTreeHelper.GetChild(root, i);
                if (current != null && current.Name != null && current.Name == name)
                {
                    child = current;
                    break;
                }
                else
                {
                    child = current.GetVisualChildByName<FrameworkElement>(name);
                }
            }

            return child as T;
        }
    }
}