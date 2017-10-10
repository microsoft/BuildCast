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
using System.Windows.Input;
using BuildCast.DataModel;

namespace BuildCast.Helpers
{
    public class FavoriteCommand : ICommand
    {
#pragma warning disable CS0067
        public event EventHandler CanExecuteChanged;
#pragma warning restore CS0067

        public bool CanExecute(object parameter) => true;

        public void Execute(object parameter)
        {
            if (parameter == null || (parameter as Episode) == null)
            {
                return;
            }

            using (var db = new LocalStorageContext())
            {
                db.Favorites.Add(new Favorite(parameter as Episode));
                db.SaveChanges();
            }
        }
    }

    public class DeleteEpisodeNoteCommand : ICommand
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteEpisodeNoteCommand"/> class.
        /// </summary>
        public DeleteEpisodeNoteCommand()
        {
        }

#pragma warning disable CS0067
        public event EventHandler CanExecuteChanged;
#pragma warning restore CS0067

        public bool CanExecute(object parameter) => true;

        public void Execute(object parameter)
        {
            var noteParameter = parameter as dynamic;

            if (noteParameter == null)
            {
                return;
            }

            System.Guid noteId = noteParameter?.InkId;

            switch (noteParameter.Type)
            {
                case "Ink":
                    using (var db = new LocalStorageContext())
                    {
                        var ink = db.Memes.Remove(db.Memes.Find(noteId));
                        db.SaveChanges();
                    }

                    break;
                case "Bookmark":
                    using (var db = new LocalStorageContext())
                    {
                        var ink = db.Memes.Remove(db.Memes.Find(noteId));
                        db.SaveChanges();
                    }

                    break;
            }
        }
    }

    public class DeleteEpisodeCommand : ICommand
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteEpisodeCommand"/> class.
        /// </summary>
        public DeleteEpisodeCommand()
        {
        }

#pragma warning disable CS0067
        public event EventHandler CanExecuteChanged;

#pragma warning restore CS0067
        public bool CanExecute(object parameter) => true;

        public async void Execute(object parameter)
        {
            var episodeWithState = parameter as EpisodeWithState;
            Episode episode = episodeWithState?.Episode;

            if (episode == null)
            {
                episode = parameter as Episode;
            }

            if (episode == null)
            {
                return;
            }

            if (episode != null)
            {
                await episode.DeleteDownloaded();
            }
        }
    }

    public class UnfavoriteCommand : ICommand
    {
#pragma warning disable CS0067
        public event EventHandler CanExecuteChanged;
#pragma warning restore CS0067
        public UnfavoriteCommand()
        {
        }

        public bool CanExecute(object parameter) => true;

        public void Execute(object parameter)
        {
            var episodeWithState = parameter as EpisodeWithState;
            Episode episode = episodeWithState?.Episode;

            if (episode == null)
            {
                episode = parameter as Episode;
            }

            if (episode == null)
            {
                return;
            }

            using (var db = new LocalStorageContext())
            {
                foreach (Favorite favEntity in db.Favorites)
                {
                    if (favEntity.EpisodeId == episode.Id)
                    {
                        db.Favorites.Remove(favEntity);
                        break;
                    }
                }

                db.SaveChanges();
            }
        }
    }

    public class DownloadCommand : ICommand
    {
        public DownloadCommand()
        {
        }

#pragma warning disable CS0067
        public event EventHandler CanExecuteChanged;
#pragma warning restore CS0067

        public bool CanExecute(object parameter) => true;

        public void Execute(object parameter)
        {
            var episodeWithState = parameter as EpisodeWithState;
            Episode episode = episodeWithState?.Episode;

            if (episode == null)
            {
                episode = parameter as Episode;
            }

            if (episode == null)
            {
                return;
            }

            var task = BackgroundDownloadHelper.Download(new Uri(episode.Key));
            task.ContinueWith(async (state) =>
            {
                if (state.Result == DownloadStartResult.AllreadyDownloaded)
                {
                    await episode.SetDownloaded();
                }
            });

            System.Diagnostics.Debug.WriteLine("Downloading episode...");
        }
    }

    public class BindingProxyCommand : ICommand
    {
        private ICommand _realCommand;

        public event EventHandler CanExecuteChanged;

        public ICommand RealCommand
        {
            get => _realCommand;
            set
            {
                _realCommand = value;
                _realCommand.CanExecuteChanged += OnRealCanExecute;
            }
        }

        public bool CanExecute(object parameter) => _realCommand?.CanExecute(parameter) ?? false;

        public void Execute(object parameter) => _realCommand.Execute(parameter);

        private void OnRealCanExecute(object sender, EventArgs e) => CanExecuteChanged?.Invoke(sender, e);
    }
}
