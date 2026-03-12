using System.IO;
using Il2CppInterop.Runtime.InteropTypes;
using MelonLoader;
using UnityEngine;
using Il2CppScheduleOne.Equipping;
using Il2CppScheduleOne.ItemFramework;
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
        public static MelonPreferences_Entry<bool> DebugEntry;

        private bool _wasReloading;
        private int _preReloadAmmo;
        private int _preReloadMagValue;
        private bool _correctionPending;

        public override void OnInitializeMelon()
        {
            Category = MelonPreferences.CreateCategory("M1911MagazineMod", "M1911 Magazine Mod");
            Category.SetFilePath(ConfigFile);
            MagazineSizeEntry = Category.CreateEntry("MagazineSize", 15,
                "Magazine Size",
                "Number of rounds the M1911 can hold (default game value is 7)");
            DebugEntry = Category.CreateEntry("Debug", false,
                "Debug Logging",
                "Enable verbose logging to MelonLoader console");

            ModSettingsEvents.OnPhonePreferencesSaved += () => ApplyToPrefab();

            LoggerInstance.Msg($"M1911 Magazine Mod loaded! Magazine size set to: {MagazineSizeEntry.Value}");
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            base.OnSceneWasLoaded(buildIndex, sceneName);
            MelonCoroutines.Start(DelayedApply());
        }

        private System.Collections.IEnumerator DelayedApply()
        {
            yield return null;
            ApplyToPrefab();
        }

        public override void OnUpdate()
        {
            var weapons = Object.FindObjectsOfType<Equippable_RangedWeapon>();
            if (weapons == null) return;

            int magSize = MagazineSizeEntry.Value;

            foreach (var weapon in weapons)
            {
                if (weapon == null) continue;
                if (weapon.GetIl2CppType().Name != "Equippable_RangedWeapon") continue;

                bool isReloading = weapon.IsReloading;

                if (isReloading && !_wasReloading)
                {
                    // Reload just started — snapshot current state
                    _preReloadAmmo = weapon.Ammo;

                    // Find the magazine that will be consumed
                    _preReloadMagValue = 0;
                    if (weapon.GetMagazine(out var mag))
                    {
                        var intMag = mag?.TryCast<IntegerItemInstance>();
                        if (intMag != null)
                            _preReloadMagValue = intMag.Value;
                    }

                    _correctionPending = true;

                    if (DebugEntry.Value)
                        LoggerInstance.Msg($"[Reload Start] Gun={_preReloadAmmo}, Mag={_preReloadMagValue}");
                }

                // Correct as soon as the ammo value changes during/after reload
                if (_correctionPending)
                {
                    var weaponItem = weapon.weaponItem;
                    if (weaponItem != null && weaponItem.Value != _preReloadAmmo)
                    {
                        int needed = magSize - _preReloadAmmo;
                        int toTake = System.Math.Min(needed, _preReloadMagValue);
                        int correctAmmo = _preReloadAmmo + toTake;

                        if (DebugEntry.Value)
                            LoggerInstance.Msg($"[Reload End] Game set ammo to {weaponItem.Value}, correcting to {correctAmmo}");

                        weaponItem.SetValue(correctAmmo);
                        _correctionPending = false;
                    }
                }

                if (!isReloading && _wasReloading)
                    _correctionPending = false;

                _wasReloading = isReloading;
                break; // Only track the first M1911
            }
        }

        private void ApplyToPrefab()
        {
            int magSize = MagazineSizeEntry.Value;

            var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (var obj in allObjects)
            {
                if (obj == null) continue;
                if (obj.name != "M1911_Equippable" || obj.transform.parent != null) continue;

                var weapon = obj.GetComponent<Equippable_RangedWeapon>();
                if (weapon == null) continue;

                weapon.MagazineSize = magSize;
                LoggerInstance.Msg($"Patched M1911 magazine capacity to {magSize}");
                break;
            }
        }
    }
}
