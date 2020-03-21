﻿using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Compliance.Notifications.Commands.CheckDiskSpace;
using LanguageExt;
using Microsoft.Toolkit.Uwp.Notifications;
using DesktopNotificationManagerCompat = Compliance.Notifications.Helper.DesktopNotificationManagerCompat;

namespace Compliance.Notifications.ToastTemplates
{
    public static class ActionSnoozeDismissToastContent
    {
        public static async Task<ToastContent> CreateToastContent(ActionSnoozeDismissToastContentInfo contentInfo)
        {
            if (contentInfo == null) throw new ArgumentNullException(nameof(contentInfo));
            // Construct the visuals of the toast (using Notifications library)
            ToastContent toastContent = new ToastContent()
            {

                // Arguments when the user taps body of toast
                Launch = contentInfo.Action,

                Visual = new ToastVisual()
                {
                    BindingGeneric = new ToastBindingGeneric()
                    {
                        Attribution = new ToastGenericAttributionText() { Text = contentInfo.CompanyName },
                        Children =
                                {
                                    new AdaptiveText()
                                    {
                                        Text = contentInfo.Title
                                    },

                                    new AdaptiveImage()
                                    {
                                        // Non-Desktop Bridge apps cannot use HTTP images, so
                                        // we download and reference the image locally
                                        Source = await DownloadImage(contentInfo.ImageUri).ConfigureAwait(false)
                                    },

                                    new AdaptiveGroup()
                                    {
                                        Children =
                                            {
                                                new AdaptiveSubgroup()
                                                {
                                                    Children =
                                                    {
                                                        new AdaptiveText()
                                                        {
                                                            Text = contentInfo.ContentSection1,
                                                            HintWrap = true
                                                        }
                                                    }
                                                }
                                            }
                                    },

                                    new AdaptiveGroup()
                                    {
                                        Children =
                                        {
                                            new AdaptiveSubgroup()
                                            {
                                                Children =
                                                {
                                                    new AdaptiveText()
                                                    {
                                                        Text = contentInfo.ContentSection2,
                                                        HintWrap = true
                                                    }
                                                }
                                            }

                                        }
                                    }
                                },

                        AppLogoOverride = new ToastGenericAppLogo()
                        {
                            Source = await DownloadImage(contentInfo.AppLogoImageUri).ConfigureAwait(false),
                            HintCrop = ToastGenericAppLogoCrop.Circle
                        }
                    }
                },

                Actions = new ToastActionsCustom()
                {
                    Inputs =
                        {
                            new ToastSelectionBox("snoozeTime")
                            {
                                DefaultSelectionBoxItemId = "1",
                                Items =
                                {
                                    new ToastSelectionBoxItem("1","1 minute"),
                                    new ToastSelectionBoxItem("30","30 minutes"),
                                    new ToastSelectionBoxItem("60","1 hour"),
                                    new ToastSelectionBoxItem("240","4 hours"),
                                    new ToastSelectionBoxItem("480","8 hours"),
                                }
                            }
                        },

                    Buttons =
                            {
                                // Note that there's no reason to specify background activation, since our COM
                                // activator decides whether to process in background or launch foreground window
                                new ToastButton("Cleanup", "ms-settings:storagesense"){ActivationType = ToastActivationType.Protocol},
                                new ToastButton("Snooze", "snooze"){HintActionId = "snoozeTime",ActivationType = ToastActivationType.Background},
                                new ToastButton("Not Now", "dismiss"){ActivationType = ToastActivationType.Background},
                            }
                }
            };
            return toastContent;
        }

        //Source: https://github.com/WindowsNotifications/desktop-toasts
        private static async Task<Option<Uri>> DownloadImageToDisk(Uri httpImage)
        {
            // Toasts can live for up to 3 days, so we cache images for up to 3 days.
            // Note that this is a very simple cache that doesn't account for space usage, so
            // this could easily consume a lot of space within the span of 3 days.

            try
            {
                if (DesktopNotificationManagerCompat.CanUseHttpImages)
                {
                    return httpImage;
                }

                var directory = Directory.CreateDirectory(System.IO.Path.GetTempPath() + "github.com.trondr.Compliance.Notifications");
                foreach (var d in directory.EnumerateDirectories())
                {
                    if (d.CreationTimeUtc.Date < DateTime.UtcNow.Date.AddDays(-3))
                    {
                        d.Delete(true);
                    }
                }


                var dayDirectory = directory.CreateSubdirectory($"{DateTime.UtcNow.Day}");
                string imagePath = dayDirectory.FullName + "\\" + (uint)httpImage.GetHashCode();

                if (File.Exists(imagePath))
                {
                    return new Uri("file://" + imagePath);
                }

                using (var c = new HttpClient())
                {
                    using (var stream = await c.GetStreamAsync(httpImage).ConfigureAwait(false))
                    {
                        using (var fileStream = File.OpenWrite(imagePath))
                        {
                            stream.CopyTo(fileStream);
                        }
                    }
                }
                return new Uri("file://" + imagePath);
            }
            catch(HttpRequestException) { return Option<Uri>.None; }
        }

        private static async Task<string> DownloadImage(Uri httpImage)
        {
            var image = await DownloadImageToDisk(httpImage).ConfigureAwait(false);
            return image.Match(uri => uri.LocalPath, () => "");
        }
    }
}
