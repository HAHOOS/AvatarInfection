using System;
using System.Collections.Generic;
using System.Linq;

using AvatarInfection.Settings;

namespace AvatarInfection.Helper
{
    public static class CollectionHelper
    {
        private static readonly Random random = new();

        public static T Random<T>(this List<T> list)
            => list[random.Next(0, list.Count)];

        public static T Random<T>(this T[] list)
            => list[random.Next(0, list.Length)];

        public static KeyValuePair<KeyT, ValT> Random<KeyT, ValT>(this Dictionary<KeyT, ValT> dictionary)
            => dictionary.ElementAt(random.Next(0, dictionary.Count));

        public static T Random<T>(this Il2CppSystem.Collections.Generic.List<T> list)
            => list[random.Next(0, list.Count)];

        public static KeyValuePair<KeyT, ValT> Random<KeyT, ValT>(this Il2CppSystem.Collections.Generic.Dictionary<KeyT, ValT> dictionary)
        {
            var entry = dictionary._entries[random.Next(0, dictionary.Count)];
            return new KeyValuePair<KeyT, ValT>(entry.key, entry.value);
        }

        internal static void Types(this List<ISetting> settings, Action<ISetting> forEach, params Type[] types)
        {
            settings.RemoveAll(x => !types.Contains(x.GetType()));
            settings.ForEach(forEach);
        }
    }
}