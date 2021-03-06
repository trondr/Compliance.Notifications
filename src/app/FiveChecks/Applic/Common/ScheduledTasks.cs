﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Threading.Tasks;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Win32.TaskScheduler;
using Pri.LongPath;
using ScheduledTask=Microsoft.Win32.TaskScheduler.Task;
using Task = System.Threading.Tasks.Task;

namespace FiveChecks.Applic.Common
{
    public static class ScheduledTasks
    {
        public static FileInfo ExeFile { get; } = new FileInfo(Assembly.GetExecutingAssembly().Location);

        public static ScheduledTaskInfo ComplianceCheck => new ScheduledTaskInfo("FiveChecks", "Compliance check at workstation unlock and logon", ExeFile, "CheckCompliance",10);

        public static ScheduledTaskInfo ComplianceSystemMeasurements => new ScheduledTaskInfo("FiveChecks System Measurement", "Measure system compliance hourly", ExeFile, "MeasureSystemComplianceItems",11);

        public static ScheduledTaskInfo ComplianceUserMeasurements => new ScheduledTaskInfo("FiveChecks User Measurement", "Measure user compliance hourly", ExeFile, "MeasureUserComplianceItems", 12);
        
        //public static ScheduledTaskInfo FullSystemDiskCleanup => new ScheduledTaskInfo("Compliance Full System Disk Cleanup", "Compliance Full System Disk Cleanup", ExeFile, "RunFullSystemDiskCleanup", 13);
        
        public static Func<Trigger> UnlockTrigger => () => new SessionStateChangeTrigger(TaskSessionStateChangeType.SessionUnlock);

        public static Func<Trigger> LoginTrigger => () => new LogonTrigger();

        public static Func<Trigger> HourlyTrigger => () => new DailyTrigger {Repetition = new RepetitionPattern(new TimeSpan(0, 1, 0, 0, 0), new TimeSpan(1, 0, 0, 0))};

        public static Func<int,Trigger> EventTrigger => (eventId) => new EventTrigger(WindowsEventLog.EventLogName,WindowsEventLog.EventSourceName, eventId);
        
        public static Func<string> BuiltInUsers => () =>
        {
            var sid = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
            var account = (NTAccount)sid.Translate(typeof(NTAccount));
            return account.Value;
        };
        
        public static Try<Unit> RegisterSystemScheduledTask(Some<string> taskName, Some<FileInfo> exeFile,
            Some<string> arguments, Some<string> taskDescription, Some<List<Trigger>> triggers) => () =>
    {
        using (var ts = TaskService.Instance)
        {
            using (var td = ts.NewTask())
            {
                td.RegistrationInfo.Description = taskDescription.Value;
                td.Settings.MultipleInstances = TaskInstancesPolicy.StopExisting;
                td.Actions.Add(new ExecAction($"\"{exeFile.Value.FullName}\"", arguments.Value, exeFile.Value.Directory.FullName));
                foreach (var trigger in triggers.Value)
                {
                    td.Triggers.Add(trigger);
                }
                td.Principal.UserId = "SYSTEM";
                td.Principal.RunLevel = TaskRunLevel.Highest;
                ts.RootFolder.RegisterTaskDefinition(taskName.Value, td);
            }
        }
        return new Result<Unit>(Unit.Default);
    };


        public static Result<Unit> RegisterSystemManualTask(Some<string> taskName, Some<FileInfo> exeFile, Some<string> arguments, Some<string> taskDescription, Some<List<Trigger>> triggers)
        {
            return F.TryFunc(() =>
            {
                using (var ts = TaskService.Instance)
                {
                    using (var td = ts.NewTask())
                    {
                        td.RegistrationInfo.Description = taskDescription.Value;
                        td.Settings.MultipleInstances = TaskInstancesPolicy.StopExisting;
                        td.Actions.Add(new ExecAction($"\"{exeFile.Value.FullName}\"", arguments.Value, exeFile.Value.Directory.FullName));
                        foreach (var trigger in triggers.Value)
                        {
                            td.Triggers.Add(trigger);
                        }
                        td.Principal.UserId = "SYSTEM";
                        td.Principal.RunLevel = TaskRunLevel.Highest;
                        ts.RootFolder.RegisterTaskDefinition(taskName.Value, td);
                    }
                }
                return new Result<Unit>(Unit.Default);
            });
        }


