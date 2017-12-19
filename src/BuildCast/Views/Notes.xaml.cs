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

using BuildCast.DataModel;
using BuildCast.Helpers;
using BuildCast.Services.Navigation;
using BuildCast.ViewModels;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace BuildCast.Views
{
    public sealed partial class Notes : Page, IPageWithViewModel<NotesViewModel>
    {
        public NotesViewModel ViewModel { get; set; }

        public Notes()
        {
            this.InitializeComponent();
        }

        public void UpdateBindings()
        {
            Bindings?.Update();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            SetupMenuFlyout();
            Canvas.SetZIndex(this, 0);
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            Canvas.SetZIndex(this, 1);
        }

        private async void Notes_Loaded(object sender, RoutedEventArgs e)
        {
            await ViewModel.LoadNotes();
        }

        private void SetupMenuFlyout()
        {
            // Associate the menu with the item requesting it.
            MenuFlyout menu = new MenuFlyout();
            menu.Opening += MenuFlyout_Opening;

            // Add click handlers to the menu flyout items.
            MenuFlyoutItem item = new MenuFlyoutItem { Text = "Remove item", Icon = new SymbolIcon { Symbol = Symbol.Delete } };
            menu.Items.Add(item);
        }

        private void NotesListView_Tapped(object sender, ItemClickEventArgs e)
        {
            ViewModel.NavigateToItem(e.ClickedItem);
        }

        private void Grid_PointerEntered(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            Grid originGrid = (Grid)sender;

            Grid hoverGrid = (Grid)originGrid.Children[originGrid.Children.Count - 1];
            hoverGrid.Visibility = Visibility.Visible;
        }

        private void Grid_PointerExited(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            Grid originGrid = (Grid)sender;

            Grid hoverGrid = (Grid)originGrid.Children[originGrid.Children.Count - 1];
            hoverGrid.Visibility = Visibility.Collapsed;
        }

        private void ReLoadNotes() => ViewModel.ReloadNotes();

        private void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            dynamic output = (sender as AppBarButton).DataContext;
            System.Guid inkId = output.InkId;

            switch (output.Type)
            {
                case "Ink":
                    using (var db = new LocalStorageContext())
                    {
                        var ink = db.Memes.Remove(db.Memes.Find(inkId));
                        db.SaveChanges();

                        ReLoadNotes();
                    }

                    break;
                case "Bookmark":
                    using (var db = new LocalStorageContext())
                    {
                        var ink = db.Memes.Remove(db.Memes.Find(inkId));
                        db.SaveChanges();

                        ReLoadNotes();
                    }

                    break;
            }
        }

        private void SwipeDelete_Invoked(SwipeItem sender, SwipeItemInvokedEventArgs args)
        {
            dynamic output = args.SwipeControl.DataContext;
            System.Guid inkId = output.InkId;

            switch (output.Type)
            {
                case "Ink":
                    using (var db = new LocalStorageContext())
                    {
                        var ink = db.Memes.Remove(db.Memes.Find(inkId));
                        db.SaveChanges();

                        ReLoadNotes();
                    }

                    break;
                case "Bookmark":
                    using (var db = new LocalStorageContext())
                    {
                        var ink = db.Memes.Remove(db.Memes.Find(inkId));
                        db.SaveChanges();

                        ReLoadNotes();
                    }

                    break;
            }
        }

        private void MenuFlyout_Opening(object sender, object e)
        {
            MenuFlyout senderAsMenuFlyout = sender as MenuFlyout;

            foreach (object menuFlyoutItem in senderAsMenuFlyout.Items)
            {
                if (menuFlyoutItem.GetType() == typeof(MenuFlyoutItem))
                {
                    // Associate the particular FeedItem with the menu flyout (so the MenuFlyoutItem knows which FeedItem to act upon)
                    ListViewItem itemContainer = senderAsMenuFlyout.Target as ListViewItem;

                    var data = notesListView.ItemFromContainer(itemContainer);

                    (menuFlyoutItem as MenuFlyoutItem).CommandParameter = data;
                }
            }
        }

        private void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            ReLoadNotes();
        }
    }
}
