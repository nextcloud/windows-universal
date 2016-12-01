using NextcloudApp.Utils;
using Windows.Storage;

namespace NextcloudApp.Models
{
    /// <summary>
    /// Class for storing roaming settings which should be synchronized between devices.
    /// </summary>
    public class RoamingSettings : ObservableSettings
    {
        private static RoamingSettings settings = new RoamingSettings();
        private const string DefaultValueEmptyString = "";

        public static RoamingSettings Default
        {
            get
            {
                return settings;
            }
        }

        public RoamingSettings()
            : base(ApplicationData.Current.RoamingSettings)
        {
        }

        public void Reset()
        {
            // Do not raise PropertyChanged event when resetting.
            this.enableRaisePropertyChanged = false;

            //  Assign default values to your settings here.

            this.enableRaisePropertyChanged = true;
        }
    }
}
