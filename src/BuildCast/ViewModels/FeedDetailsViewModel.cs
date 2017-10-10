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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using BuildCast.DataModel;
using BuildCast.Helpers;
using BuildCast.Services.Navigation;
using Windows.UI.Xaml.Navigation;

namespace BuildCast.ViewModels
{
    public class FeedDetailsViewModel : INavigableTo, INotifyPropertyChanged
    {
        private INavigationService _navService;
        private bool _loading;

        public event PropertyChangedEventHandler PropertyChanged;

        public Feed CurrentFeed { get; private set; }

        public ObservableCollection<Episode> EpisodeData { get; set; }

        public bool Loading
        {
            get => _loading;
            set
            {
                if (_loading != value)
                {
                    _loading = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Loading)));
                }
            }
        }

        public Episode PersistedEpisode { get; set; }

        public FeedDetailsViewModel(INavigationService navigationService)
        {
            _navService = navigationService;

            Loading = true;
        }

        public async Task NavigatedTo(NavigationMode navigationMode, object parameter)
        {
            Loading = true;

            if (navigationMode != NavigationMode.Back && parameter is Feed feed)
            {
                CurrentFeed = feed;
                EpisodeData = new ObservableCollection<Episode>(await feed.GetEpisodes());
            }

            if (navigationMode != NavigationMode.Back)
            {
                PersistedEpisode = null;
            }

            Loading = false;
        }

        public async Task<int> RefreshData()
        {
            var newEpisodes = await CurrentFeed.GetNewEpisodesAsync();
            foreach (var episode in newEpisodes)
            {
                EpisodeData.Insert(0, episode);
            }

            return newEpisodes.Count;
        }

        public void GoToEpisodeDetails(Episode detailsItem)
        {
            PersistedEpisode = detailsItem;
            _navService.NavigateToEpisodeAsync(detailsItem);
        }

        // TODO: Move these episode specific functions to the Episode themselves, pending further review.
        public void PlayEpisode(Episode episode)
        {
            _navService.NavigateToPlayerAsync(episode);
        }

        public void FavoriteEpisode(Episode episode)
        {
            using (var db = new LocalStorageContext())
            {
                db.Favorites.Add(new Favorite(episode));
                db.SaveChanges();
            }
        }

        public void DownloadEpisode(Episode episode)
        {
            var task = BackgroundDownloadHelper.Download(new Uri(episode.Key));
        }

        public async Task RemoveTopThree()
        {
            await CurrentFeed.RemoveTopThreeItems();
            EpisodeData.RemoveAt(0);
            EpisodeData.RemoveAt(0);
            EpisodeData.RemoveAt(0);
        }
    }
}
