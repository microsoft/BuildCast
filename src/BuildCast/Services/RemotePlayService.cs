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
using System.Linq;
using System.Threading.Tasks;
using BuildCast.DataModel;
using BuildCast.Helpers;
using BuildCast.Services.Navigation;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.System.RemoteSystems;

namespace BuildCast.Services
{
    public class RemotePlayService : IRemotePlayerService
    {
        private const string PLAYERCOMMAND = "player_command";
        private const string PLAYCOMMAND = "play";
        private const string PLAYTIME = "playtime";
        private const string SEEKCOMMAND = "seek";
        private const string PAUSECOMMAND = "pause";
        private const string STOPCOMMAND = "stop";
        private const string FFWDCOMMAND = "ffwd";
        private const string RWDCOMMAND = "bekindrewind";
        private const string SNAPBITMAPCOMMAND = "snapbitmap";
        private const string SNAPBITMAPREPLY = "snapbitmapreply";
        private const string BUILDCASTAPPID = "3493e9b8-b025-449b-99db-f6cd5b0a5439";

        private BackgroundTaskDeferral serviceDeferral;
        private AppServiceConnection connection;

        private List<IAvailableSystemsObserver> _observers = new List<IAvailableSystemsObserver>();

        private IPlayerService _playerService;
        private INavigationService _navigationService;

        private RemoteSystemWatcher _remoteSystemWatcher;
        private Dictionary<string, (RemoteSystem System, IRemoteSystemDescription Description)> _deviceMap = null;

        public RemotePlayService(IPlayerService playerService, INavigationService navigationService)
        {
            _playerService = playerService;
            _navigationService = navigationService;
            AvailableSystems = new List<IRemoteSystemDescription>();
            _deviceMap = new Dictionary<string, (RemoteSystem System, IRemoteSystemDescription Description)>();
        }

        IEnumerable<IRemoteSystemDescription> IRemotePlayerService.AvailableSystems => AvailableSystems;

        private List<IRemoteSystemDescription> AvailableSystems { get; set; }

        public void SearchCleanup()
        {
            if (_remoteSystemWatcher != null)
            {
                _remoteSystemWatcher.Stop();
                _remoteSystemWatcher = null;
            }

            if (AvailableSystems != null)
            {
                AvailableSystems.Clear();
                _deviceMap.Clear();
            }
        }

        public async Task<bool> FindDevices()
        {
            // ROME code to find proximity device
            RemoteSystemAccessStatus accessStatus = await RemoteSystem.RequestAccessAsync();
            if (accessStatus == RemoteSystemAccessStatus.Allowed)
            {
                if (_remoteSystemWatcher != null)
                {
                    _remoteSystemWatcher.Start();
                    return true;
                }
                else
                {
                    List<IRemoteSystemFilter> filters = new List<IRemoteSystemFilter>();
                    RemoteSystemDiscoveryTypeFilter discoveryFilter = new RemoteSystemDiscoveryTypeFilter(RemoteSystemDiscoveryType.Any);
                    List<string> kinds = new List<string>();
                    RemoteSystemStatusTypeFilter statusFilter = new RemoteSystemStatusTypeFilter(RemoteSystemStatusType.Any);
                    kinds.Add(RemoteSystemKinds.Desktop);
                    // kinds.Add(RemoteSystemKinds.Phone);
                    kinds.Add(RemoteSystemKinds.Xbox);
                    RemoteSystemKindFilter kindFilter = new RemoteSystemKindFilter(kinds);
                    filters.Add(kindFilter);
                    filters.Add(statusFilter);
                    filters.Add(discoveryFilter);

                    _remoteSystemWatcher = RemoteSystem.CreateWatcher(filters);

                    // Subscribing to the event that will be raised when a new remote system is found by the watcher.
                    _remoteSystemWatcher.RemoteSystemAdded += RemoteSystemWatcher_RemoteSystemAdded;

                    // Subscribing to the event that will be raised when a previously found remote system is no longer available.
                    _remoteSystemWatcher.RemoteSystemRemoved += RemoteSystemWatcher_RemoteSystemRemoved;

                    // Subscribing to the event that will be raised when a previously found remote system is updated.
                    _remoteSystemWatcher.RemoteSystemUpdated += RemoteSystemWatcher_RemoteSystemUpdated;

                    // Start the watcher.
                    _remoteSystemWatcher.Start();
                    return true;
                }
            }
            else
            {
                return false;
            }
        }

        public void RemoteActivation(BackgroundTaskDeferral backgroundTaskDeferral, AppServiceConnection appServiceConnection)
        {
            serviceDeferral = backgroundTaskDeferral;
            connection = appServiceConnection;

            connection.RequestReceived += Connection_RequestReceived;
        }

        public IDisposable SubscribeSystemsChange(IAvailableSystemsObserver observer)
        {
            IDisposable token = null;
            if (!_observers.Contains(observer))
            {
                token = new SubscriptionToken(_observers, observer);
            }

            return token;
        }

