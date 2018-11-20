using System;
using System.Threading;
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

        public static Lazy<LocalSettings> Default = new Lazy<LocalSettings>(() => new LocalSettings(), LazyThreadSafetyMode.ExecutionAndPublication);
        
        public LocalSettings()
            : base(ApplicationData.Current.LocalSettings)
        {
        }

        [DefaultSettingValue(Value = DefaultValueEmptyString)]
        public string ServerAddress
        {
            get => Get<string>();
            set => Set(value);
        }

        [DefaultSettingValue(Value = DefaultValueEmptyString)]
        public string Username
        {
            get => Get<string>();
            set => Set(value);
        }

        [DefaultSettingValue(Value = true)]
        public bool ShowFileAndFolderGroupingHeader
        {
            get => Get<bool>();
            set => Set(value);
        }

        // As only serializable objects can be stored in the LocalSettings, we use a string internally.
        [DefaultSettingValue(Value = PreviewImageDownloadMode.Always)]
        public PreviewImageDownloadMode PreviewImageDownloadMode
        {
            get
            {
                var strVal = Get<string>();

                return string.IsNullOrEmpty(strVal) ? PreviewImageDownloadMode.Always : JsonConvert.DeserializeObject<PreviewImageDownloadMode>(strVal);
            }
            set
            {
                var strVal = JsonConvert.SerializeObject(value);
                Set(strVal);
            }
        }

        [DefaultSettingValue(Value = SyncMode.LocalToRemote)]
        public SyncMode SyncMode
        {
            get
            {
                var strVal = Get<string>();

                return string.IsNullOrEmpty(strVal) ? SyncMode.LocalToRemote : JsonConvert.DeserializeObject<SyncMode>(strVal);
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
            get => Get<int>();
            set => Set(value);
        }

        [DefaultSettingValue(Value = DefaultValueEmptyString)]
        public string AppRunsAfterLastUpdateVersion
        {
            get => Get<string>();
            set => Set(value);
        }

        [DefaultSettingValue(Value = 0)]
        public int AppRunsAfterLastUpdate
        {
            get => Get<int>();
            set => Set(value);
        }

        [DefaultSettingValue(Value = false)]
        public bool UseWindowsHello
        {
            get => Get<bool>();
            set => Set(value);
        }

        [DefaultSettingValue(Value = false)]
        public bool ShowUpdateMessage
        {
            get => Get<bool>();
            set => Set(value);
        }

        [DefaultSettingValue(Value = false)]
        public bool IgnoreServerCertificateErrors
        {
            get => Get<bool>();
            set => Set(value);
        }

        [DefaultSettingValue(Value = false)]
        public bool SyncDeletions
        {
            get => Get<bool>();
            set => Set(value);
        }

        [DefaultSettingValue(Value = false)]
        public bool ExpertMode
        {
            get => Get<bool>();
            set => Set(value);
        }

        // As only serializable objects can be stored in the LocalSettings, we use a string internally.
        [DefaultSettingValue(Value = GroupMode.GroupByNameAscending)]
        public GroupMode GroupMode
        {
            get
            {
                var strVal = Get<string>();

                return string.IsNullOrEmpty(strVal) ? GroupMode.GroupByNameAscending : JsonConvert.DeserializeObject<GroupMode>(strVal);
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

            ServerAddress = DefaultValueEmptyString;
            Username = DefaultValueEmptyString;
            ShowFileAndFolderGroupingHeader = true;
            PreviewImageDownloadMode = PreviewImageDownloadMode.Always;
            UseWindowsHello = false;
            GroupMode = GroupMode.GroupByNameAscending;
            ExpertMode = false;

            EnableRaisePropertyChanged = true;
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        public void Dispose()
        {
            if (Default.IsValueCreated)
            {
                Default = new Lazy<LocalSettings>(() => new LocalSettings(), LazyThreadSafetyMode.ExecutionAndPublication);
            }
        }
    }
}
