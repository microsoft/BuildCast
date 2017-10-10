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
using BuildCast.Services;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236
namespace BuildCast.Controls
{
    public sealed partial class RemoteControl : UserControl
    {
        // Using a DependencyProperty as the backing store for RemoteConnection.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty RemoteConnectionProperty =
            DependencyProperty.Register(nameof(RemoteConnection), typeof(IRemoteConnection), typeof(RemoteControl), new PropertyMetadata(null));

        public event EventHandler CloseClicked;

        public IRemoteConnection RemoteConnection
        {
            get { return (IRemoteConnection)GetValue(RemoteConnectionProperty); }
            set { SetValue(RemoteConnectionProperty, value); }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteControl"/> class.
        /// </summary>
        public RemoteControl()
        {
            this.InitializeComponent();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            RemoteConnection?.Disconnect();
            CloseClicked?.Invoke(this, EventArgs.Empty);
        }

        private void Rewind_Click(object sender, RoutedEventArgs e)
        {
            RemoteConnection.Rewind();
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            RemoteConnection.Play();
        }

        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            RemoteConnection.Pause();
        }

        private void FastForward_Click(object sender, RoutedEventArgs e)
        {
            RemoteConnection.FastForward();
        }

        private void RemoteSnap_Click(object sender, RoutedEventArgs e)
        {
            RemoteConnection.GetRemoteBitmap();
        }
    }
}
