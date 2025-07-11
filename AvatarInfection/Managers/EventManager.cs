using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

using LabFusion.Extensions;
using LabFusion.SDK.Triggers;
using LabFusion.UI.Popups;

namespace AvatarInfection.Managers
{
    public static class EventManager
    {
        private static readonly Dictionary<TriggerEvent, Type> _events = [];

        public static IReadOnlyCollection<TriggerEvent> Events => [.. _events.Keys];

        internal static void OnUnregistered()
        {
            _events.ForEach(e => e.Key.UnregisterEvent());
            _events.Clear();
        }

        public static void RegisterEvent(string name, Action callback, bool serverOnly = false)
        {
            if (_events.Any(x => x.Key.Name == name))
                throw new ArgumentException("An event with the same name is registered!");

            var ev = new TriggerEvent(name, Infection.Instance.Relay, serverOnly);
            ev.OnTriggered += callback;
            _events.Add(new TriggerEvent(name, Infection.Instance.Relay, serverOnly), null);
        }

        public static void RegisterEvent<T>(string name, Action<T> callback, bool serverOnly = false)
        {
            if (_events.Any(x => x.Key.Name == name))
                throw new ArgumentException("An event with the same name is registered!");

            var ev = new TriggerEvent(name, Infection.Instance.Relay, serverOnly);
            ev.OnTriggeredWithValue += (val) =>
            {
                T value;
                try
                {
                    value = JsonSerializer.Deserialize<T>(val);
                }
                catch (Exception ex)
                {
                    FusionModule.Logger.Error($"Failed to convert value to type '{typeof(T).Name}' in trigger event callback, exception:\n{ex}");
                    return;
                }
                callback?.Invoke(value);
            };
            _events.Add(new TriggerEvent(name, Infection.Instance.Relay, serverOnly), typeof(T));
        }

        public static void RegisterGlobalNotification(string name, Notification notification, bool serverOnly = true)
        {
            RegisterEvent(name, () =>
            {
                if (!Infection.Instance.IsStarted)
                    return;

                Notifier.Send(notification);
            }, serverOnly);
        }

        public static void RegisterGlobalNotification(
            string name, string title, string message, float popupDuration, bool serverOnly = true,
            bool showPopup = true,
             NotificationType type = NotificationType.INFORMATION,
             bool saveToMenu = false,
             Action onAccepted = null,
             Action onDeclined = null)
        {
            RegisterEvent(name, () =>
            {
                if (!Infection.Instance.IsStarted)
                    return;

                Infection.ShowNotification(title, message, popupDuration, showPopup, type, saveToMenu, onAccepted, onDeclined);
            }, serverOnly);
        }

        public static void UnregisterEvent(string name)
        {
            var ev = _events.FirstOrDefault(x => x.Key.Name == name).Key;
            if (ev != null)
            {
                ev.UnregisterEvent();
                _events.Remove(ev);
            }
        }

        public static bool TryInvokeEvent(string name)
        {
            var ev = _events.FirstOrDefault(x => x.Key.Name == name);
            if (ev.Key != null)
            {
                if (ev.Value == null)
                    return ev.Key.TryInvoke();
                else
                    return false;
            }
            else
            {
                return false;
            }
        }

        public static bool TryInvokeEvent<T>(string name, T value)
        {
            var ev = _events.FirstOrDefault(x => x.Key.Name == name);
            if (ev.Key != null)
            {
                if (ev.Value != null)
                    return ev.Key.TryInvoke(JsonSerializer.Serialize(value));
                else
                    return false;
            }
            else
            {
                return false;
            }
        }

        public static void RegisterEvent(Enum @enum, Action callback, bool serverOnly = false)
            => RegisterEvent(@enum.ToString(), callback, serverOnly);

        public static void RegisterEvent<T>(Enum @enum, Action<T> callback, bool serverOnly = false)
            => RegisterEvent(@enum.ToString(), callback, serverOnly);

        public static void RegisterGlobalNotification(Enum @enum, Notification notification, bool serverOnly = true)
            => RegisterGlobalNotification(@enum.ToString(), notification, serverOnly);

        public static void RegisterGlobalNotification(
            Enum @enum, string title, string message, float popupDuration, bool serverOnly = true,
            bool showPopup = true,
             NotificationType type = NotificationType.INFORMATION,
             bool saveToMenu = false,
             Action onAccepted = null,
             Action onDeclined = null)
            => RegisterGlobalNotification(@enum.ToString(), title, message, popupDuration, serverOnly, showPopup, type, saveToMenu, onAccepted, onDeclined);

        public static void UnregisterEvent(Enum @enum)
            => UnregisterEvent(@enum.ToString());

        public static bool TryInvokeEvent(Enum @enum)
            => TryInvokeEvent(@enum.ToString());

        public static bool TryInvokeEvent<T>(Enum @enum, T value)
            => TryInvokeEvent(@enum.ToString(), value);
    }
}