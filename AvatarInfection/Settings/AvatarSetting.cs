using System;
using System.Linq;

using AvatarInfection.Helper;
using AvatarInfection.Managers;

using BoneLib;

using Il2CppSLZ.Marrow.Warehouse;

using LabFusion.Menu.Data;
using LabFusion.Player;
using LabFusion.SDK.Gamemodes;

using Tomlet.Attributes;

using static AvatarInfection.Infection;

namespace AvatarInfection.Settings
{
    public class AvatarSetting : ToggleServerSetting<SelectedAvatarData>
    {
        public string GroupName { get; set; }

        public bool Optional { get; set; }

        public string OptionalToggle { get; set; } = "Seperate From Main";

        public GroupElementData CreateGroup()
        {
            var avatarGroup = new GroupElementData(GroupName);
            if (Optional)
            {
                avatarGroup.AddElement(OptionalToggle, Enabled, (val) =>
                {
                    Enabled = val;
                    GamemodeMenuManager.RefreshSettingsPage();
                });
            }
            if (Optional && !Enabled)
                return avatarGroup;

            avatarGroup.AddElement("Select Mode", Value?.SelectMode ?? AvatarSelectMode.CONFIG, (val) => { SetSelectMode((AvatarSelectMode)val); GamemodeMenuManager.RefreshSettingsPage(); });

            if (Value?.SelectMode == AvatarSelectMode.CONFIG)
            {
                var title = GetBarcodeTitle(Instance.Config.SelectedAvatar.Value?.Barcode);
                avatarGroup.AddElement(title, null);

                avatarGroup.AddElement("Select From Current Avatar", SelectNewAvatar);
            }
            else if (Value?.SelectMode == AvatarSelectMode.RANDOM)
            {
                avatarGroup.AddElement($"Chosen from {GetAvatars().Length} Avatars", null);
                if (Instance.IsStarted)
                {
                    avatarGroup.AddElement("Select New Random Avatar", () =>
                    {
                        if (!Instance.IsStarted)
                            return;
                        SetRandomAvatar();
                    });
                }
            }

            return avatarGroup;
        }

        public Barcode AsBarcode()
        {
            if (Value == null)
                return null;

            return new Barcode(Value.Barcode);
        }

        public void SetRandomAvatar()
        {
            var avatars = GetAvatars();
            var avatar = avatars.Random();
            SetAvatar(avatar, PlayerIDManager.LocalID);
        }

        public static string GetRandomAvatar()
            => GetAvatars().Random();

        public static string[] GetAvatars()
        {
            var avatars = AssetWarehouse.Instance.GetCrates<AvatarCrate>();
            avatars.ExcludeRedacted();
            avatars.ExcludeNonPublic();
            return [.. avatars.ToArray().Select(x => x.Barcode.ID)];
        }

        public void SetSelectMode(AvatarSelectMode mode)
        {
            if (Value != null)
            {
                Value.SelectMode = mode;
                if (AutoSync)
                    Sync();
            }
            else
            {
                Value = new(null, mode);
            }
        }

        public void SetAvatar(string barcode, PlayerID player)
        {
            if (Value != null)
            {
                Value.Barcode = barcode;
                Value.Origin = (long)player.PlatformID;
                if (AutoSync)
                    Sync();
            }
            else
            {
                Value = new(barcode, origin: player);
            }
        }

        private void SelectNewAvatar()
        {
            if (Instance.IsStarted)
                return;

            var rigManager = Player.RigManager;
            if (rigManager?.AvatarCrate?.Barcode != null)
            {
                var avatar = rigManager.AvatarCrate.Barcode.ID;

                if (string.IsNullOrWhiteSpace(avatar))
                    return;

                if (!rigManager.AvatarCrate.Crate.IsPublic())
                {
                    MenuHelper.ShowNotification("Error", "The modded avatar does not have an associated Mod ID, which is required! That means it must be installed through mod.io in-game", 5f, type: LabFusion.UI.Popups.NotificationType.ERROR);
                    return;
                }

                SetAvatar(avatar, PlayerIDManager.LocalID);

                GamemodeMenuManager.RefreshSettingsPage();
            }
        }

        public override void Load()
        {
            base.Load();
            if (Value == null)
                return;

            if (string.IsNullOrWhiteSpace(Value.Barcode))
                return;

            if (Value.SelectMode == AvatarSelectMode.RANDOM || Value.SelectMode == AvatarSelectMode.FIRST_INFECTED)
            {
                Value.Barcode = null;
                Value.Origin = -1;
            }
            else
            {
                if (!AssetWarehouse.Instance.TryGetCrate(new Barcode(Value.Barcode), out Crate crate) || !crate.IsPublic())
                {
                    MenuHelper.ShowNotification("Error", $"The loaded avatar with barcode '{Value.Barcode}' could not be found, or is not public! It will be reset to default.", 5f, type: LabFusion.UI.Popups.NotificationType.ERROR);
                    FusionModule.Logger.Error($"The loaded avatar with barcode '{Value.Barcode}' could not be found, or is not public! It will be reset to default.");
                    Value = new(null, AvatarSelectMode.CONFIG);
                    Save();
                }
                else
                {
                    Value.Origin = (long)PlayerIDManager.LocalPlatformID;
                }
            }
        }

        public override void Save()
        {
            if (Value.SelectMode != AvatarSelectMode.CONFIG)
            {
                Entry.Value = new(null, Value.SelectMode);
                EnabledEntry.Value = Enabled;
            }
            else
            {
                base.Save();
            }
        }

        private static string GetBarcodeTitle(string barcode)
            => !string.IsNullOrWhiteSpace(barcode) ? (new AvatarCrateReference(barcode)?.Crate?.Title ?? "N/A") : "N/A";

        public AvatarSetting(Gamemode gamemode, string name, string displayName = null, bool autoSync = true) : base(gamemode, name, displayName, autoSync)
        {
            Value = new(null, AvatarSelectMode.CONFIG);
        }

        public AvatarSetting(Gamemode gamemode, string name, SelectedAvatarData value, string displayName = null, bool autoSync = true) : base(gamemode, name, value, displayName, autoSync)
        {
        }

        public AvatarSetting(Gamemode gamemode, string name, SelectedAvatarData value, bool enabled, string displayName = null, bool autoSync = true) : base(gamemode, name, value, enabled, displayName, autoSync)
        {
        }
    }

    public sealed class SelectedAvatarData(string barcode, AvatarSelectMode selectMode = AvatarSelectMode.CONFIG, long origin = -1) : IEquatable<SelectedAvatarData>
    {
        public string Barcode { get; set; } = barcode;

        [TomlNonSerialized]
        public long Origin { get; set; } = origin;

        public AvatarSelectMode SelectMode { get; set; } = selectMode;

        public override bool Equals(object obj)
            => obj is SelectedAvatarData data && data.Barcode == Barcode && data.Origin == Origin && data.SelectMode == SelectMode;

        public bool Equals(SelectedAvatarData other)
            => other is not null && other.Barcode == Barcode && other.Origin == Origin && other.SelectMode == SelectMode;

        public override int GetHashCode()
            => Barcode.GetHashCode() + Origin.GetHashCode() + SelectMode.GetHashCode();
    }

    public enum AvatarSelectMode
    {
        CONFIG = 0,
        FIRST_INFECTED = 1,
        RANDOM = 2
    }
}