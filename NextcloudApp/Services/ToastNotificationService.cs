using Microsoft.QueryStringDotNET;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.UI.Notifications;
using NextcloudApp.Models;

namespace NextcloudApp.Services
{
    class ToastNotificationService
    {
        public const string SYNCACTION = "syncAction";
        public const string SYNCONFLICTACTION = "syncConflict";

        public static void ShowSyncFinishedNotification(string folder, int changes, int errors)
        {
            if(errors == 0 && changes == 0)
            {
                // Don't spam the people when nothing happened.
                return;
            }
            ResourceLoader loader = new ResourceLoader();
            string title = loader.GetString("SyncFinishedTitle");
            string content;
            string action;
            if (errors == 0)
            {
                action = SYNCACTION;
                content = String.Format(loader.GetString("SyncFinishedSuccessful"), folder, changes);
            } else {
                action = SYNCONFLICTACTION;
                content = String.Format(loader.GetString("SyncFinishedConflicts"), folder, changes, errors);
            }
            // Construct the visuals of the toast
            ToastVisual visual = new ToastVisual()
            {
                BindingGeneric = new ToastBindingGeneric()
                {
                    Children =
                    {
                        new AdaptiveText()
                        {
                            Text = title
                        },
                        new AdaptiveText()
                        {
                            Text = content
                        }
                    }
                }
            };
            ToastContent toastContent = new ToastContent()
            {
                Visual = visual,

                // Arguments when the user taps body of toast
                Launch = new QueryString()
                {
                    { "action", action }
                }.ToString()
            };
            var toast = new ToastNotification(toastContent.GetXml());
            toast.ExpirationTime = DateTime.Now.AddMinutes(30); // TODO Replace with syncinterval from settings.
            // TODO groups/tags?
            toast.Group = action;
            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }

        internal static void ShowSyncSuspendedNotification(FolderSyncInfo fsi)
        {
            if(fsi == null)
            {
                return;
            }
            ResourceLoader loader = new ResourceLoader();
            string title = loader.GetString("SyncSuspendedTitle");
            string content = String.Format(loader.GetString("SyncSuspendedDescription"), fsi.Path);
            string action = SYNCACTION;
             
            // Construct the visuals of the toast
            ToastVisual visual = new ToastVisual()
            {
                BindingGeneric = new ToastBindingGeneric()
                {
                    Children =
                    {
                        new AdaptiveText()
                        {
                            Text = title
                        },
                        new AdaptiveText()
                        {
                            Text = content
                        }
                    }
                }
            };
            ToastContent toastContent = new ToastContent()
            {
                Visual = visual,

                // Arguments when the user taps body of toast
                Launch = new QueryString()
                {
                    { "action", action }
                }.ToString()
            };
            var toast = new ToastNotification(toastContent.GetXml());
            toast.ExpirationTime = DateTime.Now.AddMinutes(30); // TODO Replace with syncinterval from settings.
            // TODO groups/tags?
            toast.Group = action;
            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }
    }
}
