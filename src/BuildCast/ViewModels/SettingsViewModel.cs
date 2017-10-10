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
    using System.ComponentModel;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using BuildCast.Helpers;
    using BuildCast.Services;
    using Microsoft.Toolkit.Uwp.Helpers;
    using Windows.ApplicationModel;
    using Windows.Storage;

    public class SettingsViewModel : INotifyPropertyChanged
    {
        private const string PopupModeKey = "RequestedPopupMode";
        private ElementThemeExtended _elementTheme = ThemeSelectorService.Theme;
        private PipModes _currentPipMode;
        private string _versionDescription;
        private ICommand _switchThemeCommand;
        private ICommand _switchPipModeCommand;

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsViewModel"/> class.
        /// </summary>
        public SettingsViewModel()
        {
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public PipModes CurrentPipMode
        {
            get
            {
                return _currentPipMode;
            }

            set
            {
                if (_currentPipMode != value)
                {
                    _currentPipMode = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentPipMode)));
                }
            }
        }

        public static async Task<PipModes> GetCurrentMode()
        {
            return await GetCurrent();
        }

        public ElementThemeExtended ElementThemeExtended
        {
            get
            {
                return _elementTheme;
            }

            set
            {
                if (_elementTheme != value)
                {
                    _elementTheme = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(VersionDescription)));
                }
            }
        }

        public string VersionDescription
        {
            get
            {
                return _versionDescription;
            }

            set
            {
                if (_versionDescription != value)
                {
                    _versionDescription = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(VersionDescription)));
                }
            }
        }

        public ICommand SwitchThemeCommand
        {
            get
            {
                if (_switchThemeCommand == null)
                {
                    _switchThemeCommand = new RelayCommand<ElementThemeExtended>(
                        async (param) =>
                        {
                            await ThemeSelectorService.SetThemeAsync(param);
                        });
                }

                return _switchThemeCommand;
            }
        }

        public ICommand SwitchPipModeCommand
        {
            get
            {
                if (_switchPipModeCommand == null)
                {
                    _switchPipModeCommand = new RelayCommand<PipModes>(
                        async (param) =>
                        {
                            await SettingsViewModel.SetCurrent(param);
                        });
                }

                return _switchPipModeCommand;
            }
        }

        public void Initialize()
        {
            VersionDescription = GetVersionDescription();
            var task = GetCurrent().ContinueWith(async (modes) =>
            {
                await DispatcherHelper.ExecuteOnUIThreadAsync(() =>
                {
                    CurrentPipMode = modes.Result;
                });
            });
        }

        public async Task NavigatedTo()
        {
            CurrentPipMode = await GetCurrentMode();
        }

        private static async Task SetCurrent(PipModes value)
        {
            await ApplicationData.Current.LocalSettings.SaveAsync<PipModes>(PopupModeKey, value);
        }

        private static async Task<PipModes> GetCurrent()
        {
            return await ApplicationData.Current.LocalSettings.ReadAsync<PipModes>(PopupModeKey);
        }

        private string GetVersionDescription()
        {
            var package = Package.Current;
            var packageId = package.Id;
            var version = packageId.Version;

            return $"{package.DisplayName} - {version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
        }
    }
}
