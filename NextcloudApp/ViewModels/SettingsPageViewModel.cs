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

namespace NextcloudApp.ViewModels
{
    public class SettingsPageViewModel : ViewModel
    {
        private readonly INavigationService _navigationService;
        private readonly DialogService _dialogService;
        private LocalSettings _settings;
        private int _previewImageDownloadModesSelectedIndex;
        private bool _useWindowsHello;
        private readonly IResourceLoader _resourceLoader;
        private string _serverVersion;
        private bool _ignoreServerCertificateErrors;

        public ICommand ResetCommand { get; private set; }

        public SettingsPageViewModel(INavigationService navigationService, IResourceLoader resourceLoader, DialogService dialogService)
        {
            _navigationService = navigationService;
            _resourceLoader = resourceLoader;
            _dialogService = dialogService;
            Settings = SettingsService.Instance.LocalSettings;

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

            switch (Settings.PreviewImageDownloadMode)
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

            UseWindowsHello = Settings.UseWindowsHello;
            IgnoreServerCertificateErrors = Settings.IgnoreServerCertificateErrors;

            ResetCommand = new DelegateCommand(Reset);

            GetServerVersion();
        }

        private async void GetServerVersion()
        {
            var status = await NextcloudClient.NextcloudClient.GetServerStatus(Settings.ServerAddress, SettingsService.Instance.LocalSettings.IgnoreServerCertificateErrors);
            if (!string.IsNullOrEmpty(status.VersionString))
            {
                ServerVersion = string.Format(_resourceLoader.GetString("ServerVersion"), status.VersionString);
            }
        }

        public string ServerVersion
        {
            get { return _serverVersion; }
            private set { SetProperty(ref _serverVersion, value); }
        }

        public LocalSettings Settings
        {
            get { return _settings; }
            private set { SetProperty(ref _settings, value); }
        }

        public List<PreviewImageDownloadModeItem> PreviewImageDownloadModes { get; } =
            new List<PreviewImageDownloadModeItem>();


        public int PreviewImageDownloadModesSelectedIndex
        {
            get { return _previewImageDownloadModesSelectedIndex; }
            set
            {
                if (!SetProperty(ref _previewImageDownloadModesSelectedIndex, value))
                {
                    return;
                }
                switch (value)
                {
                    case 0:
                        Settings.PreviewImageDownloadMode = PreviewImageDownloadMode.Always;
                        break;

                    case 1:
                        Settings.PreviewImageDownloadMode = PreviewImageDownloadMode.WiFiOnly;
                        break;

                    case 2:
                        Settings.PreviewImageDownloadMode = PreviewImageDownloadMode.Never;
                        break;
                }
            }
        }

        public bool IgnoreServerCertificateErrors
        {
            get { return _ignoreServerCertificateErrors; }
            set
            {
                if (!SetProperty(ref _ignoreServerCertificateErrors, value))
                    return;

                Settings.IgnoreServerCertificateErrors = value;
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

        public bool UseWindowsHello
        {
            get { return _useWindowsHello; }
            set
            {
                if (!SetProperty(ref _useWindowsHello, value))
                    return;

                Settings.UseWindowsHello = value;
            }
        }

        public async void UseWindowsHelloToggled()
        {
            if (UseWindowsHello)
            {
                var available = await VerificationService.CheckAvailabilityAsync();

                if (!available)
                {
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
            }
        }

        public string AppVersion
            =>
                string.Format(_resourceLoader.GetString("ClientVersion"),
                    $"{Package.Current.Id.Version.Major}.{Package.Current.Id.Version.Minor}.{Package.Current.Id.Version.Build}")
            ;

        private void Reset()
        {
            SettingsService.Instance.Reset();
            _navigationService.Navigate(PageToken.Login.ToString(), null);
        }
    }
}