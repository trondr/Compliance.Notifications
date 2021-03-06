﻿using System;
using System.Threading.Tasks;
using FiveChecks.Applic.Common;
using LanguageExt;
using LanguageExt.Common;

namespace FiveChecks.Applic.SystemUptimeCheck
{
    public static class CheckSystemUptimeCommand
    {
        internal static async Task<Result<ToastNotificationVisibility>> CheckSystemUptimePure(
            Func<Task<SystemUptimeInfo>> loadInfo,
            Func<SystemUptimeInfo,bool> isNonCompliant,
            Func<TimeSpan,Task<Result<ToastNotificationVisibility>>> showToastNotification,
            Func<Task<Result<ToastNotificationVisibility>>> removeToastNotification, bool isDisabled)
        {
            if(isDisabled) return await removeToastNotification().ConfigureAwait(false);
            var info = await loadInfo().ConfigureAwait(false);
            if (isNonCompliant(info))
            {
                return await showToastNotification(info.Uptime).ConfigureAwait(false);
            }
            return await removeToastNotification().ConfigureAwait(false);
        }

        public static async Task<Result<ToastNotificationVisibility>> CheckSystemUptime(Some<NotificationProfile> userProfile, double maxUpTimeHours, bool isDisabled)
        {
            var category = typeof(CheckSystemUptimeCommand).GetPolicyCategory();
            var policyMaxUptimeHours = Profile.GetIntegerPolicyValue(Context.Machine, category, "MaxUptimeHours", (int)maxUpTimeHours);
            var groupName = ToastGroups.CheckSystemUptime;
            var tag = ToastGroups.CheckSystemUptime;
            var systemUptimeCheckIsDisabled = Profile.IsCheckDisabled(isDisabled, typeof(CheckSystemUptimeCommand));
            bool IsNonCompliant(SystemUptimeInfo info) => info.Uptime.TotalHours > (double)policyMaxUptimeHours;
            return await CheckSystemUptimePure(() => ComplianceInfo.LoadInfo<SystemUptimeInfo>(SystemUptime.LoadSystemUptimeInfo, IsNonCompliant, ScheduledTasks.ComplianceSystemMeasurements, true),
                IsNonCompliant,
                (uptime) => SystemUptime.ShowSystemUptimeToastNotification(userProfile, tag, groupName, uptime), 
                () => ToastHelper.RemoveToastNotification(groupName),
                systemUptimeCheckIsDisabled
                ).ConfigureAwait(false);
        }
    }
}