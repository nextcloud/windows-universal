using System;
using System.ComponentModel;
using Windows.Foundation.Metadata;
using Windows.UI;
using Windows.UI.ViewManagement;
using NextcloudApp.Utils;

namespace NextcloudApp.Services
{
    public class StatusBarService
    {
        private static StatusBarService _instance;
        private int _waitCounter;

        private StatusBarService()
        {
            if (ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
            {
                var statusBar = StatusBar.GetForCurrentView();
                statusBar.BackgroundOpacity = 1;
                var theme = SettingsService.Instance.RoamingSettings.Theme;
                switch (theme)
                {
                    case Theme.Dark:
                        statusBar.BackgroundColor = Colors.Black;
                        statusBar.ForegroundColor = Colors.White;
                        break;
                    case Theme.Light:
                        statusBar.BackgroundColor = Colors.White;
                        statusBar.ForegroundColor = Colors.Black;
                        break;
                }

                SettingsService.Instance.RoamingSettings.PropertyChanged += RoamingSettingsOnPropertyChanged;
            }
        }

        private void RoamingSettingsOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("Theme"))
            {
                var statusBar = StatusBar.GetForCurrentView();
                statusBar.BackgroundOpacity = 1;
                var theme = SettingsService.Instance.RoamingSettings.Theme;
                switch (theme)
                {
                    case Theme.Dark:
                        statusBar.BackgroundColor = Colors.Black;
                        statusBar.ForegroundColor = Colors.White;
                        break;
                    case Theme.Light:
                        statusBar.BackgroundColor = Colors.White;
                        statusBar.ForegroundColor = Colors.Black;
                        break;
                }
            }
        }

        public static StatusBarService Instance => _instance ?? (_instance = new StatusBarService());

        public async void ShowProgressIndicator()
        {
            _waitCounter++;

            if (!ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
            {
                return;
            }
            var statusBar = StatusBar.GetForCurrentView();

            var asyncAction = statusBar?.ProgressIndicator.ShowAsync();
            if (asyncAction != null)
            {
                await asyncAction;
            }
        }

        public async void HideProgressIndicator()
        {
            _waitCounter--;
            if (_waitCounter > 0)
            {
                return;
            }

            if (!ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
            {
                return;
            }
            var statusBar = StatusBar.GetForCurrentView();
            var asyncAction = statusBar?.ProgressIndicator.HideAsync();
            if (asyncAction != null)
            {
                await asyncAction;
            }
        }
    }
}
