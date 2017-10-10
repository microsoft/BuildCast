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
using System.Threading.Tasks;
using Autofac;
using BuildCast.DataModel;
using BuildCast.Helpers;
using BuildCast.Services;
using BuildCast.Services.Navigation;
using BuildCast.ViewModels;
using BuildCast.Views;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.Foundation.Collections;
using Windows.System.Profile;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Navigation;

namespace BuildCast
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public sealed partial class App : Application
    {
        private IContainer _container;
        private BackgroundTaskDeferral appServiceDeferral;
        private NavigationRoot rootPage;

        /// <summary>
        /// Initializes a new instance of the <see cref="App"/> class.
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;

            LocalStorageContext.CheckMigrations();

            this.RequiresPointerMode = ApplicationRequiresPointerMode.WhenRequested;
        }

        public static AppServiceConnection Connection { get; set; }

        public NavigationRoot GetNavigationRoot()
        {
            if (Window.Current.Content is NavigationRoot)
            {
                return Window.Current.Content as NavigationRoot;
            }
            else if (Window.Current.Content is Frame)
            {
                return ((Frame)Window.Current.Content).Content as NavigationRoot;
            }

            throw new Exception("Window content is unknown type");
        }

        public Frame GetFrame()
        {
            var root = GetNavigationRoot();
            return root.AppFrame;
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected async override void OnLaunched(LaunchActivatedEventArgs args)
        {
            //XBOX support
            if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Xbox")
            {
                ApplicationView.GetForCurrentView().SetDesiredBoundsMode(ApplicationViewBoundsMode.UseCoreWindow);
                bool result = ApplicationViewScaling.TrySetDisableLayoutScaling(true);
            }

            await InitializeAsync();
            InitWindow(skipWindowCreation: args.PrelaunchActivated);

            // Tasks after activation
            await StartupAsync();

            await Window.Current.Dispatcher.RunIdleAsync(async (s) =>
                await BackgroundDownloadHelper.AttachToDownloads());
        }

        protected override void OnWindowCreated(WindowCreatedEventArgs args)
        {
            base.OnWindowCreated(args);
        }

        protected async override void OnActivated(IActivatedEventArgs args)
        {
            await InitializeAsync();
            InitWindow(skipWindowCreation: false);

            if (args.Kind == ActivationKind.Protocol)
            {
                Window.Current.Activate();

                // Tasks after activation
                await StartupAsync();
            }
        }

        protected async override void OnBackgroundActivated(BackgroundActivatedEventArgs args)
        {
            var instance = args?.TaskInstance;

            await BackgroundDownloadHelper.CheckCompletionResult(instance);
            base.OnBackgroundActivated(args);
            if (args.TaskInstance.TriggerDetails is AppServiceTriggerDetails details && _container != null)
            {
                var remotePlayerService = _container.Resolve<IRemotePlayerService>();

                remotePlayerService.RemoteActivation(args.TaskInstance.GetDeferral(), details.AppServiceConnection);
                appServiceDeferral = args.TaskInstance.GetDeferral();

                Connection = details.AppServiceConnection;
                Connection.RequestReceived += OnRequestReceived;
                Connection.ServiceClosed += AppServiceConnection_ServiceClosed;
            }
        }

        private void OnRequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            var deferral = args.GetDeferral();
            Console.WriteLine(args.Request.Message);
            deferral.Complete();
            appServiceDeferral.Complete();
        }

        private void OnAppServicesCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            appServiceDeferral.Complete();
        }

        private void AppServiceConnection_ServiceClosed(AppServiceConnection sender, AppServiceClosedEventArgs args)
        {
            appServiceDeferral.Complete();
        }

        private void InitWindow(bool skipWindowCreation)
        {
            var builder = new ContainerBuilder();

            rootPage = Window.Current.Content as NavigationRoot;
            bool initApp = rootPage == null && !skipWindowCreation;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (initApp)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootPage = new NavigationRoot();

                FrameAdapter adapter = new FrameAdapter(rootPage.AppFrame);

                builder.RegisterInstance(adapter)
                        .AsImplementedInterfaces();

                builder.RegisterType<HomeViewModel>();

                // The feed details view model needs to be a singleton in order to better accomodate Connected Animation
                builder.RegisterType<FeedDetailsViewModel>()
                    .SingleInstance();
                builder.RegisterType<EpisodeDetailsViewModel>();
                builder.RegisterType<PlayerViewModel>();
                builder.RegisterType<InkNoteViewModel>();
                builder.RegisterType<FavoritesViewModel>();
                builder.RegisterType<NotesViewModel>();
                builder.RegisterType<DownloadsViewModel>();
                builder.RegisterType<SettingsViewModel>();

                builder.RegisterType<NavigationService>()
                        .AsImplementedInterfaces()
                        .SingleInstance();

                builder.RegisterType<RemotePlayService>()
                       .AsImplementedInterfaces();

                builder.RegisterInstance(PlayerService.Current)
                        .AsImplementedInterfaces()
                        .SingleInstance();

                _container = builder.Build();
                rootPage.InitializeNavigationService(_container.Resolve<INavigationService>());

                adapter.NavigationFailed += OnNavigationFailed;

                // Place the frame in the current Window
                Window.Current.Content = rootPage;

                Window.Current.Activate();
            }
        }

        private async Task InitializeAsync()
        {
            await ThemeSelectorService.InitializeAsync();
            await Task.CompletedTask;
        }

        private async Task StartupAsync()
        {
            BuildCast.Services.ThemeSelectorService.SetRequestedTheme();
            await FeedStore.CheckDownloadsPresent();
            await Task.CompletedTask;
        }

        private async System.Threading.Tasks.Task<Episode> FixFeedItemThumbnail(Feed feed, Episode feedItem)
        {
            var feedItems = await feed.GetEpisodes();

            var matchedItem = feedItems.FirstOrDefault(uri => uri.Key == feedItem.Key);

            if (matchedItem != null)
            {
                feedItem = matchedItem;
            }

            return feedItem;
        }

        private void ParseProtocolData(ValueSet playbackData, out Feed feed, out Episode feedItem, out TimeSpan timespan)
        {
            var values = playbackData.GetValueSet("feed");
            feed = Feed.BuildFeedFromValueSet(values);
            values = playbackData.GetValueSet("feeditem");
            feedItem = Episode.BuildFromValueSet(values);
            timespan = playbackData.GetTimeSpan("ElapsedTime");
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        private void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();

            deferral.Complete();
        }
    }
}
