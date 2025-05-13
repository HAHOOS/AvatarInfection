using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LabFusion.Marrow.Proxies;
using LabFusion.Menu.Data;
using LabFusion.Menu.Gamemodes;
using LabFusion.SDK.Gamemodes;

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

            var element = group.Elements?.FirstOrDefault(x => startsWith ? x.Title.StartsWith(elementName) : x.Title.Contains(elementName));
            return (MenuElement)element ?? null;
        }

        public static ElementT ChangeElement<ElementT>(this Gamemode gamemode, string groupName, string elementName, Action<ElementT> changes, bool startsWith = true) where ElementT : MenuElement
        {
            var element = GetElement(gamemode, groupName, elementName, startsWith);
            ElementT el = element == null ? null : (ElementT)element;
            if (el != null)
                changes.Invoke(el);
            return el;
        }

        public static FunctionElementData CreateSetting(this Gamemode gamemode, Enum @enum, string groupName, string title, Action<Enum> callback)
        {
            const string TitleFormat = "{0}: {1}";
            Enum val = @enum;

            return new FunctionElementData()
            {
                Title = string.Format(TitleFormat, title, val),
                OnPressed = () =>
                {
                    var previous = val;
                    var values = Enum.GetValues(@enum.GetType());
                    int index = Array.IndexOf(
                        values,
                        values.OfType<Enum>()
                            .First(x => x.Equals(val))
                        );

                    // Politely borrowed from BoneLib
                    // Oh sorry my bad, from LabFusion, because the BoneLib one didn't do anything
                    index++;
                    index %= values.Length;
                    val = (Enum)values.GetValue(index);

                    ChangeElement<FunctionElement>(gamemode, groupName, string.Format(TitleFormat, title, previous), (el) => el.Title = string.Format(TitleFormat, title, val), false);
                    callback.Invoke(val);
                }
            };
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

        public static FunctionElementData AddElement(this GroupElementData group, string title, Enum @enum, Gamemode gamemode, Action<Enum> callback)
        {
            var data = CreateSetting(gamemode, @enum, group.Title, title, callback);
            group.AddElement(data);
            return data;
        }

        public static GroupElementData AddGroup(this GroupElementData group, string title)
        {
            var groupData = new GroupElementData(title);
            group.AddElement(groupData);
            return groupData;
        }
    }
}