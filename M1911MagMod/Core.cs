using System.IO;
using MelonLoader;
using UnityEngine;
using Il2CppScheduleOne.Equipping;
using ModManagerPhoneApp;

[assembly: MelonInfo(typeof(M1911MagMod.Core), "M1911 Magazine Mod", "1.0.0", "LeeT")]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace M1911MagMod
{
    public class Core : MelonMod
    {
        private static readonly string ConfigFile = Path.Combine("UserData", "M1911MagazineMod.cfg");

        public static MelonPreferences_Category Category;
        public static MelonPreferences_Entry<int> MagazineSizeEntry;

        private static Core _instance;

        public override void OnInitializeMelon()
        {
            _instance = this;

            Category = MelonPreferences.CreateCategory("M1911MagazineMod", "M1911 Magazine Mod");
            Category.SetFilePath(ConfigFile);
            MagazineSizeEntry = Category.CreateEntry("MagazineSize", 15,
                "Magazine Size",
                "Number of rounds the M1911 can hold (default game value is 7)");

            ModSettingsEvents.OnPhonePreferencesSaved += OnSettingsSaved;

            LoggerInstance.Msg($"M1911 Magazine Mod loaded! Magazine size set to: {MagazineSizeEntry.Value}");
        }

        private static void OnSettingsSaved()
        {
            _instance?.LoggerInstance.Msg($"Settings saved! Magazine size is now: {MagazineSizeEntry.Value}");
            ApplyToAllWeapons();
        }

        public override void OnUpdate()
        {
            ApplyToAllWeapons();
        }

        private static void ApplyToAllWeapons()
        {
            var weapons = Object.FindObjectsOfType<Equippable_RangedWeapon>();
            if (weapons == null) return;

            foreach (var weapon in weapons)
            {
                if (weapon == null) continue;

                // Only affect the base RangedWeapon (M1911), not subclasses like Shotgun/Revolver
                if (weapon.GetIl2CppType().Name != "Equippable_RangedWeapon") continue;

                if (weapon.MagazineSize != MagazineSizeEntry.Value)
                {
                    weapon.MagazineSize = MagazineSizeEntry.Value;
                    _instance?.LoggerInstance.Msg($"Patched M1911 magazine size to {MagazineSizeEntry.Value}");
                }
            }
        }
    }
}
