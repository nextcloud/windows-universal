using System;
using System.Collections.Generic;
using System.Windows.Input;
using Windows.ApplicationModel;
using NextcloudApp.Models;
using NextcloudApp.Services;
using Prism.Commands;
using Prism.Windows.Navigation;
using NextcloudApp.Utils;
using Prism.Windows.AppModel;
using Windows.UI.Xaml.Controls;
using NextcloudApp.Constants;
using Windows.UI.Xaml;
using Prism.Unity.Windows;
using Windows.UI.Popups;
using System.Threading.Tasks;

namespace NextcloudApp.ViewModels
{
    public class SettingsPageViewModel : ViewModel
    {
        private readonly INavigationService _navigationService;
        private readonly DialogService _dialogService;
        private LocalSettings _settingsLocal;
        private RoamingSettings _settingsRoaming;
        private int _previewImageDownloadModesSelectedIndex;
        private int _themeModesSelectedIndex;
        private bool _useWindowsHello;
        private readonly IResourceLoader _resourceLoader;
        private string _serverVersion;
        private bool _ignoreServerCertificateErrors;
        private bool _expertMode;

        public ICommand ResetCommand { get; private set; }
        public ICommand ShowHelpExpertModeCommand { get; private set; }
        public ICommand ShowHelpIgnoreInvalidSslCertificatesCommand { get; private set; }

