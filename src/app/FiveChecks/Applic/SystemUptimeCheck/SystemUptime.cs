﻿using System;
using System.Globalization;
using System.Threading.Tasks;
using FiveChecks.Applic.Common;
using FiveChecks.Applic.ToastTemplates;
using FiveChecks.Resources;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Toolkit.Uwp.Notifications;

namespace FiveChecks.Applic.SystemUptimeCheck
{
    public static class SystemUptime 
    {
        /// <summary>
        /// Get the system uptime
        /// </summary>
        /// <returns></returns>
        public static TimeSpan GetSystemUptime()
        {
            var millisecondsSinceLastRestart = (long)NativeMethods.GetTickCount64();
            var ticksSinceLastRestart = millisecondsSinceLastRestart * TimeSpan.TicksPerMillisecond;
            return new TimeSpan(ticksSinceLastRestart);
        }

        /// <summary>
        /// Get the time of the last restart.
        /// </summary>
        /// <returns></returns>
        public static DateTime GetLastRestartTime()
        {
            return DateTime.Now.Add(-GetSystemUptime());
        }

        public static async Task<Result<SystemUptimeInfo>> GetSystemUptimeInfo()
        {
            SystemUptimeInfo systemUptimeInfo = new SystemUptimeInfo() { Uptime = GetSystemUptime(), LastRestart = GetLastRestartTime() };
            return await Task.FromResult(new Result<SystemUptimeInfo>(systemUptimeInfo)).ConfigureAwait(false);
        }

        public static async Task<Result<ToastNotificationVisibility>> ShowSystemUptimeToastNotification(Some<NotificationProfile> userProfile, string tag, string groupName, TimeSpan systemUptime)
        {
            return await ToastHelper.ShowToastNotification(async () =>
            {
                var toastContentInfo = GetCheckSystemUptimeToastContentInfo(userProfile, groupName, systemUptime);
                var toastContent = await ActionDismissToastContent.CreateToastContent(toastContentInfo).ConfigureAwait(true);
                return toastContent;
            }, tag, groupName).ConfigureAwait(false);
        }

        private static ActionDismissToastContentInfo GetCheckSystemUptimeToastContentInfo(Some<NotificationProfile> notificationProfile, string groupName, TimeSpan systemUptime)
        {
            var title = strings.SystemUptime_Title;
            var content = string.Format(CultureInfo.InvariantCulture, strings.SystemUptimeContent_F0, systemUptime.TimeSpanToString());
            var content2 = strings.SystemUptimeContent2;
            var action = ToastActions.Restart;
            var actionActivationType = ToastActivationType.Foreground;
            var greeting = F.GetGreeting(notificationProfile);
            return new ActionDismissToastContentInfo(greeting, title, content, content2, action, actionActivationType, strings.SystemUptime_Action_Button_Content, strings.NotNowActionButtonContent, ToastActions.Dismiss, groupName, Option<string>.None, notificationProfile.Value.CompanyName);
        }

        public static async Task<SystemUptimeInfo> LoadSystemUptimeInfo()
        {
            return await ComplianceInfo.LoadSystemComplianceItemResultOrDefault(SystemUptimeInfo.Default).ConfigureAwait(false);
        }
        
    }
}