        private void RemoteSystemWatcher_RemoteSystemUpdated(RemoteSystemWatcher sender, RemoteSystemUpdatedEventArgs args)
        {
            IRemoteSystemDescription description = null;
            if (_deviceMap.ContainsKey(args.RemoteSystem.Id))
            {
                description = _deviceMap[args.RemoteSystem.Id].Description;
                _deviceMap.Remove(args.RemoteSystem.Id);
            }

            _deviceMap.Add(args.RemoteSystem.Id, (args.RemoteSystem, description));
        }

        private void RemoteSystemWatcher_RemoteSystemRemoved(RemoteSystemWatcher sender, RemoteSystemRemovedEventArgs args)
        {
            IRemoteSystemDescription description = null;
            if (_deviceMap.ContainsKey(args.RemoteSystemId))
            {
                description = _deviceMap[args.RemoteSystemId].Description;
                AvailableSystems.Remove(description);
                _deviceMap.Remove(args.RemoteSystemId);
            }

            if (description != null)
            {
                foreach (var observer in _observers)
                {
                    observer.Removed(description);
                }
            }
        }

        private void RemoteSystemWatcher_RemoteSystemAdded(RemoteSystemWatcher sender, RemoteSystemAddedEventArgs args)
        {
            if (!_deviceMap.ContainsKey(args.RemoteSystem.Id))
            {
                IRemoteSystemDescription description = new RemoteSystemDescription(this) { Id = args.RemoteSystem.Id, Name = args.RemoteSystem.DisplayName, Kind = args.RemoteSystem.Kind };

                // deviceList and deviceMap updates instead of only latestRemoteSystem...
                AvailableSystems.Add(description);
                _deviceMap.Add(args.RemoteSystem.Id, (args.RemoteSystem, description));

                foreach (var observer in _observers)
                {
                    observer.Add(description);
                }
            }
        }

        private async Task<IRemoteConnection> CreateRemoteConnection(IRemoteSystemDescription remoteSystem)
        {
            RemoteConnection remoteConnection = null;
            var latestRemoteSystem = _deviceMap[remoteSystem.Id].System;
            if (latestRemoteSystem != null && latestRemoteSystem.Status == RemoteSystemStatus.Available)
            {
                Uri uri = new Uri(@"media-video-podcast-listenremote:");

                RemoteLauncherOptions launcherOptions = new RemoteLauncherOptions();
                launcherOptions.PreferredAppIds.Add(Windows.ApplicationModel.Package.Current.Id.FamilyName);

                RemoteLaunchUriStatus launchUriStatus = await RemoteLauncher.LaunchUriAsync(new RemoteSystemConnectionRequest(latestRemoteSystem), uri, launcherOptions);
                if (launchUriStatus == RemoteLaunchUriStatus.Success)
                {
                    RemoteSystemConnectionRequest connectionRequest = new RemoteSystemConnectionRequest(latestRemoteSystem);
                    var remoteAppServiceConnection = new AppServiceConnection
                    {
                        AppServiceName = "InProcessAppService",
                        PackageFamilyName = Windows.ApplicationModel.Package.Current.Id.FamilyName,
                    };
                    {
                        AppServiceConnectionStatus status = await remoteAppServiceConnection.OpenRemoteAsync(connectionRequest);

                        if (status == AppServiceConnectionStatus.Success)
                        {
                            remoteConnection = new RemoteConnection(remoteAppServiceConnection, _playerService);
                        }
                    }
                }
            }

            return remoteConnection;
        }

        private async void Connection_RequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            var messageDeferral = args.GetDeferral();

            try
            {
                var requestMessageValues = args.Request.Message;
                var commandString = requestMessageValues[PLAYERCOMMAND];

                switch (commandString)
                {
                    case PLAYCOMMAND:
                        LocalStorageContext context = new LocalStorageContext();
                        Episode episode = context.EpisodeCache.Where(ep => ep.Key == requestMessageValues[PLAYCOMMAND].ToString()).FirstOrDefault();
                        if (episode != null)
                        {
                            await _navigationService.NavigateToPlayerAsync(episode: null);
                            TimeSpan playTime = TimeSpan.Zero;
                            if (requestMessageValues.ContainsKey(PLAYTIME))
                            {
                                TimeSpan.TryParse(requestMessageValues[PLAYTIME].ToString(), out playTime);
                            }

                            await _playerService.Play(episode, playTime);
                        }

                        break;
                    case PAUSECOMMAND:
                        _playerService.Pause();
                        break;
                    case SEEKCOMMAND:
                        var timestamp = (TimeSpan)requestMessageValues[SEEKCOMMAND];
                        _playerService.SetTime(timestamp);
                        break;
                    case FFWDCOMMAND:
                        _playerService.FastForward();
                        break;
                    case RWDCOMMAND:
                        _playerService.Rewind();
                        break;
                    case SNAPBITMAPCOMMAND:
                        var bitmap = await _playerService.GetBitmapForCurrentFrameFromLocalFile();
                        ValueSet message = new ValueSet
                        {
                            [PLAYERCOMMAND] = SNAPBITMAPREPLY,
                            [SNAPBITMAPREPLY] = bitmap,
                        };
                        var response = await args.Request.SendResponseAsync(message);
                        break;
                    default:
                        break;
                }
            }
            finally
            {
                messageDeferral.Complete();
            }
        }