        public static Try<Unit> RegisterUserScheduledTask(Some<string> taskName, Some<FileInfo> exeFile,
            Some<string> arguments, Some<string> taskDescription, Some<List<Trigger>> triggers) => () =>
            {
                using (var ts = TaskService.Instance)
                {
                    using (var td = ts.NewTask())
                    {
                        td.RegistrationInfo.Description = taskDescription.Value;
                        td.Settings.MultipleInstances = TaskInstancesPolicy.StopExisting;
                        td.Actions.Add(new ExecAction($"\"{exeFile.Value.FullName}\"", arguments.Value, exeFile.Value.Directory.FullName));
                        foreach (var trigger in triggers.Value)
                        {
                            td.Triggers.Add(trigger);
                        }
                        td.Principal.GroupId = ScheduledTasks.BuiltInUsers();
                        td.Principal.RunLevel = TaskRunLevel.LUA;
                        ts.RootFolder.RegisterTaskDefinition(taskName.Value, td);
                    }
                }
                return new Result<Unit>(Unit.Default);
            };

        public static Result<Unit> UnRegisterScheduledTask(Some<string> taskName)
        {
            return F.TryFunc(() =>
            {
                var task = GetScheduledTask(taskName);
                return task.Match(t =>
                {
                    TaskService.Instance.RootFolder.DeleteTask(t.Name, false);
                    return new Result<Unit>(Unit.Default);
                }, () => new Result<Unit>(Unit.Default));
            });
        }

        public static Option<ScheduledTask> GetScheduledTask(Some<string> taskName)
        {
            return TaskService.Instance.AllTasks.FirstOrDefault(t => t.Name == taskName);
        }

        public static async Task<Result<Unit>> WaitForScheduledTaskExit(Some<string> taskName)
        {
            return await F.AsyncTryFunc<Unit>(async () =>
            {
                await Task.Delay(5000).ConfigureAwait(false);
                var running = true;
                while (running)
                {
                    Logging.DefaultLogger.Debug($"Waiting for scheduled task: { taskName.Value}");
                    GetScheduledTask(taskName)
                        .Match(
                            st =>
                            {
                                running = st.State == TaskState.Running;
                            },
                            () => running = false
                        );
                    await Task.Delay(2000).ConfigureAwait(false);
                }
                return await System.Threading.Tasks.Task.FromResult(new Result<Unit>(Unit.Default)).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        public static async Task<Result<Unit>> RunScheduledTask(Some<ScheduledTaskInfo> scheduledTaskInfo, bool waitForExit)
        {
            return await F.AsyncTryFunc<Unit>(async () =>
            {
                var eventLogResult = await WindowsEventLog.WriteEventLog($"Running scheduled task '{scheduledTaskInfo.Value.TaskName}'",EventLogEntryType.Information, scheduledTaskInfo.Value.EventId).ConfigureAwait(false);
                var waitForExitResult = 
                    await eventLogResult
                        .Match(
                             async unit =>
                                    {
                                        var res = waitForExit
                                            ? WaitForScheduledTaskExit(scheduledTaskInfo.Value.TaskName).ConfigureAwait(false)
                                            : Task.FromResult(new Result<Unit>(Unit.Default)).ConfigureAwait(false);
                                        return await res;
                                    },  
                            async exception => await Task.FromResult(new Result<Unit>(exception)).ConfigureAwait(false)
                     ).ConfigureAwait(false);
                return waitForExitResult;
            }).ConfigureAwait(false);
        }
    }
}