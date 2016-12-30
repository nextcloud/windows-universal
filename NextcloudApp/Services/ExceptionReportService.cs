using System;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.Email;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Networking.Connectivity;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System.Profile;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Microsoft.Practices.Unity;
using Prism.Windows.AppModel;

namespace NextcloudApp.Services
{
    public class ExceptionReportService
    {
        private static ContentDialog _dlg;
        
        public static async Task Handle(string exceptionType, string exceptionMessage, string exceptionStackTrace,
            string innerExceptionType, string exceptionHashCode)
        {
            if (_dlg != null)
            {
                return;
            }

            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                var app = Application.Current as App;
                if (app == null)
                {
                    return;
                }
                var resourceLoader = app.Container.Resolve<IResourceLoader>();
                var dialogService = app.Container.Resolve< DialogService>();

                _dlg = new ContentDialog
                {
                    Title = resourceLoader.GetString("ApplicationError"),
                    Content = new TextBlock
                    {
                        Text = resourceLoader.GetString("ApplicationError_Description"),
                        TextWrapping = TextWrapping.WrapWholeWords,
                        Margin = new Thickness(0, 20, 0, 0)
                    },
                    PrimaryButtonText = resourceLoader.GetString("Yes"),
                    SecondaryButtonText = resourceLoader.GetString("No")
                };
                _dlg.IsPrimaryButtonEnabled = _dlg.IsSecondaryButtonEnabled = true;

                try
                {
                    var result = await dialogService.ShowAsync(_dlg);

                    if (result != ContentDialogResult.Primary)
                    {
                        return;
                    }

                    var stringBuilder = new StringBuilder();

                    stringBuilder.AppendFormat("[AppId]:[{0}]", Package.Current.Id.Name);
                    stringBuilder.AppendLine();
                    stringBuilder.AppendFormat("[Type]:[{0}]", exceptionType);
                    stringBuilder.AppendLine();
                    stringBuilder.AppendFormat("[ExceptionMessage]:[{0}]", exceptionMessage);
                    stringBuilder.AppendLine();
                    stringBuilder.AppendFormat("[StackTrace]:[\n{0}\n]", exceptionStackTrace);
                    stringBuilder.AppendLine();
                    stringBuilder.AppendFormat("[InnerException]:[{0}]", innerExceptionType);
                    stringBuilder.AppendLine();

                    stringBuilder.AppendFormat("[OccurrenceDate]:[{0:yyyy-MM-dd hh:mm:ss} GMT]",
                        DateTime.Now.ToUniversalTime());
                    stringBuilder.AppendLine();

                    var packages =
                        Windows.Phone.Management.Deployment.InstallationManager.FindPackagesForCurrentPublisher();
                    var package =
                        packages.FirstOrDefault(
                            p => p.Id.ProductId == string.Concat("{", Package.Current.Id.Name.ToUpper(), "}"));
                    if (package != null)
                    {
                        stringBuilder.AppendFormat("[AppInstallDate]:[{0:yyyy-MM-dd hh:mm:ss} GMT]",
                            package.InstallDate.ToUniversalTime());
                        stringBuilder.AppendLine();
                    }

                    stringBuilder.AppendFormat("[AppTotalRuns]:[{0}]",
                        SettingsService.Instance.LocalSettings.AppTotalRuns);
                    stringBuilder.AppendLine();

                    stringBuilder.AppendFormat("[AppRunsAfterLastUpdate]:[{0}]",
                        SettingsService.Instance.LocalSettings.AppRunsAfterLastUpdate);
                    stringBuilder.AppendLine();

                    stringBuilder.Append(
                        $"[AppVersion]:[{Package.Current.Id.Version.Major}.{Package.Current.Id.Version.Minor}.{Package.Current.Id.Version.Build}.{Package.Current.Id.Version.Revision}]");
                    stringBuilder.AppendLine();

                    stringBuilder.AppendFormat("[Culture]:[{0}]", CultureInfo.CurrentCulture.Name);
                    stringBuilder.AppendLine();

                    var dispatcher = CoreWindow.GetForCurrentThread().Dispatcher;

                    if (dispatcher.HasThreadAccess && Window.Current.Content != null)
                    {
                        var rootFrame = Window.Current.Content as Frame;

                        if (rootFrame?.CurrentSourcePageType != null)
                        {
                            stringBuilder.AppendFormat("[CurrentPageSource]:[{0}]",
                                rootFrame.CurrentSourcePageType.FullName);
                            stringBuilder.AppendLine();
                            stringBuilder.AppendFormat("[NavigationStack]:[{0}]", GetNavigationStackInfo(rootFrame));
                            stringBuilder.AppendLine();
                        }
                    }

                    var deviceInfo =
                        new Windows.Security.ExchangeActiveSyncProvisioning.EasClientDeviceInformation();

                    stringBuilder.AppendFormat("[DeviceManufacturer]:[{0}]", deviceInfo.SystemManufacturer);
                    stringBuilder.AppendLine();
                    stringBuilder.AppendFormat("[DeviceModel]:[{0}]", deviceInfo.SystemProductName);
                    stringBuilder.AppendLine();
                    stringBuilder.AppendFormat("[DeviceHardwareVersion]:[{0}]", deviceInfo.SystemHardwareVersion);
                    stringBuilder.AppendLine();
                    stringBuilder.AppendFormat("[DeviceFirmwareVersion]:[{0}]", deviceInfo.SystemFirmwareVersion);
                    stringBuilder.AppendLine();

                    stringBuilder.AppendFormat("[DeviceId]:[{0}]", deviceInfo.Id);
                    stringBuilder.AppendLine();

                    stringBuilder.AppendFormat("[OperatingSystem]:[{0}]", deviceInfo.OperatingSystem);
                    stringBuilder.AppendLine();

                    var deviceFamilyVersion = AnalyticsInfo.VersionInfo.DeviceFamilyVersion;
                    var version = ulong.Parse(deviceFamilyVersion);
                    var major = (version & 0xFFFF000000000000L) >> 48;
                    var minor = (version & 0x0000FFFF00000000L) >> 32;
                    var build = (version & 0x00000000FFFF0000L) >> 16;
                    var revision = (version & 0x000000000000FFFFL);

                    stringBuilder.AppendFormat("[OSVersion]:[{0}]", $"{major}.{minor}.{build}.{revision}");
                    stringBuilder.AppendLine();

                    stringBuilder.AppendFormat("[Architecture]:[{0}]", Package.Current.Id.Architecture);
                    stringBuilder.AppendLine();

                    var connectionProfile = NetworkInformation.GetInternetConnectionProfile();

                    if (connectionProfile != null)
                    {
                        stringBuilder.AppendFormat("[NetworkType]:[{0}]", connectionProfile.ProfileName);
                        stringBuilder.AppendLine();
                    }

                    stringBuilder.AppendFormat("[FriendlyName]:[{0}]", deviceInfo.FriendlyName);
                    stringBuilder.AppendLine();

                    stringBuilder.AppendFormat("[DeviceTotalMemory(Mb)]:[{0}]",
                        Windows.System.MemoryManager.AppMemoryUsageLimit/1048576f);
                    stringBuilder.AppendLine();
                    stringBuilder.AppendFormat("[AppCurrentMemoryUsage(Mb)]:[{0}]",
                        Windows.System.MemoryManager.AppMemoryUsage/1048576f);
                    stringBuilder.AppendLine();

                    stringBuilder.AppendFormat("[IsoStorageAvailableSpaceLocal(Mb)]:[{0}]",
                        await GetFreeSpace(ApplicationData.Current.LocalFolder)/1048576f);
                    stringBuilder.AppendLine();
                    stringBuilder.AppendFormat("[IsoStorageAvailableSpaceTemporary(Mb)]:[{0}]",
                        await GetFreeSpace(ApplicationData.Current.TemporaryFolder)/1048576f);
                    stringBuilder.AppendLine();
                    stringBuilder.AppendFormat("[IsoStorageAvailableSpaceRoaming(Mb)]:[{0}]",
                        await GetFreeSpace(ApplicationData.Current.RoamingFolder)/1048576f);
                    stringBuilder.AppendLine();

                    if (dispatcher.HasThreadAccess && Window.Current.Content != null)
                    {
                        var rootFrame = Window.Current.Content as Frame;

                        var visualTreeBitmap = new RenderTargetBitmap();
                        await visualTreeBitmap.RenderAsync(rootFrame);

                        var pixels = (await visualTreeBitmap.GetPixelsAsync()).ToArray();

                        using (var memoryStream = new InMemoryRandomAccessStream())
                        {
                            // Set target compression quality
                            var propertySet = new BitmapPropertySet();
                            var qualityValue = new BitmapTypedValue(0.75, PropertyType.Single);
                            propertySet.Add("ImageQuality", qualityValue);

                            var encoder =
                                await
                                    BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, memoryStream, propertySet);
                            encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight,
                                (uint) visualTreeBitmap.PixelWidth, (uint) visualTreeBitmap.PixelHeight, 96, 96,
                                pixels);

                            // Downsize
                            const uint baselinesize = 480;
                            double h = visualTreeBitmap.PixelHeight;
                            double w = visualTreeBitmap.PixelWidth;

                            uint scaledHeight, scaledWidth;

                            if (h >= w)
                            {
                                scaledHeight = baselinesize;
                                scaledWidth = (uint) (baselinesize*(w/h));
                            }
                            else
                            {
                                scaledWidth = baselinesize;
                                scaledHeight = (uint) (baselinesize*(h/w));
                            }

                            encoder.BitmapTransform.ScaledHeight = scaledHeight;
                            encoder.BitmapTransform.ScaledWidth = scaledWidth;

                            await encoder.FlushAsync();

                            memoryStream.Seek(0L);

                            // Read the bytes
                            var reader = new DataReader(memoryStream.GetInputStreamAt(0));
                            var bytes = new byte[memoryStream.Size];
                            await reader.LoadAsync((uint) memoryStream.Size);
                            reader.ReadBytes(bytes);

                            stringBuilder.AppendFormat("[ScreenshotBase64String]:[\n{0}\n]", Convert.ToBase64String(bytes));
                            stringBuilder.AppendLine();
                        }
                    }

