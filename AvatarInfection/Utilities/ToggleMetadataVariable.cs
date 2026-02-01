using System.Text.Json;
using System.Text.Json.Serialization;

using LabFusion.SDK.Metadata;

namespace AvatarInfection.Utilities
{
    public class ToggleMetadataVariable(string key, NetworkMetadata metadata)
    {
        public NetworkMetadata Metadata { get; } = metadata;
        public string Key { get; } = key;

        [JsonIgnore]
        public bool IsEnabled
        {
            get
            {
                var toggled = Metadata.GetMetadata(ToggledKey);
                bool success = bool.TryParse(toggled, out bool res);
                if (!success) return default;
                return res;
            }
        }

        public string ToggledKey { get { return $"{Key}-Toggled"; } }

        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            IncludeFields = true,
        };

        public void Remove()
        {
            Metadata.TryRemoveMetadata(Key);
            Metadata.TryRemoveMetadata(ToggledKey);
        }

        public void SetValue(string value)
            => Metadata.TrySetMetadata(Key, value);

        public void SetValue<TValue>(TValue value)
            => SetValue(JsonSerializer.Serialize(value, SerializerOptions));

        public void Toggle()
        {
            var toggled = Metadata.GetMetadata(ToggledKey);
            bool success = bool.TryParse(toggled, out bool res);
            if (success)
                Metadata.TrySetMetadata(ToggledKey, (!res).ToString());
            else
                Metadata.TrySetMetadata(ToggledKey, (!default(bool)).ToString());
        }

        public void SetEnabled(bool enabled)
            => Metadata.TrySetMetadata(ToggledKey, enabled.ToString());

        public string GetValue()
            => Metadata.GetMetadata(Key);

        public TValue GetValue<TValue>()
        {
            string value = GetValue();

            if (string.IsNullOrEmpty(value))
                return default;

            return JsonSerializer.Deserialize<TValue>(value, SerializerOptions);
        }
    }

    public class ToggleMetadataVariableT<TValue>(string key, NetworkMetadata metadata) : ToggleMetadataVariable(key, metadata)
    {
        public void SetValue(TValue value)
            => base.SetValue<TValue>(value);

        public new TValue GetValue()
            => base.GetValue<TValue>();
    }
}