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

namespace BuildCast.DataModel
{
    public class InkNote
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InkNote"/> class.
        /// Public Constructor required by EF
        /// </summary>
        public InkNote()
        {
        }

        public InkNote(string episodeKey, double time)
        {
            Id = Guid.NewGuid();
            this.EpisodeKey = episodeKey;
            this.Time = time;
        }

        public bool HasInk { get; set; }

        public string EpisodeKey { get; set; }

        public byte[] Thumbnail { get; set; }

        public double Time { get; internal set; }

        public string NoteText { get; internal set; }

        public Guid Id { get; set; }

        internal Episode GetEpisode()
        {
            using (var db = new LocalStorageContext())
            {
                return db.EpisodeCache.Where(ep => ep.Key == this.EpisodeKey).FirstOrDefault();
            }
        }
    }
}