                    var mail = new EmailMessage
                    {
                        Subject =
                            $"Error report: [{Package.Current.Id.Name}] [{Package.Current.Id.Version.Major}.{Package.Current.Id.Version.Minor}.{Package.Current.Id.Version.Build}.{Package.Current.Id.Version.Revision}]. Hashcode: [{exceptionHashCode}]",
                        Body = "see attachment"
                    };
                    mail.To.Add(new EmailRecipient("nextcloud.app@andrefiedler.de"));

                    var file =
                        await
                            ApplicationData.Current.LocalFolder.CreateFileAsync("crash-log.txt",
                                CreationCollisionOption.ReplaceExisting);
                    await FileIO.WriteTextAsync(file, stringBuilder.ToString());

                    var attachment = new EmailAttachment(file.Name, file);
                    mail.Attachments.Add(attachment);
                    
                    await EmailManager.ShowComposeNewEmailAsync(mail);
                }
                // ReSharper disable once EmptyGeneralCatchClause
                catch
                {
                }

                _dlg = null;
            });
        }

        private static string GetNavigationStackInfo(Frame rootFrame)
        {
            if (rootFrame == null)
            {
                return string.Empty;
            }
            var stringBuilder = new StringBuilder();
            foreach (var journalEntry in rootFrame.BackStack)
            {
                stringBuilder.AppendLine();
                stringBuilder.Append(journalEntry.SourcePageType.FullName);
            }
            stringBuilder.AppendLine();
            return stringBuilder.ToString();
        }

        public static async Task<ulong> GetFreeSpace(StorageFolder folder)
        {
            var retrivedProperties = await folder.Properties.RetrievePropertiesAsync(new[] {"System.FreeSpace"});
            return (ulong) retrivedProperties["System.FreeSpace"];
        }
    }
}
