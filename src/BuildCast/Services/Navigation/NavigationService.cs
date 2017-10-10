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
using System.Threading.Tasks;
using Autofac;
using BuildCast.DataModel;
using BuildCast.ViewModels;
using BuildCast.Views;
using Microsoft.Toolkit.Uwp.Helpers;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace BuildCast.Services.Navigation
{
    public class NavigationService : INavigationService
    {
        private bool _isNavigating;

        /// <summary>
        /// Initializes a new instance of the <see cref="NavigationService"/> class.
        /// </summary>
        /// <param name="frameAdapter"></param>
        /// <param name="iocResolver"></param>
        public NavigationService(IFrameAdapter frameAdapter, IComponentContext iocResolver)
        {
            Frame = frameAdapter;
            AutofacDepedencyResolver = iocResolver;

            // Investigate a way to put these mappings into the IOC container so that we don't have a hard dependency on the page types for multiplatform
            PageViewModels = new Dictionary<Type, NavigatedToViewModelDelegate>();
            RegisterPageViewModel<Home, HomeViewModel>();
            RegisterPageViewModel<Favorites, FavoritesViewModel>();
            RegisterPageViewModel<Notes, NotesViewModel>();
            RegisterPageViewModel<Downloads, DownloadsViewModel>();
            RegisterPageViewModel<Player, PlayerViewModel>();
            RegisterPageViewModel<SettingsPage, SettingsViewModel>();
            RegisterPageViewModel<FeedDetails, FeedDetailsViewModel>();
            RegisterPageViewModel<EpisodeDetails, EpisodeDetailsViewModel>();
            RegisterPageViewModel<Player, PlayerViewModel>();
            RegisterPageViewModel<Views.InkNote, InkNoteViewModel>();
            Frame.Navigated += Frame_Navigated;
        }

        public event EventHandler<bool> IsNavigatingChanged;

        public event EventHandler Navigated;

        public Task NavigateToPodcastsAsync() => NavigateToPage<Home>();

        public Task NavigateToFavoritesAsync() => NavigateToPage<Favorites>();

        public Task NavigateToDownloadsAsync() => NavigateToPage<Downloads>();

        public Task NavigateToNotesAsync() => NavigateToPage<Notes>();

        public Task NavigateToNowPlayingAsync() => NavigateToPage<Player>();

        public Task NavigateToSettingsAsync() => NavigateToPage<SettingsPage>();

        public Task NavigateToFeedAsync(Feed feed) => NavigateToPage<FeedDetails>(feed);

        public Task NavigateToEpisodeAsync(Episode episode) => NavigateToPage<EpisodeDetails>(episode);

        public Task NavigateToPlayerAsync(Episode episode) => NavigateToPage<Player>(episode);

        public Task NavigateToPlayerAsync(DataModel.InkNote inkNote) => NavigateToPage<Player>(inkNote);

        public Task NavigateToInkNoteAsync(DataModel.InkNote inkNote) => NavigateToPage<Views.InkNote>(inkNote);

        public bool CanGoBack => Frame.CanGoBack;

        private IComponentContext AutofacDepedencyResolver { get; }

        private IFrameAdapter Frame { get; }

        private delegate Task NavigatedToViewModelDelegate(object page, object parameter, NavigationEventArgs navigationArgs);

        private Dictionary<Type, NavigatedToViewModelDelegate> PageViewModels { get; }

        public bool IsNavigating
        {
            get => _isNavigating;

            set
            {
                if (value != _isNavigating)
                {
                    _isNavigating = value;
                    IsNavigatingChanged?.Invoke(this, _isNavigating);

                    // Check that navigation just finished
                    if (!_isNavigating)
                    {
                        // Navigation finished
                        Navigated?.Invoke(this, EventArgs.Empty);
                    }
                }
            }
        }

        /// <summary>
        /// Navigate in the back direction
        /// </summary>
        /// <returns>A task that can be awaited</returns>
        public async Task GoBackAsync()
        {
            if (Frame.CanGoBack)
            {
                IsNavigating = true;

                Page navigatedPage = await DispatcherHelper.ExecuteOnUIThreadAsync(() =>
                {
                    Frame.GoBack();
                    return Frame.Content as Page;
                });
            }
        }

        /// <summary>
        /// The Navigated event. This event is raised BEFORE <see cref="Windows.UI.Xaml.Controls.Page.OnNavigatedTo(Windows.UI.Xaml.Navigation.NavigationEventArgs)"/>
        /// </summary>
        /// <param name="sender">The frame</param>
        /// <param name="e">The args coming from the frame</param>
        private void Frame_Navigated(object sender, Windows.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            IsNavigating = false;
            if (PageViewModels.ContainsKey(e.SourcePageType))
            {
                var loadViewModelDelegate = PageViewModels[e.SourcePageType];
                var ignoredTask = loadViewModelDelegate(e.Content, e.Parameter, e);
            }
        }

        private void RegisterPageViewModel<TPage, TViewModel>()
            where TViewModel : class
        {
            NavigatedToViewModelDelegate navigatedTo = async (page, parameter, navArgs) =>
            {
                if (page is IPageWithViewModel<TViewModel> pageWithVM)
                {
                    pageWithVM.ViewModel = AutofacDepedencyResolver.Resolve<TViewModel>();

                    if (pageWithVM.ViewModel is INavigableTo navVM)
                    {
                        await navVM.NavigatedTo(navArgs.NavigationMode, parameter);
                    }

                    // Async loading
                    pageWithVM.UpdateBindings();
                }
            };

            PageViewModels[typeof(TPage)] = navigatedTo;
        }

        private Task NavigateToPage<TPage>()
        {
            return NavigateToPage<TPage>(parameter: null);
        }

        private async Task NavigateToPage<TPage>(object parameter)
        {
            // Early out if already in the middle of a Navigation
            if (_isNavigating)
            {
                return;
            }

            _isNavigating = true;

            await DispatcherHelper.ExecuteOnUIThreadAsync(() =>
            {
                Frame.Navigate(typeof(TPage), parameter: parameter);
            });
        }
    }
}
