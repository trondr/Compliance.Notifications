﻿using FiveChecks.Applic.Common;
using FiveChecks.Applic.DiskSpaceCheck;
using FiveChecks.Tests.Applic;
using LanguageExt;
using NUnit.Framework;

namespace FiveChecks.Tests.Model
{
    [TestFixture]
    public class DiskCleanupTests
    {
        [Test(Description = "RunFullDiskCleanupTest - Requires admin privileges to run.")]
        [Category(TestCategory.ManualTests)]
        public void RunFullDiskCleanupTest()
        {
            var actual = DiskCleanup.RunFullDiskCleanup();
            actual.Wait();
            actual.Result.Match(unit =>
            {
                Assert.IsTrue(true);
                return Unit.Default;
            }, exception =>
            {
                Assert.Fail(exception.ToExceptionMessage());
                return Unit.Default;
            });
        }

        [Test(Description = "SetCleanupManagerStateFlagsTest - Requires admin privileges to run.")]
        [Category(TestCategory.ManualTests)]
        public void SetCleanupManagerStateFlagsTest()
        {
            DiskCleanup.SetCleanupManagerStateFlags();
        }

        [Test(Description = "ResetCleanupManagerStateFlagsTest - Requires admin privileges to run.")]
        [Category(TestCategory.ManualTests)]
        public void ResetCleanupManagerStateFlagsTest()
        {
            DiskCleanup.ResetCleanupManagerStateFlags();
        }
    }
}