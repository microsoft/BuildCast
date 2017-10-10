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
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;

namespace BuildCast.DataModel
{
    public class InkNoteData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InkNoteData"/> class.
        /// Public constructor required by EF
        /// </summary>
        public InkNoteData()
        {
        }

        public Guid InkMeme { get; set; }

        public byte[] Ink { get; set; }

        public byte[] ImageBytes { get; set; }

        public Guid Id { get; set; }

        public async Task<BitmapImage> GetImage(Action onOpened)
        {
            return await GetImage(this.ImageBytes, onOpened);
        }

        public async Task<InMemoryRandomAccessStream> GetImageStream()
        {
            return await GetImageStream(this.ImageBytes);
        }

        private static async Task<InMemoryRandomAccessStream> GetImageStream(byte[] imageBytes)
        {
            InMemoryRandomAccessStream randomAccessStream = new InMemoryRandomAccessStream();
            await RandomAccessStream.CopyAsync(imageBytes.AsBuffer().AsStream().AsRandomAccessStream(), randomAccessStream);
            randomAccessStream.Seek(0);
            return randomAccessStream;
        }

        private async Task<BitmapImage> GetImage(byte[] imageBytes, Action onOpened)
        {
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.ImageOpened += (s, e) => { onOpened(); };
            InMemoryRandomAccessStream randomAccessStream = await GetImageStream(imageBytes);
            bitmapImage.SetSource(randomAccessStream);
            return bitmapImage;
        }
    }
}