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
        private const string DefaultValueEmptyString = "";

        public static LocalSettings Default { get; } = new LocalSettings();

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

        [DefaultSettingValue(Value = false)]
        public bool UseWindowsHello
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

        [DefaultSettingValue(Value = false)]
        public bool ShowUpdateMessage
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

        [DefaultSettingValue(Value = false)]
        public bool IgnoreServerCertificateErrors
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
        [DefaultSettingValue(Value = GroupMode.GroupByNameAscending)]
        public GroupMode GroupMode
        {
            get
            {
                var strVal = Get<string>();

                if (string.IsNullOrEmpty(strVal))
                    return GroupMode.GroupByNameAscending;
                else
                    return JsonConvert.DeserializeObject<GroupMode>(strVal);
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
            enableRaisePropertyChanged = false;

            ServerAddress = DefaultValueEmptyString;
            Username = DefaultValueEmptyString;
            ShowFileAndFolderGroupingHeader = true;
            PreviewImageDownloadMode = PreviewImageDownloadMode.Always;
            UseWindowsHello = false;
            GroupMode = GroupMode.GroupByNameAscending;

            enableRaisePropertyChanged = true;
        }
    }
}
