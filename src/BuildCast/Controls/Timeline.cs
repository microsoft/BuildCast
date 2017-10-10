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
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using Microsoft.Toolkit.Uwp.UI.Animations.Expressions;
    using Windows.UI.Composition;
    using Windows.UI.Composition.Interactions;
    using Windows.UI.Core;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Hosting;
    using Windows.UI.Xaml.Input;

    public class Timeline : Control, IInteractionTrackerOwner, INotifyPropertyChanged
    {
        private Compositor compositor;
        private Visual thumbVisual;
        private Visual trackVisual;
        private InteractionTracker tracker;
        private VisualInteractionSource interactionSource;
        private ExpressionNode thumbExpressionAnimation;
        private CompositionPropertySet propSet;
        private TimeControl myPercentageTimeControl;
        private TimeControl elapsedTimeControl;

        private double currentPercentage;

        private FrameworkElement thumb;
        private FrameworkElement trackCanvas;

        private double trackCanvasWidth;
        private bool dragging;

        /// <summary>
        /// Initializes a new instance of the <see cref="Timeline"/> class.
        /// </summary>
        public Timeline()
        {
            this.DefaultStyleKey = typeof(Timeline);
            this.Loaded += Timeline_Loaded;
            this.Unloaded += this.Timeline_Unloaded;
        }

        public event EventHandler DragStart;

        public event EventHandler DragStop;

        public event PropertyChangedEventHandler PropertyChanged;

        #region Public Properties
        public double CurrentPercentage
        {
            get
            {
                return currentPercentage;
            }

            set
            {
                this.currentPercentage = value;
                myPercentageTimeControl.CurrentPercentage = value;
                if (elapsedTimeControl != null)
                {
                    elapsedTimeControl.CurrentPercentage = value;
                }

                RaisePropertyChanged();
            }
        }

        public TimeSpan CurrentTime
        {
            get
            {
                return TimeSpan.FromSeconds(TimeDuration.TotalSeconds * CurrentPercentage);
            }
        }

        public bool IsScrubbing
        {
            get
            {
                return dragging;
            }
        }
#endregion

        private TimeSpan TimeDuration
        {
            get; set;
        }

        #region public methods

        public void SetTime(TimeSpan postition, TimeSpan duration)
        {
            if (!IsScrubbing && myPercentageTimeControl != null)
            {
                myPercentageTimeControl.CurrentTime = postition;
                myPercentageTimeControl.Duration = duration;

                if (elapsedTimeControl != null)
                {
                    elapsedTimeControl.CurrentTime = postition;
                    elapsedTimeControl.Duration = duration;
                }

                TimeDuration = duration;

                var temp = (postition.TotalMilliseconds / duration.TotalMilliseconds) * 100;

                var newPos = this.CalcPosition(temp);
                if (!double.IsNaN(newPos) || !double.IsNaN(newPos))
                {
                    var anim = compositor.CreateVector3KeyFrameAnimation();
                    var easing = compositor.CreateLinearEasingFunction();

                    anim.InsertExpressionKeyFrame(1f, ExpressionFunctions.Vector3((float)-newPos, 0, 0), easing);
                    try
                    {
                        var result = this.tracker.TryUpdatePositionWithAnimation(anim);
                    }
                    catch (ArgumentException)
                    {
                        // since new if statement is somehow letting NaN through, need to catch exception when it does
                    }
                }
            }
        }

        public void SetElapsedTimeControl(TimeControl elapsed)
        {
            this.elapsedTimeControl = elapsed;
            if (elapsed != null)
            {
                this.elapsedTimeControl.CurrentTime = TimeSpan.Zero;
                this.elapsedTimeControl.Duration = TimeSpan.FromSeconds(1);
            }
        }

        public void NotifyPointerPressed(PointerEventArgs args)
        {
            try
            {
                interactionSource.TryRedirectForManipulation(args.CurrentPoint);
            }
            catch (Exception)
            {
            }
        }
        #endregion

        #region INotifyPropertyChanged
        private void RaisePropertyChanged([CallerMemberName] string caller = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(caller));
        }
        #endregion

        #region PositionHandling
        private void SetPosition(double percentage)
        {
            this.currentPercentage = percentage;

            this.UpdatePosition();
        }

        private void UpdatePosition()
        {
            var newPos = this.CalcPosition(this.currentPercentage);
            this.SetPositionAbsoluteClamped(newPos);
        }

        private double Clamp(double position)
        {
            if (position < 0)
            {
                return 0.0;
            }
            else if (position > this.trackCanvasWidth - this.thumb.Width)
            {
                return this.trackCanvasWidth - this.thumb.Width;
            }
            else
            {
                return position;
            }
        }

        private void SetPositionAbsoluteClamped(double position)
        {
            if (this.thumb != null && this.trackCanvas != null)
            {
                this.thumb.SetValue(Canvas.LeftProperty, this.Clamp(position));
            }
        }

        private double CalcPosition(double percent)
        {
            if (this.thumb == null)
            {
                return 0.0;
            }

            return ((percent / 100.0) * this.trackCanvasWidth) - (45 * (percent / 100.0));
        }
        #endregion

        #region eventhandlers
        private void Timeline_Loaded(object sender, RoutedEventArgs e)
        {
            this.SizeChanged += this.Timeline_SizeChanged;
            this.trackCanvas.SizeChanged += this.TrackCanvas_SizeChanged;

            this.StartComposition();
        }

        private void Timeline_Unloaded(object sender, RoutedEventArgs e)
        {
            this.SizeChanged -= this.Timeline_SizeChanged;

            if (this.trackCanvas != null)
            {
                this.trackCanvas.SizeChanged -= this.TrackCanvas_SizeChanged;
            }
        }

        private void Timeline_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.UpdatePosition();
        }

        // This captures when a mousewheel interaction occurs. We will then take the "delta" from the mousewheel and
        // funnel that into an animation and animate the position of InteractionTracker. Tracker's position ultimately defines position of the scrubber
        private void PointerWheel_Changed(object sender, PointerRoutedEventArgs e)
        {
            // Calling OnDragStart() so we can change the position of the scrubber
            OnDragStart();

            // Grab the delta of the mousewheel from the event handler
            var wheelDelta = e.GetCurrentPoint(trackCanvas).Properties.MouseWheelDelta;
            propSet.InsertVector3("wheelDelta", new System.Numerics.Vector3(wheelDelta, 0f, 0f));

            // Create the animation that we will use to animate the position of InteractionTracker
            var wheelKFA = this.compositor.CreateVector3KeyFrameAnimation();
            wheelKFA.Duration = TimeSpan.FromSeconds(1);

            // We are using strings here instead of ExpressionBuilder. We need an extension method added to ExpressionBuilder for TryUpdatePositionWithAnimation to take in
            // an ExpressionNode. Then we can use ExpressionNodes in the ExpressionKeyFrame instead of strings
            wheelKFA.InsertExpressionKeyFrame(1f, "this.CurrentValue + props.wheelDelta");
            wheelKFA.SetReferenceParameter("tracker", this.tracker);
            wheelKFA.SetReferenceParameter("props", propSet);

            // Update the position of InteractionTracker with the animation based on the scroll wheel delta
            this.tracker.TryUpdatePositionWithAnimation(wheelKFA);
        }

        // We are capturing the move events of the mouse. THis will be for when we want to click and drag the scrubber around.
        // Note this event gets fired for anytime the pointers moves in general. To make sure we only animate when scrubbing,
        // We are assuming the left mouse button will be pressed
        private void MousePointer_Moved(object sender, PointerRoutedEventArgs e)
        {
            // Detecting that the pointer type is of mouse
            if (e.Pointer.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Mouse)
            {
                // Detecting that the left mouse button is clicked
                if (e.GetCurrentPoint(thumb).Properties.IsLeftButtonPressed)
                {
                    // Calling OnDragStart() so we can change the position of the scrubber
                    OnDragStart();

                    // Grab the X position of the pointer in the TrackCanvas
                    float xPosition = (float)e.GetCurrentPoint(trackCanvas).Position.X;
                    propSet.InsertVector3("xMoved", new System.Numerics.Vector3(xPosition, 0f, 0f));

                    // Create the animation that we will use to animate the position of InteractionTracker
                    // Using an Expression since the deltas will be so small that a KFA would be a bit odd
                    // We are using strings here instead of ExpressionBuilder. We need an extension method added to ExpressionBuilder for TryUpdatePositionWithAnimation to take in
                    // an ExpressionNode. Then we can use ExpressionNodes in the ExpressionKeyFrame instead of strings
                    var pointerExp = this.compositor.CreateExpressionAnimation("-props.xMoved");
                    pointerExp.SetReferenceParameter("props", propSet);
                    this.tracker.TryUpdatePositionWithAnimation(pointerExp);
                }
            }
        }

        // This is for click-to-seek. Event current commented out on the trackCanvas, weird eventing issues between when this event gets called and when need to call OnDragStart() and OnDragStop()
        private void MousePointer_ThumbPressed(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Mouse)
            {
                var xPosition = (float)e.GetCurrentPoint(trackCanvas).Position.X;
                propSet.InsertVector3("xPress", new System.Numerics.Vector3(-xPosition, 0f, 0f));

                if (e.GetCurrentPoint(thumb).Properties.IsLeftButtonPressed)
                {
                    OnDragStart();

                    // var pointerExp = propSet.GetReference().GetVector3Property("xPress");
                    var pointerExp = this.compositor.CreateExpressionAnimation("props.xPress");
                    pointerExp.SetReferenceParameter("props", propSet);

                    this.tracker.TryUpdatePositionWithAnimation(pointerExp);
                }
            }
        }

        private void MousePointer_Released(object sender, PointerRoutedEventArgs e)
        {
            if (IsScrubbing)
            {
                this.OnDragStop();
            }
        }

        // This is for click-to-seek. Event current commented out on the trackCanvas, weird eventing issues between when this event gets called and when need to call OnDragStart() and OnDragStop()
        private void MousePointer_Pressed(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Mouse)
            {
                OnDragStart();
                var xPosition = (float)e.GetCurrentPoint(trackCanvas).Position.X;
                propSet.InsertVector3("xClickLocation", new System.Numerics.Vector3(-xPosition, 0f, 0f));

                var pointerKFA = this.compositor.CreateVector3KeyFrameAnimation();
                pointerKFA.Duration = TimeSpan.FromSeconds(1);

                // pointerKFA.InsertExpressionKeyFrame(1f, "props.xClickLocation");
                // pointerKFA.SetReferenceParameter("props", propSet);
                pointerKFA.InsertExpressionKeyFrame(1f, propSet.GetReference().GetVector3Property("xClickLocation"));

                this.tracker.TryUpdatePositionWithAnimation(pointerKFA);
            }
        }

        private void TrackCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.trackVisual.Size = new System.Numerics.Vector2((float)trackCanvas.ActualWidth, (float)trackCanvas.ActualHeight);
            this.thumbVisual.Size = new System.Numerics.Vector2((float)thumb.ActualWidth, (float)thumb.ActualHeight);

            this.tracker.MinPosition = new System.Numerics.Vector3(-this.trackVisual.Size.X, 0, 0);

            this.trackCanvasWidth = e.NewSize.Width;
        }

        private void OnDragStart()
        {
            this.dragging = true;
            myPercentageTimeControl.Scrubbing = true;
            if (elapsedTimeControl != null)
            {
                elapsedTimeControl.Scrubbing = true;
            }

            this.DragStart?.Invoke(this, EventArgs.Empty);
        }

        private void OnDragStop()
        {
            this.dragging = false;
            myPercentageTimeControl.Scrubbing = false;
            if (elapsedTimeControl != null)
            {
                elapsedTimeControl.Scrubbing = false;
            }

            this.DragStop?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region InternalInitialization
#pragma warning disable SA1202 // Elements should be ordered by access
        protected override void OnApplyTemplate()
#pragma warning restore SA1202 // Elements should be ordered by access
        {
            base.OnApplyTemplate();
            this.thumb = this.GetTemplateChild("positionIndicator") as FrameworkElement;
            this.trackCanvas = this.GetTemplateChild("trackCanvas") as FrameworkElement;
            this.myPercentageTimeControl = this.GetTemplateChild("timeControl") as TimeControl;
            this.myPercentageTimeControl.CurrentTime = TimeSpan.Zero;
            this.myPercentageTimeControl.Duration = TimeSpan.FromSeconds(1);
            this.trackCanvas.SizeChanged += this.TrackCanvas_SizeChanged;

            InitializeComposition();
        }

        private void InitializeComposition()
        {
            this.compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;
            this.thumbVisual = ElementCompositionPreview.GetElementVisual(thumb);
            this.trackVisual = ElementCompositionPreview.GetElementVisual(trackCanvas);

            propSet = this.compositor.CreatePropertySet();
            trackCanvas.PointerWheelChanged += new PointerEventHandler(PointerWheel_Changed);
            trackCanvas.PointerMoved += new PointerEventHandler(MousePointer_Moved);
            trackCanvas.PointerReleased += new PointerEventHandler(MousePointer_Released);

            // These pointer events are needed if want to click-to-seek. Weird eventing issues between when these get called and when need to call OnDragStart() and OnDragStop() methods
            // trackCanvas.PointerPressed += new PointerEventHandler(MousePointer_Pressed);
            // thumb.PointerPressed += new PointerEventHandler(MousePointer_ThumbPressed);
            this.interactionSource = VisualInteractionSource.Create(this.trackVisual);
            this.interactionSource.PositionXSourceMode = InteractionSourceMode.EnabledWithInertia;
            this.tracker = InteractionTracker.CreateWithOwner(this.compositor, this);
            this.tracker.InteractionSources.Add(this.interactionSource);

            // This is the Expression that we will use to drive the position of the Thumb Scrubber
            // Idea is that we will then update the position of InteractionTracker to move thes scrubber around - this is done via the Pointer Events tied to the TrackCanvas object
            // Note: this is using ExpressionBuilder
            this.thumbExpressionAnimation = -tracker.GetReference().Position.X;
        }

        private void StartComposition()
        {
            // Starting the Expression on the thumb scrubber that will leverage the position of InteractionTracker
            this.thumbVisual.StartAnimation(nameof(Visual.Offset) + ".X", thumbExpressionAnimation);
        }
        #endregion

        #region IInteractionTrackerOwner
#pragma warning disable SA1202 // Elements should be ordered by access
        public void CustomAnimationStateEntered(InteractionTracker sender, InteractionTrackerCustomAnimationStateEnteredArgs args)
        {
        }

        public void IdleStateEntered(InteractionTracker sender, InteractionTrackerIdleStateEnteredArgs args)
        {
            if (IsScrubbing)
            {
                this.OnDragStop();
            }

            Debug.WriteLine("Idle");
        }

        public void InertiaStateEntered(InteractionTracker sender, InteractionTrackerInertiaStateEnteredArgs args)
        {
            OnDragStart();
        }

        public void InteractingStateEntered(InteractionTracker sender, InteractionTrackerInteractingStateEnteredArgs args)
        {
            OnDragStart();
        }

        public void RequestIgnored(InteractionTracker sender, InteractionTrackerRequestIgnoredArgs args)
        {
        }

        public void ValuesChanged(InteractionTracker sender, InteractionTrackerValuesChangedArgs args)
        {
            if (IsScrubbing)
            {
                var percent = -args.Position.X / this.ActualWidth;
                this.CurrentPercentage = percent;
            }
        }
#pragma warning restore SA1202 // Elements should be ordered by access
        #endregion
    }
}
