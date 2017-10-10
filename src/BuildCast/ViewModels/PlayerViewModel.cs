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

namespace BuildCast.ViewModels
{
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Threading.Tasks;
    using BuildCast.DataModel;
    using BuildCast.Services;
    using BuildCast.Services.Navigation;
    using Microsoft.Toolkit.Uwp.Helpers;
    using Windows.UI.Xaml.Navigation;

    public class PlayerViewModel : INavigableTo, INotifyPropertyChanged
    {
        private INavigationService _navigationService;
        private IRemotePlayerService _remotePlayerService;
        private IDisposable _subscriptionToken;

        public PlayerViewModel(INavigationService navigationService, IRemotePlayerService remotePlayerService)
        {
            _navigationService = navigationService;
            _remotePlayerService = remotePlayerService;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<IRemoteSystemDescription> RemoteSystems { get; private set; }

        ///TODO: ON NAVIGATE AWAY REMOVE THE SUBSCRIPTION TOKEN
        public void GoToInkNote(InkNote note)
        {
            var ignored = _navigationService.NavigateToInkNoteAsync(note);
        }

        public async Task NavigatedTo(NavigationMode navigationMode, object parameter)
        {
            await _remotePlayerService.FindDevices();
            RemoteSystems = new ObservableCollection<IRemoteSystemDescription>(_remotePlayerService.AvailableSystems);
            _subscriptionToken = _remotePlayerService.SubscribeSystemsChange(new RemoteSystemsObserver(this));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RemoteSystems)));
        }

        public void RefreshRemoteSystems(object sender, EventArgs e)
        {
            var ignored = _remotePlayerService.FindDevices();
        }

        public async Task<IRemoteConnection> CreateRemoteControl(IRemoteSystemDescription remoteSystem)
        {
            IRemoteConnection remoteControl = await remoteSystem.PlayTo();

            return remoteControl;
        }

        public class RemoteSystemsObserver : IAvailableSystemsObserver
        {
            private PlayerViewModel _viewModel;

            public RemoteSystemsObserver(PlayerViewModel viewModel)
            {
                _viewModel = viewModel;
            }

            public void Add(IRemoteSystemDescription description)
            {
                var ignored = DispatcherHelper.ExecuteOnUIThreadAsync(() => _viewModel.RemoteSystems.Add(description));
            }

            public void Removed(IRemoteSystemDescription description)
            {
                var ignored = DispatcherHelper.ExecuteOnUIThreadAsync(() => _viewModel.RemoteSystems.Remove(description));
            }
        }
    }
}
