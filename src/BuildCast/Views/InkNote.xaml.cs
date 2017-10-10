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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BuildCast.DataModel;
using BuildCast.Services.Navigation;
using BuildCast.ViewModels;
using Microsoft.Graphics.Canvas;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Input.Inking;
using Windows.UI.Input.Inking.Analysis;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace BuildCast.Views
{
    public sealed partial class InkNote : Page, IPageWithViewModel<InkNoteViewModel>
    {
        private readonly InkAnalyzer _inkAnalyzer;
        private BuildCast.DataModel.InkNote _inkNote;
        private InkNoteData _data;
        private bool _editingExisting;
        private InkPresenter _inkPresenter;

        /// <summary>
        /// Initializes a new instance of the <see cref="InkNote"/> class.
        /// </summary>
        public InkNote()
        {
            this.InitializeComponent();
            inkingCanvas.InkPresenter.IsInputEnabled = true;
            inkingCanvas.InkPresenter.InputDeviceTypes = Windows.UI.Core.CoreInputDeviceTypes.Pen | CoreInputDeviceTypes.Mouse;
            inkingCanvas.Tapped += InkCanvas_Tapped;
            _inkAnalyzer = new InkAnalyzer();
            _inkPresenter = inkingCanvas.InkPresenter;
            _inkPresenter.StrokesCollected += InkPresenter_StrokesCollected;
            _inkPresenter.StrokesErased += InkPresenter_StrokesErased;
            _inkPresenter.UnprocessedInput.PointerPressed += UnprocessedInput_PointerPressed;
            _inkPresenter.InputProcessingConfiguration.RightDragAction = InkInputRightDragAction.LeaveUnprocessed;
        }

        public InkNoteViewModel ViewModel { get; set; }

        public void UpdateBindings()
        {
            Bindings.Update();
        }

        private void InkPresenter_StrokesCollected(InkPresenter sender, InkStrokesCollectedEventArgs args)
        {
            _inkAnalyzer.AddDataForStrokes(args.Strokes);
        }

        private void InkPresenter_StrokesErased(InkPresenter sender, InkStrokesErasedEventArgs args)
        {
            _inkAnalyzer.RemoveDataForStrokes(args.Strokes.Select(i => i.Id));
        }

        private async void UnprocessedInput_PointerPressed(InkUnprocessedInput sender, PointerEventArgs args)
        {
            await AnalyzeInkAsync();
        }

        private async void InkCanvas_Tapped(object sender, TappedRoutedEventArgs e)
        {
            await AnalyzeInkAsync();
        }

        private async Task AnalyzeInkAsync()
        {
            // Does all the recognition
            var result = await _inkAnalyzer.AnalyzeAsync();

            if (result.Status == InkAnalysisStatus.Updated)
            {
                // Filter recognition by shapes. Options inlcude lists, paragraphs, words etc.
                var drawings = _inkAnalyzer.AnalysisRoot.FindNodes(InkAnalysisNodeKind.InkDrawing);

                foreach (InkAnalysisInkDrawing drawing in drawings)
                {
                    if (drawing.DrawingKind == InkAnalysisDrawingKind.Circle)
                    {
                        AddHeart(drawing);
                        RemoveInkStrokes(drawing);
                    }
                    else if (drawing.DrawingKind == InkAnalysisDrawingKind.Triangle || drawing.DrawingKind == InkAnalysisDrawingKind.EquilateralTriangle)
                    {
                        if (string.IsNullOrEmpty(customEmojiGlyph))
                        {
                            await PickEmoji();
                        }

                        if (!string.IsNullOrEmpty(customEmojiGlyph))
                        {
                            AddEmoji(drawing, customEmojiGlyph);
                        }

                        RemoveInkStrokes(drawing);
                    }
                }

                _inkAnalyzer.ClearDataForAllStrokes();
                _inkPresenter.StrokeContainer.DeleteSelected();
            }
        }

        private void RemoveInkStrokes(InkAnalysisInkDrawing drawing)
        {
            foreach (var strokeId in drawing.GetStrokeIds())
            {
                InkStroke stroke = _inkPresenter.StrokeContainer.GetStrokeById(strokeId);
                if (stroke != null)
                {
                    stroke.Selected = true;
                }
            }

            _inkAnalyzer.RemoveDataForStrokes(drawing.GetStrokeIds());
        }

        private void AddHeart(InkAnalysisInkDrawing drawing)
        {
            Viewbox vb = new Viewbox();
            TextBlock tb = new TextBlock();

            vb.SetValue(Canvas.TopProperty, drawing.BoundingRect.Top);
            vb.SetValue(Canvas.LeftProperty, drawing.BoundingRect.Left);

            tb.Foreground = new SolidColorBrush(Colors.Red);
            tb.FontFamily = new FontFamily("Segoe MDL2 Assets");
            tb.Text = "\uE00B";

            tb.FontSize = 68;
            vb.Width = drawing.BoundingRect.Width;
            vb.Height = drawing.BoundingRect.Height;
            vb.Child = tb;
            vb.Stretch = Stretch.Fill;
            xamloverlay.Children.Add(vb);
        }

        private void AddEmoji(InkAnalysisInkDrawing drawing, string glyph)
        {
            Viewbox vb = new Viewbox();
            TextBlock tb = new TextBlock();

            vb.SetValue(Canvas.TopProperty, drawing.BoundingRect.Top);
            vb.SetValue(Canvas.LeftProperty, drawing.BoundingRect.Left);

            tb.Text = glyph;

            tb.FontSize = 68;
            vb.Width = drawing.BoundingRect.Width;
            vb.Height = drawing.BoundingRect.Height;
            vb.Child = tb;
            vb.Stretch = Stretch.Fill;
            xamloverlay.Children.Add(vb);
        }

        #region Navigation
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            CoreWindow.GetForCurrentThread().KeyDown += InkNote_KeyDown;

            _inkNote = e.Parameter as BuildCast.DataModel.InkNote;

            if (Player.InkNoteData != null)
            {
                framergrab.Source = await Player.InkNoteData.GetImage(Image_ImageOpened);
            }
            else
            {
                await LoadExisting(_inkNote);
                _editingExisting = true;
            }

            InkDrawingAttributes a = new InkDrawingAttributes();
            a.Color = Colors.White;
            inkingCanvas.InkPresenter.UpdateDefaultDrawingAttributes(a);

            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            CoreWindow.GetForCurrentThread().KeyDown -= InkNote_KeyDown;
            base.OnNavigatedFrom(e);
        }

        private string customEmojiGlyph;

        private async void InkNote_KeyDown(CoreWindow sender, KeyEventArgs args)
        {
            if (args.VirtualKey == Windows.System.VirtualKey.E)
            {
                ContentDialog cd = new ContentDialog();
                var tb = new TextBox();
                tb.KeyDown += (s, e) =>
                {
                    if (e.KeyStatus.ScanCode == 0)
                    {
                        customEmojiGlyph = tb.Text;
                        e.Handled = true;
                        cd.Hide();
                    }
                };
                tb.FontSize = 100;
                tb.Width = 150;
                cd.Content = tb;
                tb.Focus(FocusState.Keyboard);
                await cd.ShowAsync();
            }
        }

        private async Task PickEmoji()
        {
            ContentDialog cd = new ContentDialog();
            var tb = new TextBox();
            tb.KeyDown += (s, e) =>
            {
                if (e.KeyStatus.ScanCode == 28)
                {
                    customEmojiGlyph = tb.Text;
                    e.Handled = true;
                    cd.Hide();
                }
            };
            tb.FontSize = 100;
            tb.Width = 150;
            cd.Content = tb;
            tb.Focus(FocusState.Keyboard);
            await cd.ShowAsync();
        }

        private void Image_ImageOpened()
        {
            inkingCanvas.Width = framergrab.ActualWidth;
            inkingCanvas.Height = framergrab.ActualHeight;
        }
        #endregion

        #region LoadSave

        private async Task LoadInk(byte[] ink)
        {
            using (MemoryStream ms = new MemoryStream(ink))
            {
                using (var memStream = ms.AsRandomAccessStream())
                {
                    await inkingCanvas.InkPresenter.StrokeContainer.LoadAsync(memStream);
                }
            }
        }

        private async Task LoadExisting(BuildCast.DataModel.InkNote meme)
        {
            using (var db = new LocalStorageContext())
            {
                _data = db.MemeData.Where(l => l.InkMeme == meme.Id).FirstOrDefault();
                if (_data != null)
                {
                    var image = await _data.GetImage(Image_ImageOpened);
                    framergrab.Source = image;
                    await LoadInk(_data.Ink);
                }
            }
        }

        private async Task SaveInkNote()
        {
            if (_editingExisting)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    using (var memStream = ms.AsRandomAccessStream())
                    {
                        await inkingCanvas.InkPresenter.StrokeContainer.SaveAsync(memStream).AsTask().ConfigureAwait(false);
                    }

                    if (ms.TryGetBuffer(out ArraySegment<byte> foo))
                    {
                        _data.Ink = foo.Array;
                    }

                    using (var db = new LocalStorageContext())
                    {
                        db.MemeData.Update(_data);
                        await db.SaveChangesAsync();
                    }
                }
            }
            else
            {
                Player.InkNoteData.InkMeme = _inkNote.Id;
                _inkNote.HasInk = true;
                using (MemoryStream ms = new MemoryStream())
                {
                    using (var memStream = ms.AsRandomAccessStream())
                    {
                        await inkingCanvas.InkPresenter.StrokeContainer.SaveAsync(memStream).AsTask().ConfigureAwait(false);
                    }

                    if (ms.TryGetBuffer(out ArraySegment<byte> foo))
                    {
                        Player.InkNoteData.Ink = foo.Array;
                    }

                    using (var db = new LocalStorageContext())
                    {
                        db.Memes.Add(_inkNote);
                        await db.SaveChangesAsync();

                        db.MemeData.Add(Player.InkNoteData);
                        await db.SaveChangesAsync();
                    }
                }

                Player.InkNoteData = null;
                ViewModel.GoBack();
            }
        }
        #endregion

        #region ButtonHandlers
        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            await SaveInkNote();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_editingExisting)
            {
                using (var db = new LocalStorageContext())
                {
                    db.Memes.Remove(_inkNote);
                    db.SaveChanges();

                    if (_data != null)
                    {
                        db.MemeData.Remove(_data);
                        db.SaveChanges();
                    }
                }
            }
        }

        private void ShareButton_Click(object sender, RoutedEventArgs e)
        {
            DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
            dataTransferManager.DataRequested += new TypedEventHandler<DataTransferManager, DataRequestedEventArgs>(this.DataRequested);
            DataTransferManager.ShowShareUI();
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            inkingCanvas.InkPresenter.StrokeContainer.Clear();
        }

        private void ClearButton_RightClick(object sender, Windows.UI.Xaml.Input.RightTappedRoutedEventArgs e)
        {
            xamloverlay.Children.Clear();
        }
        #endregion

        #region Sharing

        // Prepare data package with coloring page for sharing.
        private async void DataRequested(DataTransferManager sender, DataRequestedEventArgs e)
        {
            sender.DataRequested -= new TypedEventHandler<DataTransferManager, DataRequestedEventArgs>(this.DataRequested);
            DataRequest request = e.Request;
            DataRequestDeferral deferral = request.GetDeferral();
            request.Data.Properties.Title = $"A frame from {_inkNote.GetEpisode()?.Title}";
            request.Data.Properties.ApplicationName = "BuildCast";
            request.Data.Properties.Description = "#msbuild";

            // Don't dispose as it breaks sharing
            InMemoryRandomAccessStream inMemoryStream = new InMemoryRandomAccessStream();
            {
                bool saved = await Save_InkedImagetoStream(inMemoryStream);
                inMemoryStream.Seek(0);
                request.Data.SetBitmap(RandomAccessStreamReference.CreateFromStream(inMemoryStream));
            }

            deferral.Complete();
        }

        private async Task<bool> Save_InkedImagetoStream(IRandomAccessStream stream)
        {
            bool hasXamlOverlay = xamloverlay.Children.Count > 0;

            if (_data == null)
            {
                _data = Player.InkNoteData;
            }

            if (_data == null)
            {
                return false;
            }

            using (var imageStream = await _data.GetImageStream())
            {
                CanvasDevice device = CanvasDevice.GetSharedDevice();

                var image = await CanvasBitmap.LoadAsync(device, imageStream);

                using (var renderTarget = new CanvasRenderTarget(device, (int)inkingCanvas.ActualWidth, (int)inkingCanvas.ActualHeight, 96))
                {
                    using (CanvasDrawingSession ds = renderTarget.CreateDrawingSession())
                    {
                        ds.Clear(Colors.White);

                        ds.DrawImage(image, new Rect(0, 0, (int)inkingCanvas.ActualWidth, (int)inkingCanvas.ActualHeight));

                        if (hasXamlOverlay)
                        {
                            await DrawXamlOverlay(device, renderTarget, ds);
                        }

                        ds.Units = CanvasUnits.Pixels;
                        ds.DrawInk(inkingCanvas.InkPresenter.StrokeContainer.GetStrokes());
                    }

                    await renderTarget.SaveAsync(stream, CanvasBitmapFileFormat.Png);
                }
            }

            return true;
        }

        private async Task DrawXamlOverlay(CanvasDevice device, CanvasRenderTarget renderTarget, CanvasDrawingSession ds)
        {
            IBuffer renderBitmapPixels = null;
            Size bitmapSizeAt96Dpi;
            var overlay = await Save_XAML();
            bitmapSizeAt96Dpi = new Size(
                                        overlay.PixelWidth,
                                        overlay.PixelHeight);
            renderBitmapPixels = await overlay.GetPixelsAsync();

            using (var win2dRenderedBitmap =
                      CanvasBitmap.CreateFromBytes(
                      device,
                      renderBitmapPixels,
                      (int)bitmapSizeAt96Dpi.Width,
                      (int)bitmapSizeAt96Dpi.Height,
                      Windows.Graphics.DirectX.DirectXPixelFormat.B8G8R8A8UIntNormalized,
                      96.0f))
            {
                ds.DrawImage(win2dRenderedBitmap,
                new Rect(0, 0, renderTarget.SizeInPixels.Width, renderTarget.SizeInPixels.Height),
                new Rect(0, 0, bitmapSizeAt96Dpi.Width, bitmapSizeAt96Dpi.Height));
            }
        }

        private async Task<RenderTargetBitmap> Save_XAML()
        {
            var overlay = new RenderTargetBitmap();
            await overlay.RenderAsync(xamloverlay);
            return overlay;
        }
        #endregion
    }
}
