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
        private bool _useWindowsHello;
        private readonly IResourceLoader _resourceLoader;
        private string _serverVersion;
        private bool _ignoreServerCertificateErrors;
        private bool _expertMode;

        public ICommand ResetCommand { get; }
        public ICommand ShowHelpExpertModeCommand { get; }
        public ICommand ShowHelpIgnoreInvalidSslCertificatesCommand { get; }

        public SettingsPageViewModel(INavigationService navigationService, IResourceLoader resourceLoader, DialogService dialogService)
        {
            _navigationService = navigationService;
            _resourceLoader = resourceLoader;
            _dialogService = dialogService;
            SettingsLocal = SettingsService.Default.Value.LocalSettings;
            SettingsRoaming = SettingsService.Default.Value.RoamingSettings;

            UseWindowsHello = SettingsLocal.UseWindowsHello;
            IgnoreServerCertificateErrors = SettingsLocal.IgnoreServerCertificateErrors;
            ExpertMode = SettingsLocal.ExpertMode;

            ResetCommand = new DelegateCommand(Reset);
            ShowHelpExpertModeCommand = new DelegateCommand(ShowHelpExpertMode);
            ShowHelpIgnoreInvalidSslCertificatesCommand = new DelegateCommand(ShowHelpInvalidSslCertificates);

            PreviewImageDownloadMode = SettingsLocal.PreviewImageDownloadMode;
            Theme = SettingsRoaming.Theme;

            GetServerVersion();
        }

        private async void GetServerVersion()
        {
            var status = await NextcloudClient.NextcloudClient.GetServerStatus(SettingsLocal.ServerAddress, SettingsService.Default.Value.LocalSettings.IgnoreServerCertificateErrors);

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

        private async void Reset()
        {
            var dialog = new ContentDialog
            {
                Title = _resourceLoader.GetString("ResetThisApp_Title"),
                Content = new TextBlock
                {
                    Text = _resourceLoader.GetString("ResetThisApp_Description"),
                    TextWrapping = TextWrapping.WrapWholeWords,
                    Margin = new Thickness(0, 20, 0, 0)
                },
                PrimaryButtonText = _resourceLoader.GetString("Yes"),
                SecondaryButtonText = _resourceLoader.GetString("No")
            };
            dialog.IsPrimaryButtonEnabled = dialog.IsSecondaryButtonEnabled = true;
            
            var result = await _dialogService.ShowAsync(dialog);

            if (result != ContentDialogResult.Primary)
            {
                return;
            }

            SettingsService.Default.Value.Reset();
            SyncDbUtils.Reset();
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

        public PreviewImageDownloadMode PreviewImageDownloadMode
        {
            get => SettingsLocal.PreviewImageDownloadMode;
            set
            {
                if (SettingsLocal.PreviewImageDownloadMode.Equals(value))
                {
                    return;
                }

                SettingsLocal.PreviewImageDownloadMode = value;

                // ReSharper disable once ExplicitCallerInfoArgument
                RaisePropertyChanged(nameof(PreviewImageDownloadModeAsAlways));
                // ReSharper disable once ExplicitCallerInfoArgument
                RaisePropertyChanged(nameof(PreviewImageDownloadModeAsWiFiOnly));
                // ReSharper disable once ExplicitCallerInfoArgument
                RaisePropertyChanged(nameof(PreviewImageDownloadModeAsNever));
            }
        }

        public bool PreviewImageDownloadModeAsAlways
        {
            get => PreviewImageDownloadMode.Equals(PreviewImageDownloadMode.Always);
            set
            {
                if (value)
                {
                    PreviewImageDownloadMode = PreviewImageDownloadMode.Always;
                }
            }
        }

        public bool PreviewImageDownloadModeAsWiFiOnly
        {
            get => PreviewImageDownloadMode.Equals(PreviewImageDownloadMode.WiFiOnly);
            set
            {
                if (value)
                {
                    PreviewImageDownloadMode = PreviewImageDownloadMode.WiFiOnly;
                }
            }
        }

        public bool PreviewImageDownloadModeAsNever
        {
            get => PreviewImageDownloadMode.Equals(PreviewImageDownloadMode.Never);
            set
            {
                if (value)
                {
                    PreviewImageDownloadMode = PreviewImageDownloadMode.Never;
                }
            }
        }

        public Theme Theme
        {
            get => SettingsRoaming.Theme;
            set
            {
                if (SettingsRoaming.Theme.Equals(value))
                {
                    return;
                }

                SettingsRoaming.Theme = value;

                // ReSharper disable once ExplicitCallerInfoArgument
                RaisePropertyChanged(nameof(ThemeAsLight));
                // ReSharper disable once ExplicitCallerInfoArgument
                RaisePropertyChanged(nameof(ThemeAsDark));
                // ReSharper disable once ExplicitCallerInfoArgument
                RaisePropertyChanged(nameof(ThemeAsSystem));
            }
        }

        public bool ThemeAsLight
        {
            get => Theme.Equals(Theme.Light);
            set {
                if (value)
                {
                    Theme = Theme.Light;
                }
            }
        }

        public bool ThemeAsDark
        {
            get => Theme.Equals(Theme.Dark);
            set
            {
                if (value)
                {
                    Theme = Theme.Dark;
                }
            }
        }

        public bool ThemeAsSystem
        {
            get => Theme.Equals(Theme.System);
            set
            {
                if (value)
                {
                    Theme = Theme.System;
                }
            }
        }
        
        public ThemeColor ThemeColor
        {
            get => SettingsRoaming.ThemeColor;
            set
            {
                if (SettingsRoaming.ThemeColor.Equals(value))
                {
                    return;
                }

                SettingsRoaming.ThemeColor = value;

                // ReSharper disable once ExplicitCallerInfoArgument
                RaisePropertyChanged(nameof(ThemeColorAsNextcloud));
                // ReSharper disable once ExplicitCallerInfoArgument
                RaisePropertyChanged(nameof(ThemeColorAsSystem));
            }
        }

        public bool ThemeColorAsNextcloud
        {
            get => ThemeColor.Equals(ThemeColor.Nextcloud);
            set
            {
                if (value)
                {
                    ThemeColor = ThemeColor.Nextcloud;
                }
            }
        }

        public bool ThemeColorAsSystem
        {
            get => ThemeColor.Equals(ThemeColor.System);
            set
            {
                if (value)
                {
                    ThemeColor = ThemeColor.System;
                }
            }
        }
    }
}