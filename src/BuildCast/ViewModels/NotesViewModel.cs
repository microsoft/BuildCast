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
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Threading.Tasks;
    using BuildCast.DataModel;
    using BuildCast.Helpers;
    using BuildCast.Services.Navigation;
    using Microsoft.Toolkit.Uwp.Helpers;

    public class NotesViewModel : INotifyPropertyChanged
    {
        private INavigationService _navigationService;
        private IEnumerable<IGrouping<string, dynamic>> _notes;

        public NotesViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public IEnumerable<IGrouping<string, dynamic>> Notes
        {
            get
            {
                return _notes;
            }

            private set
            {
                if (value != _notes)
                {
                    _notes = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Notes)));
                }
            }
        }

        public async void ReloadNotes()
        {
            await LoadNotes();
        }

        public async Task LoadNotes()
        {
            await Task.Run(async () =>
            {
                using (var db = new LocalStorageContext())
                {
                    await DispatcherHelper.ExecuteOnUIThreadAsync(() =>
                    {
                        BuildInkNotesForEpisode(db);
                    });
                }
            });
        }

        public void NavigateToItem(object clickedItem)
        {
            if (clickedItem is EpisodeWithState)
            {
                NavigateToEpisode((clickedItem as EpisodeWithState).Episode);
                return;
            }

            dynamic output = clickedItem;
            Guid inkId = output.InkId;

            switch (output.Type)
            {
                case "Ink":
                    using (var db = new LocalStorageContext())
                    {
                        var ink = db.Memes.Where(m => m.Id == inkId).FirstOrDefault();
                        NavigateToInkNote(ink);
                    }

                    break;
                case "Bookmark":
                    using (var db = new LocalStorageContext())
                    {
                        var ink = db.Memes.Where(m => m.Id == inkId).FirstOrDefault();
                        NavigateToPlayerWithInk(ink);
                    }

                    break;
            }
        }

        private void NavigateToEpisode(Episode episode)
        {
            var ignored = _navigationService.NavigateToPlayerAsync(episode);
        }

        private void NavigateToInkNote(InkNote ink)
        {
            var ignored = _navigationService.NavigateToInkNoteAsync(ink);
        }

        private void NavigateToPlayerWithInk(InkNote ink)
        {
            var ignored = _navigationService.NavigateToPlayerAsync(ink);
        }

        private void BuildInkNotesForEpisode(LocalStorageContext db)
        {
            var results = (from inks in db.Memes
                           join eps in db.EpisodeCache
                           on inks.EpisodeKey equals eps.Key
                           select new { InkId = inks.Id, Title = eps.Title, Time = TimeSpan.FromMilliseconds(inks.Time), Type = inks.HasInk ? "Ink" : "Bookmark", Episode = eps, NoteText=inks.NoteText }).AsEnumerable().LambdaDistinct((a, b) => a.InkId == b.InkId).OrderBy(ob => ob.Time);

            var projected = from c in results group c by c.Title;
            IEnumerable<IGrouping<string, dynamic>> ps = projected;
            Notes = ps;
        }
    }
}
