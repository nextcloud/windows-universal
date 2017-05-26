using Newtonsoft.Json;
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

        // As only serializable objects can be stored in the LocalSettings, we use a string internally.
        [DefaultSettingValue(Value = Theme.System)]
        public Theme Theme
        {
            get
            {
                var strVal = Get<string>();

                if (string.IsNullOrEmpty(strVal))
                    return Theme.System;
                else
                    return JsonConvert.DeserializeObject<Theme>(strVal);
            }
            set
            {
                var strVal = JsonConvert.SerializeObject(value);
                Set(strVal);
            }
        }

        public void Reset()
        {
            // Do not raise PropertyChanged event when resetting.
            EnableRaisePropertyChanged = false;

            //  Assign default values to your settings here.
            Theme = Theme.System;

            EnableRaisePropertyChanged = true;
        }
    }
}
