# Schedule 1 Modding Project

## Overview
MelonLoader mods for Schedule I (IL2CPP/Unity game). Built on macOS, deployed to Windows gaming PC via NAS.

## Build & Deploy
- **SDK**: .NET 6 via Homebrew at `/opt/homebrew/Cellar/dotnet@6/6.0.136_1/bin/dotnet`
- **Build command**: `dotnet build -c Release` from a mod's directory
- **Decompiler**: `DOTNET_ROOT=/opt/homebrew/Cellar/dotnet@6/6.0.136_1/libexec /Users/leetruong/.dotnet/tools/ilspycmd <dll>`
- **Deploy**: `./deploy.sh` copies built DLLs to `/Volumes/SyNas_4TB/schedule_1_modding/`
- **Output**: `<ModName>/bin/Release/net6.0/<ModName>.dll` → copy to `Schedule I/Mods/` on gaming PC

## Reference Assemblies (not in git)
- `Il2CppAssemblies/` — IL2CPP unhollowed game assemblies from MelonLoader
- `net6/` — MelonLoader runtime DLLs (MelonLoader.dll, Il2CppInterop.Runtime.dll, 0Harmony.dll, etc.)
- `references/` — Community mods for reference (ModManager&PhoneApp.dll, PackRat-IL2CPP.dll, AmmoCostCapacity.dll, etc.)

## Game Info
- **Game**: Schedule I by TVGS
- **MelonInfo assembly attribute**: `[assembly: MelonGame("TVGS", "Schedule I")]`
- **Main game assembly**: `Il2CppAssemblies/Assembly-CSharp.dll`
- **Game namespaces** use `Il2Cpp` prefix when referenced from mods (e.g. `Il2CppScheduleOne.Equipping`)

## Key Game Classes

### Weapons
- `ScheduleOne.Equipping.Equippable_RangedWeapon` — Base ranged weapon (M1911 uses this directly)
  - `MagazineSize` (int) — Max rounds the gun can hold
  - `Magazine` (StorableItemDefinition) — The magazine item definition
  - `weaponItem` (IntegerItemInstance) — The gun's item instance, `.Value` = current ammo
  - `Ammo` (int, getter only) — Read current ammo count
  - `IsReloading` (bool) — Whether a reload is in progress
  - `GetMagazine(out StorableItemInstance)` — Finds a magazine in player inventory
- `ScheduleOne.Equipping.Equippable_PumpShotgun` — Shotgun subclass
- `ScheduleOne.Equipping.Equippable_Revolver` — Revolver subclass
- `ScheduleOne.AvatarFramework.Equipping.AvatarRangedWeapon` — NPC/third-person weapon (separate from player equippable)
  - Has its own `MagazineSize` and `currentAmmo` — only relevant for NPCs, NOT the player

### Items
- `ScheduleOne.ItemFramework.IntegerItemDefinition` — Item definition with `DefaultValue` (e.g. magazine with 7 rounds)
- `ScheduleOne.ItemFramework.IntegerItemInstance` — Item instance with `Value`, `SetValue(int)`, `ChangeValue(int)`
  - **Use `SetValue()` instead of setting `Value` directly** — SetValue triggers UI change events
- `ScheduleOne.ItemFramework.StorableItemInstance` — Base item instance, use `.TryCast<IntegerItemInstance>()` to downcast

### Other
- `ScheduleOne.Equipping.Equippable_MeleeWeapon` — Melee weapons
- `ScheduleOne.ItemFramework.ItemDefinition` — Base item definitions
- `ScheduleOne.UI.Phone.*` — In-game phone UI apps

## Weapon Prefabs
- Weapon prefabs are root GameObjects with no parent, found via `Resources.FindObjectsOfTypeAll<GameObject>()`
- M1911 prefab: `obj.name == "M1911_Equippable" && obj.transform.parent == null`
- Revolver prefab: `obj.name == "Revolver_Equippable" && obj.transform.parent == null`
- Patch prefabs in `OnSceneWasLoaded` (with a one-frame delay via coroutine) so all instances inherit changes

