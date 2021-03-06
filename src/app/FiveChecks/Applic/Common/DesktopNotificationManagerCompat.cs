﻿//Source:https://raw.githubusercontent.com/WindowsNotifications/desktop-toasts/master/CS/DesktopToastsApp/DesktopNotificationManagerCompat.cs


// ******************************************************************
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THE CODE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
// TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH
// THE CODE OR THE USE OR OTHER DEALINGS IN THE CODE.
// ******************************************************************

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Windows.UI.Notifications;

namespace FiveChecks.Applic.Common
{
    public static class DesktopNotificationManagerCompat
    {
        public const string ToastActivatedLaunchArg = "ToastActivated";

        private static bool _registeredAumidAndComServer;
        private static string _aumid;
        private static bool _registeredActivator;

        /// <summary>
        /// If not running under the Desktop Bridge, you must call this method to register your AUMID with the Compat library and to
        /// register your COM CLSID and EXE in LocalServer32 registry. Feel free to call this regardless, and we will no-op if running
        /// under Desktop Bridge. Call this upon application startup, before calling any other APIs.
        /// </summary>
        /// <param name="aumid">An AUMID that uniquely identifies your application.</param>
        public static void RegisterAumidAndComServer<T>(string aumid)
            where T : NotificationActivator
        {
            if (string.IsNullOrWhiteSpace(aumid))
            {
                // ReSharper disable once LocalizableElement
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                throw new ArgumentException("An AUMID must be provided.", nameof(aumid));
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            }

            // If running as Desktop Bridge
            if (DesktopBridgeHelpers.IsRunningAsUwp())
            {
                // Clear the AUMID since Desktop Bridge doesn't use it, and then we're done.
                // Desktop Bridge apps are registered with platform through their manifest.
                // Their LocalServer32 key is also registered through their manifest.
                _aumid = null;
                _registeredAumidAndComServer = true;
                return;
            }

            _aumid = aumid;

            var exePath = Process.GetCurrentProcess().MainModule?.FileName;
            RegisterComServer<T>(exePath);
            _registeredAumidAndComServer = true;
        }

        private static void RegisterComServer<T>(string exePath)
            where T : NotificationActivator
        {
            if (exePath == null) throw new ArgumentNullException(nameof(exePath));
            // We register the EXE to start up when the notification is activated
            var regString = $"SOFTWARE\\Classes\\CLSID\\{{{typeof(T).GUID}}}\\LocalServer32";
            var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(regString);

            // Include a flag so we know this was a toast activation and should wait for COM to process
            // We also wrap EXE path in quotes for extra security
            key?.SetValue(null, '"' + exePath + '"' + " " + ToastActivatedLaunchArg);
        }

        /// <summary>
        /// Registers the activator type as a COM server client so that Windows can launch your activator.
        /// </summary>
        /// <typeparam name="T">Your implementation of NotificationActivator. Must have GUID and ComVisible attributes on class.</typeparam>
        public static void RegisterActivator<T>()
            where T : NotificationActivator
        {
            // Register type
            var regService = new RegistrationServices();

            regService.RegisterTypeForComClients(
                typeof(T),
                RegistrationClassContext.LocalServer,
                RegistrationConnectionType.MultipleUse);

            _registeredActivator = true;
        }

        /// <summary>
        /// Creates a toast notifier. You must have called <see cref="RegisterActivator{T}"/> first (and also <see cref="RegisterAumidAndComServer{T}"/> if you're a classic Win32 app), or this will throw an exception.
        /// </summary>
        /// <returns></returns>
        public static ToastNotifier CreateToastNotifier()
        {
            EnsureRegistered();

            if (_aumid != null)
            {
                // Non-Desktop Bridge
                return ToastNotificationManager.CreateToastNotifier(_aumid);
            }
            else
            {
                // Desktop Bridge
                return ToastNotificationManager.CreateToastNotifier();
            }
        }

        /// <summary>
        /// Gets the <see cref="DesktopNotificationHistoryCompat"/> object. You must have called <see cref="RegisterActivator{T}"/> first (and also <see cref="RegisterAumidAndComServer{T}"/> if you're a classic Win32 app), or this will throw an exception.
        /// </summary>
        public static DesktopNotificationHistoryCompat History
        {
            get
            {
                EnsureRegistered();

                return new DesktopNotificationHistoryCompat(_aumid);
            }
        }

        private static void EnsureRegistered()
        {
            // If not registered AUMID yet
            if (!_registeredAumidAndComServer)
            {
                // Check if Desktop Bridge
                if (DesktopBridgeHelpers.IsRunningAsUwp())
                {
                    // Implicitly registered, all good!
                    _registeredAumidAndComServer = true;
                }

                else
                {
                    // Otherwise, incorrect usage
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                    throw new Exception("You must call RegisterAumidAndComServer first.");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
                }
            }

            // If not registered activator yet
            if (!_registeredActivator)
            {
                // Incorrect usage
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                throw new Exception("You must call RegisterActivator first.");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            }
        }

        /// <summary>
        /// Gets a boolean representing whether http images can be used within toasts. This is true if running under Desktop Bridge.
        /// </summary>
        public static bool CanUseHttpImages => DesktopBridgeHelpers.IsRunningAsUwp();

        /// <summary>
        /// Code from https://github.com/qmatteoq/DesktopBridgeHelpers/edit/master/DesktopBridge.Helpers/Helpers.cs
        /// </summary>
        private static class DesktopBridgeHelpers
        {
            const long AppModelErrorNoPackage = 15700L;
            