        // TODO: not used. Delete?
        private void TaskInstance_Canceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            if (serviceDeferral != null)
            {
                // Complete the service deferral
                serviceDeferral.Complete();
                serviceDeferral = null;
            }
        }

        private class RemoteConnection : IRemoteConnection
        {
            private AppServiceConnection _connection;
            private string _episodeKey;
            private IPlayerService _playerService;

            public RemoteConnection(AppServiceConnection connection, IPlayerService playerService)
            {
                _connection = connection;
                _episodeKey = playerService.NowPlaying.CurrentEpisode.Key;
                _playerService = playerService;
            }

            public Task Disconnect()
            {
                _connection.Dispose();
                return Task.CompletedTask;
            }

            public void Dispose()
            {
                _connection.Dispose();
            }

            public async Task Pause()
            {
                ValueSet message = new ValueSet
                {
                    [PLAYERCOMMAND] = PAUSECOMMAND,
                };
                var response = await _connection.SendMessageAsync(message);
                await CheckGoodResponse(response);
            }

            public async Task Play()
            {
                _playerService.Pause();
                TimeSpan currentTime = _playerService.NowPlaying?.CurrentTime ?? TimeSpan.Zero;
                ValueSet message = new ValueSet
                {
                    [PLAYERCOMMAND] = PLAYCOMMAND,
                    [PLAYCOMMAND] = _episodeKey,
                    [PLAYTIME] = currentTime,
                };
                var response = await _connection.SendMessageAsync(message);

                await CheckGoodResponse(response);
            }

            public async Task Seek(TimeSpan timeStamp)
            {
                ValueSet message = new ValueSet
                {
                    [PLAYERCOMMAND] = SEEKCOMMAND,
                    [SEEKCOMMAND] = timeStamp,
                };
                var response = await _connection.SendMessageAsync(message);
                await CheckGoodResponse(response);
            }

            public async Task FastForward()
            {
                ValueSet message = new ValueSet
                {
                    [PLAYERCOMMAND] = FFWDCOMMAND,
                };
                var response = await _connection.SendMessageAsync(message);
                await CheckGoodResponse(response);
            }

            public async Task Rewind()
            {
                ValueSet message = new ValueSet
                {
                    [PLAYERCOMMAND] = RWDCOMMAND,
                };
                var response = await _connection.SendMessageAsync(message);
                await CheckGoodResponse(response);
            }

            public async Task GetRemoteBitmap()
            {
                ValueSet message = new ValueSet
                {
                    [PLAYERCOMMAND] = SNAPBITMAPCOMMAND,
                };
                var response = await _connection.SendMessageAsync(message);

                if (response.Message.ContainsKey(SNAPBITMAPREPLY))
                {
                    byte[] bytes = (byte[])response.Message[SNAPBITMAPREPLY];
                    StartInking(PlayerService.Current.NowPlaying, bytes);
                }
                else
                {
                    throw new Exception("TODO HANDLE THIS");
                }

                await CheckGoodResponse(response);
            }

            public void StartInking(NowPlayingState nowPlaying, byte[] imageBytes)
            {
                // Remote inking is not yet supported
                return;

                // TODO: remove this method and call into a single place to start an inknote
                if (imageBytes != null)
                {
                    BuildCast.DataModel.InkNote meme = new BuildCast.DataModel.InkNote(nowPlaying.CurrentEpisode.Key, PlayerService.Current.CurrentTime);

                    var inkNoteData = new InkNoteData() { ImageBytes = imageBytes };

                    // TODO refactor to use navigation service.
                    ((App)Windows.UI.Xaml.Application.Current).GetFrame().Navigate(typeof(InkNote), inkNoteData);

                    // ViewModel.GoToInkNote(meme);
                }
            }

            private async Task CheckGoodResponse(AppServiceResponse response)
            {
                if (response.Status != AppServiceResponseStatus.Success)
                {
                    await UIHelpers.ShowContentAsync($"Failure sending remote command");
                }
            }
        }

        private class RemoteSystemDescription : IRemoteSystemDescription
        {
            private RemotePlayService _service;

            public RemoteSystemDescription(RemotePlayService service)
            {
                _service = service;
            }

            public string Id { get; set; }

            public string Name { get; set; }

            public string Kind { get; set; }

            public string Glyph
            {
                get
                {
                    switch (Kind)
                    {
                        case "Phone":
                            return "\uE8EA";
                        case "Xbox":
                            return "\uE7FC";
                        default:
                            return "\uE770";
                    }
                }
            }

            public Task<IRemoteConnection> PlayTo()
            {
                return _service.CreateRemoteConnection(this);
            }
        }

        private class SubscriptionToken : IDisposable
        {
            private List<IAvailableSystemsObserver> _observerList;
            private IAvailableSystemsObserver _observer;

            public SubscriptionToken(List<IAvailableSystemsObserver> observerList, IAvailableSystemsObserver observer)
            {
                _observerList = observerList;
                _observer = observer;

                _observerList.Add(_observer);
            }

            public void Dispose()
            {
                _observerList.Remove(_observer);
            }
        }
    }
}
