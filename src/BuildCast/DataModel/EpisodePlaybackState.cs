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

namespace BuildCast.DataModel
{
    public class EpisodePlaybackState
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EpisodePlaybackState"/> class.
        /// Public Constructor required by EF
        /// </summary>
        public EpisodePlaybackState()
        {
        }

        public EpisodePlaybackState(Episode episode)
        {
            this.Id = Guid.NewGuid();
            this.EpisodeKey = episode?.Key;
        }

        public Guid Id { get; set; }

        public string EpisodeKey { get; set; }

        public double ListenProgress { get; set; }

        public double GetPercentDouble (Episode e)
        {
            return (ListenProgress / e.Duration.TotalMilliseconds) * 100;
        }

        public string GetPercent(Episode e)
        {
            return $"{(int)((ListenProgress / e.Duration.TotalMilliseconds) * 100)}%";
        }
    }
}
