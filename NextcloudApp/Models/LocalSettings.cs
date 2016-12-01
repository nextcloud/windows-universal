using Newtonsoft.Json;
using NextcloudApp.Utils;
using Windows.Storage;

namespace NextcloudApp.Models
{
    /// <summary>
    /// Class for storing settings which should be stored on the local device only.
    /// </summary>
    public class LocalSettings : ObservableSettings
    {
        private static LocalSettings settings = new LocalSettings();
        private const string DefaultValueEmptyString = "";

        public static LocalSettings Default
        {
            get
            {
                return settings;
            }
        }

        public LocalSettings()
            : base(ApplicationData.Current.LocalSettings)
        {
        }

        [DefaultSettingValue(Value = DefaultValueEmptyString)]
        public string ServerAddress
        {
            get
            {
                return Get<string>();
            }
            set
            {
                Set(value);
            }
        }

        [DefaultSettingValue(Value = DefaultValueEmptyString)]
        public string Username
        {
            get
            {
                return Get<string>();
            }
            set
            {
                Set(value);
            }
        }

        [DefaultSettingValue(Value = true)]
        public bool ShowFileAndFolderGroupingHeader
        {
            get
            {
                return Get<bool>();
            }
            set
            {
                Set(value);
            }
        }

        // As only serializable objects can be stored in the LocalSettings, we use a string internally.
        [DefaultSettingValue(Value = PreviewImageDownloadMode.Always)]
        public PreviewImageDownloadMode PreviewImageDownloadMode
        {
            get
            {
                var strVal = Get<string>();

                if (string.IsNullOrEmpty(strVal))
                    return PreviewImageDownloadMode.Always;
                else
                    return JsonConvert.DeserializeObject<PreviewImageDownloadMode>(strVal);
            }
            set
            {
                var strVal = JsonConvert.SerializeObject(value);
                Set(strVal);
            }
        }

        [DefaultSettingValue(Value = 0)]
        public int AppTotalRuns
        {
            get
            {
                return Get<int>();
            }
            set
            {
                Set(value);
            }
        }

        [DefaultSettingValue(Value = DefaultValueEmptyString)]
        public string AppRunsAfterLastUpdateVersion
        {
            get
            {
                return Get<string>();
            }
            set
            {
                Set(value);
            }
        }

        [DefaultSettingValue(Value = 0)]
        public int AppRunsAfterLastUpdate
        {
            get
            {
                return Get<int>();
            }
            set
            {
                Set(value);
            }
        }

        public void Reset()
        {
            // Do not raise PropertyChanged event when resetting.
            this.enableRaisePropertyChanged = false;

            this.ServerAddress = DefaultValueEmptyString;
            this.Username = DefaultValueEmptyString;
            this.ShowFileAndFolderGroupingHeader = true;
            this.PreviewImageDownloadMode = PreviewImageDownloadMode.Always;

            this.enableRaisePropertyChanged = true;
        }
    }
}
