using NextcloudApp.Utils;
using Windows.Storage;

namespace NextcloudApp.Models
{
    /// <summary>
    /// Class for storing roaming settings which should be synchronized between devices.
    /// </summary>
    public class RoamingSettings : ObservableSettings
    {
        public static RoamingSettings Default { get; } = new RoamingSettings();

        public RoamingSettings()
            : base(ApplicationData.Current.RoamingSettings)
        {
        }

        public void Reset()
        {
            // Do not raise PropertyChanged event when resetting.
            EnableRaisePropertyChanged = false;

            //  Assign default values to your settings here.

            EnableRaisePropertyChanged = true;
        }
    }
}
