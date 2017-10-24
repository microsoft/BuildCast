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
using BuildCast.Helpers;
using Windows.Foundation.Collections;
using System.Diagnostics;

namespace BuildCast.DataModel
{
    public class Episode
    {
        private string _uri;
        private string _localFilename;
        private string _title;
        private string _description;
        private string _subtitle;
        private string _itemThumbnail;
        private Feed _feed;
        private DateTimeOffset _publishDate;
        private string _formattedPublishDate;
        private TimeSpan _duration;

        /// <summary>
        /// Initializes a new instance of the <see cref="Episode"/> class.
        /// Public Constructor required by EF
        /// </summary>
        public Episode()
        {
        }

        public Episode(
            string key,
            string title,
            string description,
            string itemThumbnail,
            DateTimeOffset publishDate,
            TimeSpan duration,
            string subtitle,
            Feed feed = null,
            string feedId = "")
        {
            this.Id = Guid.NewGuid();
            this.Key = key;
            this.LocalFileName = BackgroundDownloadHelper.SafeHashUri(new Uri(this.Key));
            this._title = title;
            this._description = description;
            this._itemThumbnail = itemThumbnail;

            this._publishDate = publishDate;
            this._duration = duration;
            this.Subtitle = subtitle;

            if (feed != null)
            {
                this._feed = feed;
                this.FeedId = feed?.Uri?.ToString();
            }
            else if (!string.IsNullOrEmpty(feedId))
            {
                this.FeedId = feedId;
            }
        }

        public Episode(string key, string title, string description, string itemThumbnail)
            : this(
            key,
            title,
            description,
            itemThumbnail,
            DateTimeOffset.MinValue,
            TimeSpan.MinValue,
            string.Empty)
        {
        }

        public Guid Id { get; set; }

        public string Key
        {
            get
            {
                return _uri;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentException("Key cannot be null");
                }

                _uri = value;
                LocalFileName = BackgroundDownloadHelper.SafeHashUri(new Uri(_uri));
            }
        }

        public string LocalFileName { get => _localFilename; set => _localFilename = value; }

        public string Title { get => _title; set => _title = value; }

        public string Description { get => _description; set => _description = value; }

        public string ItemThumbnail { get => _itemThumbnail; set => _itemThumbnail = value; }

        public bool IsDownloaded { get; set; }

        public string FeedId
        {
            get; set;
        }

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public Feed Feed
        {
            get
            {
                if (_feed == null)
                {
                    _feed = GetFeed();
                }

                return _feed;
            }

            set => _feed = value;
        }

        public DateTimeOffset PublishDate { get => _publishDate; set => _publishDate = value; }
        
        public string FormatPublishDate (Episode e)
        {
            string formattedDate = e.PublishDate.Month.ToString() + "/" + e.PublishDate.Day.ToString() + "/" + e.PublishDate.Year.ToString();
            return formattedDate;
        }

        public TimeSpan Duration { get => _duration; set => _duration = value; }

        public string Subtitle { get => _subtitle; set => _subtitle = value; }

        public override string ToString()
        {
            return _title ?? string.Empty;
        }

        public async Task SetDownloaded()
        {
            using (LocalStorageContext lsc = new LocalStorageContext())
            {
                lsc.Update(this);
                this.IsDownloaded = true;
                await lsc.SaveChangesAsync();
            }
        }

        public async Task DeleteDownloaded()
        {
            if (IsDownloaded)
            {
                using (LocalStorageContext lsc = new LocalStorageContext())
                {
                    lsc.Update(this);
                    var localFileName = LocalFileName;
                    LocalFileName = null;
                    IsDownloaded = false;

                    await BackgroundDownloadHelper.DeleteDownload(localFileName);
                    await lsc.SaveChangesAsync();
                }
            }
        }

        internal static Episode BuildFromValueSet(ValueSet values)
        {
            Episode fi = new Episode(
                key: values.GetString(nameof(Key)),
                title: values.GetString(nameof(Title)),
                description: values.GetString(nameof(Description)),
                itemThumbnail: values.GetString(nameof(ItemThumbnail)),
                publishDate: values.GetDateTimeOffset(nameof(PublishDate)),
                duration: values.GetTimeSpan(nameof(Duration)),
                subtitle: values.GetString(nameof(Subtitle)),
                feedId: values.GetString(nameof(FeedId)));

            return fi;
        }

        internal void AddToValueSet(ValueSet values)
        {
            ValueSet feedItem = new ValueSet();
            values.Add("feeditem", feedItem);
            feedItem.Add(nameof(Id), Id.ToString());
            feedItem.Add(nameof(Key), Key.ToString());
            feedItem.Add(nameof(Title), Title);
            feedItem.Add(nameof(Description), Description);
            feedItem.Add(nameof(ItemThumbnail), ItemThumbnail);
            feedItem.Add(nameof(FeedId), FeedId);
            feedItem.Add(nameof(PublishDate), PublishDate.ToUnixTimeMilliseconds());
            feedItem.Add(nameof(Duration), Duration.TotalMilliseconds);
            feedItem.Add(nameof(Subtitle), Subtitle);
        }

        private Feed GetFeed()
        {
            return FeedStore.AllFeeds.Where(l => l.Uri.OriginalString == this.FeedId).FirstOrDefault();
        }
    }
}