## Reload System (IMPORTANT LESSONS)
The game's reload sets `weaponItem.Value = MagazineSize` regardless of the magazine's actual ammo count.
This means simply changing `MagazineSize` causes every reload to fill the gun to max.

### What DOESN'T work
- **Changing `MagazineSize` alone** — Reload always fills to MagazineSize, ignoring magazine contents
- **Changing `IntegerItemDefinition.DefaultValue`** — Affects ALL magazine instances globally, corrupts existing save data
- **Harmony patches on IL2CPP property getters** — Unreliable, often silently fails
- **Calling `GetMagazine()` every frame in OnUpdate** — May interfere with reload system, causes bugs

### What DOES work (M1911MagMod approach)
1. Patch `MagazineSize` on the weapon prefab (so the gun can hold more)
2. Detect reload via `IsReloading` state transitions on `Equippable_RangedWeapon`
3. On reload start: snapshot `weapon.Ammo` and `GetMagazine()` value (call once, not every frame)
4. On reload end: calculate correct ammo = `min(preAmmo + magValue, configuredMax)` and override with `weaponItem.SetValue(correctAmmo)`
5. To minimize visual flicker: apply correction as soon as `weaponItem.Value` changes (not when `IsReloading` goes false)

## Mod Manager Integration (ModManagerPhoneApp)
The community mod manager auto-discovers settings via MelonPreferences.

### Category naming (IMPORTANT)
`FindCategoriesForMod` matches categories where **Identifier.StartsWith(modName)** (case-insensitive).
It checks both the raw `MelonInfo` name and the name with spaces removed.
- Example: MelonInfo name `"M1911 Magazine Mod"` → checks `"M1911 Magazine Mod"` and `"M1911MagazineMod"`
- So category identifier must start with one of those strings

### Custom config file
Use `Category.SetFilePath(Path.Combine("UserData", "MyMod.cfg"))` to save to own file instead of shared MelonPreferences.cfg.

### Settings saved event
Subscribe to `ModSettingsEvents.OnPhonePreferencesSaved` to react when user saves settings in the phone app.
(`OnPreferencesSaved` is deprecated)

## Modding Patterns

### IL2CPP considerations
- Harmony patches on property getters/setters do NOT work reliably with IL2CPP
- Prefer direct field/property modification via `OnUpdate()` or lifecycle callbacks
- Use `Object.FindObjectsOfType<T>()` to find active game objects
- Use `instance.GetIl2CppType().Name` to check IL2CPP runtime type
- Use `.TryCast<T>()` or `.Cast<T>()` for IL2CPP type casting

### .csproj template
Required references for most mods:
- `net6/MelonLoader.dll`
- `net6/Il2CppInterop.Runtime.dll`
- `net6/0Harmony.dll`
- `Il2CppAssemblies/Assembly-CSharp.dll`
- `Il2CppAssemblies/Il2Cppmscorlib.dll`
- `Il2CppAssemblies/UnityEngine.CoreModule.dll`
- `references/ModManager&PhoneApp.dll` (for mod manager integration)

### Exploring game code
1. `strings <dll>` — Quick way to find class/method/field names
2. `ilspycmd <dll>` — Full decompilation of .NET DLLs (reference mods, unhollowed assemblies)
3. Decompile reference mods in `references/` to learn patterns (AmmoCostCapacity, PackRat are good examples)
4. Look for `NativeFieldInfoPtr_*` entries in static constructors to find all fields on a class

### Debug pattern
Add a `Debug` bool preference entry to toggle verbose logging without rebuilding:
```csharp
DebugEntry = Category.CreateEntry("Debug", false, "Debug Logging", "Enable verbose logging");
// Then: if (DebugEntry.Value) LoggerInstance.Msg(...);
```
