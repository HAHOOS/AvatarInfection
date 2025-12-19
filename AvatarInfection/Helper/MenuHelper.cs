using System;
using System.Linq;

using LabFusion.Marrow.Proxies;
using LabFusion.Menu.Data;
using LabFusion.Menu.Gamemodes;
using LabFusion.SDK.Gamemodes;
using LabFusion.UI.Popups;

namespace AvatarInfection.Helper
{
    public static class MenuHelper
    {
        public static MenuElement GetElement(this Gamemode gamemode, string groupName, string elementName, bool startsWith = true)
        {
            if (MenuGamemode.SelectedGamemode != gamemode)
                return null;

            if (MenuGamemode.SettingsPageElement.Elements.FirstOrDefault(x => x.Title == groupName) is not GroupElement group)
                return null;

            return group.Elements?.FirstOrDefault(x => startsWith ? x.Title.StartsWith(elementName) : x.Title.Contains(elementName));
        }

        public static ElementT ChangeElement<ElementT>(this Gamemode gamemode, string groupName, string elementName, Action<ElementT> changes, bool startsWith = true) where ElementT : MenuElement
        {
            var element = GetElement(gamemode, groupName, elementName, startsWith);
            ElementT el = element == null ? null : (ElementT)element;
            if (el != null)
                changes.Invoke(el);
            return el;
        }

        public static StringElementData AddElement(this GroupElementData group, string title, string value, Action<string> callback)
        {
            var data = new StringElementData()
            {
                Title = title,
                Value = value,
                OnValueChanged = (val) => callback?.Invoke(val)
            };
            group.AddElement(data);
            return data;
        }

        public static FunctionElementData AddElement(this GroupElementData group, string title, Action callback)
        {
            var data = new FunctionElementData()
            {
                Title = title,
                OnPressed = () => callback?.Invoke()
            };
            group.AddElement(data);
            return data;
        }

        public static IntElementData AddElement(this GroupElementData group, string title, int value, Action<int> callback, int increment = 1, int min = 0, int max = 100)
        {
            var data = new IntElementData()
            {
                Title = title,
                Increment = increment,
                MaxValue = max,
                MinValue = min,
                Value = value,
                OnValueChanged = (val) => callback?.Invoke(val)
            };
            group.AddElement(data);
            return data;
        }

        public static FloatElementData AddElement(this GroupElementData group, string title, float value, Action<float> callback, float increment = 0.2f, float min = 0.2f, float max = 100f)
        {
            var data = new FloatElementData()
            {
                Title = title,
                Increment = increment,
                MaxValue = max,
                MinValue = min,
                Value = value,
                OnValueChanged = (val) => callback?.Invoke(val)
            };
            group.AddElement(data);
            return data;
        }

        public static BoolElementData AddElement(this GroupElementData group, string title, bool value, Action<bool> callback)
        {
            var data = new BoolElementData()
            {
                Title = title,
                Value = value,
                OnValueChanged = (val) => callback?.Invoke(val)
            };
            group.AddElement(data);
            return data;
        }

        public static EnumElementData AddElement(this GroupElementData group, string title, Enum @enum, Action<Enum> callback)
        {
            var data = new EnumElementData()
            {
                Title = title,
                EnumType = @enum.GetType(),
                Value = @enum,
                OnValueChanged = callback
            };
            group.AddElement(data);
            return data;
        }

        public static GroupElementData AddGroup(this GroupElementData group, string title)
        {
            var groupData = new GroupElementData(title);
            group.AddElement(groupData);
            return groupData;
        }

        public static void ShowNotification
            (string title,
             string message,
             float popupLength,
             bool showPopup = true,
             NotificationType type = NotificationType.INFORMATION,
             bool saveToMenu = false)
        {
            Notifier.Send(new Notification
            {
                Message = message,
                Title = title,
                PopupLength = popupLength,
                ShowPopup = showPopup,
                Type = type,
                OnAccepted = null,
                OnDeclined = null,
                SaveToMenu = saveToMenu
            });
        }
    }
}