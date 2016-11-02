using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Windows.ApplicationModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using NextcloudApp.Models;
using NextcloudApp.Services;
using Prism.Commands;
using Prism.Windows.Navigation;
using Windows.UI.Xaml.Data;
using NextcloudApp.Utils;
using Prism.Windows.AppModel;

namespace NextcloudApp.ViewModels
{
    public class SettingsPageViewModel : ViewModel
    {
        private readonly INavigationService _navigationService;
        private Settings _settngs;
        private int _previewImageDownloadModesSelectedIndex;
        private IResourceLoader _resourceLoader;
        private string _serverVersion;

        public ICommand ResetCommand { get; private set; }

        public SettingsPageViewModel(INavigationService navigationService, IResourceLoader resourceLoader)
        {
            _navigationService = navigationService;
            _resourceLoader = resourceLoader;
            Settings = SettingsService.Instance.Settings;

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

            ResetCommand = new DelegateCommand(Reset);

            GetServerVersion();
        }

        private async void GetServerVersion()
        {
            var status = await NextcloudClient.NextcloudClient.GetServerStatus(Settings.ServerAddress);
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

        public Settings Settings
        {
            get { return _settngs; }
            private set { SetProperty(ref _settngs, value); }
        }

        public List<PreviewImageDownloadModeItem> PreviewImageDownloadModes { get; } =
            new List<PreviewImageDownloadModeItem>() {};


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

        public string AppVersion
            =>
                string.Format(_resourceLoader.GetString("ClientVersion"),
                    $"{Package.Current.Id.Version.Major}.{Package.Current.Id.Version.Minor}.{Package.Current.Id.Version.Build}.{Package.Current.Id.Version.Revision}")
            ;

        private void Reset()
        {
            SettingsService.Instance.Reset();

            _navigationService.Navigate(PageTokens.Login.ToString(), null);
        }
    }
}