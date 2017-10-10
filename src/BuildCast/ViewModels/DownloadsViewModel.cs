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
    using BuildCast.DataModel;
    using BuildCast.Services.Navigation;
    using Microsoft.Toolkit.Uwp.Helpers;

    public class DownloadsViewModel : INotifyPropertyChanged
    {
        private INavigationService _navigationService;

        private IQueryable<EpisodeWithState> _downloads;

        public event PropertyChangedEventHandler PropertyChanged;

        public DownloadsViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
        }

        public async void RemoveDownloadedEpisode(Episode episode)
        {
            if (episode != null)
            {
                await episode.DeleteDownloaded();
                await LoadDownloads();
            }
        }

        public async void ReloadDownloadList()
        {
            await LoadDownloads();
        }

        public IQueryable<EpisodeWithState> Downloads
        {
            get
            {
                return _downloads;
            }

            private set
            {
                if (value != _downloads)
                {
                    _downloads = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Downloads)));
                }
            }
        }

        public async Task LoadDownloads()
        {
            await Task.Run(async () =>
            {
                using (var db = new LocalStorageContext())
                {
                    await DispatcherHelper.ExecuteOnUIThreadAsync(() =>
                    {
                        BuildDownloads(db);
                    });
                }
            });
        }

        internal void NavigateToEpisode(Episode episode)
        {
            var ignored = _navigationService.NavigateToPlayerAsync(episode);
        }

        internal void NavigateToInkNote(InkNote ink)
        {
            var ignored = _navigationService.NavigateToInkNoteAsync(ink);
        }

        internal void NavigateToPlayerWithInk(InkNote ink)
        {
            var ignored = _navigationService.NavigateToPlayerAsync(ink);
        }

        private void BuildDownloads(LocalStorageContext db)
        {
            var results2 = from eps in db.EpisodeCache
                           join state in db.PlaybackState
                           on eps.Key equals state.EpisodeKey into myJoin
                           from sub in myJoin.DefaultIfEmpty()
                           where eps.IsDownloaded == true
                           select new EpisodeWithState { Episode = eps, PlaybackState = sub ?? new EpisodePlaybackState() };

            Downloads = results2;
        }
    }
}
