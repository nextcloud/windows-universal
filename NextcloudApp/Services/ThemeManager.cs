using System.ComponentModel;
using Windows.Foundation.Metadata;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using NextcloudApp.Utils;

namespace NextcloudApp.Services
{
    class ThemeManager
    {
        private static ThemeManager _instance;

        public static ThemeManager Instance => _instance ?? (_instance = new ThemeManager());

        public void Initialize()
        {
            if (!new AccessibilitySettings().HighContrast)
            {
                var color = (Color)Application.Current.Resources["SystemAccentColor"];

                if (SettingsService.Default.Value.RoamingSettings.ThemeColor == ThemeColor.Nextcloud)
                {
                    color = Color.FromArgb(255, 0, 130, 201);

                    Application.Current.Resources["SystemAccentColorLight3"] = ChangeColorBrightness(color, 0.3f);
                    Application.Current.Resources["SystemAccentColorLight2"] = ChangeColorBrightness(color, 0.2f);
                    Application.Current.Resources["SystemAccentColorLight1"] = ChangeColorBrightness(color, 0.1f);
                    Application.Current.Resources["SystemAccentColor"] = color;
                    Application.Current.Resources["SystemAccentColorDark1"] = ChangeColorBrightness(color, -0.1f);
                    Application.Current.Resources["SystemAccentColorDark2"] = ChangeColorBrightness(color, -0.2f);
                    Application.Current.Resources["SystemAccentColorDark3"] = ChangeColorBrightness(color, -0.3f);
                }

                Application.Current.Resources["HyperlinkButtonForeground"] = color;

                // Get the instance of the Title Bar
                var titleBar = ApplicationView.GetForCurrentView().TitleBar;

                if (titleBar != null)
                {
                    // Set the color of the Title Bar content
                    titleBar.BackgroundColor = (Color) Application.Current.Resources["SystemAccentColorDark1"];
                    titleBar.ForegroundColor = Colors.White;

                    titleBar.InactiveBackgroundColor = (Color) Application.Current.Resources["SystemAccentColorDark1"];
                    titleBar.InactiveForegroundColor = ChangeColorBrightness(color, 0.5f);

                    // Set the color of the Title Bar buttons
                    titleBar.ButtonBackgroundColor = (Color) Application.Current.Resources["SystemAccentColorDark1"];
                    titleBar.ButtonForegroundColor = Colors.White;

                    titleBar.ButtonInactiveBackgroundColor =
                        (Color) Application.Current.Resources["SystemAccentColorDark1"];
                    titleBar.ButtonInactiveForegroundColor = ChangeColorBrightness(color, 0.7f);

                    titleBar.ButtonHoverBackgroundColor = color;
                    titleBar.ButtonHoverForegroundColor = Colors.White;

                    titleBar.ButtonPressedBackgroundColor =
                        (Color) Application.Current.Resources["SystemAccentColorLight1"];
                    titleBar.ButtonPressedForegroundColor = Colors.White;
                }

                if (!ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
                {
                    return;
                }

                var statusBar = StatusBar.GetForCurrentView();
                statusBar.BackgroundOpacity = 1;

                statusBar.BackgroundColor = (Color)Application.Current.Resources["SystemAccentColorDark1"];
                statusBar.ForegroundColor = Colors.White;

                //var theme = SettingsService.Default.Value.RoamingSettings.Theme;
                //switch (theme)
                //{
                //    case Theme.Dark:
                //        statusBar.BackgroundColor = Colors.Black;
                //        statusBar.ForegroundColor = Colors.White;
                //        break;
                //    case Theme.Light:
                //        statusBar.BackgroundColor = Colors.White;
                //        statusBar.ForegroundColor = Colors.Black;
                //        break;
                //}

                //SettingsService.Default.Value.RoamingSettings.PropertyChanged += RoamingSettingsOnPropertyChanged;
            }
        }

        //private void RoamingSettingsOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        //{
        //    if (!e.PropertyName.Equals("Theme"))
        //    {
        //        return;
        //    }
        //    var statusBar = StatusBar.GetForCurrentView();
        //    statusBar.BackgroundOpacity = 1;
        //    var theme = SettingsService.Default.Value.RoamingSettings.Theme;
        //    switch (theme)
        //    {
        //        case Theme.Dark:
        //            statusBar.BackgroundColor = Colors.Black;
        //            statusBar.ForegroundColor = Colors.White;
        //            break;
        //        case Theme.Light:
        //            statusBar.BackgroundColor = Colors.White;
        //            statusBar.ForegroundColor = Colors.Black;
        //            break;
        //    }
        //}

        /// <summary>
        /// Creates color with corrected brightness.
        /// </summary>
        /// <param name="color">Color to correct.</param>
        /// <param name="correctionFactor">The brightness correction factor. Must be between -1 and 1. 
        /// Negative values produce darker colors.</param>
        /// <returns>
        /// Corrected <see cref="Color"/> structure.
        /// </returns>
        public static Color ChangeColorBrightness(Color color, float correctionFactor)
        {
            var red = (float)color.R;
            var green = (float)color.G;
            var blue = (float)color.B;

            if (correctionFactor < 0)
            {
                correctionFactor = 1 + correctionFactor;
                red *= correctionFactor;
                green *= correctionFactor;
                blue *= correctionFactor;
            }
            else
            {
                red = (255 - red) * correctionFactor + red;
                green = (255 - green) * correctionFactor + green;
                blue = (255 - blue) * correctionFactor + blue;
            }

            return Color.FromArgb(color.A, (byte)red, (byte)green, (byte)blue);
        }
    }
}
