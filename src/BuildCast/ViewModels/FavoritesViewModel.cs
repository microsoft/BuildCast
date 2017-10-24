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

namespace BuildCast.ViewModels
{
    using System.ComponentModel;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using BuildCast.DataModel;
    using BuildCast.Helpers;
    using BuildCast.Services.Navigation;
    using Microsoft.Toolkit.Uwp.Helpers;

    public class FavoritesViewModel : INotifyPropertyChanged
    {
        private INavigationService _navigationService;
        private IQueryable<EpisodeWithState> _favorites;

        public event PropertyChangedEventHandler PropertyChanged;

        public FavoritesViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
        }

        public void DownloadEpisode(Episode episode)
        {
            var task = BackgroundDownloadHelper.Download(new System.Uri(episode.Key));
        }

        public async void RemoveDownloadedEpisode(Episode episode)
        {
            if (episode != null)
            {
                await episode.DeleteDownloaded();
                await LoadFavorites();
            }
        }

        public async void RemoveFavoritedEpisode(Episode episode)
        {
            using (var db = new LocalStorageContext())
            {
                foreach (Favorite favEntity in db.Favorites)
                {
                    if (episode == null)
                    {
                        break;
                    }

                    if (favEntity.EpisodeId == episode.Id)
                    {
                        db.Favorites.Remove(favEntity);
                        break;
                    }
                }

                db.SaveChanges();
            }

            await LoadFavorites();
        }

        public async void Refresh()
        {
            await LoadFavorites();
        }

        public IQueryable<EpisodeWithState> Favorites
        {
            get
            {
                return _favorites;
            }

            private set
            {
                if (value != _favorites)
                {
                    _favorites = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Favorites)));
                }
            }
        }

        public Task NavigateToEpisodeAsync(Episode episode) => _navigationService.NavigateToPlayerAsync(episode);

        internal async Task LoadFavorites()
        {
            await Task.Run(async () =>
            {
                using (var db = new LocalStorageContext())
                {
                    await DispatcherHelper.ExecuteOnUIThreadAsync(() =>
                    {
                        BuildFavorites(db);
                    });
                }
            });
        }

        private void BuildFavorites(LocalStorageContext db)
        {
            var results2 = from fav in db.Favorites
                           join eps in db.EpisodeCache
                           on fav.EpisodeId equals eps.Id
                           join state in db.PlaybackState
                           on eps.Key equals state.EpisodeKey into myJoin
                           from sub in myJoin.DefaultIfEmpty()
                           select new EpisodeWithState { Episode = eps, PlaybackState = sub ?? new EpisodePlaybackState() };

            Favorites = results2;
        }
    }
}
