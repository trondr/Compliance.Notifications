﻿using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Compliance.Notifications.Applic.Common;
using Compliance.Notifications.Applic.ToastTemplates;
using Compliance.Notifications.Resources;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Toolkit.Uwp.Notifications;
using DirectoryInfo = Pri.LongPath.DirectoryInfo;

namespace Compliance.Notifications.Applic.DesktopDataCheck
{
    public static class DesktopData
    {
        public static Task<Result<DesktopDataInfo>> GetDesktopDataInfo()
        {
            //get desktop folder
            var desktopDirectory = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));
            //get desktop files
            var allFiles = desktopDirectory.GetFiles("*.*", SearchOption.AllDirectories);
            var allNonShortcutFiles = 
                allFiles
                .Where(info => !info.Name.EndsWith(".lnk",StringComparison.InvariantCulture))
                .Where(info => !info.Name.EndsWith("desktop.ini", StringComparison.InvariantCulture))
                .ToArray();
            var numberOfAllNonShortcutFiles = allNonShortcutFiles.Length;
            var sizeofAllNonShortcutFilesInBytes = allNonShortcutFiles.Sum(info => info.Length);
            return Task.FromResult(new Result<DesktopDataInfo>(new DesktopDataInfo {HasDesktopData = numberOfAllNonShortcutFiles > 0, NumberOfFiles = numberOfAllNonShortcutFiles, TotalSizeInBytes = sizeofAllNonShortcutFilesInBytes}));
        }

        public static async Task<Result<ToastNotificationVisibility>> ShowDesktopDataToastNotification(DesktopDataInfo desktopDataInfo, string companyName,string tag, string groupName)
        {
            return await F.ShowToastNotification(async () =>
            {
                var toastContentInfo = await GetCheckDesktopDataToastContentInfo(companyName, groupName, desktopDataInfo).ConfigureAwait(false);
                var toastContent = await ActionDismissToastContent.CreateToastContent(toastContentInfo).ConfigureAwait(true);
                return toastContent;
            }, tag, groupName).ConfigureAwait(false);
        }

        private static async Task<ActionDismissToastContentInfo> GetCheckDesktopDataToastContentInfo(string companyName, string groupName, DesktopDataInfo desktopDataInfo)
        {
            var title = string.Format(CultureInfo.InvariantCulture, strings.DesktopData_Title_F0, desktopDataInfo.TotalSizeInBytes.BytesToReadableString());
            var imageUri = new Uri($"https://picsum.photos/364/202?image={F.Rnd.Next(1, 900)}");
            var appLogoImageUri = new Uri("https://unsplash.it/64?image=1005");
            var content = strings.DesktopData_Content;
            var content2 = strings.DesktopData_Content2;
            var action = ToastActions.CreateMyDocumentsShortcut;
            var actionActivationType = ToastActivationType.Foreground;
            var greeting = await F.GetGreeting().ConfigureAwait(false);
            return new ActionDismissToastContentInfo(greeting, title, companyName, content, content2,
                imageUri, appLogoImageUri, action, actionActivationType, strings.Desktop_Action_Button_Content, strings.NotNowActionButtonContent, ToastActions.Dismiss, groupName);
        }

        public static async Task<DesktopDataInfo> LoadDesktopDataInfo()
        {
            return await F.LoadUserComplianceItemResultOrDefault(DesktopDataInfo.Default).ConfigureAwait(false);
        }
    }
}