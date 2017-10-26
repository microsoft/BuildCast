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
using System.Linq;
using System.Threading.Tasks;

namespace BuildCast.DataModel
{
    public class NowPlayingState
    {
        private static Feed _currentFeed;
        private static Episode _currentEpisode;
        private static TimeSpan _currentTime;

        public Episode CurrentEpisode { get => _currentEpisode; set => _currentEpisode = value; }

        public Feed CurrentFeed { get => _currentFeed; set => _currentFeed = value; }

        public TimeSpan CurrentTime { get => _currentTime; set => _currentTime = value; }

        public bool HasItem
        {
            get
            {
                return _currentEpisode != null;
            }
        }

        public async Task LoadStateAsync()
        {
            await Task.Run(() =>
            {
                DePersistLastPlayed(out Feed feed, out Episode episode, out TimeSpan position);
                this.CurrentFeed = feed;
                this.CurrentEpisode = episode;
            });
        }

        public async Task<Tuple<bool, bool>> HandlePlayRequest(Episode paramItem)
        {
            var shouldSwitch = ShouldSwitch(paramItem);

            if (shouldSwitch)
            {
                _currentEpisode = paramItem;
                _currentFeed = _currentEpisode?.Feed;
                _currentTime = TimeSpan.Zero;
            }

            if (_currentFeed != null && _currentEpisode != null)
            {
                await LoadEpisodePlaybackState();
                PersistLastPlayed(_currentFeed, _currentEpisode, _currentTime);
            }

            return new Tuple<bool, bool>(shouldSwitch, paramItem != null);
        }

        public bool HandlePlayRequest(InkNote paramItem)
        {
            Episode newEpisode = null;
            using (LocalStorageContext lsc = new LocalStorageContext())
            {
                newEpisode = lsc.EpisodeCache.Where(ep => ep.Key == paramItem.EpisodeKey).FirstOrDefault();
            }

            var shouldSwitch = ShouldSwitch(newEpisode);

            if (shouldSwitch)
            {
                _currentEpisode = newEpisode;
                _currentFeed = _currentEpisode?.Feed;
                _currentTime = TimeSpan.Zero;
            }

            _currentTime = TimeSpan.FromMilliseconds(paramItem.Time);
            if (_currentFeed != null)
            {
                PersistLastPlayed(_currentFeed, _currentEpisode, _currentTime);
            }

            return paramItem != null;
        }

        public bool ShouldSwitch(Episode parameter)
        {
            if (parameter == null)
            {
                return false;
            }

            if (_currentFeed == null || (_currentFeed != null && string.Compare(parameter.Key, _currentEpisode?.Key, StringComparison.Ordinal) != 0))
            {
                return true;
            }

            return false;
        }

        public void SetNowPlaying(Feed feed, Episode feedItem, TimeSpan currenttime)
        {
            _currentFeed = feed;
            _currentEpisode = feedItem;
            _currentTime = currenttime;
        }

        public async Task PeristEpisodePlaybackState()
        {
            using (LocalStorageContext lsc = new LocalStorageContext())
            {
                EpisodePlaybackState state = lsc.PlaybackState.Where(i => i.EpisodeKey == _currentEpisode.Key).FirstOrDefault();

                if (state == null)
                {
                    state = new EpisodePlaybackState(this.CurrentEpisode);
                    lsc.PlaybackState.Add(state);
                }
                else
                {
                    state.ListenProgress = this.CurrentTime.TotalMilliseconds;
                    lsc.PlaybackState.Update(state);
                }

                await lsc.SaveChangesAsync();
            }
        }

        public async Task LoadEpisodePlaybackState()
        {
            await Task.Run(async () =>
            {
                using (LocalStorageContext lsc = new LocalStorageContext())
                {
                    EpisodePlaybackState state = null;
                    if (_currentEpisode != null)
                    {
                        state = lsc.PlaybackState.Where(i => i.EpisodeKey == CurrentEpisode.Key).FirstOrDefault();

                        if (state == null)
                        {
                            EpisodePlaybackState eps = new EpisodePlaybackState(CurrentEpisode);
                            lsc.PlaybackState.Add(eps);
                            await lsc.SaveChangesAsync();
                        }
                        else
                        {
                            CurrentTime = TimeSpan.FromMilliseconds(state.ListenProgress);
                        }
                    }
                }
            });
        }

        private void PersistLastPlayed(Feed feed, Episode episode, TimeSpan timespan)
        {
            var settings = Windows.Storage.ApplicationData.Current.LocalSettings;

            var composite = new Windows.Storage.ApplicationDataCompositeValue();
            composite["feedUri"] = feed.Uri.ToString();
            composite["feedTitle"] = feed.Title;
            composite["feedDescription"] = feed.Description;
            composite["feedImageUri"] = feed.ImageUri.ToString();
            composite["feedAuthor"] = feed.Author;
            composite["episodeUri"] = episode.Key.ToString();

            settings.Values["currentState"] = composite;
        }

        private void DePersistLastPlayed(out Feed feed, out Episode episode, out TimeSpan timespan)
        {
            var settings = Windows.Storage.ApplicationData.Current.LocalSettings;

            var composite = (Windows.Storage.ApplicationDataCompositeValue)settings.Values["currentState"];

            if (composite != null)
            {
                feed = new Feed(
                    feedUri: new Uri((string)composite["feedUri"]),
                    title: (string)composite["feedTitle"],
                    description: (string)composite["feedDescription"],
                    imageUri: new Uri((string)composite["feedImageUri"]),
                    author: (string)composite["feedAuthor"]);

                var uri = (string)composite["episodeUri"];
                using (LocalStorageContext lcs = new LocalStorageContext())
                {
                    episode = lcs.EpisodeCache.Where(e => e.Key == uri).FirstOrDefault();
                }
            }
            else
            {
                feed = null;
                episode = null;
                timespan = TimeSpan.MinValue;
            }
        }
    }
}
