using System;
using System.Threading;
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
        public static Lazy<RoamingSettings> Default = new Lazy<RoamingSettings>(() => new RoamingSettings(), LazyThreadSafetyMode.ExecutionAndPublication);

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

                return string.IsNullOrEmpty(strVal) ? Theme.System : JsonConvert.DeserializeObject<Theme>(strVal);
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

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        public void Dispose()
        {
            if (Default.IsValueCreated)
            {
                Default = new Lazy<RoamingSettings>(() => new RoamingSettings(), LazyThreadSafetyMode.ExecutionAndPublication);
            }
        }
    }
}
