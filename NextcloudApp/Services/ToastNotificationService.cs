using System;
using Windows.ApplicationModel.Resources;
using Windows.UI.Notifications;
using Microsoft.QueryStringDotNET;
using Microsoft.Toolkit.Uwp.Notifications;
using NextcloudApp.Models;

namespace NextcloudApp.Services
{
    internal class ToastNotificationService
    {
        public const string SyncAction = "syncAction";
        public const string SyncConflictAction = "syncConflict";

        public static void ShowSyncFinishedNotification(string folder, int changes, int errors)
        {
            if(errors == 0 && changes == 0)
            {
                // Don't spam the people when nothing happened.
                return;
            }
            var loader = new ResourceLoader();
            var title = loader.GetString("SyncFinishedTitle");
            string content;
            string action;
            if (errors == 0)
            {
                action = SyncAction;
                content = string.Format(loader.GetString("SyncFinishedSuccessful"), folder, changes);
            }
            else
            {
                action = SyncConflictAction;
                content = string.Format(loader.GetString("SyncFinishedConflicts"), folder, changes, errors);
            }
            // Construct the visuals of the toast
            var visual = new ToastVisual
            {
                BindingGeneric = new ToastBindingGeneric
                {
                    Children =
                    {
                        new AdaptiveText
                        {
                            Text = title
                        },
                        new AdaptiveText
                        {
                            Text = content
                        }
                    }
                }
            };
            var toastContent = new ToastContent
            {
                Visual = visual,

                // Arguments when the user taps body of toast
                Launch = new QueryString
                {
                    { "action", action }
                }.ToString()
            };
            var toast = new ToastNotification(toastContent.GetXml())
            {
                ExpirationTime = DateTime.Now.AddMinutes(30),
                Group = action
            };
            // TODO Replace with syncinterval from settings.
            // TODO groups/tags?
            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }

        internal static void ShowSyncSuspendedNotification(FolderSyncInfo fsi)
        {
            if(fsi == null)
            {
                return;
            }
            var loader = new ResourceLoader();
            var title = loader.GetString("SyncSuspendedTitle");
            var content = string.Format(loader.GetString("SyncSuspendedDescription"), fsi.Path);
            const string action = SyncAction;
             
            // Construct the visuals of the toast
            var visual = new ToastVisual
            {
                BindingGeneric = new ToastBindingGeneric
                {
                    Children =
                    {
                        new AdaptiveText
                        {
                            Text = title
                        },
                        new AdaptiveText
                        {
                            Text = content
                        }
                    }
                }
            };
            var toastContent = new ToastContent
            {
                Visual = visual,

                // Arguments when the user taps body of toast
                Launch = new QueryString
                {
                    { "action", action }
                }.ToString()
            };
            var toast = new ToastNotification(toastContent.GetXml())
            {
                ExpirationTime = DateTime.Now.AddMinutes(30),
                Group = action
            };
            // TODO Replace with syncinterval from settings.
            // TODO groups/tags?
            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }
    }
}
