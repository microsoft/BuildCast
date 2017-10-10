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
    public class Favorite
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Favorite"/> class.
        /// Public Constructor required by EF
        /// </summary>
        public Favorite()
        {
        }

        public Favorite(Episode item)
        {
            this.Id = Guid.NewGuid();
            this.EpisodeId = item.Id;
        }

        public Guid Id { get; set; }

        public Guid EpisodeId { get; set; }

        internal Episode GetEpisode()
        {
            using (var db = new LocalStorageContext())
            {
                return db.EpisodeCache.Where(ep => ep.Id == this.EpisodeId).FirstOrDefault();
            }
        }
    }
}
