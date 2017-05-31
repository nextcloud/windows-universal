using System.ComponentModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using NextcloudApp.Services;
using NextcloudApp.Utils;

namespace NextcloudApp.Controls
{
    public class ThemeablePage : Page
    {
        public ThemeablePage()
        {
            var theme = SettingsService.Instance.RoamingSettings.Theme;
            switch (theme)
            {
                case Theme.Dark:
                    RequestedTheme = ElementTheme.Dark;
                    break;
                case Theme.Light:
                    RequestedTheme = ElementTheme.Light;
                    break;
            }

            SettingsService.Instance.RoamingSettings.PropertyChanged += RoamingSettingsOnPropertyChanged;
        }

        private void RoamingSettingsOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("Theme"))
            {
                var theme = SettingsService.Instance.RoamingSettings.Theme;
                switch (theme)
                {
                    case Theme.Dark:
                        RequestedTheme = ElementTheme.Dark;
                        break;
                    case Theme.Light:
                        RequestedTheme = ElementTheme.Light;
                        break;
                }
            }
        }
    }
}