        public SettingsPageViewModel(INavigationService navigationService, IResourceLoader resourceLoader, DialogService dialogService)
        {
            _navigationService = navigationService;
            _resourceLoader = resourceLoader;
            _dialogService = dialogService;
            SettingsLocal = SettingsService.Instance.LocalSettings;
            SettingsRoaming = SettingsService.Instance.RoamingSettings;

            PreviewImageDownloadModes.Add(new PreviewImageDownloadModeItem
            {
                Name = resourceLoader.GetString("Always"),
                Value = PreviewImageDownloadMode.Always
            });

            PreviewImageDownloadModes.Add(new PreviewImageDownloadModeItem
            {
                Name = resourceLoader.GetString("WiFiOnly"),
                Value = PreviewImageDownloadMode.WiFiOnly
            });

            PreviewImageDownloadModes.Add(new PreviewImageDownloadModeItem
            {
                Name = resourceLoader.GetString("Never"),
                Value = PreviewImageDownloadMode.Never
            });

            switch (SettingsLocal.PreviewImageDownloadMode)
            {
                case PreviewImageDownloadMode.Always:
                    PreviewImageDownloadModesSelectedIndex = 0;
                    break;
                case PreviewImageDownloadMode.WiFiOnly:
                    PreviewImageDownloadModesSelectedIndex = 1;
                    break;
                case PreviewImageDownloadMode.Never:
                    PreviewImageDownloadModesSelectedIndex = 2;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            UseWindowsHello = SettingsLocal.UseWindowsHello;
            IgnoreServerCertificateErrors = SettingsLocal.IgnoreServerCertificateErrors;
            ExpertMode = SettingsLocal.ExpertMode;

            ResetCommand = new DelegateCommand(Reset);
            ShowHelpExpertModeCommand = new DelegateCommand(ShowHelpExpertMode);
            ShowHelpIgnoreInvalidSslCertificatesCommand = new DelegateCommand(ShowHelpInvalidSslCertificates);

            ThemeItems.Add(new ThemeItem
            {
                Name = resourceLoader.GetString(ResourceConstants.ThemeSystem),
                Value = Theme.System
            });

            ThemeItems.Add(new ThemeItem
            {
                Name = resourceLoader.GetString(ResourceConstants.ThemeDark),
                Value = Theme.Dark
            });

            ThemeItems.Add(new ThemeItem
            {
                Name = resourceLoader.GetString(ResourceConstants.ThemeLight),
                Value = Theme.Light
            });

            switch (SettingsRoaming.Theme)
            {
                case Theme.System:
                   ThemeModeSelectedIndex = 0;
                    break;
                case Theme.Dark:
                    ThemeModeSelectedIndex = 1;
                    break;
                case Theme.Light:
                    ThemeModeSelectedIndex = 2;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            GetServerVersion();
        }

        private async void GetServerVersion()
        {
            var status = await NextcloudClient.NextcloudClient.GetServerStatus(SettingsLocal.ServerAddress, SettingsService.Instance.LocalSettings.IgnoreServerCertificateErrors);

            if (!string.IsNullOrEmpty(status.VersionString))
            {
                ServerVersion = string.Format(_resourceLoader.GetString("ServerVersion"), status.VersionString);
            }
        }

        public string ServerVersion
        {
            get => _serverVersion;
            private set => SetProperty(ref _serverVersion, value);
        }

        public LocalSettings SettingsLocal
        {
            get => _settingsLocal;
            private set => SetProperty(ref _settingsLocal, value);
        }

        public RoamingSettings SettingsRoaming
        {
            get => _settingsRoaming;
            private set => SetProperty(ref _settingsRoaming, value);
        }

        public List<PreviewImageDownloadModeItem> PreviewImageDownloadModes { get; } = new List<PreviewImageDownloadModeItem>();

        public List<ThemeItem> ThemeItems { get; } = new List<ThemeItem>();


        public int PreviewImageDownloadModesSelectedIndex
        {
            get => _previewImageDownloadModesSelectedIndex;
            set
            {
                if (!SetProperty(ref _previewImageDownloadModesSelectedIndex, value))
                {
                    return;
                }

                switch (value)
                {
                    case 0:
                        SettingsLocal.PreviewImageDownloadMode = PreviewImageDownloadMode.Always;
                        break;
                    case 1:
                        SettingsLocal.PreviewImageDownloadMode = PreviewImageDownloadMode.WiFiOnly;
                        break;
                    case 2:
                        SettingsLocal.PreviewImageDownloadMode = PreviewImageDownloadMode.Never;
                        break;
                }
            }
        }

        public int ThemeModeSelectedIndex
        {
            get => _themeModesSelectedIndex;
            set
            {
                if (!SetProperty(ref _themeModesSelectedIndex, value))
                {
                    return;
                }

                switch (value)
                {
                    case 0:
                        SettingsRoaming.Theme = Theme.System;
                        break;
                    case 1:
                        SettingsRoaming.Theme = Theme.Dark;
                        break;
                    case 2:
                        SettingsRoaming.Theme = Theme.Light;
                        break;
                }
            }
        }

        public bool IgnoreServerCertificateErrors
        {
            get => _ignoreServerCertificateErrors;
            set
            {
                if (!SetProperty(ref _ignoreServerCertificateErrors, value))
                    return;

                SettingsLocal.IgnoreServerCertificateErrors = value;
            }
        }

        public async void IgnoreServerCertificateErrorsToggled()
        {
            ClientService.Reset();

            var dialog = new ContentDialog
            {
                Title = _resourceLoader.GetString("Hint"),
                Content = new TextBlock
                {
                    Text = _resourceLoader.GetString("AppMustBeRestarted"),
                    TextWrapping = TextWrapping.WrapWholeWords,
                    Margin = new Thickness(0, 20, 0, 0)
                },
                PrimaryButtonText = _resourceLoader.GetString("OK")
            };

            await _dialogService.ShowAsync(dialog);
            PrismUnityApplication.Current.Exit();
        }

        public bool ExpertMode
        {
            get => _expertMode;
            set
            {
                if (!SetProperty(ref _expertMode, value))
                    return;

                SettingsLocal.ExpertMode = value;
            }
        }

        public bool UseWindowsHello
        {
            get => _useWindowsHello;
            set
            {
                if (!SetProperty(ref _useWindowsHello, value))
                    return;

                SettingsLocal.UseWindowsHello = value;
            }
        }

        public async void UseWindowsHelloToggled()
        {
            if (!UseWindowsHello)
            {
                return;
            }
            var available = await VerificationService.CheckAvailabilityAsync();

            if (available)
            {
                return;
            }
            var dialog = new ContentDialog
            {
                Title = _resourceLoader.GetString(ResourceConstants.DialogTitle_GeneralNextCloudApp),
                Content = new TextBlock
                {
                    Text = _resourceLoader.GetString(ResourceConstants.WindowsHelloNotAvailable),
                    TextWrapping = TextWrapping.WrapWholeWords,
                    Margin = new Thickness(0, 20, 0, 0)
                },
                PrimaryButtonText = _resourceLoader.GetString("OK")
            };
            await _dialogService.ShowAsync(dialog);

            UseWindowsHello = false;
        }

        public string AppVersion
            =>
                string.Format(_resourceLoader.GetString("ClientVersion"),
                    $"{Package.Current.Id.Version.Major}.{Package.Current.Id.Version.Minor}.{Package.Current.Id.Version.Build}");

        private void Reset()
        {
            SettingsService.Instance.Reset();
            _navigationService.Navigate(PageToken.Login.ToString(), null);
        }

        private async void ShowHelpExpertMode()
        {
            var text = _resourceLoader.GetString(ResourceConstants.HelpText_ExpertMode);
            await ShowHelp(text);
        }

        private async void ShowHelpInvalidSslCertificates()
        {
            var text = _resourceLoader.GetString(ResourceConstants.HelpText_HelpTextIgnoreInvalidSelfSignedSslCertificates);
            await ShowHelp(text);
        }

        private async Task ShowHelp(string message)
        {
            var messageDialog = new MessageDialog(message, string.Empty);
            await _dialogService.ShowAsync(messageDialog);
        }
    }
}