            private static bool? _isRunningAsUwp;
            public static bool IsRunningAsUwp()
            {
                if (_isRunningAsUwp == null)
                {
                    if (IsWindows7OrLower)
                    {
                        _isRunningAsUwp = false;
                    }
                    else
                    {
                        var length = 0;
                        var sb = new StringBuilder(length);
                        var result = NativeMethods.GetCurrentPackageFullName(ref length, sb);
                        _isRunningAsUwp = result != AppModelErrorNoPackage;
                    }
                }

                return _isRunningAsUwp.Value;
            }

            private static bool IsWindows7OrLower
            {
                get
                {
                    var versionMajor = Environment.OSVersion.Version.Major;
                    var versionMinor = Environment.OSVersion.Version.Minor;
                    var version = versionMajor + (double)versionMinor / 10;
                    return version <= 6.1;
                }
            }
        }
    }

    /// <summary>
    /// Manages the toast notifications for an app including the ability the clear all toast history and removing individual toasts.
    /// </summary>
    public sealed class DesktopNotificationHistoryCompat
    {
        private readonly string _aumid;
        private readonly ToastNotificationHistory _history;

        /// <summary>
        /// Do not call this. Instead, call <see cref="DesktopNotificationManagerCompat.History"/> to obtain an instance.
        /// </summary>
        /// <param name="aumid"></param>
        internal DesktopNotificationHistoryCompat(string aumid)
        {
            _aumid = aumid;
            _history = ToastNotificationManager.History;
        }

        /// <summary>
        /// Removes all notifications sent by this app from action center.
        /// </summary>
        public void Clear()
        {
            if (_aumid != null)
            {
                _history.Clear(_aumid);
            }
            else
            {
                _history.Clear();
            }
        }

        /// <summary>
        /// Gets all notifications sent by this app that are currently still in Action Center.
        /// </summary>
        /// <returns>A collection of toasts.</returns>
        public IReadOnlyList<ToastNotification> GetHistory()
        {
            return _aumid != null ? _history.GetHistory(_aumid) : _history.GetHistory();
        }

        /// <summary>
        /// Removes an individual toast, with the specified tag label, from action center.
        /// </summary>
        /// <param name="tag">The tag label of the toast notification to be removed.</param>
        public void Remove(string tag)
        {
            if (_aumid != null)
            {
                _history.Remove(tag, string.Empty, _aumid);
            }
            else
            {
                _history.Remove(tag);
            }
        }

        /// <summary>
        /// Removes a toast notification from the action using the notification's tag and group labels.
        /// </summary>
        /// <param name="tag">The tag label of the toast notification to be removed.</param>
        /// <param name="group">The group label of the toast notification to be removed.</param>
        public void Remove(string tag, string group)
        {
            if (_aumid != null)
            {
                _history.Remove(tag, group, _aumid);
            }
            else
            {
                _history.Remove(tag, group);
            }
        }

        /// <summary>
        /// Removes a group of toast notifications, identified by the specified group label, from action center.
        /// </summary>
        /// <param name="group">The group label of the toast notifications to be removed.</param>
        public void RemoveGroup(string group)
        {
            if (_aumid != null)
            {
                _history.RemoveGroup(group, _aumid);
            }
            else
            {
                _history.RemoveGroup(group);
            }
        }
    }

    /// <summary>
    /// Apps must implement this activator to handle notification activation.
    /// </summary>
    public abstract class NotificationActivator : INotificationActivationCallback
    {
        public void Activate(string appUserModelId, string invokedArgs, NotificationUserInputData[] data, uint dataCount)
        {
            OnActivated(invokedArgs, new NotificationUserInputCollection(data), appUserModelId);
        }

        /// <summary>
        /// This method will be called when the user clicks on a foreground or background activation on a toast. Parent app must implement this method.
        /// </summary>
        /// <param name="arguments">The arguments from the original notification. This is either the launch argument if the user clicked the body of your toast, or the arguments from a button on your toast.</param>
        /// <param name="userInputCollection">Text and selection values that the user entered in your toast.</param>
        /// <param name="appUserModelId">Your AUMID.</param>
        public abstract void OnActivated(string arguments, NotificationUserInputCollection userInputCollection, string appUserModelId);

        // These are the new APIs for Windows 10
        #region NewAPIs
       
        #endregion
    }

    /// <summary>
    /// Text and selection values that the user entered on your notification. The Key is the ID of the input, and the Value is what the user entered.
    /// </summary>
    public class NotificationUserInputCollection : IReadOnlyDictionary<string, string>
    {
        private readonly NotificationUserInputData[] _data;

        internal NotificationUserInputCollection(NotificationUserInputData[] data)
        {
            _data = data;
        }

        public string this[string key] => _data.First(i => i.Key == key).Value;

        public IEnumerable<string> Keys => _data.Select(i => i.Key);

        public IEnumerable<string> Values => _data.Select(i => i.Value);

        public int Count => _data.Length;

        public bool ContainsKey(string key)
        {
            return _data?.Any(i => i.Key == key) ?? false;
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {

            return _data != null ? _data.Select(i => new KeyValuePair<string, string>(i.Key, i.Value)).GetEnumerator(): Enumerable.Empty<KeyValuePair<string, string>>().GetEnumerator();
        }

        public bool TryGetValue(string key, out string value)
        {
            foreach (var item in _data)
            {
                if (item.Key == key)
                {
                    value = item.Value;
                    return true;
                }
            }

            value = null;
            return false;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}