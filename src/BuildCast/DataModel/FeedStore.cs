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

namespace BuildCast.DataModel
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using BuildCast.Helpers;

    public class FeedStore
    {
        private static List<Feed> _allFeeds = new List<Feed>();

        static FeedStore()
        {
            AddFeeds();
        }

        public static List<Feed> AllFeeds { get => _allFeeds; }

        public static async Task CheckDownloadsPresent()
        {
            await Task.Run(async () =>
            {
                try
                {
                    using (var db = new LocalStorageContext())
                    {
                        var results2 = from eps in db.EpisodeCache
                                       join state in db.PlaybackState
                                       on eps.Key equals state.EpisodeKey into myJoin
                                       from sub in myJoin.DefaultIfEmpty()
                                       where eps.IsDownloaded == true
                                       select new EpisodeWithState { Episode = eps, PlaybackState = sub ?? new EpisodePlaybackState() };

                        foreach (var item in results2)
                        {
                            if ((await BackgroundDownloadHelper.CheckLocalFileExistsFromUriHash(new Uri(item.Episode.Key))) == null
                            && item.Episode.IsDownloaded)
                            {
                                // Item is flagged as downloaded but isn't in the local cache hence update db
                                Debug.WriteLine($"Episode {item.Episode.Title} is flagged as downloaded but file not present");
                                item.Episode.IsDownloaded = false;
                            }
                        }
                        await db.SaveChangesAsync();

                        await ScanDownloads();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Download scan failed with error {ex.Message}");
                    throw ex;
                }
            });
        }

        private static async Task ScanDownloads()
        {
            using (var db = new LocalStorageContext())
            {
                var items = await BackgroundDownloadHelper.GetAllFiles();
                foreach (var item in items)
                {
                    var found = db.EpisodeCache.Where(e => e.LocalFileName == item.Name).FirstOrDefault();
                    if (found != null && found.IsDownloaded == false)
                    {
                        found.IsDownloaded = true;
                    }
                }

                await db.SaveChangesAsync();
            }
        }

        private static void AddFeeds()
        {
            // Developer
            AllFeeds.Add(new Feed(new Uri("https://s.ch9.ms/Events/Ch9Live/Windows-Community-Standup/RSS"), "Windows Community Standup  Sessions", "Sessions for Windows Community Standup", new Uri("https://sec.ch9.ms/content/feedimage.png"), "Microsoft"));
            AllFeeds.Add(new Feed(new Uri("https://s.ch9.ms/Events/Build/2017/RSS"), "Build 2017 Sessions", "Sessions for Build 2017", new Uri("https://f.ch9.ms/thumbnail/c2635543-13a9-4082-892a-1da55baa9bce.png"), "Microsoft"));
            AllFeeds.Add(new Feed(new Uri("https://s.ch9.ms/Feeds/RSS"), "Channel9", "Channel 9 keeps you up to date with the latest news and behind the scenes info from Microsoft that developers love to keep up with. From LINQ to SilverLight – Watch videos and hear about all the cool technologies coming and the people behind them.", new Uri("https://sec.ch9.ms/content/feedimage.png"), "Microsoft"));
            AllFeeds.Add(new Feed(new Uri("https://channel9.msdn.com/Shows/msdevshow/feed/mp4high"), "MS Dev Show - Channel 9", "A NEW podcast for Microsoft developers covering topics such as Azure/cloud, Windows, Windows Phone, .NET, Visual Studio, and more! Hosted by Jason Young and Carl Schweitzer. Check out the full episode archive at http://msdevshow.com. ", new Uri("http://files.channel9.msdn.com/thumbnail/e35f4617-32e1-4f71-a4f3-7691de443952.png"), "Microsoft"));
            AllFeeds.Add(new Feed(new Uri("https://s.ch9.ms/Shows/XamarinShow/feed"), "The Xamarin Show  - Channel 9", "The Xamarin Show is all about native cross-platform mobile development for iOS, Android, macOS, and Windows with Xamarin. Join your host James Montemagno and his guests as they discuss building mobiles apps, integrating SDKs, extending mobile apps, the latest Xamarin news, awesome apps developers are building, and so much more. Follow @JamesMontemagno and send him topics with #XamarinShow of what and who you would like to see on the show.", new Uri("https://f.ch9.ms/thumbnail/64fd4835-d6d3-4004-89ea-3f31b43b5dcf.png"), "Microsoft"));
            AllFeeds.Add(new Feed(new Uri("http://feeds.twit.tv/ww_video_hd.xml"), "Windows Weekly", "A weekly look at all things Microsoft, including Windows, Windows Phone, Office, Xbox, and more, from two of the foremost Windows watchers in the world, Paul Thurrott of Thurrott.com and Mary Jo Foley of All About Microsoft.\n\nRecords live every Wednesday at 2:00pm Eastern / 11:00am Pacific/ 18:00 UTC.", new Uri("https://elroycdn.twit.tv/sites/default/files/styles/twit_album_art_2048x2048/public/images/shows/windows_weekly/album_art/sd/ww1400videohi.jpg?itok=GHeTXWKP"), "TwIT"));
            AllFeeds.Add(new Feed(new Uri("https://channel9.msdn.com/Shows/cloud+cover/feed/mp4high"), "Microsoft Azure Cloud Cover Show (HD) - Channel 9", "Microsoft Azure Cloud Cover is your eye on the Microsoft Cloud. Join Chris Risner and Thiago Almeida as they cover Microsoft Azure, demonstrate features, discuss the latest news &amp;#43; announcements, and share tips and tricks.", new Uri("https://f.ch9.ms/thumbnail/19ef0544-3f18-4e11-8379-0326d8e493b9.png"), "Microsoft"));
            AllFeeds.Add(new Feed(new Uri("https://channel9.msdn.com/Blogs/One-Dev-Minute/feed"), "One-Dev-minute", "These short videos - usually 1-3 minutes - give developers a quick look at different Windows technologies and how to use them to build great apps.", new Uri("https://f.ch9.ms/thumbnail/3e1f59af-78b2-41ea-adcd-30856ed4c756.png"), "Microsoft"));
            AllFeeds.Add(new Feed(new Uri("https://channel9.msdn.com/Shows/GALs/feed/mp4high"), "GALs", "GALs is a show about the women who work in Tech (at Microsoft or outside) from three ladies that currently work on the Channel 9 team. Golnaz Alibeigi, Soumow Atitallah, and Kaitlin McKinnon have started a new series featuring women in Tech who work in development, management, marketing and research who have interesting stories to share about their success in the industry and ideas on how to grow diversity in IT.", new Uri("https://f.ch9.ms/thumbnail/37d388be-13a3-48f0-9063-5deddcec05d4.jpg"), "Microsoft"));
            AllFeeds.Add(new Feed(new Uri("https://channel9.msdn.com/Shows/C9-goingnative/feed/mp4high"), "Going Native", "C9::GoingNative is a show dedicated to native development with an emphasis on C&amp;#43;&amp;#43; and C&amp;#43;&amp;#43; developers. Each episode will have a segment including an interview with a native dev in his/her native habitat (office) where we&#39;ll talk about what they do and how they use native code and associated toolchains, as well as get their insights and wisdom—geek out. There will be a small news component or segment, but the show will primarily focus on technical tips and conversations with active C/C&amp;#43;&amp;#43; coders, demonstrations of new core language features, libraries, compilers, toolchains, etc. We will bring in guests from around the industry for conversations, tutorials, and demos. As we progress, we will also have segments on other native languages (C, D, Go, etc...). It&#39;s all native all the time. You, our viewers, fly first class. We&#39;ll deliver what you want to see. That&#39;s how it works. Go native! ---&amp;gt; Please follow us at @C9GoingNative on Twitter!", new Uri("http://files.channel9.msdn.com/itunesimage/0593d56e-a15c-4666-b229-f2250bfdd485.png"), "Microsoft"));
            AllFeeds.Add(new Feed(new Uri("https://s.ch9.ms/Events/Build/2016/RSS"), "Build 2016 Sessions", "Sessions for Build 2016", new Uri("http://files.channel9.msdn.com/thumbnail/dc1f8b69-a4a3-422c-b7a7-b29a9183bc79.jpg"), "Microsoft"));
        }
    }
}
