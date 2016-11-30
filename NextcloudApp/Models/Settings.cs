using Newtonsoft.Json;
using NextcloudApp.Utils;
using Windows.Storage;

namespace NextcloudApp.Models
{
    public class Settings : ObservableSettings
    {
        private static Settings settings = new Settings();
        private const string DefaultValueEmptyString = "";

        public static Settings Default
        {
            get
            {
                return settings;
            }
        }

        public Settings()
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

        [DefaultSettingValue(Value = PreviewImageDownloadMode.Always)]
        public PreviewImageDownloadMode PreviewImageDownloadMode
        {
            get
            {                
                var strVal = Get<string>();
                return JsonConvert.DeserializeObject<PreviewImageDownloadMode>(strVal);
            }
            set
            {
                var enumVal = JsonConvert.SerializeObject(value);
                Set(enumVal);
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
    }
}
