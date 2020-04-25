﻿using System;
using System.Collections.Generic;
using System.Linq;
using LanguageExt;

namespace Compliance.Notifications.Applic.PendingRebootCheck
{
    public class PendingRebootInfo : Record<PendingRebootInfo>
    {
        public bool RebootIsPending { get; set; }

        public List<RebootSource> Sources { get; internal set; } = new List<RebootSource>();

        public static PendingRebootInfo Default => new PendingRebootInfo() { RebootIsPending = false};

        public string ToSourceDescription()
        {
            return string.Join(",", Sources);
        }
    }

    public static class PendingRebootInfoExtensions
    {
        public static PendingRebootInfo Update(this PendingRebootInfo org, PendingRebootInfo add)
        {
            if (org == null) throw new ArgumentNullException(nameof(org));
            if (add == null) throw new ArgumentNullException(nameof(add));
            if (!add.RebootIsPending)
                return new PendingRebootInfo
                {
                    RebootIsPending = org.RebootIsPending, 
                    Sources = new List<RebootSource>(org.RebootIsPending? org.Sources : new List<RebootSource>())
                };
            return new PendingRebootInfo
            {
                RebootIsPending = true, 
                Sources = new List<RebootSource>(org.RebootIsPending? org.Sources.Concat(add.Sources): add.Sources)
            };
        }
    }
}