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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using BuildCast.Helpers;
using Windows.ApplicationModel;
using Windows.Foundation.Collections;
using Windows.Storage.Streams;
using Windows.Web.Http;

namespace BuildCast.DataModel
{
    public class Feed
    {
        public Feed(Uri feedUri, string title, string description, Uri imageUri, string author)
        {
            Uri = feedUri;
            Title = title;
            Description = description;
            ImageUri = imageUri;
            Author = author;
        }

        public Uri Uri { get; internal set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public string Author { get; set; }

        public Uri ImageUri { get; set; }

        public async Task<List<Episode>> GetEpisodes()
        {
            using (var db = new LocalStorageContext())
            {
                var cached = db.EpisodeCache.Where(i => i.FeedId == this.Uri.ToString())
                    .OrderByDescending(ob => ob.PublishDate);
                if (cached.Count() > 0)
                {
                    foreach (var item in cached)
                    {
                        item.Feed = this;
                    }

                    return cached.ToList();
                }
            }

            return await GetNewEpisodesAsync();
        }

        internal async Task<List<Episode>> GetNewEpisodesAsync()
        {
            List<Episode> newEpisodes = new List<Episode>();
            var results = await GetEpisodesInternalAsync();
            using (var db = new LocalStorageContext())
            {
                foreach (var item in results)
                {
                    if (db.EpisodeCache.Where(i => (i.Key == item.Key && i.Feed == item.Feed)).Count() == 0)
                    {
                        db.EpisodeCache.Add(item);
                        newEpisodes.Add(item);
                    }
                }

                db.SaveChanges();
            }

            return newEpisodes;
        }

        internal async Task RemoveTopThreeItems()
        {
            using (var db = new LocalStorageContext())
            {
                var episodes = await GetEpisodes();
                db.EpisodeCache.Remove(episodes[0]);
                db.EpisodeCache.Remove(episodes[1]);
                db.EpisodeCache.Remove(episodes[2]);
                await db.SaveChangesAsync();
            }
        }

        internal async Task<List<Episode>> GetEpisodesInternalAsync()
        {
            switch (this.Uri.Scheme)
            {
                case "http":
                case "https":
                    return await GetEpisodesInternalFromWebAsync();
                case "local":
                    return await GetEpisodesInternalFromResAsync(this.Uri.DnsSafeHost);
                default:
                    return null;
            }
        }

        internal async Task<List<Episode>> GetEpisodesInternalFromWebAsync()
        {
            HttpClient client = new HttpClient();
            using (var stream = await client.GetInputStreamAsync(this.Uri))
            {
                return ParseRssFeed(stream, this);
            }
        }

        internal async Task<List<Episode>> GetEpisodesInternalFromResAsync(string name)
        {
            var file = await Package.Current.InstalledLocation.GetFileAsync($"Assets\\{name}");

            using (var stream = await file.OpenStreamForReadAsync())
            {
                return ParseRssFeed(stream.AsInputStream(), this);
            }
        }

        internal static List<Episode> ParseRssFeed(IInputStream stream, Feed parent)
        {
            List<Episode> results = new List<Episode>();

            XNamespace ns = "http://purl.org/rss/1.0/";
            XNamespace mrss = "http://search.yahoo.com/mrss/";
            XNamespace itunes = "http://www.itunes.com/dtds/podcast-1.0.dtd";
            var media = mrss.GetName("media");

            XDocument xdoc = XDocument.Load(stream.AsStreamForRead());

            var items = xdoc.Descendants("item");
            foreach (var item in items)
            {
                string thumbUri = string.Empty;
                XAttribute mediadownload;

                var pubDateStr = item.Element("pubDate")?.Value;
                DateTimeOffset pubDate = DateTimeOffset.MinValue;
                if (!string.IsNullOrEmpty(pubDateStr))
                {
                    pubDate = DateTimeHelper.ParseDateTimeRFC822(pubDateStr);
                }

                var durationStr = item.Element(itunes + "duration")?.Value;
                TimeSpan duration = TimeSpan.MinValue;
                if (!string.IsNullOrEmpty(durationStr) && durationStr.Contains(":"))
                {
                    duration = TimeSpan.Parse(durationStr);
                }
#pragma warning disable SA1108 // Block statements must not contain embedded comments
                else if (!string.IsNullOrEmpty(durationStr) && !durationStr.Contains(":")) // channel9
#pragma warning restore SA1108 // Block statements must not contain embedded comments
                {
                    duration = TimeSpan.FromSeconds(Convert.ToInt32(durationStr));
                }

                var subtitleStr = item.Element(itunes + "subtitle")?.Value;

                if (string.IsNullOrEmpty(subtitleStr))
                {
                    var cats = item.Elements("category");
                    if (cats != null && cats.Count() > 0)
                    {
                        StringBuilder sb = new StringBuilder();
                        cats.ToList().ForEach(f => { sb.Append($"{f.Value} "); });
                        subtitleStr = sb.ToString();
                    }
                }

                var desc = item.Element("description");
                var content = item.Elements(mrss + "content").FirstOrDefault();
                if (content != null)
                {
                    mediadownload = content.Attribute("url");
                    var thumbnailElement = content.Element(mrss + "thumbnail");
                    if (thumbnailElement != null)
                    {
                        var thumbnail = thumbnailElement.Attribute("url");
                        thumbUri = thumbnail.Value;
                    }
                }
                else
                {
                    var thumbnailElements = item.Elements(mrss + "thumbnail");
                    XElement thumbElement = null;
                    if (thumbnailElements.Count() >= 4)
                    {
                        thumbElement = thumbnailElements.ElementAt(3);
                    }
                    else if (thumbnailElements.Count() >= 1)
                    {
                        thumbElement = thumbnailElements.ElementAt(0);
                    }

                    if (thumbElement != null)
                    {
                        thumbUri = thumbElement.Attribute("url").Value;
                    }

                    var mediaGroup = item.Elements(mrss + "group");
                    var mediaUriElements = mediaGroup.Elements(mrss + "content");
                    XElement mediaUriElement = null;
                    if (mediaUriElements.Count() >= 4)
                    {
                        mediaUriElement = mediaUriElements.ElementAt(3);
                    }

                    if (mediaUriElements.Count() == 1)
                    {
                        mediaUriElement = mediaUriElements.ElementAt(0);
                    }

                    mediadownload = mediaUriElement?.Attribute("url");
                }

                if (mediadownload == null)
                {
                    var mediadownloadElements = item.Element("enclosure");
                    mediadownload = mediadownloadElements?.Attribute("url");
                }

                if (mediadownload != null)
                {
                    var feed = new Episode(
                               title: item.Element("title").Value,
                               description: desc.Value,
                               key: mediadownload.Value,
                               itemThumbnail: thumbUri,
                               feed: parent,
                               publishDate: pubDate,
                               duration: duration,
                               subtitle: subtitleStr);
                    results.Add(feed);
                }
                else
                {
                    Debug.WriteLine($"Skipping");
                }
            }

            return results;
        }

        internal static Feed BuildFeedFromValueSet(ValueSet values)
        {
            Feed built = new Feed(
                feedUri: values.GetURI(nameof(Uri)),
                title: values.GetString(nameof(Title)),
                description: values.GetString(nameof(Description)),
                author: values.GetString(nameof(Author)),
                imageUri: values.GetURI(nameof(ImageUri)));

            return built;
        }

        internal void AddToValueSet(ValueSet values)
        {
            ValueSet feedvs = new ValueSet();
            values.Add("feed", feedvs);
            feedvs.Add(nameof(Description), Description);
            feedvs.Add(nameof(ImageUri), ImageUri?.ToString());
            feedvs.Add(nameof(Title), Title);
            feedvs.Add(nameof(Uri), Uri?.ToString());
            feedvs.Add(nameof(Author), Author);
        }
    }
}