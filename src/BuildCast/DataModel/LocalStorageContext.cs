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
using System.Threading.Tasks;
using BuildCast.Helpers;
using Microsoft.EntityFrameworkCore;
using Windows.Storage;

namespace BuildCast.DataModel
{
    // documnetation is here: https://docs.microsoft.com/en-us/ef/core/get-started/uwp/getting-started
    // under package manager console, use Add-Migration <nameofchange> every time this class updated
    public class LocalStorageContext : DbContext
    {
        private static AsyncInitilizer<LocalStorageContext> _initializer = new AsyncInitilizer<LocalStorageContext>();
        private DbSet<Favorite> _favorites;
        private DbSet<Episode> _episodeCache;
        private DbSet<InkNote> _memes;
        private DbSet<InkNoteData> _inkNotes;
        private DbSet<EpisodePlaybackState> _playbackState;

        static LocalStorageContext()
        {
            _initializer.InitializeWith(CheckForDatabase);
        }

        public DbSet<Favorite> Favorites
        {
            get
            {
                _initializer.CheckInitialized();
                return _favorites;
            }

            set
            {
                _favorites = value;
            }
        }

        public DbSet<Episode> EpisodeCache
        {
            get
            {
                _initializer.CheckInitialized();
                return _episodeCache;
            }

            set
            {
                _episodeCache = value;
            }
        }

        public DbSet<InkNote> Memes
        {
            get
            {
                _initializer.CheckInitialized();
                return _memes;
            }

            set
            {
                _memes = value;
            }
        }

        public DbSet<InkNoteData> MemeData
        {
            get
            {
                _initializer.CheckInitialized();
                return _inkNotes;
            }

            set
            {
                _inkNotes = value;
            }
        }

        public DbSet<EpisodePlaybackState> PlaybackState
        {
            get
            {
                _initializer.CheckInitialized();
                return _playbackState;
            }

            set
            {
                _playbackState = value;
            }
        }

        public static void CheckMigrations()
        {
            _initializer.CheckInitialized();
            using (var db = new LocalStorageContext())
            {
                db.Database.Migrate();
            }
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=buildcast.db");
        }

        private static async Task CheckForDatabase()
        {
            var mainDbFileName = "buildcast.db";
            var mainDbAssetPath = $"ms-appx:///Assets/{mainDbFileName}";

            var data = Windows.Storage.ApplicationData.Current.LocalFolder;

            var exists = await data.TryGetItemAsync(mainDbFileName);

            if (exists == null)
            {
                var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri(mainDbAssetPath)).AsTask().ConfigureAwait(false);
                var database = await file.CopyAsync(data).AsTask().ConfigureAwait(false);
            }
        }
    }
}
