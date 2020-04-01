﻿using System.Collections.Generic;
using NUnit.Framework;
using System.Threading.Tasks;
using Compliance.Notifications.Common.Tests;
using Compliance.Notifications.Model;
using LanguageExt.Common;

namespace Compliance.Notifications.Commands.CheckDiskSpace.Tests
{
    [TestFixture()]
    [Category(TestCategory.UnitTests)]
    public class CheckPendingRebootCommandTests
    {
        public static class ShowPendingRebootToastNotificationCallCount
        {
            public const int One = 1;
            public const int Zero = 0;
        }

        public static class RemovePendingRebootToastNotificationCallCount
        {
            public const int One = 1;
            public const int Zero = 0;
        }

        public static class LoadPendingRebootCallCount
        {
            public const int One = 1;
            public const int Zero = 0;
        }

        public static class PendingReboot
        {
            public const bool True = true;
            public const bool False = false;
        }

        [Test]
        [TestCase(PendingReboot.True, LoadPendingRebootCallCount.One, ShowPendingRebootToastNotificationCallCount.One, RemovePendingRebootToastNotificationCallCount.Zero, Description = "Pending reboot is true")]
        [TestCase(PendingReboot.False, LoadPendingRebootCallCount.One, ShowPendingRebootToastNotificationCallCount.Zero, RemovePendingRebootToastNotificationCallCount.One, Description = "Pending reboot is false")]
        public void CheckPendingRebootTest(bool isPendingReboot,int expectedLoadPendingRebootCallCount, int expectedShowPendingRebootToastNotificationCount, int expectedRemovePendingRebootToastNotificationCallCount)
        {
            var actualLoadPendingRebootCallCount = 0;
            var actualShowPendingRebootToastNotificationCount = 0;
            var actualRemovePendingRebootToastNotificationCount = 0;
            var actual = 
                    CheckPendingRebootCommand.CheckPendingRebootF(
                        async () =>
                        {
                            actualLoadPendingRebootCallCount++;
                            await Task.CompletedTask;
                            return new PendingRebootInfo {RebootIsPending = isPendingReboot,Source=new List<RebootSource>()};
                        },
                        (s) => { 
                            actualShowPendingRebootToastNotificationCount++;
                            return new Task<Result<int>>(() => 0);
                        }, () =>
                        {
                            actualRemovePendingRebootToastNotificationCount++;
                            return new Task<Result<int>>(() => 0);
                        });

            Assert.AreEqual(expectedLoadPendingRebootCallCount, actualLoadPendingRebootCallCount, "LoadPendingRebootResult");
            Assert.AreEqual(expectedShowPendingRebootToastNotificationCount, actualShowPendingRebootToastNotificationCount,"ShowPendingRebootToastNotification");
            Assert.AreEqual(expectedRemovePendingRebootToastNotificationCallCount, actualRemovePendingRebootToastNotificationCount, "RemovePendingRebootToastNotification");
        }
    }
}