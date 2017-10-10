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
using BuildCast.DataModel;

namespace BuildCast.Services
{
    public interface IPlayerService
    {
        NowPlayingState NowPlaying { get; }

        Task SetNewItem(Uri currentItem, TimeSpan currentTime, bool startPlayback, string localFilename);

        void SetTime(TimeSpan currentTime);

        Task Play(Episode episode, TimeSpan playTime);

        void Pause();

        void FastForward();

        void Rewind();

        Task<byte[]> GetBitmapForCurrentFrameFromLocalFile();
    }
}